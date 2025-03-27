/*
 * Certain versions of software accessible here may contain branding from Hewlett-Packard Company (now HP Inc.) and Hewlett Packard Enterprise Company.
 * This software was acquired by Micro Focus on September 1, 2017, and is now offered by OpenText.
 * Any reference to the HP and Hewlett Packard Enterprise/HPE marks is historical in nature, and the HP and Hewlett Packard Enterprise/HPE marks are the property of their respective owners.
 * __________________________________________________________________
 * MIT License
 *
 * Copyright 2012-2024 Open Text
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

using HpToolsLauncher.Common;
using HpToolsLauncher.Interfaces;
using HpToolsLauncher.ParallelRunner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Environment = System.Environment;
using Resources = HpToolsLauncher.Properties.Resources;
namespace HpToolsLauncher.TestRunners
{
    /// <summary>
    /// The ParallelTestRunner class
    /// Contains all methods for running a file system test using parallel runner.
    /// </summary>
    public class ParallelTestRunner : IFileSysTestRunner
    {
        private const string QT_APP = "Quicktest.Application";
        private const string PER_PROCESS_MUTEX_UFT = "per_process_mutex_UFT";
        private const string NOT_LAUNCHED = "Not launched";
        private const string READY = "Ready";
        private const string WAITING = "Waiting";
        private const string BUSY = "Busy";
        private const string RUNNING = "Running";
        private const string RECORDING = "Recording";
        private const string PAUSED = "Paused";
        private const string EXPLORER = "explorer";

        // each test has a list of environments that it will run on
        private readonly Dictionary<string, List<string>> _environments;
        private readonly IAssetRunner _runner;
        private readonly McConnectionInfo _mcConnectionInfo;
        private string _parallelRunnerFullPathFile;
        private RunCancelledDelegate _runCancelled;
        private const int POLLING_TIME_MS = 500;
        private readonly bool _canRun = false;
        private const string PARALLEL_RUNNER_ARGS = "-o static -c \"{0}\"";

        public ParallelTestRunner(IAssetRunner runner, McConnectionInfo mcConnectionInfo, Dictionary<string, List<string>> environments)
        {
            _runner = runner;
            _mcConnectionInfo = mcConnectionInfo;
            _environments = environments;
            _canRun = TrySetupParallelRunner();
        }

        /// <summary>
        /// Tries to find the parallel runner executable.
        /// </summary>
        /// <returns>
        /// True if the executable has been found,False otherwise
        /// </returns>
        private bool TrySetupParallelRunner()
        {
            _parallelRunnerFullPathFile = Helper.GetParallelRunnerFullPathFile();

            ConsoleWriter.WriteLine("Attempting to start parallel runner from: " + _parallelRunnerFullPathFile);

            return _parallelRunnerFullPathFile != null && File.Exists(_parallelRunnerFullPathFile);
        }

        /// <summary>
        /// Set the test run results based on the parallel runner exit code.
        /// </summary>
        /// <param name="runResults"></param>
        /// <param name="exitCode"></param>
        /// <param name="failureReason"></param>
        /// <param name="errorReason"></param>
        private void RunResultsFromParallelRunnerExitCode(TestRunResults runResults, int exitCode, string failureReason, ref string errorReason)
        {
            // set the status of the build based on the exit code
            switch (exitCode)
            {
                case (int)ParallelRunResult.Pass:
                    runResults.TestState = TestState.Passed;
                    break;
                case (int)ParallelRunResult.Warning:
                    runResults.TestState = TestState.Warning;
                    break;
                case (int)ParallelRunResult.Fail:
                    runResults.ErrorDesc = "ParallelRunner test has FAILED!";
                    runResults.TestState = TestState.Failed;
                    break;
                case (int)ParallelRunResult.Canceled:
                    runResults.ErrorDesc = "ParallelRunner was stopped since job has timed out!";
                    ConsoleWriter.WriteErrLine(runResults.ErrorDesc);
                    runResults.TestState = TestState.Error;
                    break;
                case (int)ParallelRunResult.Error:
                    errorReason = failureReason;
                    runResults.ErrorDesc = errorReason;
                    ConsoleWriter.WriteErrLine(runResults.ErrorDesc);
                    runResults.TestState = TestState.Error;
                    break;
                case (int)ParallelRunResult.NotStarted:
                    runResults.ErrorDesc = "Failed to start ParallelRunner!";
                    ConsoleWriter.WriteErrLine(runResults.ErrorDesc);
                    runResults.TestState = TestState.Error;
                    break;
                default:
                    ConsoleWriter.WriteErrLine(errorReason);
                    runResults.ErrorDesc = errorReason;
                    runResults.TestState = TestState.Error;
                    break;
            }
        }

        /// <summary>
        /// Runs the provided test on all the environments.
        /// </summary>
        /// <param name="testInfo"> The test information. </param>
        /// <param name="errorReason"> failure reason </param>
        /// <param name="runCancelled"> delegate to RunCancelled </param>
        /// <returns>
        /// The run results for the current test.
        /// </returns>
        public TestRunResults RunTest(TestInfo testInfo, ref string errorReason, RunCancelledDelegate runCancelled)
        {
            ConsoleWriter.WriteLine(DateTime.Now.ToString(Launcher.DateFormat) + " Running in parallel: " + testInfo.TestPath);

            if (string.IsNullOrWhiteSpace(testInfo.ReportPath))
            {
                // maybe the report base directory is set, if so,
                // the report path for parallel runner shall be populated here
                if (!string.IsNullOrWhiteSpace(testInfo.ReportBaseDirectory))
                {
                    // "<report-base-dir>\<test-name>_ParallelReport"
                    testInfo.ReportPath = Path.Combine(testInfo.ReportBaseDirectory,
                        testInfo.TestName.Substring(testInfo.TestName.LastIndexOf('\\') + 1) + "_ParallelReport");
                    ConsoleWriter.WriteLine(DateTime.Now.ToString(Launcher.DateFormat) + " Report path is generated under base directory: " + testInfo.ReportPath);
                }
                else
                {
                    // neither ReportPath nor ReportBaseDirectory is given, use default report path:
                    // "<TestPath>\ParallelReport"
                    testInfo.ReportPath = testInfo.TestPath + @"\ParallelReport";
                    ConsoleWriter.WriteLine(DateTime.Now.ToString(Launcher.DateFormat) + " Report path is automatically generated: " + testInfo.ReportPath);
                }
            }
            else
            {
                ConsoleWriter.WriteLine(DateTime.Now.ToString(Launcher.DateFormat) + " Report path is set explicitly: " + testInfo.ReportPath);
            }

            // this is to make sure that we do not overwrite the report
            // when we run the same test multiple times on the same build
            string resFolder = Helper.GetNextResFolder(testInfo.ReportPath, "Res");

            TestRunResults runResults = new()
            {
                ReportLocation = testInfo.ReportPath,
                ErrorDesc = errorReason,
                TestState = TestState.Unknown,
                TestPath = testInfo.TestPath,
                TestType = TestType.ParallelRunner.ToString()
            };

            // set the active test run
            ConsoleWriter.ActiveTestRun = runResults;

            if (!_canRun)
            {
                ConsoleWriter.WriteLine("Could not find parallel runner executable!");
                errorReason = Resources.ParallelRunnerExecutableNotFound;
                runResults.TestState = TestState.Error;
                runResults.ErrorDesc = errorReason;
                return runResults;
            }

            // change the DCOM setting for qtp application
            Helper.ChangeDCOMSettingToInteractiveUser();

            // try to check if the Functional Testing process already exists
            bool uftProcessExist = false;
            using (Mutex m = new(true, PER_PROCESS_MUTEX_UFT, out bool isNewInstance))
            {
                if (!isNewInstance)
                {
                    uftProcessExist = true;
                }
            }

            // try to get qtp status via qtp automation object since the Functional Testing process exists
            if (uftProcessExist)
            {
                var type = Type.GetTypeFromProgID(QT_APP);
                var qtpApplication = Activator.CreateInstance(type) as QTObjectModelLib.Application;
                // status: Not launched / Ready / Busy / Running / Recording / Waiting / Paused
                string status = qtpApplication.GetStatus();
                switch (status)
                {
                    case NOT_LAUNCHED:
                        if (uftProcessExist)
                        {
                            // Functional Testing process exist but the status retrieved from qtp automation object is Not launched
                            // it means the Functional Testing is launched but not shown the main window yet
                            // in which case it shall be considered as Functional Testing is not used at all
                            // so here can kill the Functional Testing process to continue
                            Helper.KillUftProcess();
                        }
                        break;

                    case READY:
                    case WAITING:
                        // Functional Testing is launched but not running or recording, shall be considered as Functional Testing is not used
                        // so here can kill the Functional Testing process to continue
                        Helper.KillUftProcess();
                        break;

                    case BUSY:
                    case RUNNING:
                    case RECORDING:
                    case PAUSED:
                        // Functional Testing is launched and somehow in use now, shouldn't kill Functional Testing process
                        // here make the test fail
                        errorReason = Resources.UFT_Running;
                        runResults.ErrorDesc = errorReason;
                        runResults.TestState = TestState.Error;
                        return runResults;

                    default:
                        // by default, let the tool run test, the behavior might be unexpected
                        break;
                }
            }

            // Try to create the ParalleReport path
            try
            {
                Directory.CreateDirectory(runResults.ReportLocation);
            }
            catch (Exception)
            {
                errorReason = string.Format(Resources.FailedToCreateTempDirError, runResults.ReportLocation);
                runResults.TestState = TestState.Error;
                runResults.ErrorDesc = errorReason;

                Environment.ExitCode = (int)Launcher.ExitCodeEnum.Failed;
                return runResults;
            }

            runResults.StartDateTime = DateTime.Now;
            ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} => Using ParallelRunner to execute test: {testInfo.TestPath}");

            _runCancelled = runCancelled;

            // prepare the json file for the process
            string configFilePath;
            try
            {
                configFilePath = ParallelRunnerEnvironmentUtil.GetConfigFilePath(testInfo, _mcConnectionInfo, _environments);
            }
            catch (ParallelRunnerConfigurationException ex) // invalid configuration
            {
                errorReason = ex.Message;
                runResults.ErrorDesc = errorReason;
                runResults.TestState = TestState.Error;
                return runResults;
            }

            // Parallel runner argument "-c" for config path and "-o static" so that
            // the output from ParallelRunner is compatible with Jenkins
            var arguments = string.Format(PARALLEL_RUNNER_ARGS, configFilePath);

            // the test can be started now
            runResults.TestState = TestState.Running;

            Stopwatch runTime = new();
            runTime.Start();

            string failureReason = null;
            runResults.ErrorDesc = null;

            // execute parallel runner and get the run result status
            int exitCode = ExecuteProcess(_parallelRunnerFullPathFile, arguments, ref failureReason);

            // set the status of the build based on the exit code
            RunResultsFromParallelRunnerExitCode(runResults, exitCode, failureReason, ref errorReason);

            // update the run time
            runResults.Runtime = runTime.Elapsed;

            // update the report location as the report should be 
            // generated by now
            runResults.ReportLocation = resFolder;

            return runResults;
        }

        public void CleanUp()
        {
        }

        #region Process

        /// <summary>
        /// Check if the parent of the current process is running in the user session.
        /// </summary>
        /// <returns>true if the parent process is running in the user session, false otherwise.</returns>
        private bool IsParentProcessRunningInUserSession()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process parentProcess = currentProcess.Parent();

            // if they are not in the same session we will assume it is a service
            Process explorer;
            try
            {
                explorer = Process.GetProcessesByName(EXPLORER).FirstOrDefault();
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            // could not retrieve the explorer process
            if (explorer == null)
            {
                // try to start the process from the current session
                return false;
            }

            return parentProcess.SessionId != explorer.SessionId;
        }

        /// <summary>
        /// Return the corresponding process object based on the type of jenkins instance.
        /// </summary>
        /// <param name="fileName">the filename to be ran</param>
        /// <param name="arguments">the arguments for the process</param>
        /// <returns>the corresponding process type, based on the jenkins instance</returns>
        private object GetProcessTypeForCurrentSession(string fileName, string arguments)
        {
            try
            {
                if (!IsParentProcessRunningInUserSession())
                {
                    Process process = new();
                    InitProcess(process, fileName, arguments);
                    return process;
                }

                ConsoleWriter.WriteLine("Starting ParallelRunner from service session!");

                // the process must be started in the user session
                ElevatedProcess elevatedProcess = new(fileName, arguments, Helper.GetSTInstallPath());
                return elevatedProcess;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// executes the run of the test by using the Init and RunProcss routines
        /// </summary>
        /// <param name="fileName">the prcess file name</param>
        /// <param name="arguments">the arguments for the process</param>
        /// <param name="failureReason"> the reason why the process failed </param>
        /// <returns> the exit code of the process </returns>
        private int ExecuteProcess(string fileName, string arguments, ref string failureReason)
        {
            IProcessAdapter processAdapter = ProcessAdapterFactory.CreateAdapter(GetProcessTypeForCurrentSession(fileName, arguments));

            if (processAdapter == null)
            {
                failureReason = "Could not create ProcessAdapter instance!";
                return (int)ParallelRunResult.Error;
            }

            ConsoleWriter.WriteLine($"{fileName} {arguments}");

            try
            {
                int exitCode = RunProcess(processAdapter);

                if (_runCancelled())
                {
                    if (!processAdapter.HasExited)
                    {
                        processAdapter.Kill();
                        return (int)ParallelRunResult.Canceled;
                    }
                }

                return exitCode;
            }
            catch (Exception e)
            {
                failureReason = e.Message;
                return (int)ParallelRunResult.Error;
            }
            finally
            {
                processAdapter?.Close();
            }
        }

        /// <summary>
        /// Initializes the ParallelRunner process
        /// </summary>
        /// <param name="proc"> the process </param>
        /// <param name="fileName">the file name</param>
        /// <param name="arguments"> the process arguments </param>
        private void InitProcess(Process proc, string fileName, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            proc.StartInfo = processStartInfo;

            proc.OutputDataReceived += OnProcessOutputDataReceived;
            proc.ErrorDataReceived += OnProcessErrorDataReceived;
        }

        private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!e.Data.IsNullOrEmpty())
            {
                ConsoleWriter.WriteLine(e.Data);
            }
        }

        private void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!e.Data.IsNullOrEmpty())
            {
                ConsoleWriter.WriteRawErrLine(e.Data);
            }
        }

        /// <summary>
        /// runs the ParallelRunner process after initialization
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="enableRedirection"></param>
        private int RunProcess(IProcessAdapter proc)
        {
            ConsoleWriter.WriteLine(Resources.ParallelRunMessages + "\n-------------------------------------------------------------------------------------------------------");

            proc.Start();

            proc.WaitForExit(POLLING_TIME_MS);

            while (!_runCancelled() && !proc.HasExited)
            {
                proc.WaitForExit(POLLING_TIME_MS);
            }

            ConsoleWriter.WriteLine("-------------------------------------------------------------------------------------------------------");

            return proc.ExitCode;
        }

        #endregion
    }

    public enum ParallelRunResult : int
    {
        NotStarted = 1000,
        Pass = 1004,
        Warning = 1005,
        Fail = 1006,
        Canceled = 1007,
        Error = 1008,
    }
}