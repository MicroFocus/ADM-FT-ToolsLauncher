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
        public string MobileUserName { get; set; }
        public string MobilePassword { get; set; }
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


        public McConnectionInfo()
        {
            MobileHostPort = "8080";
            MobileUserName = "";
            MobilePassword = "";
            MobileHostAddress = "";
            MobileTenantId = "";
            MobileUseSSL = 0;

            MobileUseProxy = 0;
            MobileProxyType = 0;
            MobileProxySetting_Address = "";
            MobileProxySetting_Port = 0;
            MobileProxySetting_Authentication = 0;
            MobileProxySetting_UserName = "";
            MobileProxySetting_Password = "";

        }

        public override string ToString()
        {
            string McConnectionStr =
                 string.Format("UFT Mobile HostAddress: {0}, Port: {1}, Username: {2}, TenantId: {3}, UseSSL: {4}, UseProxy: {5}, ProxyType: {6}, ProxyAddress: {7}, ProxyPort: {8}, ProxyAuth: {9}, ProxyUser: {10}",
                 MobileHostAddress, MobileHostPort, MobileUserName, MobileTenantId, MobileUseSSL, MobileUseProxy, MobileProxyType, MobileProxySetting_Address, MobileProxySetting_Port, MobileProxySetting_Authentication,
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
                                 "almApiKeySecretBasicAuth",
                                 "almDomain",
                                 "almProject"};

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
            Unstable = -2,
            Aborted = -3
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

            if (!_runType.Equals(TestStorageType.MBT))
            {
                RunTests(runner, resultsFilename, results);
            }


            if (_runType.Equals(TestStorageType.FileSystem))
            {
                string onCheckFailedTests = (_ciParams.ContainsKey("onCheckFailedTest") ? _ciParams["onCheckFailedTest"] : "");

                _rerunFailedTests = !string.IsNullOrEmpty(onCheckFailedTests) && Convert.ToBoolean(onCheckFailedTests.ToLower());

                 //the "On failure" option is selected and the run build contains failed tests
                if (_rerunFailedTests.Equals(true) && Launcher.ExitCode != ExitCodeEnum.Passed)
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
                }
            }
        }

        public static void DeleteDirectory(String dirPath)
        {
            DirectoryInfo directory = Directory.CreateDirectory(dirPath);
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
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
                    if (_ciParams.ContainsKey("SSOEnabled") && string.Compare(_ciParams["SSOEnabled"], "true", true) == 0)
                    {
                        foreach (string param1 in requiredParamsForQcRunInSSOMode)
                        {
                            if (!_ciParams.ContainsKey(param1))
                            {
                                ConsoleWriter.WriteLine(string.Format(Resources.LauncherParamRequired, param1));
                                return null;
                            }
                        }
                    }
                    else
                    {
                        foreach (string param1 in requiredParamsForQcRun)
                        {
                            if (!_ciParams.ContainsKey(param1))
                            {
                                ConsoleWriter.WriteLine(string.Format(Resources.LauncherParamRequired, param1));
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
                    ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayTimout, dblQcTimeout));

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
                    List<string> sets = GetParamsWithPrefix("TestSet");

                    if (sets.Count == 0)
                    {
                        ConsoleWriter.WriteLine(Resources.LauncherNoTests);
                        return null;
                    }

                    //check if filterTests flag is selected; if yes apply filters on the list
                    bool isFilterSelected;
                    string filter = _ciParams.ContainsKey("FilterTests") ? _ciParams["FilterTests"] : "";

                    isFilterSelected = !string.IsNullOrEmpty(filter) && Convert.ToBoolean(filter.ToLower());

                    string filterByName = _ciParams.ContainsKey("FilterByName") ? _ciParams["FilterByName"] : "";

                    string statuses = _ciParams.ContainsKey("FilterByStatus") ? _ciParams["FilterByStatus"] : "";

                    List<string> filterByStatuses = new List<string>();

                    if (statuses != "")
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

                    bool isSSOEnabled = _ciParams.ContainsKey("SSOEnabled") ? Convert.ToBoolean(_ciParams["SSOEnabled"]) : false;
                    string clientID = _ciParams.ContainsKey("almClientID") ? _ciParams["almClientID"] : "";
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

                    string almRunHost = _ciParams.ContainsKey("almRunHost") ? _ciParams["almRunHost"] : "";

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
                    string analysisTemplate = (_ciParams.ContainsKey("analysisTemplate") ? _ciParams["analysisTemplate"] : "");

                    List<TestData> validBuildTests = GetValidTests("Test", Resources.LauncherNoTestsFound, Resources.LauncherNoValidTests, "");

                    if (validBuildTests.Count == 0)
                    {
                        Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                    }

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
                        string fsTestType = (_ciParams.ContainsKey("testType") ? _ciParams["testType"] : "");

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
                    if (!analysisTemplate.Equals("") && !Helper.FileExists(analysisTemplate))
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
                                mcConnectionInfo.MobileHostAddress = strArray[1].Replace("/", "");
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
                    string mobileinfo = "";
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
                        // !! special code for Jenkins plugin only !!
                        // the "fsReportPath" might be a parameterized string like "${var}"
                        // which is not a valid file system path and the real path shall be
                        // retrieved from the Jenkins environment variables
                        if (Directory.Exists(_ciParams["fsReportPath"]))
                        {   //path is not parameterized
                            reportPath = _ciParams["fsReportPath"];
                        }
                        else
                        {   //path is parameterized
                            string fsReportPath = _ciParams["fsReportPath"];

                            //get parameter name
                            fsReportPath = fsReportPath.Trim(new Char[] { ' ', '$', '{', '}' });

                            //get parameter value
                            fsReportPath = fsReportPath.Trim(new Char[] { ' ', '\t' });
                            try
                            {
                                reportPath = jenkinsEnvVariables[fsReportPath];
                            }
                            catch (KeyNotFoundException)
                            {
                                Console.WriteLine("=====================================================================================");
                                Console.WriteLine(" The provided results folder path {0} is not found in Jenkins environment variables.", fsReportPath);
                                Console.WriteLine("=====================================================================================");
                                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                            }
                        }
                    }

                    SummaryDataLogger summaryDataLogger = GetSummaryDataLogger();
                    List<ScriptRTSModel> scriptRTSSet = GetScriptRtsSet();
                    if (_ciParams.ContainsKey("fsUftRunMode"))
                    {
                        string uftRunMode = "Fast";
                        uftRunMode = _ciParams["fsUftRunMode"];
                        runner = new FileSystemTestsRunner(validTests, timeout, uftRunMode, pollingInterval, perScenarioTimeOutMinutes, ignoreErrorStrings, jenkinsEnvVariables, mcConnectionInfo, mobileinfo, parallelRunnerEnvironments, displayController, analysisTemplate, summaryDataLogger, scriptRTSSet, reportPath);
                    }
                    else
                    {
                        runner = new FileSystemTestsRunner(validTests, timeout, pollingInterval, perScenarioTimeOutMinutes, ignoreErrorStrings, jenkinsEnvVariables, mcConnectionInfo, mobileinfo, parallelRunnerEnvironments, displayController, analysisTemplate, summaryDataLogger, scriptRTSSet, reportPath);
                    }

                    break;

                case TestStorageType.MBT:
                    // sample: parentFolder=c:\\my_tests\\mbt
                    string parentFolder = _ciParams["parentFolder"];

                    int counter = 1;
                    string testProp = "test" + counter;
                    List<MBTTest> tests = new List<MBTTest>();
                    while (_ciParams.ContainsKey(testProp))
                    {
                        MBTTest test = new MBTTest();
                        tests.Add(test);

                        // sample: test1=MBT123
                        test.Name = _ciParams[testProp];
                        // sample: script1=c:\\my_tests\\scripts\\mbt_action99.txt
                        // sample: script1=LoadAndRunAction \"c:\\my_tests\\GUITest3\\\",\"Action2\"
                        test.Script = _ciParams.GetOrDefault("script" + counter, "");
                        // sample: unitIds1=1075,1103,1197
                        test.UnitIds = _ciParams.GetOrDefault("unitIds" + counter, "");
                        // sample: underlyingTests1=c:\\my_tests\\GUITest3;c:\\my_tests\\GUITest8
                        test.UnderlyingTests = new List<string>(_ciParams.GetOrDefault("underlyingTests" + counter, "").Split(';'));
                        // sample: recoveryScenarios1=
                        string recScenarioValue = _ciParams.GetOrDefault("recoveryScenarios" + counter, "");
                        // sample: functionLibraries1=c:\\my_tests\\fl1;c:\\my_tests\\fl2
                        string funcLibraries = _ciParams.GetOrDefault("functionLibraries" + counter, "");
                        if (!string.IsNullOrEmpty(funcLibraries))
                        {
                            test.FunctionLibraries = new List<string>(funcLibraries.Split(';'));
                        }
                        else
                        {
                            test.FunctionLibraries = new List<string>();
                        }
                        // sample: package1=MBT_123
                        test.PackageName = _ciParams.GetOrDefault("package" + counter, "");
                        // sample: datableParams1=<base64-encoding> (raw as below)
                        // Time,Enabled,OutValue
                        // 15:16,1,
                        test.DatableParams = _ciParams.GetOrDefault("datableParams" + counter, "");
                        testProp = "test" + (++counter);
                    }

                    runner = new MBTRunner(parentFolder, tests);
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

        private List<string> GetParamsWithPrefix(string prefix)
        {
            int idx = 1;
            List<string> parameters = new List<string>();
            while (_ciParams.ContainsKey(prefix + idx))
            {
                string set = _ciParams[prefix + idx];
                if (set.StartsWith("Root\\"))
                    set = set.Substring(5);
                set = set.TrimEnd("\\".ToCharArray());
                parameters.Add(set.TrimEnd());
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
                set = set.TrimEnd("\\".ToCharArray());
                string key = prefix + idx;
                dict[key] = set.TrimEnd();
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

                if ((numErrors <= 0) && (numFailures > 0))
                {
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                }

                if ((numErrors <= 0) && (numFailures > 0) && (numSuccess > 0)){
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Unstable;
                }

                foreach (var testRun in results.TestRuns)
                {
                    if (testRun.FatalErrors > 0 && !testRun.TestPath.Equals(""))
                    {
                        Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                        break;
                    }
                }
               
                //this is the total run summary
                ConsoleWriter.ActiveTestRun = null;
                string runStatus = "";
          
                switch (Launcher.ExitCode)
                {
                    case ExitCodeEnum.Passed:
                        runStatus = "Job succeeded";
                        break;
                    case ExitCodeEnum.Unstable:
                        runStatus = "Job unstable (Passed with failed tests)";
                        break;
                    case ExitCodeEnum.Aborted:
                        runStatus = "Job failed due to being Aborted";
                        break;
                    case ExitCodeEnum.Failed:
                        runStatus = "Job failed";
                        break;
                    default:
                        runStatus = "Error: Job status is Undefined";
                        break;
                }

                ConsoleWriter.WriteLine(Resources.LauncherDoubleSeperator);
                ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayStatistics, runStatus, results.TestRuns.Count, numSuccess, numFailures, numErrors, numWarnings));
                
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

                    string onCheckFailedTests = (_ciParams.ContainsKey("onCheckFailedTest") ? _ciParams["onCheckFailedTest"] : "");

                    _rerunFailedTests = !string.IsNullOrEmpty(onCheckFailedTests) && Convert.ToBoolean(onCheckFailedTests.ToLower());

                    if (!_rerunFailedTests)
                    {
                        Environment.Exit((int)Launcher.ExitCode);
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

        // TODO: remove this unused method
        public void deleteOldReportFolders(string testFolderPath)
        {
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

                //no valid tests found
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
