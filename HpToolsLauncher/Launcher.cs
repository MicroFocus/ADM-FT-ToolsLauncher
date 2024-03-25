/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2023 Open Text
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HpToolsLauncher.Properties;
using HpToolsLauncher.TestRunners;
using HpToolsLauncher.RTS;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using HpToolsLauncher.Utils;
using HpToolsLauncher.Common;

namespace HpToolsLauncher
{
    public enum CiName
    {
        Hudson,
        Jenkins,
        TFS,
        CCNET
    }

    public class Launcher
    {
        private const string MINUS_ONE = "-1";
        private const string ZERO = "0";
        private const string ONE = "1";
        private const string TRUE = "true";
        private const string FALSE = "false";
        private const string YES = "yes";
        private const string NO = "no";
        private const string N = "N";
        private const string TEST = "Test";
        private const string LEAVE_UFT_OPEN_IF_VISIBLE = "leaveUftOpenIfVisible";
        private const string FS_UFT_RUN_MODE = "fsUftRunMode";
        private const string CLOUD_BROWSER = "cloudBrowser";
        private const string MOBILE_INFO = "mobileinfo";
        private const string CANCEL_RUN_ON_FAILURE = "cancelRunOnFailure";
        private const string FS_REPORT_PATH = "fsReportPath";
        private const string PARALLEL_RUNNER_MODE = "parallelRunnerMode";
        private const string DISPLAY_CONTROLLER = "displayController";
        private const string ANALYSIS_TEMPLATE = "analysisTemplate";
        private const string IGNORE_ERROR_STRINGS = "ignoreErrorStrings";
        private const string PER_SCENARIO_TIMEOUT = "PerScenarioTimeOut";
        private const string RERUN_ALL_TESTS = "Rerun the entire set of tests";
        private const string RERUN_SPECIFIC_TESTS = "Rerun specific tests in the build";
        private const string RERUN_FAILED_TESTS = "Rerun only failed tests";
        private const string CLEANUP_TEST = "CleanupTest";
        private const string JENKINS_ENV = "JenkinsEnv";
        private const string CONTROLLER_POLLING_INTERVAL = "controllerPollingInterval";
        private const string FS_TIMEOUT = "fsTimeout";
        private const string RERUNS = "Reruns";
        private const string FAILED_TEST = "FailedTest";
        private const string TEST_TYPE = "testType";
        private const string UNSTABLE_AS_FAIL = "unstableAsFailure";
        private const string ON_CHECK_FAILED_TEST = "onCheckFailedTest";
        private const string DDMMYYYYHHmmssfff = "ddMMyyyyHHmmssfff";
        private const string RUN_TYPE = "runType";
        private const string UNIQUE_TIMESTAMP = "uniqueTimeStamp";
        private const string RESULTS_FILENAME = "resultsFilename";
        private const string ADDITIONAL_ATTRIBUTE = "AdditionalAttribute";
        private const string SCRIPT_RTS = "ScriptRTS";
        private const string SUMMARY_DATA_LOG = "SummaryDataLog";
        private const string WARNING = "Warning";
        private const string RESULT_TEST_NAME_ONLY = "resultTestNameOnly";
        private const string RESULT_UNIFIED_TEST_CLASSNAME = "resultUnifiedTestClassname";
        private const string ALM_CLIENT_ID = "almClientID";
        private const string ALM_API_KEY_SECRET_BASIC_AUTH = "almApiKeySecretBasicAuth";
        private const string ALM_API_KEY_SECRET = "almApiKeySecret";
        private const string ALM_RUN_HOST = "almRunHost";
        private const string ALM_PASSWORD_BASIC_AUTH = "almPasswordBasicAuth";
        private const string ALM_PASSWORD = "almPassword";
        private const string ALM_SERVER_URL = "almServerUrl";
        private const string ALM_USERNAME = "almUsername";
        private const string ALM_DOMAIN = "almDomain";
        private const string ALM_PROJECT = "almProject";
        private const string FILTER_TESTS = "FilterTests";
        private const string FILTER_BY_NAME = "FilterByName";
        private const string FILTER_BY_STATUS = "FilterByStatus";

