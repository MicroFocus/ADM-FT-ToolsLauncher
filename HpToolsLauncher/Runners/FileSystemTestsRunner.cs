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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using HpToolsLauncher.Properties;
using HpToolsLauncher.TestRunners;
using HpToolsLauncher.RTS;
using HpToolsLauncher.Common;
using HpToolsLauncher.Interfaces;

namespace HpToolsLauncher
{
    public class FileSystemTestsRunner : RunnerBase, IDisposable
    {
        #region Members

        private readonly Dictionary<string, string> _jenkinsEnvVariables;
        private readonly List<TestInfo> _tests;
        private int _errors, _fails, _skipped;
        private readonly bool _displayController;
        private readonly string _analysisTemplate;
        private readonly SummaryDataLogger _summaryDataLogger;
        private readonly List<ScriptRTSModel> _scriptRTSSet;
        private readonly TimeSpan _timeout = TimeSpan.MaxValue;
        private readonly UftProps _uftProps;
        private readonly Stopwatch _stopwatch = null;
        private readonly string _abortFilename = $@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\stop{Launcher.UniqueTimeStamp}.txt";
        private readonly RunAsUser _uftRunAsUser;

        //LoadRunner Arguments
        private readonly int _pollingInterval;
        private readonly TimeSpan _perScenarioTimeOutMinutes;
        private readonly List<string> _ignoreErrorStrings;

        // parallel runner related information
        private readonly Dictionary<string, List<string>> _parallelRunnerEnvironments;

        //saves runners for cleaning up at the end.
        private readonly Dictionary<TestType, IFileSysTestRunner> _colRunnersForCleanup = [];

        private readonly bool _cancelRunOnFailure;
        private readonly bool _zipTestReports;
        private readonly bool _generateJUnitReport;
        private readonly bool _deletePreviousTestReports;

        private const string TEST_GROUP = "Test group";
        private const string UNKNOWN_TESTTYPE = "Unknown TestType";
        private const string NEW_LINE_AND_DASH_SEPARATOR = "\n-------------------------------------------------------------------------------------------------------";
        private const string REPORT = "Report";
        private const string JUNIT_REPORT_FILENAME = "junitResult.xml";
        private const string STAR = "*";
        private const char SLASH = '/';
        private const string RPT_ZIP_GENERATED_MSG = "Test report archive is generated at: {0}";
        private const string RPT_ZIP_ERR_MSG = "Unable to archive test report '{0}': {1}";

