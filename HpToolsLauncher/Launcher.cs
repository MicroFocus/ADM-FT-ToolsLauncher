/*
 *
 *  Certain versions of software and/or documents ("Material") accessible here may contain branding from
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
 * and licensors ("Micro Focus") are set forth in the express warranty statements
 * accompanying such products and services. Nothing herein should be construed as
 * constituting an additional warranty. Micro Focus shall not be liable for technical
 * or editorial errors or omissions contained herein.
 * The information contained herein is subject to change without notice.
 * ___________________________________________________________________
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HpToolsLauncher.Properties;
using HpToolsLauncher.TestRunners;
using HpToolsLauncher.RTS;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace HpToolsLauncher
{

    public enum CiName
    {
        Hudson,
        Jenkins,
        TFS,
        CCNET
    }

    public class McConnectionInfo
    {
        public enum AuthType
        {
            UsernamePassword,
            AuthToken
        }

        public string MobileUserName { get; set; }
        public string MobilePassword { get; set; }
        public string MobileClientId { get; set; }
        public string MobileSecretKey { get; set; }
        public string MobileHostAddress { get; set; }
        public string MobileHostPort { get; set; }

        public string MobileTenantId { get; set; }

        public int MobileUseSSL { get; set; }

        public int MobileUseProxy { get; set; }
        public int MobileProxyType { get; set; }
        public string MobileProxySetting_Address { get; set; }
        public int MobileProxySetting_Port { get; set; }
        public int MobileProxySetting_Authentication { get; set; }
        public string MobileProxySetting_UserName { get; set; }
        public string MobileProxySetting_Password { get; set; }

        private const string YES = "Yes";
        private const string NO = "No";
        private const string SYSTEM = "System";
        private const string HTTP = "Http";
        private const string DEFAULT_PORT = "8080";
        private const int ZERO = 0;
        private const int ONE = 1;

        private string GetMobileUseSslAsString()
        {
            return MobileUseSSL == ONE ? YES : NO;
        }

        private string GetMobileUseProxyAsString()
        {
            return MobileUseProxy == ONE ? YES : NO;
        }

        private string GetMobileProxyTypeAsString()
        { 
            return MobileUseProxy == ONE ? (MobileProxyType == ONE ? SYSTEM : HTTP) : string.Empty;
        }

        private string GetMobileProxyAuthenticationAsString()
        {
            return MobileUseProxy == ONE ? (MobileProxySetting_Authentication == ONE ? YES : NO) : string.Empty;
        }

        public McConnectionInfo()
        {
            MobileHostPort = DEFAULT_PORT;
            MobileUserName = string.Empty;
            MobilePassword = string.Empty;
            MobileHostAddress = string.Empty;
            MobileTenantId = string.Empty;
            MobileUseSSL = ZERO;

            MobileUseProxy = ZERO;
            MobileProxyType = ZERO;
            MobileProxySetting_Address = string.Empty;
            MobileProxySetting_Port = ZERO;
            MobileProxySetting_Authentication = ZERO;
            MobileProxySetting_UserName = string.Empty;
            MobileProxySetting_Password = string.Empty;
        }

        public override string ToString()
        {
            string usernameOrClientId;
            if (!string.IsNullOrEmpty(MobileClientId) && !string.IsNullOrEmpty(MobileSecretKey))
            {
                usernameOrClientId = string.Format("ClientId: {0}", MobileClientId);
            }
            else
            {
                usernameOrClientId = string.Format("Username: {0}", MobileSecretKey);
            }
            string McConnectionStr =
                 string.Format("UFT Mobile HostAddress: {0}, Port: {1}, {2}, TenantId: {3}, UseSSL: {4}, UseProxy: {5}, ProxyType: {6}, ProxyAddress: {7}, ProxyPort: {8}, ProxyAuth: {9}, ProxyUser: {10}",
                 MobileHostAddress, MobileHostPort, usernameOrClientId, MobileTenantId, GetMobileUseSslAsString(), GetMobileUseProxyAsString(), GetMobileProxyTypeAsString(), MobileProxySetting_Address, MobileProxySetting_Port, GetMobileProxyAuthenticationAsString(),
                 MobileProxySetting_UserName);
            return McConnectionStr;
        }
    }

    public class Launcher
    {
        private IXmlBuilder _xmlBuilder;
        private bool _ciRun = false;
        private readonly string _paramFileName = null;
        private JavaProperties _ciParams = new JavaProperties();
        private TestStorageType _runType;
        private readonly string _failOnUftTestFailed;
        private static ExitCodeEnum _exitCode = ExitCodeEnum.Passed;
        private static string _dateFormat = "dd/MM/yyyy HH:mm:ss";
        private static bool _rerunFailedTests = false;
        XmlSerializer _serializer = new XmlSerializer(typeof(testsuites));

        testsuites _testSuites = new testsuites();

        //public const string ClassName = "HPToolsFileSystemRunner";


        public static string DateFormat
        {
            get { return Launcher._dateFormat; }
            set { Launcher._dateFormat = value; }
        }

        /// <summary>
        /// if running an alm job theses strings are mandatory:
        /// </summary>
        private string[] requiredParamsForQcRun = { "almServerUrl",
                                 "almUsername",
                                 //"almPassword",
                                 "almDomain",
                                 "almProject"/*,
                                 "almRunMode",
                                 "almTimeout",
                                 "almRunHost"*/};
        private string[] requiredParamsForQcRunInSSOMode = { "almServerUrl",
                                 "almClientID",
                                 "almDomain",
                                 "almProject"};
        private readonly string[] requiredAlmApiKeyParams = { "almApiKeySecretBasicAuth", "almApiKeySecret" }; // if SSO then one ApiKey param is required

        /// <summary>
        /// a place to save the unique timestamp which shows up in properties/results/abort file names
        /// this timestamp per job run.
        /// </summary>
        public static string UniqueTimeStamp { get; set; }

        /// <summary>
        /// saves the exit code in case we want to run all tests but fail at the end since a file wasn't found
        /// </summary>
        public static ExitCodeEnum ExitCode
        {
            get { return Launcher._exitCode; }
            set { Launcher._exitCode = value; }
        }

        public enum ExitCodeEnum
        {
            Passed = 0,
            Failed = -1,
            PartialFailed = -2,
            Aborted = -3,
            Unstable = -4,
            AlmNotConnected = -5
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="failOnTestFailed"></param>
        /// <param name="paramFileName"></param>
        /// <param name="runType"></param>
        public Launcher(string failOnTestFailed, string paramFileName, TestStorageType runType)
        {
            _runType = runType;
            if (paramFileName != null)
            {
                _ciParams.Load(paramFileName);
                if (_ciParams.NotSupportedFileBOM)
                {
                    IsParamFileEncodingNotSupported = true;
                    return;
                }
            }
            _paramFileName = paramFileName;

            _failOnUftTestFailed = string.IsNullOrEmpty(failOnTestFailed) ? "N" : failOnTestFailed;
        }

        public bool IsParamFileEncodingNotSupported { get; private set; }

        private static String _secretKey = "EncriptionPass4Java";

        /// <summary>
        /// decrypts strings which were encrypted by Encrypt (in the c# or java code, mainly for qc passwords)
        /// </summary>
        /// <param name="textToDecrypt"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string Decrypt(string textToDecrypt, string key)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            byte[] encryptedData = Convert.FromBase64String(textToDecrypt);
            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;
            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            byte[] plainText = rijndaelCipher.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            return Encoding.UTF8.GetString(plainText);
        }

        /// <summary>
        /// encrypts strings to be decrypted by decrypt function(in the c# or java code, mainly for qc passwords)
        /// </summary>
        /// <param name="textToEncrypt"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string Encrypt(string textToEncrypt, string key)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();
            rijndaelCipher.Mode = CipherMode.CBC;
            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 0x80;
            rijndaelCipher.BlockSize = 0x80;
            byte[] pwdBytes = Encoding.UTF8.GetBytes(key);
            byte[] keyBytes = new byte[0x10];
            int len = pwdBytes.Length;
            if (len > keyBytes.Length)
            {
                len = keyBytes.Length;
            }
            Array.Copy(pwdBytes, keyBytes, len);
            rijndaelCipher.Key = keyBytes;
            rijndaelCipher.IV = keyBytes;
            ICryptoTransform transform = rijndaelCipher.CreateEncryptor();
            byte[] plainText = Encoding.UTF8.GetBytes(textToEncrypt);
            return Convert.ToBase64String(transform.TransformFinalBlock(plainText, 0, plainText.Length));
        }

        /// <summary>
        /// writes to console using the ConsolWriter class
        /// </summary>
        /// <param name="message"></param>
        private static void WriteToConsole(string message)
        {
            ConsoleWriter.WriteLine(message);
        }

        /// <summary>
        /// analyzes and runs the tests given in the param file.
        /// </summary>
        public void Run()
        {
            _ciRun = true;
            if (_runType == TestStorageType.Unknown)
            {
                if (_ciParams.ContainsKey("runType"))
                {
                    Enum.TryParse<TestStorageType>(_ciParams["runType"], true, out _runType);
                }
            }
            if (_runType == TestStorageType.Unknown)
            {
                WriteToConsole(Resources.LauncherNoRuntype);
                return;
            }

            if (!_ciParams.ContainsKey("resultsFilename"))
            {
                WriteToConsole(Resources.LauncherNoResFilenameFound);
                return;
            }
            string resultsFilename = _ciParams["resultsFilename"];

            UniqueTimeStamp = string.Empty;
            if (_ciParams.ContainsKey("uniqueTimeStamp"))
            {
                UniqueTimeStamp = _ciParams["uniqueTimeStamp"];
            }
            if (string.IsNullOrWhiteSpace(UniqueTimeStamp))
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(resultsFilename);
                Regex regex = new Regex(@"Results(\d{6,17})");
                Match m = regex.Match(fileNameOnly);
                if (m.Success)
                {
                    UniqueTimeStamp = m.Groups[1].Value;
                }
            }
            if (string.IsNullOrWhiteSpace(UniqueTimeStamp))
            {
                UniqueTimeStamp = DateTime.Now.ToString("ddMMyyyyHHmmssfff");
            }

            List<TestData> failedTests = new List<TestData>();

            //run the entire set of test once
            //create the runner according to type
            IAssetRunner runner = CreateRunner(_runType, _ciParams, true, failedTests);

            //runner instantiation failed (no tests to run or other problem)
            if (runner == null)
            {
                //ConsoleWriter.WriteLine("empty runner;");
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
            }

            TestSuiteRunResults results = runner.Run();
     
            RunTests(runner, resultsFilename, results);

            var lastExitCode = ExitCode;
            Console.WriteLine("The reported status is: {0}", lastExitCode);

            if (_runType.Equals(TestStorageType.FileSystem))
            {
                string onCheckFailedTests = (_ciParams.ContainsKey("onCheckFailedTest") ? _ciParams["onCheckFailedTest"] : string.Empty);

                _rerunFailedTests = !string.IsNullOrEmpty(onCheckFailedTests) && Convert.ToBoolean(onCheckFailedTests.ToLower());

                 //the "On failure" option is selected and the run build contains failed tests
                if (_rerunFailedTests.Equals(true) && (ExitCode == ExitCodeEnum.Failed || ExitCode == ExitCodeEnum.PartialFailed))
                {
                    ConsoleWriter.WriteLine("There are failed tests.");

                    //rerun the selected tests (either the entire set, just the selected tests or only the failed tests)
                    List<TestRunResults> runResults = results.TestRuns;
                    int index = 0;
                    foreach (var item in runResults)
                    {
                        if (item.TestState == TestState.Failed || item.TestState == TestState.Error)
                        {
                            index++;
                            failedTests.Add(new TestData(item.TestPath, "FailedTest" + index)
                            {
                                ReportPath = item.TestInfo == null ? null : item.TestInfo.ReportPath
                            });
                        }
                    }


                    //create the runner according to type
                    runner = CreateRunner(_runType, _ciParams, false, failedTests);

                    //runner instantiation failed (no tests to run or other problem)
                    if (runner == null)
                    {
                        Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                    }

                    TestSuiteRunResults rerunResults = runner.Run();

                    results.AppendResults(rerunResults);
                    RunTests(runner, resultsFilename, results);

                    // check if all the rerun tests are passed
                    if (rerunResults.NumErrors == 0 && rerunResults.NumFailures == 0)
                    {
                        // it is the Unstable case
                        lastExitCode = ExitCodeEnum.Unstable;
                    }
                }
            }

            Console.WriteLine("The final status is: {0}", lastExitCode);
            Launcher.ExitCode = lastExitCode;
            Environment.ExitCode = (int)lastExitCode;

            // if the launcher reported Unstable, the process exit code might be 0 or non-zero
            // depends on whether treats the Unstable as a Failure
            if (lastExitCode == ExitCodeEnum.Unstable)
            {
                string unstableAsFailKey = "unstableAsFailure";
                if (!_ciParams.ContainsKey(unstableAsFailKey))
                {
                    // unstable is not treated as a failure by default
                    Environment.ExitCode = (int)ExitCodeEnum.Passed;
                }
                else
                {
                    string unstableAsFailValue = _ciParams[unstableAsFailKey];
                    if (unstableAsFailValue.Trim().ToLower() == "false" || unstableAsFailValue.Trim() == "0")
                    {
                        // explicitly specify that unstable shall not be treated as a failure
                        Environment.ExitCode = (int)ExitCodeEnum.Passed;
                    }
                }
            }
        }

        public static void DeleteDirectory(String dirPath)
        {
            DirectoryInfo directory = Directory.CreateDirectory(dirPath);
            foreach (FileInfo file in directory.GetFiles()) file.Delete();
            foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            Directory.Delete(dirPath);
        }

        /// <summary>
        /// creates the correct runner according to the given type
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="ciParams"></param>
        /// <param name="initialTestRun"></param>
        private IAssetRunner CreateRunner(TestStorageType runType, JavaProperties ciParams, bool initialTestRun, List<TestData> failedTests)
        {
            IAssetRunner runner = null;

            switch (runType)
            {
                case TestStorageType.AlmLabManagement:

                case TestStorageType.Alm:
                    //check that all required parameters exist
                    bool isSSOEnabled = _ciParams.ContainsKey("SSOEnabled") && Convert.ToBoolean(_ciParams["SSOEnabled"]);
                    if (isSSOEnabled)
                    {
                        foreach (string param1 in requiredParamsForQcRunInSSOMode)
                        {
                            if (!_ciParams.ContainsKey(param1))
                            {
                                ConsoleWriter.WriteErrLine(string.Format(Resources.LauncherParamRequired, param1));
                                return null;
                            }
                        }
                        IList<string> apiKeyProps = _ciParams.Keys.Intersect(requiredAlmApiKeyParams).ToList();
                        if (!apiKeyProps.Any())
                        {
                            ConsoleWriter.WriteErrLine(string.Format(Resources.LauncherApiKeyParamRequiredForSSO, string.Join("' or '", requiredAlmApiKeyParams)));
                            return null;
                        }
                        else if (apiKeyProps.Count > 1)
                        {
                            ConsoleWriter.WriteErrLine(string.Format(Resources.LauncherApiKeyParamsRequiredForSSOCantBeUsedSimultaneously, string.Join("' and '", requiredAlmApiKeyParams)));
                            return null;
                        }
                    }
                    else
                    {
                        foreach (string param1 in requiredParamsForQcRun)
                        {
                            if (!_ciParams.ContainsKey(param1))
                            {
                                ConsoleWriter.WriteErrLine(string.Format(Resources.LauncherParamRequired, param1));
                                return null;
                            }
                        }
                    }

                    //parse params that need parsing
                    double dblQcTimeout = int.MaxValue;
                    if (_ciParams.ContainsKey("almTimeout"))
                    {
                        if (!double.TryParse(_ciParams["almTimeout"], out dblQcTimeout))
                        {
                            ConsoleWriter.WriteLine(Resources.LauncherTimeoutNotNumeric);
                            dblQcTimeout = int.MaxValue;
                        }
                    }
                    if (dblQcTimeout == -1)
                    {
                        ConsoleWriter.WriteLine(Resources.AlmTimeoutInfinite);
                    }
                    else
                    {
                        ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayTimout, dblQcTimeout));
                    }

                    QcRunMode enmQcRunMode = QcRunMode.RUN_LOCAL;
                    if (_ciParams.ContainsKey("almRunMode"))
                    {
                        if (!Enum.TryParse<QcRunMode>(_ciParams["almRunMode"], true, out enmQcRunMode))
                        {
                            ConsoleWriter.WriteLine(Resources.LauncherIncorrectRunmode);
                            enmQcRunMode = QcRunMode.RUN_LOCAL;
                        }
                    }
                    ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayRunmode, enmQcRunMode.ToString()));

                    //go over test sets in the parameters, and collect them
                    List<string> sets = GetParamsWithPrefix("TestSet", true);

                    if (sets.Count == 0)
                    {
                        ConsoleWriter.WriteErrLine(Resources.LauncherNoTests); // is important to print the error to STDERR stream, so that ADM-TFS-Extension can handle it properly
                        return null;
                    }

                    //check if filterTests flag is selected; if yes apply filters on the list
                    bool isFilterSelected;
                    string filter = _ciParams.ContainsKey("FilterTests") ? _ciParams["FilterTests"] : string.Empty;

                    isFilterSelected = !string.IsNullOrEmpty(filter) && Convert.ToBoolean(filter.ToLower());

                    string filterByName = _ciParams.ContainsKey("FilterByName") ? _ciParams["FilterByName"] : string.Empty;

                    string statuses = _ciParams.ContainsKey("FilterByStatus") ? _ciParams["FilterByStatus"] : string.Empty;

                    List<string> filterByStatuses = new List<string>();

                    if (statuses != string.Empty)
                    {
                        if (statuses.Contains(","))
                        {
                            filterByStatuses = statuses.Split(',').ToList();
                        }
                        else
                        {
                            filterByStatuses.Add(statuses);
                        }
                    }

                    string clientID = _ciParams.ContainsKey("almClientID") ? _ciParams["almClientID"] : string.Empty;
                    string apiKey = string.Empty;
                    if (_ciParams.ContainsKey("almApiKeySecretBasicAuth"))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(_ciParams["almApiKeySecretBasicAuth"]);
                        apiKey = Encoding.Default.GetString(data);
                    }
                    else if (_ciParams.ContainsKey("almApiKeySecret"))
                    {
                        apiKey = Decrypt(_ciParams["almApiKeySecret"], _secretKey);
                    }

                    string almRunHost = _ciParams.ContainsKey("almRunHost") ? _ciParams["almRunHost"] : string.Empty;

                    string almPassword = string.Empty;
                    if (_ciParams.ContainsKey("almPasswordBasicAuth"))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(_ciParams["almPasswordBasicAuth"]);
                        almPassword = Encoding.Default.GetString(data);
                    }
                    else if (_ciParams.ContainsKey("almPassword"))
                    {
                        almPassword = Decrypt(_ciParams["almPassword"], _secretKey);
                    }

                    //create an Alm runner
                    runner = new AlmTestSetsRunner(_ciParams["almServerUrl"],
                                     _ciParams.ContainsKey("almUsername") ? _ciParams["almUsername"] : string.Empty,
                                     almPassword,
                                     _ciParams["almDomain"],
                                     _ciParams["almProject"],
                                     dblQcTimeout,
                                     enmQcRunMode,
                                     almRunHost,
                                     sets,
                                     isFilterSelected,
                                     filterByName,
                                     filterByStatuses,
                                     initialTestRun,
                                     runType,
                                     isSSOEnabled,
                                     clientID, apiKey);
                    break;
                case TestStorageType.FileSystem:
                    bool displayController = false;
                    if (_ciParams.ContainsKey("displayController"))
                    {
                        if (_ciParams["displayController"] == "1")
                        {
                            displayController = true;
                        }
                    }
                    string analysisTemplate = _ciParams.ContainsKey("analysisTemplate") ? _ciParams["analysisTemplate"] : string.Empty;

                    List<TestData> validBuildTests = GetValidTests("Test", Resources.LauncherNoTestsFound, Resources.LauncherNoValidTests, string.Empty);

                    // report path specified for each test
                    foreach (TestData t in validBuildTests)
                    {
                        // the test Id is something like "Test{i}"
                        string idx = t.Id.Remove(0, "Test".Length);
                        string reportPathKey = string.Format("fsReportPath{0}", idx);
                        if (_ciParams.ContainsKey(reportPathKey))
                        {
                            t.ReportPath = _ciParams[reportPathKey];
                            if (!string.IsNullOrEmpty(t.ReportPath))
                            {
                                t.ReportPath = t.ReportPath.Trim();
                            }
                        }
                    }

                    //add build tests and cleanup tests in correct order
                    List<TestData> validTests = new List<TestData>();

                    if (!_rerunFailedTests)
                    {
                        //ConsoleWriter.WriteLine("Run build tests");

                        //run only the build tests
                        foreach (var item in validBuildTests)
                        {
                            validTests.Add(item);
                        }

                       /* foreach (var item in validTests)
                        {
                            if (!Directory.Exists(item.Tests) && item.Tests.Contains("mtbx"))
                            {
                                List<TestInfo> mtbxTests = MtbxManager.Parse(item.Tests, null, item.Tests);
                                foreach (var currentTest in mtbxTests)
                                {
                                    deleteOldReportFolders(currentTest.TestPath);
                                }
                            }
                            else
                            {
                                deleteOldReportFolders(item.Tests);
                            }
                        }*/
                    }
                    else
                    { //add also cleanup tests
                        string fsTestType = _ciParams.ContainsKey("testType") ? _ciParams["testType"] : string.Empty;

                        List<TestData> validFailedTests = GetValidTests("FailedTest", Resources.LauncherNoFailedTestsFound, Resources.LauncherNoValidFailedTests, fsTestType);
                        List<TestData> validCleanupTests = new List<TestData>();
                        if (GetValidTests("CleanupTest", Resources.LauncherNoCleanupTestsFound, Resources.LauncherNoValidCleanupTests, fsTestType).Count > 0)
                        {
                            validCleanupTests = GetValidTests("CleanupTest", Resources.LauncherNoCleanupTestsFound, Resources.LauncherNoValidCleanupTests, fsTestType);
                        }
                        List<string> reruns = GetParamsWithPrefix("Reruns");
                        List<int> numberOfReruns = new List<int>();
                        foreach (var item in reruns)
                        {
                            numberOfReruns.Add(int.Parse(item));
                        }

                        bool noRerunsSet = CheckListOfRerunValues(numberOfReruns);

                        if (noRerunsSet)
                        {
                            ConsoleWriter.WriteLine("In order to rerun the tests the number of reruns should be greater than zero.");
                        }
                        else
                        {
                            if (string.Compare(fsTestType, "Rerun the entire set of tests", true) == 0)
                            {
                                ConsoleWriter.WriteLine("The entire test set will run again.");
                                int rerunNum = numberOfReruns[0];
                                for (int i = 0; i < rerunNum; i++)
                                {
                                    // for each rerun, always run cleanup tests before entire test set
                                    if (validCleanupTests.Count > 0)
                                    {
                                        validTests.AddRange(validCleanupTests);
                                    }

                                    // rerun all tests
                                    validTests.AddRange(validBuildTests);
                                }
                            }
                            else if (string.Compare(fsTestType, "Rerun specific tests in the build", true) == 0)
                            {
                                ConsoleWriter.WriteLine("Only the specific tests will run again.");
                                int rerunNum = numberOfReruns[0];
                                for (int i = 0; i < rerunNum; i++)
                                {
                                    // for each rerun, always run cleanup tests before run specific tests 
                                    if (validCleanupTests.Count > 0)
                                    {
                                        validTests.AddRange(validCleanupTests);
                                    }

                                    // run specific tests
                                    if (validFailedTests.Count > 0)
                                    {
                                        validTests.AddRange(validFailedTests);
                                    }
                                }
                            }
                            else if (string.Compare(fsTestType, "Rerun only failed tests", true) == 0)
                            {
                                ConsoleWriter.WriteLine("Only the failed tests will run again.");
                                Dictionary<string, TestData> failedDict = new Dictionary<string, TestData>();
                                foreach (TestData t in failedTests)
                                {
                                    failedDict.Add(t.Tests, t);
                                }

                                int rerunNum = 0;
                                while (true)
                                {
                                    List<TestData> tmpList = new List<TestData>();
                                    for (int i = 0; i < numberOfReruns.Count; i++)
                                    {
                                        int n = numberOfReruns[i] - rerunNum;
                                        if (n <= 0)
                                        {
                                            continue;
                                        }

                                        if (i >= validBuildTests.Count)
                                        {
                                            break;
                                        }

                                        if (!failedDict.ContainsKey(validBuildTests[i].Tests))
                                        {
                                            continue;
                                        }

                                        tmpList.Add(failedDict[validBuildTests[i].Tests]);
                                    }

                                    if (tmpList.Count == 0)
                                    {
                                        break;
                                    }

                                    if (validCleanupTests.Count > 0)
                                    {
                                        validTests.AddRange(validCleanupTests);
                                    }
                                    validTests.AddRange(tmpList);
                                    rerunNum++;
                                }
                            }
                            else
                            {
                                ConsoleWriter.WriteLine("Unknown testType, skip rerun tests.");
                            }
                        }
                    }

                    if (validTests.Count == 0)
                    {
                        return null;
                    }

                    //get the tests
                    //IEnumerable<string> tests = GetParamsWithPrefix("Test");

                    IEnumerable<string> jenkinsEnvVariablesWithCommas = GetParamsWithPrefix("JenkinsEnv");
                    Dictionary<string, string> jenkinsEnvVariables = new Dictionary<string, string>();
                    foreach (string var in jenkinsEnvVariablesWithCommas)
                    {
                        string[] nameVal = var.Split(",;".ToCharArray());
                        jenkinsEnvVariables.Add(nameVal[0], nameVal[1]);
                    }
                    //parse the timeout into a TimeSpan
                    TimeSpan timeout = TimeSpan.MaxValue;
                    if (_ciParams.ContainsKey("fsTimeout"))
                    {
                        string strTimeoutInSeconds = _ciParams["fsTimeout"];
                        if (strTimeoutInSeconds.Trim() != "-1")
                        {
                            double timeoutInSeconds = 0;
                            if (double.TryParse(strTimeoutInSeconds, out timeoutInSeconds))
                            {
                                if (timeoutInSeconds >= 0)
                                {
                                    timeout = TimeSpan.FromSeconds(Math.Round(timeoutInSeconds));
                                }
                            }
                        }
                    }
                    ConsoleWriter.WriteLine("Launcher timeout is " + timeout.ToString(@"dd\:\:hh\:mm\:ss"));

                    //LR specific values:
                    //default values are set by JAVA code, in com.hpe.application.automation.tools.model.RunFromFileSystemModel.java

                    int pollingInterval = 30;
                    if (_ciParams.ContainsKey("controllerPollingInterval"))
                    {
                        double value = 0;
                        if (double.TryParse(_ciParams["controllerPollingInterval"], out value))
                        {
                            if (value >= 0)
                            {
                                pollingInterval = (int)Math.Round(value);
                            }
                        }
                    }
                    ConsoleWriter.WriteLine("Controller Polling Interval: " + pollingInterval + " seconds");

                    TimeSpan perScenarioTimeOutMinutes = TimeSpan.MaxValue;
                    if (_ciParams.ContainsKey("PerScenarioTimeOut"))
                    {
                        string strTimeoutInMinutes = _ciParams["PerScenarioTimeOut"];
                        //ConsoleWriter.WriteLine("reading PerScenarioTimeout: "+ strTimoutInMinutes);
                        if (strTimeoutInMinutes.Trim() != "-1")
                        {
                            double timoutInMinutes = 0;
                            if (double.TryParse(strTimeoutInMinutes, out timoutInMinutes))
                            {
                                var totalSeconds = Math.Round(TimeSpan.FromMinutes(timoutInMinutes).TotalSeconds);
                                if (totalSeconds >= 0)
                                {
                                    perScenarioTimeOutMinutes = TimeSpan.FromSeconds(totalSeconds);
                                }
                            }
                            //ConsoleWriter.WriteLine("PerScenarioTimeout: "+perScenarioTimeOutMinutes+" minutes");
                        }
                    }
                    ConsoleWriter.WriteLine("PerScenarioTimeout: " + perScenarioTimeOutMinutes.ToString(@"dd\:\:hh\:mm\:ss"));

                    char[] delimiter = { '\n' };
                    List<string> ignoreErrorStrings = new List<string>();
                    if (_ciParams.ContainsKey("ignoreErrorStrings"))
                    {
                        if (_ciParams.ContainsKey("ignoreErrorStrings"))
                        {
                            ignoreErrorStrings.AddRange(Array.ConvertAll(_ciParams["ignoreErrorStrings"].Split(delimiter, StringSplitOptions.RemoveEmptyEntries), ignoreError => ignoreError.Trim()));
                        }
                    }

                    //If a file path was provided and it doesn't exist stop the analysis launcher
                    if (!analysisTemplate.Equals(string.Empty) && !Helper.FileExists(analysisTemplate))
                    {
                        return null;
                    }

                    //--MC connection info
                    McConnectionInfo mcConnectionInfo = new McConnectionInfo();
                    if (_ciParams.ContainsKey("MobileHostAddress"))
                    {
                        string mcServerUrl = _ciParams["MobileHostAddress"];

                        if (!string.IsNullOrEmpty(mcServerUrl))
                        {
                            //url is something like http://xxx.xxx.xxx.xxx:8080
                            string[] strArray = mcServerUrl.Split(new Char[] { ':' });
                            if (strArray.Length == 3)
                            {
                                mcConnectionInfo.MobileHostAddress = strArray[1].Replace("/", string.Empty);
                                mcConnectionInfo.MobileHostPort = strArray[2];
                            }

                            //mc username
                            if (_ciParams.ContainsKey("MobileUserName"))
                            {
                                string mcUsername = _ciParams["MobileUserName"];
                                if (!string.IsNullOrEmpty(mcUsername))
                                {
                                    mcConnectionInfo.MobileUserName = mcUsername;
                                }
                            }

                            //mc password
                            if (_ciParams.ContainsKey("MobilePasswordBasicAuth"))
                            {
                                // base64 decode
                                byte[] data = Convert.FromBase64String(_ciParams["MobilePasswordBasicAuth"]);
                                mcConnectionInfo.MobilePassword = Encoding.Default.GetString(data);
                            }
                            else if (_ciParams.ContainsKey("MobilePassword"))
                            {
                                string mcPassword = _ciParams["MobilePassword"];
                                if (!string.IsNullOrEmpty(mcPassword))
                                {
                                    mcConnectionInfo.MobilePassword = Decrypt(mcPassword, _secretKey);
                                }
                            }

                            //mc tenantId
                            if (_ciParams.ContainsKey("MobileTenantId"))
                            {
                                string mcTenantId = _ciParams["MobileTenantId"];
                                if (!string.IsNullOrEmpty(mcTenantId))
                                {
                                    mcConnectionInfo.MobileTenantId = mcTenantId;
                                }
                            }

                            if (_ciParams.ContainsKey("MobileClientId"))
                            {
                                string mcClientId = _ciParams["MobileClientId"];
                                if (!string.IsNullOrEmpty(mcClientId))
                                {
                                    mcConnectionInfo.MobileClientId = mcClientId;
                                    if (_ciParams.ContainsKey("MobileSecretKeyBasicAuth"))
                                    {
                                        // base64 decode
                                        byte[] data = Convert.FromBase64String(_ciParams["MobileSecretKeyBasicAuth"]);
                                        mcConnectionInfo.MobileSecretKey = Encoding.Default.GetString(data);
                                    }
                                    else if (_ciParams.ContainsKey("MobileSecretKey"))
                                    {
                                        string mcSecretKey = _ciParams["MobileSecretKey"];
                                        if (!string.IsNullOrEmpty(mcSecretKey))
                                        {
                                            mcConnectionInfo.MobileSecretKey = Decrypt(mcSecretKey, _secretKey);
                                        }
                                    }
                                }
                            }

                            //ssl
                            if (_ciParams.ContainsKey("MobileUseSSL"))
                            {
                                string mcUseSSL = _ciParams["MobileUseSSL"];
                                if (!string.IsNullOrEmpty(mcUseSSL))
                                {
                                    mcConnectionInfo.MobileUseSSL = int.Parse(mcUseSSL);
                                }
                            }

                            //Proxy enabled flag
                            if (_ciParams.ContainsKey("MobileUseProxy"))
                            {
                                string useProxy = _ciParams["MobileUseProxy"];
                                if (!string.IsNullOrEmpty(useProxy))
                                {
                                    mcConnectionInfo.MobileUseProxy = int.Parse(useProxy);
                                }
                            }


                            //Proxy type
                            if (_ciParams.ContainsKey("MobileProxyType"))
                            {
                                string proxyType = _ciParams["MobileProxyType"];
                                if (!string.IsNullOrEmpty(proxyType))
                                {
                                    mcConnectionInfo.MobileProxyType = int.Parse(proxyType);
                                }
                            }


                            //proxy address
                            if (_ciParams.ContainsKey("MobileProxySetting_Address"))
                            {
                                string proxyAddress = _ciParams["MobileProxySetting_Address"];
                                if (!string.IsNullOrEmpty(proxyAddress))
                                {
                                    // data is something like "16.105.9.23:8080"
                                    string[] strArrayForProxyAddress = proxyAddress.Split(new Char[] { ':' });

                                    if (strArrayForProxyAddress.Length == 2)
                                    {
                                        mcConnectionInfo.MobileProxySetting_Address = strArrayForProxyAddress[0];
                                        mcConnectionInfo.MobileProxySetting_Port = int.Parse(strArrayForProxyAddress[1]);
                                    }
                                }
                            }

                            //Proxy authentication
                            if (_ciParams.ContainsKey("MobileProxySetting_Authentication"))
                            {
                                string proxyAuthentication = _ciParams["MobileProxySetting_Authentication"];
                                if (!string.IsNullOrEmpty(proxyAuthentication))
                                {
                                    mcConnectionInfo.MobileProxySetting_Authentication = int.Parse(proxyAuthentication);
                                }
                            }

                            //Proxy username
                            if (_ciParams.ContainsKey("MobileProxySetting_UserName"))
                            {
                                string proxyUsername = _ciParams["MobileProxySetting_UserName"];
                                if (!string.IsNullOrEmpty(proxyUsername))
                                {
                                    mcConnectionInfo.MobileProxySetting_UserName = proxyUsername;
                                }
                            }

                            //Proxy password
                            if (_ciParams.ContainsKey("MobileProxySetting_PasswordBasicAuth"))
                            {
                                // base64 decode
                                byte[] data = Convert.FromBase64String(_ciParams["MobileProxySetting_Password"]);
                                mcConnectionInfo.MobileProxySetting_Password = Encoding.Default.GetString(data);
                            }
                            else if (_ciParams.ContainsKey("MobileProxySetting_Password"))
                            {
                                string proxyPassword = _ciParams["MobileProxySetting_Password"];
                                if (!string.IsNullOrEmpty(proxyPassword))
                                {
                                    mcConnectionInfo.MobileProxySetting_Password = Decrypt(proxyPassword, _secretKey);
                                }
                            }

                        }
                    }

                    // other mobile info
                    string mobileinfo = string.Empty;
                    if (_ciParams.ContainsKey("mobileinfo"))
                    {
                        mobileinfo = _ciParams["mobileinfo"];
                    }

                    Dictionary<string, List<String>> parallelRunnerEnvironments = new Dictionary<string, List<string>>();

                    // retrieve the parallel runner environment for each test
                    if (_ciParams.ContainsKey("parallelRunnerMode"))
                    {
                        if (Convert.ToBoolean(_ciParams["parallelRunnerMode"]))
                        {

                            foreach (var test in validTests)
                            {
                                string envKey = "Parallel" + test.Id + "Env";
                                List<string> testEnvironments = GetParamsWithPrefix(envKey);

                                // add the environments for all the valid tests
                                parallelRunnerEnvironments.Add(test.Id, testEnvironments);
                            }
                        }
                    }

                    // users can provide a custom report path which is the report base directory for all valid tests
                    string reportPath = null;
                    if (_ciParams.ContainsKey("fsReportPath"))
                    {
                        reportPath = _ciParams["fsReportPath"];
                    }
                    bool cancelRunOnFailure = false;
                    if (_ciParams.ContainsKey("cancelRunOnFailure"))
                    {
                        string crof = _ciParams["cancelRunOnFailure"].Trim().ToLower();
                        cancelRunOnFailure = (crof == "1" || crof == "true" || crof == "yes");
                    }

                    SummaryDataLogger summaryDataLogger = GetSummaryDataLogger();
                    List<ScriptRTSModel> scriptRTSSet = GetScriptRtsSet();
                    if (_ciParams.ContainsKey("fsUftRunMode"))
                    {
                        string uftRunMode = _ciParams["fsUftRunMode"].Trim();
                        if (string.IsNullOrEmpty(uftRunMode))
                        {
                            uftRunMode = "Fast";
                        }
                        runner = new FileSystemTestsRunner(validTests, timeout, uftRunMode, pollingInterval, perScenarioTimeOutMinutes, ignoreErrorStrings, jenkinsEnvVariables, mcConnectionInfo, mobileinfo, parallelRunnerEnvironments, displayController, analysisTemplate, summaryDataLogger, scriptRTSSet, reportPath, cancelRunOnFailure);
                    }
                    else
                    {
                        runner = new FileSystemTestsRunner(validTests, timeout, pollingInterval, perScenarioTimeOutMinutes, ignoreErrorStrings, jenkinsEnvVariables, mcConnectionInfo, mobileinfo, parallelRunnerEnvironments, displayController, analysisTemplate, summaryDataLogger, scriptRTSSet, reportPath, cancelRunOnFailure);
                    }

                    break;

                default:
                    runner = null;
                    break;
            }
            return runner;
        }

        private Dictionary<string, int> createDictionary(List<TestData> validTests)
        {
            var rerunList = new Dictionary<string, int>();
            foreach (var item in validTests)
            {
                if (!rerunList.ContainsKey(item.Tests))
                {
                    rerunList.Add(item.Tests, 1);
                }
                else
                {
                    rerunList[item.Tests]++;
                }
            }

            return rerunList;
        }

        private List<string> GetParamsWithPrefix(string prefix, bool skipEmptyEntries = false)
        {
            int idx = 1;
            List<string> parameters = new List<string>();
            while (_ciParams.ContainsKey(prefix + idx))
            {
                string set = _ciParams[prefix + idx];
                if (set.StartsWith("Root\\"))
                    set = set.Substring(5);
                set = set.TrimEnd(" \\".ToCharArray());
                if (!(skipEmptyEntries && string.IsNullOrWhiteSpace(set)))
                {
                    parameters.Add(set);
                }
                ++idx;
            }
            return parameters;
        }

        private Dictionary<string, string> GetKeyValuesWithPrefix(string prefix)
        {
            int idx = 1;

            Dictionary<string, string> dict = new Dictionary<string, string>();

            while (_ciParams.ContainsKey(prefix + idx))
            {
                string set = _ciParams[prefix + idx];
                if (set.StartsWith("Root\\"))
                    set = set.Substring(5);
                set = set.TrimEnd(" \\".ToCharArray());
                string key = prefix + idx;
                dict[key] = set;
                ++idx;
            }

            return dict;
        }

        /// <summary>
        /// used by the run fuction to run the tests
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="resultsFile"></param>
        /// 

        private object lockObject = new object();

        private void RunTests(IAssetRunner runner, string resultsFile, TestSuiteRunResults results)
        {
            try
            {
                if (_ciRun)
                {
                    _xmlBuilder = new JunitXmlBuilder();
                    _xmlBuilder.XmlName = resultsFile;

                    // decide the culture used to generate Junit Xml content
                    CultureInfo culture = CultureInfo.InvariantCulture;
                    if (_ciParams.ContainsKey("resultFormatLanguage"))
                    {
                        string langTag = _ciParams["resultFormatLanguage"];
                        langTag = string.IsNullOrWhiteSpace(langTag) ? string.Empty : langTag.Trim().ToLowerInvariant();
                        switch (langTag)
                        {
                            case "auto":
                            case "system":
                                culture = null;
                                break;

                            case "invariant":
                            case "default":
                            case "":
                                culture = CultureInfo.InvariantCulture;
                                break;

                            default:
                                try
                                {
                                    culture = CultureInfo.CreateSpecificCulture(langTag);
                                }
                                catch
                                {
                                    culture = CultureInfo.InvariantCulture;
                                }
                                break;
                        }
                    }
                    _xmlBuilder.Culture = culture;

                    // resultTestNameOnly for Junit Xml content
                    if (_ciParams.ContainsKey("resultTestNameOnly"))
                    {
                        string paramValue = _ciParams["resultTestNameOnly"].Trim().ToLower();
                        _xmlBuilder.TestNameOnly = paramValue == "true";
                    }
                }

                if (results == null)
                    Environment.Exit((int)Launcher.ExitCodeEnum.Failed);

                FileInfo fi = new FileInfo(resultsFile);
                string error = string.Empty;
                if (_xmlBuilder.CreateXmlFromRunResults(results, out error))
                {
                    Console.WriteLine(Properties.Resources.SummaryReportGenerated, fi.FullName);
                }
                else
                {
                    Console.WriteLine(Properties.Resources.SummaryReportFailedToGenerate, fi.FullName);
                }

                if (results.TestRuns.Count == 0)
                {
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                    Console.WriteLine("No tests were run, exit with code " + ((int)Launcher.ExitCode).ToString());
                    Environment.Exit((int)Launcher.ExitCode);
                }

                //if there is an error
                if (results.TestRuns.Any(tr => tr.TestState == TestState.Failed || tr.TestState == TestState.Error))
                {
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                }

                int numFailures = results.TestRuns.Count(t => t.TestState == TestState.Failed);
                int numSuccess = results.TestRuns.Count(t => t.TestState == TestState.Passed);
                int numErrors = results.TestRuns.Count(t => t.TestState == TestState.Error);
                int numWarnings = results.TestRuns.Count(t => t.TestState == TestState.Warning);
                int numOthers = results.TestRuns.Count - numFailures - numSuccess - numErrors - numWarnings;

                if ((numErrors <= 0) && (numFailures > 0))
                {
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                }

                if ((numErrors <= 0) && (numFailures > 0) && (numSuccess > 0)){
                    Launcher.ExitCode = Launcher.ExitCodeEnum.PartialFailed;
                }

                foreach (var testRun in results.TestRuns)
                {
                    if (testRun.FatalErrors > 0 && !testRun.TestPath.Equals(string.Empty))
                    {
                        Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                        break;
                    }
                }
               
                //this is the total run summary
                ConsoleWriter.ActiveTestRun = null;
                string runStatus = string.Empty;
          
                switch (Launcher.ExitCode)
                {
                    case ExitCodeEnum.Passed:
                        runStatus = "Job succeeded";
                        break;
                    case ExitCodeEnum.PartialFailed:
                        runStatus = "Job partial failed (Passed with failed tests)";
                        break;
                    case ExitCodeEnum.Aborted:
                        runStatus = "Job failed due to being Aborted";
                        break;
                    case ExitCodeEnum.Failed:
                        runStatus = "Job failed";
                        break;
                    case ExitCodeEnum.Unstable:
                        runStatus = "Job unstable";
                        break;
                    default:
                        runStatus = "Error: Job status is Undefined";
                        break;
                }

                ConsoleWriter.WriteLine(Resources.LauncherDoubleSeperator);
                ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayStatistics, runStatus, results.TestRuns.Count, numSuccess, numFailures, numErrors, numWarnings, numOthers));
                
                int testIndex = 1;
                if (!runner.RunWasCancelled)
                {
                    
                    results.TestRuns.ForEach(tr => { ConsoleWriter.WriteLine(((tr.HasWarnings) ? "Warning".PadLeft(7) : tr.TestState.ToString().PadRight(7)) + ": " + tr.TestPath + "[" + testIndex + "]"); testIndex++; });

                    ConsoleWriter.WriteLine(Resources.LauncherDoubleSeperator);

                    if (ConsoleWriter.ErrorSummaryLines != null && ConsoleWriter.ErrorSummaryLines.Count > 0)
                    {
                        ConsoleWriter.WriteLine("Job Errors summary:");
                        ConsoleWriter.ErrorSummaryLines.ForEach(line => ConsoleWriter.WriteLine(line));
                    }
                }
            }
            finally
            {
                try
                {
                    runner.Dispose();
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteLine(string.Format(Resources.LauncherRunnerDisposeError, ex.Message));
                };
            }

        }

        public void deleteOldReportFolders(string testFolderPath)
        {
            ConsoleWriter.WriteLine("Delete old report folders");
            String partialName = "Report";
            
            DirectoryInfo testDirectory = new DirectoryInfo(testFolderPath);

            DirectoryInfo[] directories = testDirectory.GetDirectories("*" + partialName + "*");

            foreach (DirectoryInfo foundDir in directories)
            {
                DeleteDirectory(foundDir.FullName);
            }
        }

        private SummaryDataLogger GetSummaryDataLogger()
        {
            SummaryDataLogger summaryDataLogger;

            if (_ciParams.ContainsKey("SummaryDataLog"))
            {
                string[] summaryDataLogFlags = _ciParams["SummaryDataLog"].Split(";".ToCharArray());

                if (summaryDataLogFlags.Length == 4)
                {
                    int summaryDataLoggerPollingInterval;
                    //If the polling interval is not a valid number, set it to default (10 seconds)
                    if (!Int32.TryParse(summaryDataLogFlags[3], out summaryDataLoggerPollingInterval))
                    {
                        summaryDataLoggerPollingInterval = 10;
                    }

                    summaryDataLogger = new SummaryDataLogger(
                        summaryDataLogFlags[0].Equals("1"),
                        summaryDataLogFlags[1].Equals("1"),
                        summaryDataLogFlags[2].Equals("1"),
                        summaryDataLoggerPollingInterval
                    );
                }
                else
                {
                    summaryDataLogger = new SummaryDataLogger();
                }
            }
            else
            {
                summaryDataLogger = new SummaryDataLogger();
            }

            return summaryDataLogger;
        }

        private List<ScriptRTSModel> GetScriptRtsSet()
        {
            List<ScriptRTSModel> scriptRtsSet = new List<ScriptRTSModel>();

            IEnumerable<string> scriptNames = GetParamsWithPrefix("ScriptRTS");
            foreach (string scriptName in scriptNames)
            {
                ScriptRTSModel scriptRts = new ScriptRTSModel(scriptName);

                IEnumerable<string> additionalAttributes = GetParamsWithPrefix("AdditionalAttribute");
                foreach (string additionalAttribute in additionalAttributes)
                {
                    //Each additional attribute contains: script name, aditional attribute name, value and description
                    string[] additionalAttributeArguments = additionalAttribute.Split(";".ToCharArray());
                    if (additionalAttributeArguments.Length == 4 && additionalAttributeArguments[0].Equals(scriptName))
                    {
                        scriptRts.AddAdditionalAttribute(new AdditionalAttributeModel(
                            additionalAttributeArguments[1],
                            additionalAttributeArguments[2],
                            additionalAttributeArguments[3])
                        );
                    }
                }

                scriptRtsSet.Add(scriptRts);
            }

            return scriptRtsSet;
        }

        /// <summary>
        /// Retrieve the list of valid test to run
        /// </summary>
        /// <param name="propertiesParameter"></param>
        /// <param name="errorNoTestsFound"></param>
        /// <param name="errorNoValidTests"></param>
        /// <returns>a list of tests</returns>
        private List<TestData> GetValidTests(string propertiesParameter, string errorNoTestsFound, string errorNoValidTests, String fsTestType)
        {
            if (!fsTestType.Equals("Rerun only failed tests") ||
               (fsTestType.Equals("Rerun only failed tests") && propertiesParameter.Equals("CleanupTest")))
            {
                List<TestData> tests = new List<TestData>();
                Dictionary<string, string> testsKeyValue = GetKeyValuesWithPrefix(propertiesParameter);
                if (propertiesParameter.Equals("CleanupTest") && testsKeyValue.Count == 0)
                {
                    return tests;
                }

                foreach (var item in testsKeyValue)
                {
                    tests.Add(new TestData(item.Value, item.Key));
                }

                if (tests.Count == 0)
                {
                    WriteToConsole(errorNoTestsFound);
                }

                List<TestData> validTests = Helper.ValidateFiles(tests);

                if (tests.Count <= 0 || validTests.Count != 0) return validTests;
                ConsoleWriter.WriteLine(errorNoValidTests);
            }

            return new List<TestData>();
        }

        /// <summary>
        /// Check if at least one test needs to run again
        /// </summary>
        /// <param name="numberOfReruns"></param>
        /// <returns>true if there is at least a test that needs to run again, false otherwise</returns>
        private bool CheckListOfRerunValues(List<int> numberOfReruns)
        {
            bool noRerunsSet = true;
            for (var j = 0; j < numberOfReruns.Count; j++)
            {
                if (numberOfReruns.ElementAt(j) <= 0) continue;
                noRerunsSet = false;
                break;
            }

            return noRerunsSet;
        }
    }

}
