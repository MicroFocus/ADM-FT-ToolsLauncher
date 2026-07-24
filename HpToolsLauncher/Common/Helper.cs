/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2026 Open Text
 *
 * The only warranties for products and services of Open Text and
 * its affiliates and licensors ("Open Text") are as may be set forth
 * in the express warranty statements accompanying such products and services.
 * Nothing herein should be construed as constituting an additional warranty.
 * Open Text shall not be liable for technical or editorial errors or
 * omissions contained herein. The information contained herein is subject
 * to change without notice.
 *
 * Except as specifically indicated otherwise, this document contains
 * confidential information and a valid license is required for possession,
 * use or copying. If this work is provided to the U.S. Government,
 * consistent with FAR 12.211 and 12.212, Commercial Computer Software,
 * Computer Software Documentation, and Technical Data for Commercial Items are
 * licensed to the U.S. Government under vendor's standard commercial license.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ___________________________________________________________________
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using HpToolsLauncher.Properties;
using Microsoft.Win32;

namespace HpToolsLauncher.Common
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
        public const string BIN = "bin";

        private const string QTP_APPID_REG_KEY = @"SOFTWARE\Classes\AppID\{A67EB23A-1B8F-487D-8E38-A6A3DD150F0B}";
        private const string QTP_REG_ROOT = @"SOFTWARE\Mercury Interactive\QuickTest Professional\CurrentVersion";
        private const string QTP_REG_ROOT_64 = @"SOFTWARE\Wow6432Node\Mercury Interactive\QuickTest Professional\CurrentVersion";

        private const string FT_ROOT_PATH_KEY = "QuickTest Professional";
        private const string QTP_ROOT_ENV_VAR_NAME = "QTP_TESTS_ROOT";

        private const string ST_REG_KEY = @"SOFTWARE\Hewlett-Packard\HP Service Test";
        private const string ST_CRT_VER_REG_KEY = ST_REG_KEY + @"\CurrentVersion";

        private const string ST_REG_KEY_64 = @"SOFTWARE\Wow6432Node\Hewlett-Packard\HP Service Test";
        private const string ST_CRT_VER_REG_KEY_64 = ST_REG_KEY_64 + @"\CurrentVersion";

        private const string LR_REG_KEY = @"SOFTWARE\Mercury Interactive\LoadRunner";
        private const string LR_REG_KEY_64 = @"SOFTWARE\Wow6432Node\Mercury Interactive\LoadRunner";
        private const string LR_CONTROLLER_SUB_KEY = @"CustComponent\Controller\CurrentVersion";
        private const string CURRENT_VERSION = "CurrentVersion";
        private const string CONTROLLER = "Controller";

        private static readonly ReadOnlyCollection<string> _loadRunnerEnvVars = new(["LG_PATH", "LR_PATH"]);
        private const string INTEROP_WLRUN = "Interop.Wlrun";
        private const string HTL_XML_SERIALIZERS = "HpToolsLauncher.XmlSerializers";
        private const string LOCAL_MLROOT = "LOCAL_MLROOT";

        private const string MANUAL_RUNNER_PROC_REG_KEY = @"SOFTWARE\Hewlett-Packard\Manual Runner\Process";

        private const string UFT = "UFT";
        private const string STATUS = "status";
        private const string RESULT = "Result";
        private const string RES_RPT_NODE_DATA_XPATH = "/Results/ReportNode/Data";
        private const string RPT_DOC_NODE_ARGS_XPATH = "//Report/Doc/NodeArgs";
        private const string TEST_FAILED = "Test failed";
        private const string PARALLELRUN_RESULTS_HTML = "parallelrun_results.html";
        private const string RESULTS_XML = "Results.xml";
        private const string SLA_XML = "SLA.xml";
        private const string RUN_RESULTS_XML = "run_results.xml";

        private const string ST_SEARCH_PATTERN = @"*.st?";
        public const string _ST = ".st";
        public const string _TSP = ".tsp";
        public const string _LRS = ".lrs";
        public const string _MTB = ".mtb";
        public const string _MTBX = ".mtbx";
        private const string _RESOURCES = ".resources";
        private const string PARALLEL_RUNNER_EXE = "ParallelRunner.exe";
        private const string LFT_RUNTIME = "LFTRuntime";

        private const char BACKSLASH = '\\';
        private const string BACKSLASH_ = "\\";
        #endregion

        public static Assembly HPToolsAssemblyResolver(object sender, ResolveEventArgs args)
        {
            AssemblyName asmName = new(args.Name);
            if (asmName == null) return null;

            string assemblyName = asmName.Name;
            if (assemblyName.EndsWith(_RESOURCES)) return null;

            if (assemblyName == HTL_XML_SERIALIZERS) return null;
            string installtionPath = GetLRInstallPath();
            if (installtionPath == null)
            {
                ConsoleWriter.WriteErrLine(string.Format(Resources.LoadRunnerNotInstalled, Environment.MachineName));
                Environment.Exit((int) Launcher.ExitCodeEnum.Aborted);
            }

            installtionPath = Path.Combine(installtionPath, BIN);

            Assembly ans;
            if (!File.Exists(Path.Combine(installtionPath, $"{assemblyName}.dll")))
            {
                //resource!
                ConsoleWriter.WriteErrLine($"cannot locate {assemblyName}.dll in installation directory");
                Environment.Exit((int) Launcher.ExitCodeEnum.Aborted);
            }
            else
            {
                //Console.WriteLine("loading " + assemblyName + " from " + Path.Combine(installtionPath, assemblyName + ".dll"));
                ans = Assembly.LoadFrom(Path.Combine(installtionPath, $"{assemblyName}.dll"));

                AssemblyName loadedName = ans.GetName();
                if (loadedName.Name == INTEROP_WLRUN)
                {
                    if (loadedName.Version.Major > 11 || (loadedName.Version.Major == 11 && loadedName.Version.Minor >= 52))
                    {
                        return ans;
                    }
                    else
                    {
                        ConsoleWriter.WriteErrLine(string.Format(Resources.HPToolsAssemblyResolverWrongVersion, Environment.MachineName));
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
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(QTP_REG_ROOT);
            regkey ??= Registry.LocalMachine.OpenSubKey(QTP_REG_ROOT_64);
            return regkey is null ? GetRootFromEnvironment() : (string)regkey.GetValue(FT_ROOT_PATH_KEY);
        }

        //verify that files/folders exist (does not recurse into folders)
        public static List<TestData> ValidateFiles(IEnumerable<TestData> tests)
        {
            //Console.WriteLine("[ValidateFiles]");
            List<TestData> validTests = [];
            foreach (TestData test in tests)
            {
                //Console.WriteLine("ValidateFiles, test Id: " + test.Id +  ", test path " + test.Tests);
                if (!File.Exists(test.Tests) && !Directory.Exists(test.Tests))
                {
                    ConsoleWriter.WriteLine($"Error: File/Folder not found: '{test.Tests}'");
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
                ConsoleWriter.WriteLine($"Error: File not found: '{filePath}'");
                isFileValid = false;
                Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
            }

            return isFileValid;
        }

        public static bool IsTestingToolsInstalled(TestStorageType type)
        {
            //we want to check if we have Service Test, OpenText Functional Testing installed on a machine

            return IsQtpInstalled() || IsServiceTestInstalled() || IsLoadRunnerInstalled();

        }

        public static bool IsLoadRunnerInstalled()
        {
            //try 32 bit
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(LR_REG_KEY);
            //try 64-bit
            regkey ??= Registry.LocalMachine.OpenSubKey(LR_REG_KEY_64);

            if (regkey != null)
            {
                //LoadRunner Exist.
                //check if Controller is installed (not SA version)
                if (regkey.OpenSubKey(LR_CONTROLLER_SUB_KEY) != null)
                {
                    return true;
                }

            }
            return false;
        }

        public static bool IsQtpInstalled()
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(QTP_REG_ROOT);
            regkey ??= Registry.LocalMachine.OpenSubKey(QTP_REG_ROOT_64);
            return regkey is not null && !((string)regkey.GetValue(FT_ROOT_PATH_KEY)).IsNullOrEmpty();
        }

        public static bool IsServiceTestInstalled()
        {
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(ST_CRT_VER_REG_KEY);
            regkey ??= Registry.LocalMachine.OpenSubKey(ST_CRT_VER_REG_KEY_64);
            return regkey is not null && !((string)regkey.GetValue(LOCAL_MLROOT)).IsNullOrEmpty();
        }

        private static string GetRootFromEnvironment()
        {
            string qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.Process);

            if (qtpRoot.IsNullOrEmpty())
            {
                qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.User);

                if (qtpRoot.IsNullOrEmpty())
                {
                    qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.Machine);

                    if (qtpRoot.IsNullOrEmpty())
                    {
                        qtpRoot = Environment.CurrentDirectory;
                    }
                }
            }

            return qtpRoot;
        }

        public static string GetSTInstallPath()
        {
            string ret = string.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(ST_CRT_VER_REG_KEY);
            if (regKey != null)
            {
                var val = regKey.GetValue(LOCAL_MLROOT);
                if (null != val)
                {
                    ret = val.ToString();
                }
            }
            else
            {
                regKey = Registry.LocalMachine.OpenSubKey(ST_CRT_VER_REG_KEY_64);
                if (regKey != null)
                {
                    var val = regKey.GetValue(LOCAL_MLROOT);
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

            if (!ret.IsNullOrEmpty())
            {
                if (ret.TrimEnd(BACKSLASH).EndsWith($@"\{BIN}"))
                    ret = ret.Substring(0, ret.LastIndexOf(BIN));

                if (!ret.EndsWith(BACKSLASH_)) ret += BACKSLASH_;
            }

            return ret;
        }

        public static string GetLRInstallPath()
        {
            string installPath = null;
            IDictionary envVars = Environment.GetEnvironmentVariables();

            //try to find LoadRunner install path in environment vars
            foreach (string v in _loadRunnerEnvVars)
            {
                if (envVars.Contains(v))
                    return envVars[v] as string;
            }

            //Fallback to registry
            //try 32 bit
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(LR_REG_KEY);

            //try 64-bit
            regkey ??= Registry.LocalMachine.OpenSubKey(LR_REG_KEY_64);

            if (regkey != null)
            {
                //LoadRunner Exists. check if Controller is installed (not SA version)
                regkey = regkey.OpenSubKey(CURRENT_VERSION);
                if (regkey != null)
                    return regkey.GetValue(CONTROLLER).ToString();
            }
            return installPath;
        }

        public static List<string> GetTestsLocations(string baseDir)
        {
            List<string> testsLocations = [];
            if (baseDir.IsNullOrEmpty() || !Directory.Exists(baseDir))
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
                //ST and Functional Testing uses folder as test locations
                return Directory.EnumerateFiles(path, ST_SEARCH_PATTERN, SearchOption.TopDirectoryOnly).Any() ? TestType.ST : TestType.QTP;
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

        static void WalkDirectoryTree(DirectoryInfo root, ref List<string> results)
        {
            FileInfo[] files = null;
            
            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles($"*{_ST}");
                files = files.Union(root.GetFiles($"*{_TSP}")).ToArray();
                files = files.Union(root.GetFiles($"*{_LRS}")).ToArray();
            }
            catch
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                //log.Add(e.Message);
            }

            if (files != null)
            {
                foreach (FileInfo fi in files)
                {
                    if (fi.Extension == _LRS)
                        results.Add(fi.FullName);
                    else
                        results.Add(fi.Directory.FullName);

                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                }

                // Now find all the subdirectories under this directory.
                DirectoryInfo[] subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    // Recursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref results);
                }
            }
        }

        public static string GetTempDir()
        {
            const string DASH = "-";
            string baseTemp = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string dirName = Guid.NewGuid().ToString().Replace(DASH, string.Empty).Substring(0, 6);
            string tempDirPath = Path.Combine(baseTemp, dirName);

            return tempDirPath;
        }

        public static bool IsLeanFTRunning()
        {
            bool bRet = false;
            Process[] procArray = Process.GetProcessesByName(LFT_RUNTIME);
            // Hardcoded temporarily since LeanFT does not store the process name anywhere
            if (procArray.Length != 0)
            {
                bRet = true;
            }
            return bRet;
        }

        public static bool IsSprinterRunning()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(MANUAL_RUNNER_PROC_REG_KEY);
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
            //Close Functional Testing when some of the Sprinter processes is running
            if (IsSprinterRunning())
            {
                reason = Resources.UFT_Sprinter_Running;
                return false;
            }

            //Close Functional Testing when LeanFT engine is running
            if (IsLeanFTRunning())
            {
                reason = Resources.UFT_LeanFT_Running;
                return false;
            }
            reason = string.Empty;
            return true;
        }

        public static void KillUftProcess()
        {
            Process[] procs = Process.GetProcessesByName(UFT);
            procs?.ForEach(p => p.Kill());
        }

        public static string GetParallelRunnerFullPathFile()
        {
            var uftFolder = GetSTInstallPath();
            if (uftFolder == null) return null;

            return Path.Combine(uftFolder, BIN, PARALLEL_RUNNER_EXE);
        }

        /// <summary>
        /// Why we need this? If we run jenkins in a master slave node where there is a jenkins service installed in the slave machine, we need to change the DCOM settings as follow:
        /// dcomcnfg.exe -> My Computer -> DCOM Config -> QuickTest Professional Automation -> Identity -> and select The Interactive User
        /// </summary>
        public static void ChangeDCOMSettingToInteractiveUser()
        {
            string errorMsg = "Unable to change DCOM settings. To change it manually: " +
                  "run dcomcnfg.exe -> My Computer -> DCOM Config -> QuickTest Professional Automation -> Identity -> and select The Interactive User";

            const string INTERACTIVE_USER = "Interactive User";
            const string RUN_AS = "RunAs";

            try
            {
                var regKey = GetQTPAutomationRegKey() ?? throw new Exception(@$"Unable to find in registry key {RegistryHive.LocalMachine}\{QTP_APPID_REG_KEY}");
                string runAsKey = regKey.GetValue(RUN_AS) as string;

                if (runAsKey != INTERACTIVE_USER)
                {
                    regKey.SetValue(RUN_AS, INTERACTIVE_USER);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{errorMsg} : {ex.Message}");
            }
        }

        private static RegistryKey GetQTPAutomationRegKey()
        {
            RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            localKey = localKey.OpenSubKey(QTP_APPID_REG_KEY, true);
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

            return @$"{reportPath}\{resultFolderName}{resultFolderIndex}";
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
            string testName = testInfo.TestName.Substring(testInfo.TestName.LastIndexOf(BACKSLASH) + 1);
            string reportLocation = GetNextResFolder(testInfo.ReportBaseDirectory, $"{testName}_");

            // set the report location for the run results
            runResults.ReportLocation = reportLocation;

            try
            {
                Directory.CreateDirectory(runResults.ReportLocation);
            }
            catch
            {
                SetTestReportPathError(runResults, ref errorReason, testInfo);
                return false;
            }

            return true;
        }

        public static TestState GetTestStateFromUFTReport(TestRunResults runDesc, string resultsFileFullPath)
        {
            try
            {
                TestState finalState = GetStateFromUFTResultsFile(resultsFileFullPath, out string desc);
                if (!desc.IsNullOrWhiteSpace())
                {
                    if (finalState == TestState.Error)
                    {
                        runDesc.ErrorDesc = desc;
                    }
                    else if (finalState == TestState.Failed)
                    {
                        runDesc.FailureDesc = desc; 
                    }
                }

                if (finalState == TestState.Failed && runDesc.FailureDesc.IsNullOrWhiteSpace())
                    runDesc.FailureDesc = TEST_FAILED;

                runDesc.TestState = finalState;
                return runDesc.TestState;
            }
            catch
            {
                return TestState.Unknown;
            }
        }

        public static TestState GetTestStateFromLRReport(TestRunResults runDesc, string[] resultFiles)
        {
            foreach (string resultFileFullPath in resultFiles)
            {
                runDesc.TestState = GetTestStateFromLRReport(resultFileFullPath, out string desc);
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
                //if there is Result.xml -> Functional Testing
                //if there is sla.xml file -> LR
                //if there is parallelrun_results.xml -> ParallelRunner

                string[] resultFiles = Directory.GetFiles(runDesc.ReportLocation, RESULTS_XML, SearchOption.TopDirectoryOnly);
                if (resultFiles.Length == 0)
                    resultFiles = Directory.GetFiles(runDesc.ReportLocation, RUN_RESULTS_XML, SearchOption.TopDirectoryOnly);

                if (resultFiles.Any())
                    return GetTestStateFromUFTReport(runDesc, resultFiles[0]);

                resultFiles = Directory.GetFiles(runDesc.ReportLocation, SLA_XML, SearchOption.AllDirectories);

                if (resultFiles.Any())
                {
                    return GetTestStateFromLRReport(runDesc, resultFiles);
                }

                resultFiles = Directory.GetFiles(runDesc.ReportLocation, PARALLELRUN_RESULTS_HTML, SearchOption.TopDirectoryOnly);

                // the overall status is given by parallel runner
                // at the end of the run
                if (resultFiles?.Length > 0)
                {
                    return runDesc.TestState;
                }

                //no LR or Functional Testing => error
                runDesc.ErrorDesc = string.Format($"no results file found for {runDesc.TestName}");
                runDesc.TestState = TestState.Error;
                return runDesc.TestState;
            }
            catch
            {
                return TestState.Unknown;
            }
        }

        private static TestState GetTestStateFromLRReport(string resultFileFullPath, out string desc)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(resultFileFullPath);
            return CheckNodeStatus(xdoc.DocumentElement, out desc);
        }

        private static TestState CheckNodeStatus(XmlNode node, out string desc)
        {
            desc = string.Empty;
            if (node == null)
                return TestState.Failed;

            if (node.ChildNodes.Count == 1 && node.ChildNodes[0].NodeType == XmlNodeType.Text)
            {
                if (node.InnerText.ToLowerInvariant() == "failed")
                {
                    if (node.Attributes?["FullName"] != null)
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
                TestState res = CheckNodeStatus(childNode, out desc);
                if (res == TestState.Failed)
                {
                    if (desc.IsNullOrEmpty() && node.Attributes?["FullName"] != null)
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

        private static TestState GetStateFromUFTResultsFile(string resFileFullPath, out string desc)
        {
            desc = string.Empty;
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(resFileFullPath);
            string strFileName = Path.GetFileName(resFileFullPath);
            string status = $"{TestState.Unknown}";
            TestState finalState = TestState.Failed;
            if (strFileName == RUN_RESULTS_XML)
            {
                XmlNodeList rNodeList = doc.SelectNodes(RES_RPT_NODE_DATA_XPATH);
                if (rNodeList == null)
                {
                    desc = string.Format(Resources.XmlNodeNotExistError, RES_RPT_NODE_DATA_XPATH);
                }
                else
                {
                    var node = rNodeList.Item(0) as XmlElement;
                    XmlNode resultNode = node.GetElementsByTagName(RESULT).Item(0);
                    status = resultNode.InnerText;
                }
            }
            else
            {
                var testStatusPathNode = doc.SelectSingleNode(RPT_DOC_NODE_ARGS_XPATH);
                if (testStatusPathNode == null)
                    desc = string.Format(Resources.XmlNodeNotExistError, RPT_DOC_NODE_ARGS_XPATH);
                else if (testStatusPathNode.Attributes[STATUS].Specified)
                    status = testStatusPathNode.Attributes[STATUS].Value;
            }

            if (Enum.TryParse(status, out TestResult result))
            {
                if (result.In(TestResult.Passed, TestResult.Done))
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
            }
            return finalState;
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