        private const string AUTO = "auto";
        private const string SYSTEM = "system";
        private const string INVARIANT = "invariant";
        private const string DEFAULT = "default";
        private const string EMPTY = "";
        private const string RESULT_FORMAT_LANGUAGE = "resultFormatLanguage";
        private const string PARALLEL = "Parallel";
        private const string ENV = "Env";
        private const string ROOT = "Root\\";
        private const string TEST_SET = "TestSet";
        private const string ALM_RUN_MODE = "almRunMode";
        private const string ALM_TIMEOUT = "almTimeout";
        private const string SSO_ENABLED = "SSOEnabled";

        private const string JOB_SUCCEEDED = "Job succeeded";
        private const string JOB_PARTIAL_FAILED = "Job partial failed (Passed with failed tests)";
        private const string JOB_ABORTED = "Job failed due to being Aborted";
        private const string JOB_FAILED = "Job failed";
        private const string JOB_UNSTABLE = "Job unstable";
        private const string JOB_UNDEFINED = "Error: Job status is Undefined";
        private const string THERE_ARE_FAILED_TESTS = "There are failed tests.";

        private static readonly string[] _one_true_yes = [ONE, TRUE, YES];

        private IXmlBuilder _xmlBuilder;
        private bool _ciRun = false;
        private readonly string _paramFileName = null;
        private JavaProperties _ciParams = [];
        private TestStorageType _runType;
        private readonly string _failOnUftTestFailed;
        private static ExitCodeEnum _exitCode = ExitCodeEnum.Passed;
        private static bool _rerunFailedTests = false;
        XmlSerializer _serializer = new(typeof(testsuites));

        testsuites _testSuites = new();

        public static string DateFormat { get; set; } = "dd/MM/yyyy HH:mm:ss";

        /// <summary>
        /// if running an alm job theses strings are mandatory:
        /// </summary>
        private static readonly string[] requiredParamsForQcRun = [ ALM_SERVER_URL,
                                 ALM_USERNAME,
                                 //"almPassword",
                                 ALM_DOMAIN,
                                 ALM_PROJECT/*,
                                 "almRunMode",
                                 "almTimeout",
                                 "almRunHost"*/];
        private static readonly string[] requiredParamsForQcRunInSSOMode = [ ALM_SERVER_URL,
                                 ALM_CLIENT_ID,
                                 ALM_DOMAIN,
                                 ALM_PROJECT];
        private static readonly string[] requiredAlmApiKeyParams = [ ALM_API_KEY_SECRET_BASIC_AUTH, ALM_API_KEY_SECRET ]; // if SSO then one ApiKey param is required

        private readonly char[] _comma_semicolon = [',',';'];
        private readonly char[] _semicolon = [';'];
        private readonly char[] _space_backslash = [' ', '\\'];

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
            get { return _exitCode; }
            set { _exitCode = value; }
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

            _failOnUftTestFailed = failOnTestFailed.IsNullOrEmpty() ? N : failOnTestFailed;
        }