        #endregion
        /// <summary>
        /// Validates the environment and builds the internal test list from <paramref name="sources"/>.
        /// Exits the process if no testing tool is installed or no valid tests are found.
        /// </summary>
        /// <param name="sources">Test sources: directories, <c>.lrs</c>, <c>.mtb</c>, or <c>.mtbx</c> files.</param>
        /// <param name="timeout">Overall run timeout; <see cref="TimeSpan.MaxValue"/> for no limit.</param>
        /// <param name="uftProps">UFT run mode, Digital Lab connection, and license settings.</param>
        /// <param name="controllerPollingInterval">Polling interval in seconds for LoadRunner Controller.</param>
        /// <param name="perScenarioTimeOutMinutes">Per-scenario timeout for LoadRunner; <see cref="TimeSpan.MaxValue"/> for no limit.</param>
        /// <param name="ignoreErrorStrings">Error substrings to suppress in LoadRunner results.</param>
        /// <param name="jenkinsEnvVariables">CI environment variables substituted into MTBX parameters.</param>
        /// <param name="parallelRunnerEnvironments">Per-test-ID environment strings for Parallel Runner.</param>
        /// <param name="displayController">Show the LoadRunner Controller window during the run.</param>
        /// <param name="analysisTemplate">Path to a LoadRunner Analysis template; <see langword="null"/> for default.</param>
        /// <param name="summaryDataLogger">LoadRunner online monitor logging settings.</param>
        /// <param name="scriptRtsSet">Run-Time Settings overrides per LoadRunner script.</param>
        /// <param name="reportPath">Base directory for all test reports; <see langword="null"/> to use each test's own folder.</param>
        /// <param name="cancelRunOnFailure">Stop the run on the first test failure or error.</param>
        /// <param name="zipTestReports">Create ZIP archives for test reports after all tests complete.</param>
        /// <param name="xmlBuilder">Serializes results to a JUnit-compatible XML file.</param>
        /// <param name="uftRunAsUser">Windows credentials for the UFT process; <see langword="null"/> for the current user.</param>
        /// <param name="deletePreviousTestReports">Delete direct Report* subfolders of selected test folders before execution; defaults to false.</param>
        public FileSystemTestsRunner(
            List<TestData> sources,
            TimeSpan timeout,
            UftProps uftProps,
            int controllerPollingInterval,
            TimeSpan perScenarioTimeOutMinutes,
            List<string> ignoreErrorStrings,
            Dictionary<string, string> jenkinsEnvVariables,
            Dictionary<string, List<string>> parallelRunnerEnvironments,
            bool displayController,
            string analysisTemplate,
            SummaryDataLogger summaryDataLogger,
            List<ScriptRTSModel> scriptRtsSet,
            string reportPath,
            bool cancelRunOnFailure,
            IXmlBuilder xmlBuilder,
            RunAsUser uftRunAsUser,
            bool zipTestReports,
            bool generateJUnitReport,
            bool deletePreviousTestReports
        ) : base(xmlBuilder)
        {
            _jenkinsEnvVariables = jenkinsEnvVariables;
            //search if we have any testing tools installed
            if (!Helper.IsTestingToolsInstalled(TestStorageType.FileSystem))
            {
                ConsoleWriter.WriteErrLine(string.Format(Resources.FileSystemTestsRunner_No_HP_testing_tool_is_installed_on, Environment.MachineName));
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
            }

            _timeout = timeout;
            ConsoleWriter.WriteLine($@"FileSystemTestRunner timeout is {timeout:dd\:\:hh\:mm\:ss}");

            _stopwatch = Stopwatch.StartNew();

            _pollingInterval = controllerPollingInterval;
            _perScenarioTimeOutMinutes = perScenarioTimeOutMinutes;
            _ignoreErrorStrings = ignoreErrorStrings;

            _uftProps = uftProps;
            _displayController = displayController;
            _analysisTemplate = analysisTemplate;
            _summaryDataLogger = summaryDataLogger;
            _scriptRTSSet = scriptRtsSet;
            _tests = [];

            _parallelRunnerEnvironments = parallelRunnerEnvironments;
            _cancelRunOnFailure = cancelRunOnFailure;
            _zipTestReports = zipTestReports;
            _generateJUnitReport = generateJUnitReport;
            _deletePreviousTestReports = deletePreviousTestReports;

            _uftRunAsUser = uftRunAsUser;

            if (_uftProps.DigitalLab.ConnectionInfo != null)
                ConsoleWriter.WriteLine($"Functional Testing Lab connection info is - {_uftProps.DigitalLab.ConnectionInfo}");

            if (reportPath != null)
                ConsoleWriter.WriteLine($"Results base directory (for all tests) is: {reportPath}");

            //go over all sources, and create a list of all tests
            bool hasLRTests = false;
            foreach (TestData source in sources)
            {
                List<TestInfo> testGroup = [];
                try
                {
                    //--handle directories which contain test subdirectories (recursively)
                    if (Helper.IsDirectory(source.Tests))
                    {

                        var testsLocations = Helper.GetTestsLocations(source.Tests);
                        foreach (var loc in testsLocations)
                        {
                            TestInfo test = new(loc, loc, source.Tests, source.Id)
                            {
                                ReportPath = source.ReportPath
                            };
                            testGroup.Add(test);
                        }
                    }
                    //--handle mtb files (which contain links to tests)
                    else //file might be LoadRunner scenario or mtb file (which contain links to tests) other files are dropped
                    {
                        FileInfo fi = new(source.Tests);
                        if (fi.Extension == Helper._LRS)
                        {
                            testGroup.Add(new(source.Tests, source.Tests, source.Tests, source.Id)
                            {
                                ReportPath = source.ReportPath
                            });
                            hasLRTests = true;
                        }
                        else if (fi.Extension == Helper._MTB)
                        {
                            MtbManager manager = new();
                            var paths = manager.Parse(source.Tests);
                            foreach (var p in paths)
                            {
                                testGroup.Add(new(p, p, source.Tests, source.Id));
                            }
                        }
                        else if (fi.Extension == Helper._MTBX)
                        {
                            testGroup = MtbxManager.Parse(source.Tests, _jenkinsEnvVariables, source.Tests);

                            // set the test Id for each test from the group, this is important for parallel runner
                            testGroup?.ForEach(testInfo => testInfo.TestId = source.Id);
                        }
                    }
                }
                catch
                {
                    testGroup = null;
                }

                if (testGroup?.Count > 0)
                {
                    if (testGroup.Count == 1) //--handle single test dir, add it with no group
                    {
                        testGroup[0].TestGroup = TEST_GROUP;
                    }
                    _tests.AddRange(testGroup);
                }
            }

            if (_tests.IsNullOrEmpty())
            {
                ConsoleWriter.WriteLine(Resources.FsRunnerNoValidTests);
                ConsoleWriter.ErrorSummaryLines?.ForEach(ConsoleWriter.WriteErrLine);
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
            }

            if (hasLRTests)
            {
                ConsoleWriter.WriteLine($"Controller Polling Interval: {controllerPollingInterval} seconds");
                ConsoleWriter.WriteLine($@"PerScenarioTimeOut: {perScenarioTimeOutMinutes:dd\:\:hh\:mm\:ss}");
            }

            // if a custom path was provided,set the custom report path for all the valid tests(this will overwrite the default location)
            if (reportPath != null)
            {
                foreach (TestInfo t in _tests)
                {
                    if (t.ReportBaseDirectory.IsNullOrWhiteSpace())
                    {
                        t.ReportBaseDirectory = reportPath;
                    }
                }
            }

            ConsoleWriter.WriteLine(string.Format(Resources.FsRunnerTestsFound, _tests.Count));

            foreach (var test in _tests)
            {
                ConsoleWriter.WriteLine($"{test.TestName}");
                if (parallelRunnerEnvironments.ContainsKey(test.TestId))
                {
                    parallelRunnerEnvironments[test.TestId].ForEach(env => ConsoleWriter.WriteLine($"    {env}"));
                }
            }

            ConsoleWriter.WriteLine(Resources.GeneralDoubleSeperator);
        }

