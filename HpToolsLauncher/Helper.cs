/*
 *
 *  Certain versions of software and/or documents (“Material”) accessible here may contain branding from
 *  Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.  As of September 1, 2017,
 *  the Material is now offered by Micro Focus, a separately owned and operated company.  Any reference to the HP
 *  and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE
 *  marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * © Copyright 2012-2019 Micro Focus or one of its affiliates..
 *
 * The only warranties for products and services of Micro Focus and its affiliates
 * and licensors (“Micro Focus”) are set forth in the express warranty statements
 * accompanying such products and services. Nothing herein should be construed as
 * constituting an additional warranty. Micro Focus shall not be liable for technical
 * or editorial errors or omissions contained herein.
 * The information contained herein is subject to change without notice.
 * ___________________________________________________________________
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Timers;
using System.Web.UI;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using HpToolsLauncher.Properties;
using Microsoft.Win32;

namespace HpToolsLauncher
{
    public enum TestType
    {
        QTP,
        ST,
        LoadRunner,
        ParallelRunner
    }

    public enum TestState
    {
        Waiting,
        Running,
        NoRun,
        Passed,
        Failed,
        Error,
        Warning,
        Unknown,
    }

    public enum TestResult
    {
        Passed,
        Failed,
        Warning,
        Done,
    }

    public static class Helper
    {
        #region Constants

        public const string FT_REG_ROOT = @"SOFTWARE\Mercury Interactive\QuickTest Professional\CurrentVersion";

        internal const string FT_REG_ROOT_64_BIT =
            @"SOFTWARE\Wow6432Node\Mercury Interactive\QuickTest Professional\CurrentVersion";

        public const string FT_ROOT_PATH_KEY = "QuickTest Professional";
        private const string QTP_ROOT_ENV_VAR_NAME = "QTP_TESTS_ROOT";


        public const string ServiceTestRegistryKey = @"SOFTWARE\Hewlett-Packard\HP Service Test";
        public const string ServiceTesCurrentVersionRegistryKey = ServiceTestRegistryKey + @"\CurrentVersion";

        public const string ServiceTesWOW64RegistryKey = @"SOFTWARE\Wow6432Node\Hewlett-Packard\HP Service Test";
        public const string ServiceTesCurrentVersionWOW64RegistryKey = ServiceTesWOW64RegistryKey + @"\CurrentVersion";

        public const string LoadRunnerRegistryKey = @"SOFTWARE\Mercury Interactive\LoadRunner";
        public const string LoadRunner64RegisryKey = @"SOFTWARE\Wow6432Node\Mercury Interactive\LoadRunner";
        public const string LoadRunnerControllerRegistryKey = @"CustComponent\Controller\CurrentVersion";
        public const string LoadRunnerControllerDirRegistryKey = @"\CurrentVersion";

        public const string LoadRunnerControllerDirRegistryValue = @"\Controller";

        public static readonly System.Collections.ObjectModel.ReadOnlyCollection<string> LoadRunnerENVVariables =
            new System.Collections.ObjectModel.ReadOnlyCollection<string>(new[] {"LG_PATH", "LR_PATH"});


        public const string InstalltionFolderValue = "LOCAL_MLROOT";

        public const string UftViewerInstalltionFolderRegistryKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{E86D56AE-6660-4357-890F-8B79AB7A8F7B}";

        public const string UftViewerInstalltionFolderRegistryKey64Bit =
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{E86D56AE-6660-4357-890F-8B79AB7A8F7B}";


        public const string ResultsFileName = "Results.xml";
        public const string QTPReportProcessPath = @"bin\reportviewer.exe";

        public const string STFileExtention = ".st";
        public const string QTPFileExtention = ".tsp";
        public const string LoadRunnerFileExtention = ".lrs";

        #endregion

        public static Assembly HPToolsAssemblyResolver(object sender, ResolveEventArgs args)
        {
            AssemblyName asmName = new AssemblyName(args.Name);
            if (asmName == null) return null;

            string assemblyName = asmName.Name;
            if (assemblyName.EndsWith(".resources")) return null;

            if (assemblyName == "HpToolsLauncher.XmlSerializers") return null;

            string installtionPath = null;
            installtionPath = Helper.getLRInstallPath();
            if (installtionPath == null)
            {
                ConsoleWriter.WriteErrLine(string.Format(Resources.LoadRunnerNotInstalled,
                    System.Environment.MachineName));
                Environment.Exit((int) Launcher.ExitCodeEnum.Aborted);
            }

            installtionPath = Path.Combine(installtionPath, "bin");

            Assembly ans;
            if (!File.Exists(Path.Combine(installtionPath, assemblyName + ".dll")))
            {
                //resource!
                ConsoleWriter.WriteErrLine("cannot locate " + assemblyName + ".dll in installation directory");
                Environment.Exit((int) Launcher.ExitCodeEnum.Aborted);
            }
            else
            {
                //Console.WriteLine("loading " + assemblyName + " from " + Path.Combine(installtionPath, assemblyName + ".dll"));
                ans = Assembly.LoadFrom(Path.Combine(installtionPath, assemblyName + ".dll"));

                AssemblyName loadedName = ans.GetName();
                if (loadedName.Name == "Interop.Wlrun")
                {
                    if (loadedName.Version.Major > 11 ||
                        (loadedName.Version.Major == 11 && loadedName.Version.Minor >= 52))
                    {
                        return ans;
                    }
                    else
                    {
                        ConsoleWriter.WriteErrLine(string.Format(Resources.HPToolsAssemblyResolverWrongVersion,
                            Environment.MachineName));
                        Environment.Exit((int) Launcher.ExitCodeEnum.Aborted);
                    }
                }
                else
                {
                    return ans;
                }

            }


            return null;

        }


        public static string GetRootDirectoryPath()
        {
            String directoryPath;
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT);

            if (regkey != null)
                directoryPath = (string) regkey.GetValue(FT_ROOT_PATH_KEY);
            else
            {
//TRY 64 bit REG path
                regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT_64_BIT);
                if (regkey != null)
                    directoryPath = (string) regkey.GetValue(FT_ROOT_PATH_KEY);
                else
                    directoryPath = GetRootFromEnvironment();
            }

            return directoryPath;
        }


        //verify that files/folders exist (does not recurse into folders)
        public static List<TestData> ValidateFiles(IEnumerable<TestData> tests)
        {
            //Console.WriteLine("[ValidateFiles]");
            List<TestData> validTests = new List<TestData>();
            foreach (TestData test in tests)
            {
                //Console.WriteLine("ValidateFiles, test Id: " + test.Id +  ", test path " + test.Tests);
                if (!File.Exists(test.Tests) && !Directory.Exists(test.Tests))
                {
                    ConsoleWriter.WriteLine(string.Format(">>>> File/Folder not found: '{0}'", test.Tests));
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                }
                else
                {
                    validTests.Add(test);
                }
            }
            return validTests;
        }

        public static bool FileExists(string filePath)
        {
            bool isFileValid = true;
            if (!File.Exists(filePath)) {
                ConsoleWriter.WriteLine(string.Format(">>>> File not found: '{0}'", filePath));
                isFileValid = false;
                Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
            }

            return isFileValid;
        }

        public static bool IsTestingToolsInstalled(TestStorageType type)
        {
            //we want to check if we have Service Test, QTP installed on a machine

            return IsQtpInstalled() || IsServiceTestInstalled() || isLoadRunnerInstalled();

        }

        public static bool isLoadRunnerInstalled()
        {
            //try 32 bit
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(LoadRunnerRegistryKey);

            if (regkey == null)
            {
                //try 64-bit
                regkey = Registry.LocalMachine.OpenSubKey(LoadRunner64RegisryKey);

            }

            if (regkey != null)
            {

                //LoadRunner Exist.
                //check if Controller is installed (not SA version)

                if (regkey.OpenSubKey(LoadRunnerControllerRegistryKey) != null)
                {
                    return true;
                }

            }
            return false;

        }

        public static bool IsQtpInstalled()
        {
            RegistryKey regkey;
            string value;
            regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT);
            if (regkey == null)
            {
                //try 64 bit
                regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT_64_BIT);
            }

            if (regkey != null)
            {
                value = (string) regkey.GetValue(FT_ROOT_PATH_KEY);
                if (!string.IsNullOrEmpty(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsServiceTestInstalled()
        {
            RegistryKey regkey;
            string value;
            regkey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionRegistryKey);
            if (regkey == null)
            {
                //try 64 bit
                regkey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionWOW64RegistryKey);
            }

            if (regkey != null)
            {
                value = (string) regkey.GetValue(InstalltionFolderValue);
                if (!string.IsNullOrEmpty(value))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetRootFromEnvironment()
        {
            string qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(qtpRoot))
            {
                qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.User);

                if (string.IsNullOrEmpty(qtpRoot))
                {
                    qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME,
                        EnvironmentVariableTarget.Machine);

                    if (string.IsNullOrEmpty(qtpRoot))
                    {
                        qtpRoot = Environment.CurrentDirectory;
                    }
                }
            }

            return qtpRoot;
        }

        public static string GetSTInstallPath()
        {
            string ret = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionRegistryKey);
            if (regKey != null)
            {
                var val = regKey.GetValue(InstalltionFolderValue);
                if (null != val)
                {
                    ret = val.ToString();
                }
            }
            else
            {
                regKey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionWOW64RegistryKey);
                if (regKey != null)
                {
                    var val = regKey.GetValue(InstalltionFolderValue);
                    if (null != val)
                    {
                        ret = val.ToString();
                    }
                }
                else
                {
                    ret = GetRootDirectoryPath() ?? string.Empty;
                }
            }

            if (!String.IsNullOrEmpty(ret))
            {
                ret = ret.EndsWith("\\") ? ret : (ret + "\\");
                if (ret.EndsWith("\\bin\\"))
                {
                    int endIndex = ret.LastIndexOf("\\bin\\");
                    if (endIndex != -1)
                    {
                        ret = ret.Substring(0, endIndex) + "\\";
                    }
                }
            }

            return ret;
        }

        public static string getLRInstallPath()
        {
            string installPath = null;
            System.Collections.IDictionary envVariables = Environment.GetEnvironmentVariables();

            //try to find LoadRunner install path in environment vars
            foreach (string variable in LoadRunnerENVVariables)
            {
                if (envVariables.Contains(variable))
                    return envVariables[variable] as string;
            }

            //Fallback to registry
            //try 32 bit
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(LoadRunnerRegistryKey);

            if (regkey == null)
            {
                //try 64-bit
                regkey = Registry.LocalMachine.OpenSubKey(LoadRunner64RegisryKey);

            }

            if (regkey != null)
            {

                //LoadRunner Exists. check if Controller is installed (not SA version)
                regkey = regkey.OpenSubKey(LoadRunnerControllerDirRegistryKey);
                if (regkey != null)
                    return regkey.GetValue("Controller").ToString();

            }

            return installPath;
        }

        public static List<string> GetTestsLocations(string baseDir)
        {
            var testsLocations = new List<string>();
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir))
            {
                return testsLocations;
            }

            WalkDirectoryTree(new DirectoryInfo(baseDir), ref testsLocations);
            return testsLocations;
        }

        public static TestType GetTestType(string path)
        {


            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
//ST and QTP uses folder as test locations
                var stFiles = Directory.GetFiles(path,
                    @"*.st?",
                    SearchOption.TopDirectoryOnly);

                return (stFiles.Count() > 0) ? TestType.ST : TestType.QTP;
            }
            else //not directory
            {
//loadrunner is a path to file...
                return TestType.LoadRunner;
            }
        }

        public static bool IsDirectory(string path)
        {
            var fa = File.GetAttributes(path);
            var isDirectory = false;
            if ((fa & FileAttributes.Directory) != 0)
            {
                isDirectory = true;
            }
            return isDirectory;
        }

        static void WalkDirectoryTree(System.IO.DirectoryInfo root, ref List<string> results)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {

                files = root.GetFiles("*" + STFileExtention);
                files = files.Union(root.GetFiles("*" + QTPFileExtention)).ToArray();
                files = files.Union(root.GetFiles("*" + LoadRunnerFileExtention)).ToArray();
            }
            catch (Exception)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                //log.Add(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    if (fi.Extension == LoadRunnerFileExtention)
                        results.Add(fi.FullName);
                    else
                        results.Add(fi.Directory.FullName);

                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Recursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref results);
                }
            }
        }

        public static string GetTempDir()
        {
            string baseTemp = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string dirName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 6);
            string tempDirPath = Path.Combine(baseTemp, dirName);

            return tempDirPath;
        }

        public static string CreateTempDir()
        {
            string tempDirPath = GetTempDir();

            Directory.CreateDirectory(tempDirPath);

            return tempDirPath;
        }

        public static bool IsNetworkPath(String path)
        {
            if (path.StartsWith(@"\\"))
                return true;
            var dir = new DirectoryInfo(path);
            var drive = new DriveInfo(dir.Root.ToString());
            return drive.DriveType == DriveType.Network;
        }

        public static bool IsLeanFTRunning()
        {
            bool bRet = false;
            Process[] procArray = Process.GetProcessesByName("LFTRuntime");
                // Hardcoded temporarily since LeanFT does not store the process name anywhere
            if (procArray.Length != 0)
            {
                bRet = true;
            }
            return bRet;
        }

        public static bool IsSprinterRunning()
        {
            RegistryKey key =
                Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Hewlett-Packard\\Manual Runner\\Process");
            if (key == null)
                return false;

            var arrayName = key.GetSubKeyNames();
            if (arrayName.Length == 0)
                return false;
            foreach (string s in arrayName)
            {
                Process[] sprinterProcArray = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(s));
                if (sprinterProcArray.Length != 0)
                    return true;

            }
            return false;
        }

        public static bool CanUftProcessStart(out string reason)
        {
            //Close UFT when some of the Sprinter processes is running
            if (IsSprinterRunning())
            {
                reason = Resources.UFT_Sprinter_Running;
                return false;
            }

            //Close UFT when LeanFT engine is running
            if (IsLeanFTRunning())
            {
                reason = Resources.UFT_LeanFT_Running;
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public static string GetParallelRunnerDirectory(string parallelRunnerExecutable)
        {
            if (parallelRunnerExecutable == null) return null;

            var uftFolder = GetSTInstallPath();

            if (uftFolder == null) return null;

            return uftFolder + @"bin\" + parallelRunnerExecutable;
        }

        public static void ChangeDCOMSettingToInteractiveUser()
        {
            string errorMsg = "Unable to change DCOM settings. To chage it manually: " +
                  "run dcomcnfg.exe -> My Computer -> DCOM Config -> QuickTest Professional Automation -> Identity -> and select The Interactive User";

            string interactiveUser = "Interactive User";
            string runAs = "RunAs";

            try
            {
                var regKey = GetQuickTestProfessionalAutomationRegKey(RegistryView.Registry32);

                if (regKey == null)
                {
                    regKey = GetQuickTestProfessionalAutomationRegKey(RegistryView.Registry64);
                }

                if (regKey == null)
                    throw new Exception(@"Unable to find in registry SOFTWARE\Classes\AppID\{A67EB23A-1B8F-487D-8E38-A6A3DD150F0B");

                object runAsKey = regKey.GetValue(runAs);

                if (runAsKey == null || !runAsKey.ToString().Equals(interactiveUser))
                {
                    regKey.SetValue(runAs, interactiveUser);
                }

            }
            catch (Exception ex)
            {
                throw new Exception(errorMsg + "detailed error is : " + ex.Message);
            }
        }

        public static RegistryKey GetQuickTestProfessionalAutomationRegKey(RegistryView registry32)
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(@"SOFTWARE\Classes\AppID\{A67EB23A-1B8F-487D-8E38-A6A3DD150F0B}", true);

            return localKey;
        }

        /// <summary>
        /// Return the path of the available results folder for the parallel runner.
        /// </summary>
        /// <param name="testInfo"> The test information. </param>
        /// <returns>
        /// the path to the results folder 
        /// </returns>
        public static string GetNextResFolder(string reportPath, string resultFolderName)
        {
            // since ParallelRunner will store the report as "Res1...ResN"
            // we need to know before parallel runner creates the result folder
            // what the folder name will be
            // so we know which result is ours(or if there was any result)
            int resultFolderIndex = 1;

            while (Directory.Exists(Path.Combine(reportPath, resultFolderName + resultFolderIndex)))
            {
                resultFolderIndex += 1;
            }

            return reportPath + "\\" + resultFolderName + resultFolderIndex;
        }

        #region Report Related

        /// <summary>
        /// Set the error for a test when the report path is invalid.
        /// </summary>
        /// <param name="runResults"> The test run results </param>
        /// <param name="errorReason"> The error reason </param>
        /// <param name="testInfo"> The test informatio </param>
        public static void SetTestReportPathError(TestRunResults runResults, ref string errorReason, TestInfo testInfo)
        {
            // Invalid path was provided, return useful description
            errorReason = string.Format(Resources.InvalidReportPath, runResults.ReportLocation);

            // since the report path is invalid, the test should fail
            runResults.TestState = TestState.Error;
            runResults.ErrorDesc = errorReason;

            // output the error for the current test run
            ConsoleWriter.WriteErrLine(runResults.ErrorDesc);

            // include the error in the summary
            ConsoleWriter.ErrorSummaryLines.Add(runResults.ErrorDesc);

            // provide the appropriate exit code for the launcher
            Environment.ExitCode = (int)Launcher.ExitCodeEnum.Failed;
        }

        /// <summary>
        /// Try to set the custom report path for a given test.
        /// </summary>
        /// <param name="runResults"> The test run results </param>
        /// <param name="testInfo"> The test information </param>
        /// <param name="errorReason"> The error reason </param>
        /// <returns> True if the report path was set, false otherwise </returns>
        public static bool TrySetTestReportPath(TestRunResults runResults, TestInfo testInfo, ref string errorReason)
        {
            string testName = testInfo.TestName.Substring(testInfo.TestName.LastIndexOf('\\') + 1) + "_";
            string reportLocation = Helper.GetNextResFolder(testInfo.ReportPath, testName);

            // set the report location for the run results
            runResults.ReportLocation = reportLocation;

            try
            {
                Directory.CreateDirectory(runResults.ReportLocation);
            }
            catch (Exception)
            {
                SetTestReportPathError(runResults, ref errorReason, testInfo);
                return false;
            }

            return true;
        }

        public static string GetUftViewerInstallPath()
        {
            string ret = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(UftViewerInstalltionFolderRegistryKey) ??
                         Registry.LocalMachine.OpenSubKey(UftViewerInstalltionFolderRegistryKey64Bit);

            if (regKey != null)
            {
                var val = regKey.GetValue("InstallLocation");
                if (null != val)
                {
                    ret = val.ToString();
                }
            }

            if (!String.IsNullOrEmpty(ret))
            {
                ret = ret.EndsWith("\\") ? ret : (ret + "\\");
            }

            return ret;
        }

        public static string GetLastRunFromReport(string reportPath)
        {
            if (!Directory.Exists(reportPath))
                return null;
            string resultsFileFullPath = reportPath + @"\" + ResultsFileName;
            if (!File.Exists(resultsFileFullPath))
                return null;
            try
            {
                var root = XElement.Load(resultsFileFullPath);

                var element = root.XPathSelectElement("//Doc/Summary");
                var lastStartRun = element.Attribute("sTime");
                return lastStartRun.Value.Split(new char[] {' '})[0];

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TestState GetTestStateFromUFTReport(TestRunResults runDesc, string[] resultFiles)
        {
            try
            {
                TestState finalState = TestState.Unknown;

                foreach (string resultsFileFullPath in resultFiles)
                {
                    finalState = TestState.Unknown;
                    string desc = "";
                    TestState state = GetStateFromUFTResultsFile(resultsFileFullPath, out desc);
                    if (finalState == TestState.Unknown || finalState == TestState.Passed)
                    {
                        finalState = state;
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            if (finalState == TestState.Error)
                            {
                                runDesc.ErrorDesc = desc;
                            }
                            if (finalState == TestState.Failed)
                            {
                                runDesc.FailureDesc = desc; 
                            }
                        }
                    }
                }

                if (finalState == TestState.Unknown)
                    finalState = TestState.Passed;

                if (finalState == TestState.Failed && string.IsNullOrWhiteSpace(runDesc.FailureDesc))
                    runDesc.FailureDesc = "Test failed";

                runDesc.TestState = finalState;
                return runDesc.TestState;
            }
            catch (Exception)
            {
                return TestState.Unknown;
            }

        }


        public static TestState GetTestStateFromLRReport(TestRunResults runDesc, string[] resultFiles)
        {

            foreach (string resultFileFullPath in resultFiles)
            {
                string desc = "";
                runDesc.TestState = GetTestStateFromLRReport(resultFileFullPath, out desc);
                if (runDesc.TestState == TestState.Failed)
                {
                    runDesc.ErrorDesc = desc;
                    break;
                }
            }

            return runDesc.TestState;
        }

        public static TestState GetTestStateFromReport(TestRunResults runDesc)
        {
            try
            {
                if (!Directory.Exists(runDesc.ReportLocation))
                {
                    runDesc.ErrorDesc = string.Format(Resources.DirectoryNotExistError, runDesc.ReportLocation);

                    runDesc.TestState = TestState.Error;
                    return runDesc.TestState;
                }
                //if there is Result.xml -> UFT
                //if there is sla.xml file -> LR
                //if there is parallelrun_results.xml -> ParallelRunner

                string[] resultFiles = Directory.GetFiles(runDesc.ReportLocation, "Results.xml",
                    SearchOption.TopDirectoryOnly);
                if (resultFiles.Length == 0)
                    resultFiles = Directory.GetFiles(runDesc.ReportLocation, "run_results.xml",
                        SearchOption.TopDirectoryOnly);
                //resultFiles = Directory.GetFiles(Path.Combine(runDesc.ReportLocation, "Report"), "Results.xml", SearchOption.TopDirectoryOnly);

                if (resultFiles != null && resultFiles.Length > 0)
                    return GetTestStateFromUFTReport(runDesc, resultFiles);

                resultFiles = Directory.GetFiles(runDesc.ReportLocation, "SLA.xml", SearchOption.AllDirectories);

                if (resultFiles != null && resultFiles.Length > 0)
                {
                    return GetTestStateFromLRReport(runDesc, resultFiles);
                }

                resultFiles = Directory.GetFiles(runDesc.ReportLocation, "parallelrun_results.html", SearchOption.TopDirectoryOnly);

                // the overall status is given by parallel runner
                // at the end of the run
                if (resultFiles != null && resultFiles.Length > 0)
                {
                    return runDesc.TestState;
                }

                //no LR or UFT => error
                runDesc.ErrorDesc = string.Format("no results file found for " + runDesc.TestName);
                runDesc.TestState = TestState.Error;
                return runDesc.TestState;
            }
            catch (Exception)
            {
                return TestState.Unknown;
            }

        }

        private static TestState GetTestStateFromLRReport(string resultFileFullPath, out string desc)
        {
            desc = "";

            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(resultFileFullPath);
            return checkNodeStatus(xdoc.DocumentElement, out desc);
        }

        private static TestState checkNodeStatus(XmlNode node, out string desc)
        {
            desc = "";
            if (node == null)
                return TestState.Failed;

            if (node.ChildNodes.Count == 1 && node.ChildNodes[0].NodeType == XmlNodeType.Text)
            {
                if (node.InnerText.ToLowerInvariant() == "failed")
                {
                    if (node.Attributes != null && node.Attributes["FullName"] != null)
                    {
                        desc = string.Format(Resources.LrSLARuleFailed, node.Attributes["FullName"].Value,
                            node.Attributes["GoalValue"].Value, node.Attributes["ActualValue"].Value);
                        ConsoleWriter.WriteLine(desc);
                    }
                    return TestState.Failed;
                }
                else
                {
                    return TestState.Passed;
                }
            }
            //node has children
            foreach (XmlNode childNode in node.ChildNodes)
            {
                TestState res = checkNodeStatus(childNode, out desc);
                if (res == TestState.Failed)
                {
                    if (string.IsNullOrEmpty(desc) && node.Attributes != null && node.Attributes["FullName"] != null)
                    {
                        desc = string.Format(Resources.LrSLARuleFailed, node.Attributes["FullName"].Value,
                            node.Attributes["GoalValue"].Value, node.Attributes["ActualValue"].Value);
                        ConsoleWriter.WriteLine(desc);
                    }
                    return TestState.Failed;
                }
            }
            return TestState.Passed;
        }


        private static TestState GetStateFromUFTResultsFile(string resultsFileFullPath, out string desc)
        {
            TestState finalState = TestState.Unknown;
            desc = "";
            var status = "";
            var doc = new XmlDocument {PreserveWhitespace = true};
            doc.Load(resultsFileFullPath);
            string strFileName = Path.GetFileName(resultsFileFullPath);
            if (strFileName.Equals("run_results.xml"))
            {
                XmlNodeList rNodeList = doc.SelectNodes("/Results/ReportNode/Data");
                if (rNodeList == null)
                {
                    desc = string.Format(Resources.XmlNodeNotExistError, "/Results/ReportNode/Data");
                    finalState = TestState.Error;
                }

                var node = rNodeList.Item(0);
                XmlNode resultNode = ((XmlElement) node).GetElementsByTagName("Result").Item(0);

                status = resultNode.InnerText;

            }
            else
            {
                var testStatusPathNode = doc.SelectSingleNode("//Report/Doc/NodeArgs");
                if (testStatusPathNode == null)
                {
                    desc = string.Format(Resources.XmlNodeNotExistError, "//Report/Doc/NodeArgs");
                    finalState = TestState.Error;
                }

                if (!testStatusPathNode.Attributes["status"].Specified)
                    finalState = TestState.Unknown;

                status = testStatusPathNode.Attributes["status"].Value;
            }

            var result = (TestResult) Enum.Parse(typeof(TestResult), status);
            if (result == TestResult.Passed || result == TestResult.Done)
            {
                finalState = TestState.Passed;
            }
            else if (result == TestResult.Warning)
            {
                finalState = TestState.Warning;
            }
            else
            {
                finalState = TestState.Failed;
            }

            return finalState;
        }

        public static string GetUnknownStateReason(string reportPath)
        {
            if (!Directory.Exists(reportPath))
            {
                return string.Format("Directory '{0}' doesn't exist", reportPath);
            }
            string resultsFileFullPath = reportPath + @"\" + ResultsFileName;
            if (!File.Exists(resultsFileFullPath))
            {
                return string.Format("Could not find results file '{0}'", resultsFileFullPath);
            }

            var doc = new XmlDocument {PreserveWhitespace = true};
            doc.Load(resultsFileFullPath);
            var testStatusPathNode = doc.SelectSingleNode("//Report/Doc/NodeArgs");
            if (testStatusPathNode == null)
            {
                return string.Format("XML node '{0}' could not be found", "//Report/Doc/NodeArgs");
            }
            return string.Empty;
        }

        public static void OpenReport(string reportDirectory, ref string optionalReportViewerPath)
        {
            Process p = null;
            try
            {
                string viewerPath = optionalReportViewerPath;
                string reportPath = reportDirectory;
                string resultsFilePath = reportPath + "\\" + ResultsFileName;

                if (!File.Exists(resultsFilePath))
                {
                    return;
                }

                var si = new ProcessStartInfo();
                if (string.IsNullOrEmpty(viewerPath))
                {
                    viewerPath = GetUftViewerInstallPath();
                    optionalReportViewerPath = viewerPath;
                }
                si.Arguments = " -r \"" + reportPath + "\"";

                si.FileName = Path.Combine(viewerPath, QTPReportProcessPath);
                si.WorkingDirectory = Path.Combine(viewerPath, @"bin" + @"\");

                p = Process.Start(si);
                if (p != null)
                {
                }
                return;
            }
            catch (Exception)
            {
            }
            finally
            {
                if (p != null)
                {
                    p.Close();
                }
            }
            return;
        }

        #endregion

        #region Export Related

        /// <summary>
        /// Copy directories from source to target
        /// </summary>
        /// <param name="sourceDir">full path source directory</param>
        /// <param name="targetDir">full path target directory</param>
        /// <param name="includeSubDirectories">if true, all subdirectories and contents will be copied</param>
        /// <param name="includeRoot">if true, the source directory will be created too</param>
        public static void CopyDirectories(string sourceDir, string targetDir,
            bool includeSubDirectories = false, bool includeRoot = false)
        {
            var source = new DirectoryInfo(sourceDir);
            var target = new DirectoryInfo(targetDir);
            DirectoryInfo workingTarget = target;

            if (includeRoot)
                workingTarget = Directory.CreateDirectory(target.FullName);

            CopyContents(source, workingTarget, includeSubDirectories);
        }

        private static void CopyContents(DirectoryInfo source, DirectoryInfo target, bool includeSubDirectories)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }


            foreach (FileInfo fi in source.GetFiles())
            {
                string targetFile = Path.Combine(target.ToString(), fi.Name);

                fi.CopyTo(targetFile, true);
            }

            if (includeSubDirectories)
            {
                DirectoryInfo[] subDirectories = source.GetDirectories();
                foreach (DirectoryInfo diSourceSubDir in subDirectories)
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyContents(diSourceSubDir, nextTargetSubDir, true);
                }
            }
        }

        public static void CopyFilesFromFolder(string sourceFolder, IEnumerable<string> fileNames, string targetFolder)
        {
            foreach (var fileName in fileNames)
            {
                var sourceFullPath = Path.Combine(sourceFolder, fileName);
                var targetFullPath = Path.Combine(targetFolder, fileName);
                File.Copy(sourceFullPath, targetFullPath, true);
            }
        }

        /// <summary>
        /// Create html file according to a matching xml
        /// </summary>
        /// <param name="xmlPath">the values xml</param>
        /// <param name="xslPath">the xml transformation file</param>
        /// <param name="targetFile">the full file name - where to save the product</param>
        public static void CreateHtmlFromXslt(string xmlPath, string xslPath, string targetFile)
        {
            var xslTransform = new XslCompiledTransform();
            //xslTransform.Load(xslPath);
            xslTransform.Load(xslPath, new XsltSettings(false, true), null);

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var xmlWriter = new XhtmlTextWriter(sw);
            xslTransform.Transform(xmlPath, null, xmlWriter);


            File.WriteAllText(targetFile, sb.ToString());
        }

        #endregion


    }

    public class Stopper {
        private readonly int _milliSeconds;


        public Stopper(int milliSeconds)
        {
            this._milliSeconds = milliSeconds;
        }


        /// <summary>
        /// Creates timer in seconds to replace thread.sleep due to ui freezes in jenkins. 
        /// Should be replaced in the future with ASync tasks
        /// </summary>
        public void Start()
        {
            if (_milliSeconds < 1)
            {
                return;
            }
            DateTime desired = DateTime.Now.AddMilliseconds(_milliSeconds);
            var a = 0;
            while (DateTime.Now < desired)
            {
                a += 1;
            }
        }
    }

}