        public bool IsParamFileEncodingNotSupported { get; private set; }

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
                if (_ciParams.ContainsKey(RUN_TYPE))
                {
                    Enum.TryParse(_ciParams[RUN_TYPE], true, out _runType);
                }
            }
            if (_runType == TestStorageType.Unknown)
            {
                WriteToConsole(Resources.LauncherNoRuntype);
                return;
            }

            if (!_ciParams.ContainsKey(RESULTS_FILENAME))
            {
                WriteToConsole(Resources.LauncherNoResFilenameFound);
                return;
            }
            string resultsFilename = _ciParams[RESULTS_FILENAME];

            UniqueTimeStamp = string.Empty;
            if (_ciParams.ContainsKey(UNIQUE_TIMESTAMP))
            {
                UniqueTimeStamp = _ciParams[UNIQUE_TIMESTAMP];
            }
            if (UniqueTimeStamp.IsNullOrWhiteSpace())
            {
                string fileNameOnly = Path.GetFileNameWithoutExtension(resultsFilename);
                Regex regex = new(@"Results(\d{6,17})");
                Match m = regex.Match(fileNameOnly);
                if (m.Success)
                {
                    UniqueTimeStamp = m.Groups[1].Value;
                }
            }
            if (UniqueTimeStamp.IsNullOrWhiteSpace())
            {
                UniqueTimeStamp = DateTime.Now.ToString(DDMMYYYYHHmmssfff);
            }

            List<TestData> failedTests = [];
            InitXmlBuilder(resultsFilename);
            //run the entire set of test once
            //create the runner according to type
            IAssetRunner runner = CreateRunner(_runType, _ciParams, true, failedTests, _xmlBuilder);

            //runner instantiation failed (no tests to run or other problem)
            if (runner == null)
            {
                //ConsoleWriter.WriteLine("empty runner;");
                Environment.Exit((int)ExitCodeEnum.Failed);
            }

            TestSuiteRunResults results = runner.Run();

            RunSummary(runner, resultsFilename, results);

            var lastExitCode = ExitCode;
            Console.WriteLine($"The reported status is: {lastExitCode}");

            if (_runType.Equals(TestStorageType.FileSystem))
            {
                string onCheckFailedTests = _ciParams.GetOrDefault(ON_CHECK_FAILED_TEST);
                _rerunFailedTests = !onCheckFailedTests.IsNullOrEmpty() && Convert.ToBoolean(onCheckFailedTests.ToLower());

                //the "On failure" option is selected and the run build contains failed tests
                if (_rerunFailedTests && ExitCode.In(ExitCodeEnum.Failed, ExitCodeEnum.PartialFailed))
                {
                    ConsoleWriter.WriteLine(THERE_ARE_FAILED_TESTS);

                    //rerun the selected tests (either the entire set, just the selected tests or only the failed tests)
                    List<TestRunResults> runResults = results.TestRuns;
                    int index = 0;
                    foreach (var item in runResults)
                    {
                        if (item.TestState.In(TestState.Failed, TestState.Error))
                        {
                            index++;
                            failedTests.Add(new(item.TestPath, $"{FAILED_TEST}{index}")
                            {
                                ReportPath = item.TestInfo?.ReportPath
                            });
                        }
                    }

                    //create the runner according to type
                    runner = CreateRunner(_runType, _ciParams, false, failedTests, _xmlBuilder);

                    //runner instantiation failed (no tests to run or other problem)
                    if (runner == null)
                    {
                        Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
                    }

                    TestSuiteRunResults rerunResults = runner.Run();

                    results.AppendResults(rerunResults);
                    RunSummary(runner, resultsFilename, results);

                    // check if all the rerun tests are passed
                    if (rerunResults.NumErrors == 0 && rerunResults.NumFailures == 0)
                    {
                        // it is the Unstable case
                        lastExitCode = ExitCodeEnum.Unstable;
                    }
                }
            }

            Console.WriteLine($"The final status is: {lastExitCode}");
            Launcher.ExitCode = lastExitCode;
            Environment.ExitCode = (int)lastExitCode;

            // if the launcher reported Unstable, the process exit code might be 0 or non-zero
            // depends on whether treats the Unstable as a Failure
            if (lastExitCode == ExitCodeEnum.Unstable)
            {
                string unstableAsFail = _ciParams.GetOrDefault(UNSTABLE_AS_FAIL, ZERO).Trim().ToLower();
                if (unstableAsFail.In(FALSE, ZERO, NO))
                {
                    // explicitly specify that unstable shall not be treated as a failure
                    Environment.ExitCode = (int)ExitCodeEnum.Passed;
                }
            }
        }

        public static void DeleteDirectory(string dirPath)
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
        private IAssetRunner CreateRunner(TestStorageType runType, JavaProperties ciParams, bool initialTestRun, List<TestData> failedTests, IXmlBuilder xmlBuilder)
        {
            IAssetRunner runner = null;

            switch (runType)
            {
                case TestStorageType.AlmLabManagement:
                case TestStorageType.Alm:
                    //check that all required parameters exist
                    bool isSSOEnabled = _ciParams.ContainsKey(SSO_ENABLED) && Convert.ToBoolean(_ciParams[SSO_ENABLED]);
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
                    if (_ciParams.ContainsKey(ALM_TIMEOUT))
                    {
                        if (!double.TryParse(_ciParams[ALM_TIMEOUT], out dblQcTimeout))
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
                    if (_ciParams.ContainsKey(ALM_RUN_MODE))
                    {
                        if (!Enum.TryParse(_ciParams[ALM_RUN_MODE], true, out enmQcRunMode))
                        {
                            ConsoleWriter.WriteLine(Resources.LauncherIncorrectRunmode);
                            enmQcRunMode = QcRunMode.RUN_LOCAL;
                        }
                    }
                    ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayRunmode, enmQcRunMode.ToString()));

                    //go over test sets in the parameters, and collect them
                    List<string> sets = GetParamsWithPrefix(TEST_SET, true);

                    if (sets.Count == 0)
                    {
                        ConsoleWriter.WriteErrLine(Resources.LauncherNoTests); // is important to print the error to STDERR stream, so that ADM-TFS-Extension can handle it properly
                        return null;
                    }

                    //check if filterTests flag is selected; if yes apply filters on the list
                    bool isFilterSelected;
                    string filter = _ciParams.ContainsKey(FILTER_TESTS) ? _ciParams[FILTER_TESTS] : string.Empty;
                    isFilterSelected = !filter.IsNullOrEmpty() && Convert.ToBoolean(filter.ToLower());
                    string filterByName = _ciParams.ContainsKey(FILTER_BY_NAME) ? _ciParams[FILTER_BY_NAME] : string.Empty;
                    string statuses = _ciParams.ContainsKey(FILTER_BY_STATUS) ? _ciParams[FILTER_BY_STATUS] : string.Empty;
                    List<string> filterByStatuses = [];

                    if (statuses != string.Empty)
                    {
                        if (statuses.Contains(","))
                        {
                            filterByStatuses = [.. statuses.Split(',')];
                        }
                        else
                        {
                            filterByStatuses.Add(statuses);
                        }
                    }

                    string clientID = _ciParams.GetOrDefault(ALM_CLIENT_ID);
                    string apiKey = string.Empty;
                    if (_ciParams.ContainsKey(ALM_API_KEY_SECRET_BASIC_AUTH))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(_ciParams[ALM_API_KEY_SECRET_BASIC_AUTH]);
                        apiKey = Encoding.Default.GetString(data);
                    }
                    else if (_ciParams.ContainsKey(ALM_API_KEY_SECRET))
                    {
                        apiKey = Encrypter.Decrypt(_ciParams[ALM_API_KEY_SECRET]);
                    }

                    string almRunHost = _ciParams.GetOrDefault(ALM_RUN_HOST);

                    string almPassword = string.Empty;
                    if (_ciParams.ContainsKey(ALM_PASSWORD_BASIC_AUTH))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(_ciParams[ALM_PASSWORD_BASIC_AUTH]);
                        almPassword = Encoding.Default.GetString(data);
                    }
                    else if (_ciParams.ContainsKey(ALM_PASSWORD))
                    {
                        almPassword = Encrypter.Decrypt(_ciParams[ALM_PASSWORD]);
                    }

                    //create an Alm runner
                    runner = new AlmTestSetsRunner(_ciParams[ALM_SERVER_URL],
                                     _ciParams.GetOrDefault(ALM_USERNAME),
                                     almPassword,
                                     _ciParams[ALM_DOMAIN],
                                     _ciParams[ALM_PROJECT],
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
                    bool displayController = _ciParams.GetOrDefault(DISPLAY_CONTROLLER).Trim().ToLower().In(_one_true_yes);
                    string analysisTemplate = _ciParams.GetOrDefault(ANALYSIS_TEMPLATE).Trim();

                    List<TestData> validBuildTests = GetValidTests(TEST, Resources.LauncherNoTestsFound, Resources.LauncherNoValidTests, string.Empty);

                    // report path specified for each test
                    foreach (TestData t in validBuildTests)
                    {
                        // the test Id is something like "Test{i}"
                        string idx = t.Id.Remove(0, TEST.Length);
                        string reportPathKey = $"{FS_REPORT_PATH}{idx}";
                        if (_ciParams.ContainsKey(reportPathKey))
                        {
                            t.ReportPath = _ciParams[reportPathKey];
                            if (!t.ReportPath.IsNullOrEmpty())
                            {
                                t.ReportPath = t.ReportPath.Trim();
                            }
                        }
                    }

                    //add build tests and cleanup tests in correct order
                    List<TestData> validTests = [];

                    if (!_rerunFailedTests)
                    {
                        //ConsoleWriter.WriteLine("Run build tests");

                        //run only the build tests
                        foreach (var item in validBuildTests)
                        {
                            validTests.Add(item);
                        }
                    }
                    else
                    { //add also cleanup tests
                        string fsTestType = _ciParams.GetOrDefault(TEST_TYPE);

                        List<TestData> validFailedTests = GetValidTests(FAILED_TEST, Resources.LauncherNoFailedTestsFound, Resources.LauncherNoValidFailedTests, fsTestType);
                        List<TestData> validCleanupTests = [];
                        if (GetValidTests(CLEANUP_TEST, Resources.LauncherNoCleanupTestsFound, Resources.LauncherNoValidCleanupTests, fsTestType).Count > 0)
                        {
                            validCleanupTests = GetValidTests(CLEANUP_TEST, Resources.LauncherNoCleanupTestsFound, Resources.LauncherNoValidCleanupTests, fsTestType);
                        }
                        List<string> reruns = GetParamsWithPrefix(RERUNS);
                        List<int> numberOfReruns = [];
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
                            if (fsTestType.EqualsIgnoreCase(RERUN_ALL_TESTS))
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
                            else if (fsTestType.EqualsIgnoreCase(RERUN_SPECIFIC_TESTS))
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
                            else if (fsTestType.EqualsIgnoreCase(RERUN_FAILED_TESTS))
                            {
                                ConsoleWriter.WriteLine("Only the failed tests will run again.");
                                Dictionary<string, TestData> failedDict = [];
                                foreach (TestData t in failedTests)
                                {
                                    failedDict.Add(t.Tests, t);
                                }

                                int rerunNum = 0;
                                while (true)
                                {
                                    List<TestData> tmpList = [];
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

                    IEnumerable<string> jenkinsEnvVariablesWithCommas = GetParamsWithPrefix(JENKINS_ENV);
                    Dictionary<string, string> jenkinsEnvVariables = [];
                    foreach (string var in jenkinsEnvVariablesWithCommas)
                    {
                        string[] nameVal = var.Split(_comma_semicolon);
                        jenkinsEnvVariables.Add(nameVal[0], nameVal[1]);
                    }
                    //parse the timeout into a TimeSpan
                    TimeSpan timeout = TimeSpan.MaxValue;
                    if (_ciParams.ContainsKey(FS_TIMEOUT))
                    {
                        string strTimeoutInSeconds = _ciParams[FS_TIMEOUT];
                        if (strTimeoutInSeconds.Trim() != MINUS_ONE)
                        {
                            if (double.TryParse(strTimeoutInSeconds, out double timeoutInSeconds))
                            {
                                if (timeoutInSeconds >= 0)
                                {
                                    timeout = TimeSpan.FromSeconds(Math.Round(timeoutInSeconds));
                                }
                            }
                        }
                    }
                    ConsoleWriter.WriteLine($@"Launcher timeout is {timeout:dd\:\:hh\:mm\:ss}");

                    //LR specific values:
                    //default values are set by JAVA code, in com.hpe.application.automation.tools.model.RunFromFileSystemModel.java

                    int pollingInterval = 30;
                    if (_ciParams.ContainsKey(CONTROLLER_POLLING_INTERVAL))
                    {
                        if (double.TryParse(_ciParams[CONTROLLER_POLLING_INTERVAL], out double value))
                        {
                            if (value >= 0)
                            {
                                pollingInterval = (int)Math.Round(value);
                            }
                        }
                    }
                    ConsoleWriter.WriteLine($"Controller Polling Interval: {pollingInterval} seconds");

                    TimeSpan perScenarioTimeOutMinutes = TimeSpan.MaxValue;
                    if (_ciParams.ContainsKey(PER_SCENARIO_TIMEOUT))
                    {
                        string strTimeoutInMinutes = _ciParams[PER_SCENARIO_TIMEOUT].Trim();
                        if (strTimeoutInMinutes != MINUS_ONE)
                        {
                            if (double.TryParse(strTimeoutInMinutes, out double timoutInMinutes))
                            {
                                var totalSeconds = Math.Round(TimeSpan.FromMinutes(timoutInMinutes).TotalSeconds);
                                if (totalSeconds >= 0)
                                {
                                    perScenarioTimeOutMinutes = TimeSpan.FromSeconds(totalSeconds);
                                }
                            }
                        }
                    }
                    ConsoleWriter.WriteLine($@"{PER_SCENARIO_TIMEOUT}: {perScenarioTimeOutMinutes:dd\:\:hh\:mm\:ss}");

                    char[] delimiter = ['\n'];
                    List<string> ignoreErrorStrings = [];
                    if (_ciParams.ContainsKey(IGNORE_ERROR_STRINGS))
                    {
                        if (_ciParams.ContainsKey(IGNORE_ERROR_STRINGS))
                        {
                            ignoreErrorStrings.AddRange(Array.ConvertAll(_ciParams[IGNORE_ERROR_STRINGS].Split(delimiter, StringSplitOptions.RemoveEmptyEntries), ignoreError => ignoreError.Trim()));
                        }
                    }

                    //If a file path was provided and it doesn't exist stop the analysis launcher
                    if (!analysisTemplate.IsNullOrWhiteSpace() && !Helper.FileExists(analysisTemplate))
                    {
                        return null;
                    }

                    //--MC connection info
                    McConnectionInfo mcConnectionInfo = null;
                    try
                    {
                        mcConnectionInfo = new(_ciParams);
                    }
                    catch (NoMcConnectionException)
                    {
                        // no action, the Test will use the default UFT One settings
                    }
                    catch (Exception ex)
                    {
                        ConsoleWriter.WriteErrLine(ex.Message);
                        Environment.Exit((int)ExitCodeEnum.Failed);
                    }

                    // other mobile info
                    string mobileinfo = string.Empty;
                    if (_ciParams.ContainsKey(MOBILE_INFO))
                    {
                        mobileinfo = _ciParams[MOBILE_INFO];
                    }

                    CloudBrowser cloudBrowser = null;
                    string strCloudBrowser = _ciParams.GetOrDefault(CLOUD_BROWSER).Trim();
                    if (!strCloudBrowser.IsNullOrEmpty())
                    {
                        CloudBrowser.TryParse(strCloudBrowser, out cloudBrowser);
                    }
                    DigitalLab digitalLab = new(mcConnectionInfo, mobileinfo, cloudBrowser);

                    Dictionary<string, List<string>> parallelRunnerEnvironments = [];

                    // retrieve the parallel runner environment for each test
                    if (_ciParams.ContainsKey(PARALLEL_RUNNER_MODE))
                    {
                        if (Convert.ToBoolean(_ciParams[PARALLEL_RUNNER_MODE]))
                        {
                            foreach (var test in validTests)
                            {
                                string envKey = $"{PARALLEL}{test.Id}{ENV}";
                                List<string> testEnvironments = GetParamsWithPrefix(envKey);

                                // add the environments for all the valid tests
                                parallelRunnerEnvironments.Add(test.Id, testEnvironments);
                            }
                        }
                    }

                    // users can provide a custom report path which is the report base directory for all valid tests
                    string reportPath = null;
                    if (_ciParams.ContainsKey(FS_REPORT_PATH))
                    {
                        reportPath = _ciParams[FS_REPORT_PATH];
                    }
                    bool cancelRunOnFailure = false;
                    if (_ciParams.ContainsKey(CANCEL_RUN_ON_FAILURE))
                    {
                        string crof = _ciParams[CANCEL_RUN_ON_FAILURE].Trim().ToLower();
                        cancelRunOnFailure = crof.In(_one_true_yes);
                    }

                    SummaryDataLogger summaryDataLogger = GetSummaryDataLogger();
                    List<ScriptRTSModel> scriptRTSSet = GetScriptRtsSet();

                    UftProps uftProps;
                    string leaveUftOpen = _ciParams.GetOrDefault(LEAVE_UFT_OPEN_IF_VISIBLE).Trim().ToLower();
                    bool leaveUftOpenIfVisible = leaveUftOpen.In(_one_true_yes);
                    if (_ciParams.ContainsKey(FS_UFT_RUN_MODE))
                    {
                        string strUftRunMode = _ciParams[FS_UFT_RUN_MODE].Trim();
                        Enum.TryParse(strUftRunMode, out UftRunMode uftRunMode);
                        uftProps = new(leaveUftOpenIfVisible, digitalLab, uftRunMode);
                    }
                    else
                    {
                        uftProps = new(leaveUftOpenIfVisible, digitalLab);
                    }
                    runner = new FileSystemTestsRunner(validTests, timeout, uftProps, pollingInterval, perScenarioTimeOutMinutes, ignoreErrorStrings, jenkinsEnvVariables, parallelRunnerEnvironments, displayController, analysisTemplate, summaryDataLogger, scriptRTSSet, reportPath, cancelRunOnFailure, _xmlBuilder);

                    break;

                default:
                    runner = null;
                    break;
            }
            return runner;
        }

        private Dictionary<string, int> CreateDictionary(List<TestData> validTests)
        {
            Dictionary<string, int> rerunList = [];
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
            List<string> parameters = [];
            while (_ciParams.ContainsKey(prefix + idx))
            {
                string set = _ciParams[prefix + idx];
                if (set.StartsWith(ROOT))
                    set = set.Substring(5);
                set = set.TrimEnd(_space_backslash);
                if (!(skipEmptyEntries && set.IsNullOrWhiteSpace()))
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

            Dictionary<string, string> dict = [];

            while (_ciParams.ContainsKey(prefix + idx))
            {
                string set = _ciParams[prefix + idx];
                if (set.StartsWith(ROOT))
                    set = set.Substring(5);
                set = set.TrimEnd(_space_backslash);
                string key = prefix + idx;
                dict[key] = set;
                ++idx;
            }

            return dict;
        }

        /// <summary>
        /// used by the run fuction to run the tests
        /// </summary>
        /// <param name="resultsFile"></param>
        /// 
        private void InitXmlBuilder(string resultsFile)
        {
            if (_xmlBuilder != null)
            {
                return;
            }
            _xmlBuilder = new JunitXmlBuilder { XmlName = resultsFile };

            // decide the culture used to generate Junit Xml content
            CultureInfo culture = CultureInfo.InvariantCulture;
            if (_ciParams.ContainsKey(RESULT_FORMAT_LANGUAGE))
            {
                string langTag = _ciParams[RESULT_FORMAT_LANGUAGE];
                langTag = langTag.IsNullOrWhiteSpace() ? string.Empty : langTag.Trim().ToLowerInvariant();
                switch (langTag)
                {
                    case AUTO:
                    case SYSTEM:
                        culture = null;
                        break;

                    case INVARIANT:
                    case DEFAULT:
                    case EMPTY:
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
            if (_ciParams.ContainsKey(RESULT_TEST_NAME_ONLY))
            {
                string paramValue = _ciParams[RESULT_TEST_NAME_ONLY].Trim().ToLower();
                _xmlBuilder.TestNameOnly = paramValue == TRUE;
            }
            if (_runType == TestStorageType.FileSystem && _ciParams.ContainsKey(RESULT_UNIFIED_TEST_CLASSNAME))
            {
                string paramValue = _ciParams[RESULT_UNIFIED_TEST_CLASSNAME].Trim().ToLower();
                _xmlBuilder.UnifiedTestClassname = paramValue == TRUE;
            }
        }

        private void RunSummary(IAssetRunner runner, string resultsFile, TestSuiteRunResults results)
        {
            try
            {
                if (results == null)
                    Environment.Exit((int)ExitCodeEnum.Failed);

                if (_ciRun && _runType != TestStorageType.FileSystem) // for FileSystem the report is already generated inside FileSystemTestsRunner.Run()
                {
                    InitXmlBuilder(resultsFile);
                    FileInfo fi = new(resultsFile);
                    string error = string.Empty;
                    if (_xmlBuilder.CreateXmlFromRunResults(results, out error))
                    {
                        Console.WriteLine(Resources.SummaryReportGenerated, fi.FullName);
                    }
                    else
                    {
                        Console.WriteLine(Resources.SummaryReportFailedToGenerate, fi.FullName);
                    }
                }

                if (results.TestRuns.Count == 0)
                {
                    ExitCode = ExitCodeEnum.Failed;
                    Console.WriteLine($"No tests were run, exit with code {(int)ExitCode}");
                    Environment.Exit((int)ExitCode);
                }

                //if there is an error
                if (results.TestRuns.Any(tr => tr.TestState.In(TestState.Failed, TestState.Error)))
                {
                    ExitCode = ExitCodeEnum.Failed;
                }

                int numFailures = results.TestRuns.Count(t => t.TestState == TestState.Failed);
                int numSuccess = results.TestRuns.Count(t => t.TestState == TestState.Passed);
                int numErrors = results.TestRuns.Count(t => t.TestState == TestState.Error);
                int numWarnings = results.TestRuns.Count(t => t.TestState == TestState.Warning);
                int numOthers = results.TestRuns.Count - numFailures - numSuccess - numErrors - numWarnings;

                if ((numErrors <= 0) && (numFailures > 0))
                {
                    ExitCode = ExitCodeEnum.Failed;
                }

                if ((numErrors <= 0) && (numFailures > 0) && (numSuccess > 0))
                {
                    ExitCode = ExitCodeEnum.PartialFailed;
                }

                foreach (var testRun in results.TestRuns)
                {
                    if (testRun.FatalErrors > 0 && !testRun.TestPath.Equals(string.Empty))
                    {
                        ExitCode = ExitCodeEnum.Failed;
                        break;
                    }
                }

                //this is the total run summary
                ConsoleWriter.ActiveTestRun = null;
                string runStatus = string.Empty;

                runStatus = ExitCode switch
                {
                    ExitCodeEnum.Passed => JOB_SUCCEEDED,
                    ExitCodeEnum.PartialFailed => JOB_PARTIAL_FAILED,
                    ExitCodeEnum.Aborted => JOB_ABORTED,
                    ExitCodeEnum.Failed => JOB_FAILED,
                    ExitCodeEnum.Unstable => JOB_UNSTABLE,
                    _ => JOB_UNDEFINED
                };
                ConsoleWriter.WriteLine(Resources.LauncherDoubleSeperator);
                ConsoleWriter.WriteLine(string.Format(Resources.LauncherDisplayStatistics, runStatus, results.TestRuns.Count, numSuccess, numFailures, numErrors, numWarnings, numOthers));

                int testIndex = 1;
                if (!runner.RunWasCancelled)
                {
                    results.TestRuns.ForEach(tr => { string state = tr.HasWarnings ? $"{WARNING}" : $"{tr.TestState,-7}"; ConsoleWriter.WriteLine($"{state}: {tr.TestPath}[{testIndex}]"); testIndex++; });

                    ConsoleWriter.WriteLine(Resources.LauncherDoubleSeperator);

                    if (ConsoleWriter.ErrorSummaryLines?.Count > 0)
                    {
                        ConsoleWriter.WriteLine("Job Errors summary:");
                        ConsoleWriter.ErrorSummaryLines.ForEach(ConsoleWriter.WriteErrLine);
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

        private SummaryDataLogger GetSummaryDataLogger()
        {
            SummaryDataLogger summaryDataLogger;

            if (_ciParams.ContainsKey(SUMMARY_DATA_LOG))
            {
                string[] summaryDataLogFlags = _ciParams[SUMMARY_DATA_LOG].Split(_semicolon);

                if (summaryDataLogFlags.Length == 4)
                {
                    //If the polling interval is not a valid number, set it to default (10 seconds)
                    if (!int.TryParse(summaryDataLogFlags[3], out int summaryDataLoggerPollingInterval))
                    {
                        summaryDataLoggerPollingInterval = 10;
                    }

                    summaryDataLogger = new(
                        summaryDataLogFlags[0] == ONE,
                        summaryDataLogFlags[1] == ONE,
                        summaryDataLogFlags[2] == ONE,
                        summaryDataLoggerPollingInterval
                    );
                }
                else
                {
                    summaryDataLogger = new();
                }
            }
            else
            {
                summaryDataLogger = new();
            }

            return summaryDataLogger;
        }

        private List<ScriptRTSModel> GetScriptRtsSet()
        {
            List<ScriptRTSModel> scriptRtsSet = [];

            IEnumerable<string> scriptNames = GetParamsWithPrefix(SCRIPT_RTS);
            foreach (string scriptName in scriptNames)
            {
                ScriptRTSModel scriptRts = new(scriptName);

                IEnumerable<string> additionalAttrs = GetParamsWithPrefix(ADDITIONAL_ATTRIBUTE);
                foreach (string additionalAttr in additionalAttrs)
                {
                    //Each additional attribute contains: script name, aditional attribute name, value and description
                    string[] additionalAttributeArguments = additionalAttr.Split(_semicolon);
                    if (additionalAttributeArguments.Length == 4 && additionalAttributeArguments[0] == scriptName)
                    {
                        scriptRts.AddAdditionalAttribute(new(
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
        private List<TestData> GetValidTests(string propertiesParameter, string errorNoTestsFound, string errorNoValidTests, string fsTestType)
        {
            if (fsTestType != RERUN_FAILED_TESTS || propertiesParameter == CLEANUP_TEST)
            {
                List<TestData> tests = [];
                Dictionary<string, string> testsKeyValue = GetKeyValuesWithPrefix(propertiesParameter);
                if (propertiesParameter.Equals(CLEANUP_TEST) && testsKeyValue.Count == 0)
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
                ConsoleWriter.WriteErrLine(errorNoValidTests);
            }

            return [];
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