        /// <summary>
        /// Executes all tests sequentially, writes an incremental XML report after each test,
        /// and short-circuits remaining tests when <c>cancelRunOnFailure</c> is set.
        /// DCOM interactive-user permissions are verified once before the first UFT/QTP test.
        /// </summary>
        /// <returns>Aggregated results with per-test states and total pass/fail/error/skip counts.</returns>
        public override TestSuiteRunResults Run()
        {
            if (_xmlBuilder == null)
            {
                ConsoleWriter.WriteErrLine(Resources.InvalidXmlBuilder);
                Environment.Exit((int)Launcher.ExitCodeEnum.Failed);
            }
            //create a new Run Results object
            TestSuiteRunResults activeRunDesc = new();
            testsuite ts = _xmlBuilder.TestSuites.GetTestSuiteOrDefault(activeRunDesc.SuiteName, JunitXmlBuilder.ClassName, out bool isNewTestSuite);
            ts.tests += _tests.Count;

            // if we have at least one environment for parallel runner, then it must be enabled
            var isParallelRunnerEnabled = _parallelRunnerEnvironments.Count > 0;
            List<KeyValuePair<TestInfo, TestRunResults>> reportsToZip = [];
            double totalTime = 0;
            try
            {
                var start = DateTime.Now;
                bool skipRemainingTests = false;
                bool isDcomVerified = false;
                Exception dcomEx = null;
                if (_deletePreviousTestReports && !_blnRunCancelled && !RunCancelled())
                    DeletePreviousTestReports();

                for (int x = 0; x < _tests.Count; x++)
                {
                    var test = _tests[x];
                    if (skipRemainingTests || _blnRunCancelled || RunCancelled())
                    {
                        if (_skipped == 0)
                        {
                            Console.WriteLine(Resources.FileSystemTestsRunner_Run_Auto_Cancelled + NEW_LINE_AND_DASH_SEPARATOR);
                        }

                        activeRunDesc.TestRuns.Add(new()
                        {
                            TestState = TestState.NoRun,
                            ConsoleOut = Resources.FileSystemTestsRunner_Run_Auto_Cancelled,
                            ReportLocation = null,
                            TestGroup = test.TestGroup,
                            TestName = test.TestName,
                            TestPath = test.TestPath
                        });
                        _skipped++;
                        continue;
                    }

                    // run test
                    var testStart = DateTime.Now;

                    string errorReason = string.Empty;
                    TestRunResults runResult = null;
                    try
                    {
                        var type = Helper.GetTestType(test.TestPath);
                        if (isParallelRunnerEnabled && type == TestType.QTP)
                        {
                            type = TestType.ParallelRunner;
                        }

                        if (type == TestType.QTP)
                        {
                            if (!isDcomVerified)
                            {
                                try
                                {
                                    Helper.ChangeDCOMSettingToInteractiveUser();
                                }
                                catch (Exception ex)
                                {
                                    dcomEx = ex;
                                }
                                finally
                                {
                                    isDcomVerified = true;
                                }
                            }

                            if (dcomEx != null)
                                throw dcomEx;
                        }

                        runResult = RunHpToolsTest(test, ref errorReason);
                    }
                    catch (Exception ex)
                    {
                        runResult = new()
                        {
                            TestState = TestState.Error,
                            ErrorDesc = ex.Message,
                            TestName = test.TestName,
                            TestPath = test.TestPath
                        };
                    }

                    //get the original source for this test, for grouping tests under test classes
                    runResult.TestGroup = test.TestGroup;

                    activeRunDesc.TestRuns.Add(runResult);

                    //if fail was terminated before this step, continue
                    if (runResult.TestState != TestState.Failed)
                    {
                        if (runResult.TestState != TestState.Error)
                        {
                            Helper.GetTestStateFromReport(runResult);
                        }
                        else
                        {
                            if (runResult.ErrorDesc.IsNullOrEmpty())
                            {
                                runResult.ErrorDesc = RunCancelled() ? Resources.ExceptionUserCanceledOrTimeoutExpired : Resources.ExceptionExternalProcess;
                            }
                            runResult.ReportLocation = null;
                            runResult.TestState = TestState.Error;
                        }
                    }

                    if (_zipTestReports)
                        reportsToZip.Add(new(test, runResult));

                    if (runResult.TestState == TestState.Passed && runResult.HasWarnings)
                    {
                        runResult.TestState = TestState.Warning;
                        ConsoleWriter.WriteLine(Resources.FsRunnerTestDoneWarnings);
                    }
                    else
                    {
                        ConsoleWriter.WriteLine(string.Format(Resources.FsRunnerTestDone, runResult.TestState));
                        if (runResult.TestState == TestState.Error)
                        {
                            ConsoleWriter.WriteErrLine(runResult.ErrorDesc);
                            ts.errors++;
                            _errors++;
                        }
                        else if (runResult.TestState == TestState.Failed)
                        {
                            ts.failures++;
                            _fails++;
                        }
                    }

                    var testTotalTime = (DateTime.Now - testStart).TotalSeconds;
                    ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Test completed in {((int)Math.Ceiling(testTotalTime))} seconds: {runResult.TestPath}");
                    if (!runResult.ReportLocation.IsNullOrWhiteSpace())
                    {
                        ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Test report is generated at: {runResult.ReportLocation}{NEW_LINE_AND_DASH_SEPARATOR}");
                    }

                    // Create or update the xml report. This function is called after each test execution in order to have a report available in case of job interruption
                    _xmlBuilder.CreateOrUpdatePartialXmlReport(ts, runResult, isNewTestSuite && x == 0);

                    // skip remaining tests if the current test is failure or error and cancelRunOnFailure is true
                    if (_cancelRunOnFailure && runResult.TestState.In(TestState.Failed, TestState.Error))
                    {
                        skipRemainingTests = true;
                    }
                }

                totalTime = (DateTime.Now - start).TotalSeconds;
            }
            finally
            {
                if (_zipTestReports)
                    ZipTestReports(reportsToZip);

                if (_generateJUnitReport)
                    GenerateJUnitReport();

                activeRunDesc.NumTests = _tests.Count;
                activeRunDesc.NumErrors = _errors;
                activeRunDesc.TotalRunTime = TimeSpan.FromSeconds(totalTime);
                activeRunDesc.NumFailures = _fails;
                activeRunDesc.NumSkipped = _skipped;

                foreach (IFileSysTestRunner cleanupRunner in _colRunnersForCleanup.Values)
                {
                    cleanupRunner.CleanUp();
                }
            }

            return activeRunDesc;
        }

        private void GenerateJUnitReport()
        {
            string inputFile = Path.GetFullPath(_xmlBuilder.XmlName);
            string outputFile = Path.Combine(Path.GetDirectoryName(inputFile), JUNIT_REPORT_FILENAME);
            try
            {
                if (!File.Exists(inputFile))
                {
                    ConsoleWriter.WriteErrLine($"Failed to create JUnit report: results file '{inputFile}' not found.");
                    return;
                }

                if (JUnitResultConverter.ConvertAndSave(inputFile, outputFile))
                    ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} JUnit report is generated at: {outputFile}");
                else
                    ConsoleWriter.WriteErrLine($"Failed to create JUnit report: {outputFile}");
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteErrLine($"Failed to create JUnit report: {ex.Message}");
            }
        }

        private void DeletePreviousTestReports()
        {
            HashSet<string> testPaths = new(StringComparer.OrdinalIgnoreCase);
            foreach (var test in _tests)
            {
                try
                {
                    if (!test.TestPath.IsNullOrWhiteSpace())
                        testPaths.Add(Path.GetFullPath(test.TestPath).TrimTrailingSeparators());
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteLine($"Warning: Unable to inspect old reports for '{test.TestPath}': {ex.Message}");
                    return;
                }
            }

            HashSet<string> reportDirsToDelete = new(StringComparer.OrdinalIgnoreCase);
            foreach (string testPath in testPaths)
            {
                try
                {
                    if (!Directory.Exists(testPath))
                        continue;

                    if ((File.GetAttributes(testPath) & FileAttributes.ReparsePoint) != 0)
                    {
                        ConsoleWriter.WriteLine($"Warning: Skipping old report cleanup in linked test folder '{testPath}'.");
                        continue;
                    }

                    foreach (string reportDir in Directory.GetDirectories(testPath, $"{REPORT}{STAR}", SearchOption.TopDirectoryOnly))
                    {
                        if (!IsNumberedReportDir(reportDir))
                            continue;

                        try
                        {
                            bool containsTest = false;
                            foreach (string selectedTestPath in testPaths)
                            {
                                if (IsSameOrSubdirectory(selectedTestPath, reportDir))
                                {
                                    containsTest = true;
                                    break;
                                }
                            }

                            if (containsTest || (File.GetAttributes(reportDir) & FileAttributes.ReparsePoint) != 0)
                            {
                                ConsoleWriter.WriteLine($"Warning: Skipping old report folder '{reportDir}' because it contains a selected test or is a link.");
                                continue;
                            }

                            reportDirsToDelete.Add(reportDir);
                        }
                        catch (Exception ex)
                        {
                            ConsoleWriter.WriteLine($"Warning: Unable to inspect old report folder '{reportDir}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteLine($"Warning: Unable to inspect old reports in '{testPath}': {ex.Message}");
                }
            }

            object logLock = new();
            Parallel.ForEach(reportDirsToDelete, new ParallelOptions { MaxDegreeOfParallelism = 10 }, reportDir =>
            {
                string message;
                try
                {
                    if ((File.GetAttributes(reportDir) & FileAttributes.ReparsePoint) != 0)
                    {
                        message = $"Warning: Skipping linked old report folder '{reportDir}'.";
                    }
                    else
                    {
                        Directory.Delete(reportDir, true);
                        message = $"Deleted old report folder: {reportDir}";
                    }
                }
                catch (Exception ex)
                {
                    message = $"Warning: Unable to delete old report folder '{reportDir}': {ex.Message}";
                }

                lock (logLock)
                {
                    ConsoleWriter.WriteLine(message);
                }
            });
        }

        /// <summary>
        /// Returns true when the folder name is "Report" followed by one or more ASCII digits (e.g. Report1, Report10).
        /// </summary>
        private static bool IsNumberedReportDir(string reportDir)
        {
            string name = Path.GetFileName(reportDir?.TrimTrailingSeparators());
            if (name == null || name.Length <= REPORT.Length || !name.StartsWith(REPORT, StringComparison.OrdinalIgnoreCase))
                return false;

            for (int i = REPORT.Length; i < name.Length; i++)
            {
                if (name[i] < '0' || name[i] > '9')
                    return false;
            }

            return true;
        }

        private void ZipTestReports(List<KeyValuePair<TestInfo, TestRunResults>> reports)
        {
            string outDir = Path.GetDirectoryName(Path.GetFullPath(_xmlBuilder.XmlName));
            List<(string reportDir, string testName, string testPath)> candidates = GetZipCandidates(reports, outDir);
            if (candidates.Count == 0)
                return;

            List<string> parentDirs = GetTestParentDirs(candidates);
            string commonParentDirPath = GetClosestParentDir(parentDirs);
            string commonParentDirName = GetCommonParentDirName(commonParentDirPath, candidates.Count);
            List<Task> zipTasks = CreateZipTasks(candidates, outDir, commonParentDirPath, commonParentDirName);
            WaitForZipTasks(zipTasks);
        }

        private static List<(string reportDir, string testName, string testPath)> GetZipCandidates(
            List<KeyValuePair<TestInfo, TestRunResults>> reports,
            string outDir)
        {
            List<(string reportDir, string testName, string testPath)> candidates = [];

            foreach (var report in reports)
            {
                try
                {
                    if (report.Value?.ReportLocation.IsNullOrWhiteSpace() != false
                        || report.Key?.TestPath.IsNullOrWhiteSpace() != false)
                        continue;

                    string reportDir = Path.GetFullPath(report.Value.ReportLocation).TrimTrailingSeparators();
                    if (!Directory.Exists(reportDir))
                        continue;

                    if (IsSameOrSubdirectory(outDir, reportDir))
                    {
                        ConsoleWriter.WriteErrLine(string.Format(RPT_ZIP_ERR_MSG, reportDir,
                            $"Archive output directory '{outDir}' must be outside all report directories."));
                        return [];
                    }

                    string testPath = Path.GetFullPath(report.Key.TestPath).TrimTrailingSeparators();
                    string nameSource = report.Key.TestName.IsNullOrWhiteSpace() ? testPath : report.Key.TestName;
                    string testName = Path.GetFileName(nameSource.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                        .SanitizeFileName();
                    if (!testName.IsNullOrWhiteSpace())
                        candidates.Add((reportDir, testName, testPath));
                }
                catch (Exception ex)
                {
                    ConsoleWriter.WriteErrLine(string.Format(RPT_ZIP_ERR_MSG, report.Value?.ReportLocation, ex.Message));
                }
            }

            return candidates;
        }

        private static List<string> GetTestParentDirs(List<(string reportDir, string testName, string testPath)> candidates)
        {
            List<string> parentDirs = [];
            foreach (var candidate in candidates)
            {
                string parentDir = Path.GetDirectoryName(candidate.testPath);
                if (!parentDir.IsNullOrWhiteSpace())
                    parentDirs.Add(parentDir);
            }
            return parentDirs;
        }

        private static string GetCommonParentDirName(string commonParentDirectoryPath, int candidateCount)
        {
            return candidateCount > 1 && !commonParentDirectoryPath.IsNullOrWhiteSpace()
                ? Path.GetFileName(commonParentDirectoryPath.TrimTrailingSeparators()).SanitizeFileName()
                : string.Empty;
        }

        private static List<Task> CreateZipTasks(
            List<(string reportDir, string testName, string testPath)> candidates,
            string outDir,
            string commonParentDirPath,
            string commonParentDirName)
        {
            HashSet<string> zipNames = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> nextZipSuffixes = new(StringComparer.OrdinalIgnoreCase);
            List<Task> zipTasks = new(candidates.Count);

            foreach (var candidate in candidates)
            {
                string archiveRoot = GetArchiveRoot(candidate.testPath, candidates.Count, commonParentDirPath, commonParentDirName);
                string uniqueZipName = GetUniqueZipName(candidate.testName, zipNames, nextZipSuffixes);
                string zipPath = Path.Combine(outDir, $"{uniqueZipName}.zip");
                string reportDir = candidate.reportDir;
                zipTasks.Add(Task.Run(() => ZipTestReport(reportDir, zipPath, archiveRoot)));
            }

            return zipTasks;
        }

        private static string GetArchiveRoot(string testPath, int candidateCount, string commonParentDirPath, string commonParentDirName)
        {
            if (candidateCount == 1)
                return REPORT;

            string relativeTestPath = GetPathRelativeToCommonParent(testPath, commonParentDirPath);
            return commonParentDirName.IsNullOrWhiteSpace()
                ? $"{relativeTestPath}/{REPORT}"
                : $"{commonParentDirName}/{relativeTestPath}/{REPORT}";
        }

        private static string GetUniqueZipName(string testName, HashSet<string> zipNames, Dictionary<string, int> nextZipSuffixes)
        {
            int suffix = nextZipSuffixes.TryGetValue(testName, out int nextSuffix) ? nextSuffix : 2;
            string uniqueZipName = testName;
            while (!zipNames.Add(uniqueZipName))
                uniqueZipName = $"{testName}_{suffix++}";
            nextZipSuffixes[testName] = suffix;
            return uniqueZipName;
        }

        private static void WaitForZipTasks(List<Task> zipTasks)
        {
            try
            {
                Task.WaitAll([.. zipTasks]);
            }
            catch (AggregateException ex)
            {
                ConsoleWriter.WriteErrLine(ex.Flatten().Message);
            }
        }

        private static string GetClosestParentDir(List<string> directories)
        {
            if (directories.Count == 0)
                return string.Empty;

            string commonParent = directories[0];
            for (int i = 1; i < directories.Count; i++)
            {
                while (!IsSameOrSubdirectory(directories[i], commonParent))
                {
                    string parent = Path.GetDirectoryName(commonParent.TrimTrailingSeparators());
                    if (parent.IsNullOrWhiteSpace() || parent.EqualsIgnoreCase(commonParent))
                        return string.Empty;

                    commonParent = parent;
                }
            }

            return commonParent;
        }

        private static string GetPathRelativeToCommonParent(string path, string commonParent)
        {
            string relativePath = null;
            if (!commonParent.IsNullOrWhiteSpace() && IsSameOrSubdirectory(path, commonParent))
            {
                relativePath = path.Substring(commonParent.TrimTrailingSeparators().Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            if (relativePath.IsNullOrWhiteSpace())
                relativePath = Path.GetFileName(path);

            return relativePath.Replace(Path.DirectorySeparatorChar, SLASH).Replace(Path.AltDirectorySeparatorChar, SLASH);
        }

        private static bool IsSameOrSubdirectory(string path, string dir)
        {
            string normalizedDir = dir.TrimTrailingSeparators();
            if (path.EqualsIgnoreCase(normalizedDir))
                return true;

            string prefix = normalizedDir.EndsWith($"{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                ? normalizedDir
                : normalizedDir + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static void ZipTestReport(string reportDir, string zipPath, string archiveRoot)
        {
            if (reportDir.IsNullOrWhiteSpace() || !Directory.Exists(reportDir))
                return;

            string tempPath = null;
            try
            {
                reportDir = Path.GetFullPath(reportDir).TrimTrailingSeparators();
                zipPath = Path.GetFullPath(zipPath);
                string outDir = Path.GetDirectoryName(zipPath);
                if (IsSameOrSubdirectory(outDir, reportDir))
                    throw new IOException($"Archive output directory '{outDir}' must be outside the report directory.");

                tempPath = Path.Combine(outDir, $"{Guid.NewGuid():N}.zip.tmp");
                CreateTempZip(reportDir, tempPath, archiveRoot);
                PublishZip(tempPath, zipPath);
                LogZipCreated(zipPath);
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteErrLine(string.Format(RPT_ZIP_ERR_MSG, reportDir, ex.Message));
            }
            finally
            {
                TryDeleteTemporaryZip(tempPath);
            }
        }

        /// <summary>
        /// Creates a temporary archive containing the report files.
        /// </summary>
        private static void CreateTempZip(string reportDir, string tempPath, string archiveRoot)
        {
            using FileStream zipStream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using ZipArchive zip = new(zipStream, ZipArchiveMode.Create);
            zip.CreateEntry($"{archiveRoot}/");
            foreach (string filePath in Directory.EnumerateFiles(reportDir, STAR, SearchOption.AllDirectories))
                AddFileToZip(zip, reportDir, filePath, archiveRoot);
        }

        private static void AddFileToZip(ZipArchive zip, string reportDir, string filePath, string archiveRoot)
        {
            string relativePath = filePath.Substring(reportDir.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, SLASH);
            ZipArchiveEntry entry = zip.CreateEntry($"{archiveRoot}/{relativePath}", CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            using FileStream sourceStream = File.OpenRead(filePath);
            sourceStream.CopyTo(entryStream);
        }

        private static void PublishZip(string tempPath, string zipPath)
        {
            if (File.Exists(zipPath))
                File.Replace(tempPath, zipPath, null);
            else
                File.Move(tempPath, zipPath);
        }

        private static void LogZipCreated(string zipPath)
        {
            ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} {string.Format(RPT_ZIP_GENERATED_MSG, zipPath)}");
        }

        private static void TryDeleteTemporaryZip(string tempPath)
        {
            try
            {
                if (!tempPath.IsNullOrWhiteSpace() && File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteErrLine($"Unable to delete temporary archive '{tempPath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves the test type from <paramref name="testInfo"/>'s path, selects the matching runner
        /// (UFT/QTP, LoadRunner, Service Test, or Parallel Runner), and executes the test.
        /// QTP tests are promoted to Parallel Runner when parallel environments are configured;
        /// ST tests are always run directly, even in parallel mode.
        /// Returns an error result for an unknown test type or if an abort file is detected.
        /// </summary>
        /// <param name="testInfo">Metadata for the test to run.</param>
        /// <param name="errorReason">Populated with a diagnostic message when the test cannot be executed.</param>
        /// <returns>The result of the test execution, including elapsed runtime and test info.</returns>
        private TestRunResults RunHpToolsTest(TestInfo testInfo, ref string errorReason)
        {
            var testPath = testInfo.TestPath;

            var type = Helper.GetTestType(testPath);

            // if we have at least one environment for parallel runner,
            // then it must be enabled
            var isParallelRunnerEnabled = _parallelRunnerEnvironments.Count > 0;

            if (isParallelRunnerEnabled && type == TestType.QTP)
            {
                type = TestType.ParallelRunner;
            }
            // if the current test is an api test ignore the parallel runner flag
            // and just continue as usual
            else if (isParallelRunnerEnabled && type == TestType.ST)
            {
                ConsoleWriter.WriteLine("ParallelRunner does not support API tests, treating as normal test.");
            }

            IFileSysTestRunner runner = null;
            switch (type)
            {
                case TestType.ST:
                    runner = new ApiTestRunner(_uftRunAsUser);
                    break;

                case TestType.QTP:
                    runner = new GuiTestRunner(this, _uftProps, _uftRunAsUser);
                    break;

                case TestType.LoadRunner:
                    AppDomain.CurrentDomain.AssemblyResolve += Helper.HPToolsAssemblyResolver;
                    runner = new PerformanceTestRunner(
                        this,
                        _pollingInterval,
                        _perScenarioTimeOutMinutes,
                        _ignoreErrorStrings,
                        _displayController,
                        _analysisTemplate,
                        _summaryDataLogger,
                        _scriptRTSSet);
                    break;

                case TestType.ParallelRunner:
                    runner = new ParallelTestRunner(
                        this,
                        _uftProps.DigitalLab.ConnectionInfo,
                        _parallelRunnerEnvironments,
                        _uftRunAsUser);
                    break;
            }

            if (runner != null)
            {
                if (!_colRunnersForCleanup.ContainsKey(type))
                    _colRunnersForCleanup.Add(type, runner);

                Stopwatch s = Stopwatch.StartNew();

                var results = runner.RunTest(testInfo, ref errorReason, RunCancelled);
                results.TestInfo = testInfo;

                results.Runtime = s.Elapsed;

                if (type == TestType.LoadRunner)
                    AppDomain.CurrentDomain.AssemblyResolve -= Helper.HPToolsAssemblyResolver;

                return results;
            }

            //check for abortion
            if (File.Exists(_abortFilename))
            {
                ConsoleWriter.WriteLine(Resources.GeneralStopAborted);

                Environment.Exit((int)Launcher.ExitCodeEnum.Aborted);
            }

            return new()
            {
                TestInfo = testInfo,
                ErrorDesc = UNKNOWN_TESTTYPE,
                TestState = TestState.Error
            };
        }


        /// <summary>
        /// checks if run was cancelled/aborted
        /// </summary>
        /// <returns></returns>
        public bool RunCancelled()
        {
            //if timeout has passed
            if (_stopwatch.Elapsed > _timeout && !_blnRunCancelled)
            {
                ConsoleWriter.WriteLine(Resources.SmallDoubleSeparator);
                ConsoleWriter.WriteLine(Resources.GeneralTimedOut);
                ConsoleWriter.WriteLine(Resources.SmallDoubleSeparator);

                Launcher.ExitCode = Launcher.ExitCodeEnum.Aborted;
                _blnRunCancelled = true;
            }

            return _blnRunCancelled;
        }
    }
}
