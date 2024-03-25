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
using System.Linq;
using System.IO;
using System.Xml;
using QTObjectModelLib;
using Resources = HpToolsLauncher.Properties.Resources;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using HpToolsLauncher.TestRunners;
using HpToolsLauncher.Common;
using HpToolsLauncher.Utils;
using static HpToolsLauncher.Common.McConnectionInfo;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics.Eventing.Reader;

namespace HpToolsLauncher
{
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="runNotifier"></param>
    /// <param name="useUftLicense"></param>
    /// <param name="timeLeftUntilTimeout"></param>
    public class GuiTestRunner(IAssetRunner runNotifier, UftProps uftProps, TimeSpan timeLeftUntilTimeout) : IFileSysTestRunner
    {
        // Setting keys for mobile
        private const string MOBILE_HOST_ADDRESS = "ALM_MobileHostAddress";
        private const string MOBILE_HOST_PORT = "ALM_MobileHostPort";
        private const string MOBILE_USER   = "ALM_MobileUserName";
        private const string MOBILE_PASSWORD = "ALM_MobilePassword";
        private const string MOBILE_TENANT = "EXTERNAL_MobileTenantId";
        private const string MOBILE_CLIENT_ID = "EXTERNAL_MobileClientID";
        private const string MOBILE_SECRET_KEY = "EXTERNAL_MobileSecretKey";
        private const string MOBILE_AUTH_TYPE = "EXTERNAL_MobileAuthType";
        private const string MOBILE_USE_SSL = "ALM_MobileUseSSL";
        private const string MOBILE_USE_PROXY= "MobileProxySetting_UseProxy";
        private const string MOBILE_PROXY_SETTING_ADDRESS = "MobileProxySetting_Address";
        private const string MOBILE_PROXY_SETTING_PORT = "MobileProxySetting_Port";
        private const string MOBILE_PROXY_SETTING_AUTHENTICATION = "MobileProxySetting_Authentication";
        private const string MOBILE_PROXY_SETTING_USERNAME = "MobileProxySetting_UserName";
        private const string MOBILE_PROXY_SETTING_PASSWORD = "MobileProxySetting_Password";
        private const string MOBILE_INFO = "mobileinfo";
        private const string PROTECT_BstrToBase64_FAILED = "ProtectBSTRToBase64 failed for {0}.";
        private const string WEB = "Web";
        private const string CLOUD_BROWSER = "CloudBrowser";

        private const string NOT_LAUNCHED = "Not launched";
        private const string READY = "Ready";
        private const string WAITING = "Waiting";
        private const string BUSY = "Busy";
        private const string RUNNING = "Running";
        private const string PASSED = "Passed";
        private const string WARNING = "Warning";
        private const string RECORDING = "Recording";
        private const string PAUSED = "Paused";
        private const string REPORT = "Report";
        private const string UFT = "UFT";
        private const string TEST_FAILED = "Test failed";
        private const string DDMMYYYYHHmmssfff = "ddMMyyyyHHmmssfff";
        private const string QT_APP = "Quicktest.Application";
        private const string PER_PROCESS_MUTEX_UFT = "per_process_mutex_UFT";
        private const string QTPAUTOMATIONAGENT = "qtpAutomationAgent";
        private const string QT_RUNRESOPTS = "QuickTest.RunResultsOptions";
        private const string WEBLAUNCHER_NOT_FOUND = "WebLauncher not found. Please make sure the Web Addin is selected at Test level.";

        private readonly IAssetRunner _runNotifier = runNotifier;
        private readonly object _lockObject = new();
        private readonly TimeSpan _timeLeftUntilTimeout = timeLeftUntilTimeout;
        private readonly string _uftRunMode = uftProps.UftRunMode?.ToString();
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private Application _qtpApplication;
        private ParameterDefinitions _qtpParamDefs;
        private Parameters _qtpParameters;
        private readonly bool _useUftLicense = uftProps.UseUftLicense;
        private RunCancelledDelegate _runCancelled;
        private readonly McConnectionInfo _mcConnection = uftProps.DigitalLab?.ConnectionInfo;
        private readonly string _mobileInfo = uftProps.DigitalLab?.MobileInfo;
        private readonly CloudBrowser _cloudBrowser = uftProps.DigitalLab?.CloudBrowser;
        private readonly bool _leaveUftOpenIfLaunched = uftProps.LeaveUftOpenIfLaunched;

        #region QTP

        /// <summary>
        /// runs the given test and returns resutls
        /// </summary>
        /// <param name="testPath"></param>
        /// <param name="errorReason"></param>
        /// <param name="runCancelled"></param>
        /// <returns></returns>
        public TestRunResults RunTest(TestInfo testinf, ref string errorReason, RunCancelledDelegate runCancelled)
        {
            var testPath = testinf.TestPath;
            TestRunResults runDesc = new() { StartDateTime = DateTime.Now };
            ConsoleWriter.ActiveTestRun = runDesc;
            ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Running test: {testPath} ...");

            runDesc.TestPath = testPath;
            if (!TrySetReportLocation(testinf, runDesc, ref errorReason))
                return runDesc;

            runDesc.TestState = TestState.Unknown;

            _runCancelled = runCancelled;

            if (!Helper.IsQtpInstalled())
            {
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = string.Format(Resources.GeneralQtpNotInstalled, Environment.MachineName);
                ConsoleWriter.WriteErrLine(runDesc.ErrorDesc);
                Environment.ExitCode = (int)Launcher.ExitCodeEnum.Failed;
                return runDesc;
            }

            if (!Helper.CanUftProcessStart(out string reason))
            {
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = reason;
                ConsoleWriter.WriteErrLine(runDesc.ErrorDesc);
                Environment.ExitCode = (int)Launcher.ExitCodeEnum.Failed;
                return runDesc;
            }

            Version qtpVersion;
            try
            {
                ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} {Resources.LaunchingTestingTool}");

                var type = Type.GetTypeFromProgID(QT_APP);

                lock (_lockObject)
                {
                    // before creating qtp automation object which creates UFT process, try to check if the UFT process already exists
                    bool uftProcessExist = false;
                    using (Mutex m = new(true, PER_PROCESS_MUTEX_UFT, out bool isNewInstance))
                    {
                        if (!isNewInstance)
                        {
                            uftProcessExist = true;
                        }
                    }

                    // this will create UFT process
                    _qtpApplication = Activator.CreateInstance(type) as Application;

                    // try to get qtp status via qtp automation object, this might fail if UFT is launched and waiting for user input on addins manage window
                    // status: Not launched / Ready / Busy / Running / Recording / Waiting / Paused
                    string status = _qtpApplication.GetStatus();
                    switch (status)
                    {
                        case NOT_LAUNCHED:
                            if (uftProcessExist)
                            {
                                // UFT process exist but the status retrieved from qtp automation object is Not launched
                                // it means the UFT is launched but not shown the main window yet
                                // in which case it shall be considered as UFT is not used at all
                                // so here can kill the UFT process to continue
                                Process[] procs = Process.GetProcessesByName(UFT);
                                procs?.ForEach(p => p.Kill());
                                uftProcessExist = false;
                            }
                            break;

                        case READY:
                        case WAITING:
                            // UFT is launched but not running or recording, shall be considered as UFT is not used
                            // no need kill UFT process here since the qtp automation object can work properly
                            break;

                        case BUSY:
                        case RUNNING:
                        case RECORDING:
                        case PAUSED:
                            // UFT is launched and somehow in use now, shouldn't kill UFT process here, make the test fail
                            errorReason = Resources.UFT_Running;
                            runDesc.TestState = TestState.Error;
                            runDesc.ReportLocation = string.Empty;
                            runDesc.ErrorDesc = errorReason;
                            return runDesc;

                        default:
                            // by default, let the tool run test, the behavior might be unexpected
                            break;
                    }

                    qtpVersion = Version.Parse(_qtpApplication.Version);
                    if (qtpVersion.Equals(new Version(11, 0)))
                    {
                        runDesc.ReportLocation = GetReportLocationV11(testinf, testPath);
                    }

                    // Check for required Addins
                    if (_qtpApplication.Launched && _qtpApplication.Visible && _leaveUftOpenIfLaunched)
                        QTPTestCleanup();
                    else
                        LoadNeededAddins(testPath);

                    // set Mc connection and other mobile info into rack if neccesary
                    SetMobileInfo();

                    if (!_qtpApplication.Launched)
                    {
                        if (_runCancelled())
                        {
                            QTPTestCleanup();
                            KillQtp();
                            runDesc.TestState = TestState.Error;
                            return runDesc;
                        }
                        // Launch application after set Addins
                        _qtpApplication.Launch();
                        _qtpApplication.Visible = false;
                    }
                }
            }
            catch (Exception e)
            {
                string errorMsg = e.Message;
                string errorStacktrace = e.StackTrace.IsNullOrWhiteSpace() ? string.Empty : e.StackTrace;
                errorReason = Resources.QtpNotLaunchedError + "\n" + string.Format(Resources.ExceptionDetails, errorMsg, errorStacktrace);
                if (e is SystemException)
                {
                    errorReason += $"\n{Resources.QtpNotLaunchedError_PossibleSolution_RegModellib}";
                }
                    
                runDesc.TestState = TestState.Error;
                runDesc.ReportLocation = string.Empty;
                runDesc.ErrorDesc = errorReason;
                return runDesc;
            }

            if (_qtpApplication.Test != null && _qtpApplication.Test.Modified)
            {
                var message = Resources.QtpNotLaunchedError;
                errorReason = message;
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = errorReason;
                return runDesc;
            }

            _qtpApplication.UseLicenseOfType(_useUftLicense ? tagUnifiedLicenseType.qtUnifiedFunctionalTesting : tagUnifiedLicenseType.qtNonUnified);

            Dictionary<string, object> @params;
            try
            {
                @params = testinf.GetParameterDictionaryForQTP();
            }
            catch (ArgumentException)
            {
                ConsoleWriter.WriteErrLine(Resources.FsDuplicateParamNames);
                throw;
            }

            if (!HandleInputParameters(testPath, ref errorReason, @params, testinf))
            {
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = errorReason;
                return runDesc;
            }

            if (!HandleCloudBrowser(qtpVersion, ref errorReason))
            {
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = errorReason;
                return runDesc;
            }

            GuiTestRunResult guiTestRunResult = ExecuteQTPRun(runDesc);

            // consider backward compatibility, here move the report folder one outside
            // that is, after test run, the report file might be at "path\to\tests\GUITest1\Report123\Report\run_results.html"
            // here move the last directory "Report" one level outside, which is, "path\to\tests\GUITest1\Report123\run_results.html"
            // steps:
            //   1. move directory "path\to\tests\GUITest1\Report123" to "path\to\tests\GUITest1\tmp_ddMMyyyyHHmmssfff"
            string guiTestReportPath = guiTestRunResult.ReportPath;         // guiTestReportPath: path\to\tests\GUITest1\Report123\Report
            string targetReportDir = Path.GetDirectoryName(guiTestReportPath);    // reportDir: path\to\tests\GUITest1\Report123
            string reportBaseDir = Path.GetDirectoryName(targetReportDir);        // reportBaseDir: path\to\tests\GUITest1
            string tmpDir = Path.Combine(reportBaseDir, $"tmp_{DateTime.Now.ToString(DDMMYYYYHHmmssfff)}"); // tmpDir: path\to\tests\GUITest1\tmp_ddMMyyyyHHmmssfff
            //   1.a) directory move may fail because UFT might still be writting report files, need retry
            const int maxMoveDirRetry = 30;
            int moveDirRetry = 0;
            bool dirMoved = false;
            do
            {
                try
                {
                    Directory.Move(targetReportDir, tmpDir);
                    dirMoved = true;
                    break;
                }
                catch (IOException)
                {
                    moveDirRetry++;
                    if (moveDirRetry == 1)
                    {
                        ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Report is not ready yet, wait up to {maxMoveDirRetry} seconds ...");
                    }
                    Thread.Sleep(1000);
                }
            } while (moveDirRetry < maxMoveDirRetry);
            
            if (dirMoved)
            {
                // 2. move directory "path\to\tests\GUITest1\tmp_ddMMyyyyHHmmssfff\Report" to "path\to\tests\GUITest1\Report123"
                string tmpReportDir = Path.Combine(tmpDir, REPORT);           // tmpReportDir: path\to\tests\GUITest1\tmp_ddMMyyyyHHmmssfff\Report
                Directory.Move(tmpReportDir, targetReportDir);
                // 3. delete the temp directory "path\to\test1\tmp_ddMMyyyyHHmmssfff"
                Directory.Delete(tmpDir, true);
                runDesc.ReportLocation = targetReportDir;
            }
            else
            {
                runDesc.ReportLocation = guiTestReportPath;
                ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Warning: Report folder is still in use, leave it in: {guiTestReportPath}");
            }

            if (!guiTestRunResult.IsSuccess)
            {
                runDesc.TestState = TestState.Error;
                return runDesc;
            }

            if (!HandleOutputArguments(ref errorReason))
            {
                runDesc.TestState = TestState.Error;
                runDesc.ErrorDesc = errorReason;
                return runDesc;
            }

            QTPTestCleanup();
            return runDesc;
        }

        private bool TrySetReportLocation(TestInfo testinf, TestRunResults runDesc, ref string errorReason)
        {
            // check if the report path has been defined
            if (!testinf.ReportPath.IsNullOrWhiteSpace())
            {
                runDesc.ReportLocation = Path.GetFullPath(testinf.ReportPath);
                ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Report path is explicitly set as: {runDesc.ReportLocation}");
            }
            else if (!testinf.ReportBaseDirectory.IsNullOrEmpty())
            {
                testinf.ReportBaseDirectory = Path.GetFullPath(testinf.ReportBaseDirectory);
                if (!Helper.TrySetTestReportPath(runDesc, testinf, ref errorReason))
                {
                    return false;
                }
                ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Report path is generated under base directory: {runDesc.ReportLocation}");
            }
            else
            {
                // default report location is the next available folder under test path
                // for example, "path\to\tests\GUITest1\Report123", the name "Report123" will also be used as the report name
                string reportBasePath = Path.GetFullPath(testinf.TestPath);
                string testReportPath = Path.Combine(reportBasePath, $"{REPORT}{DateTime.Now.ToString(DDMMYYYYHHmmssfff)}");
                int index = 0;
                while (index < int.MaxValue)
                {
                    index++;
                    string dir = Path.Combine(reportBasePath, $"{REPORT}{index}");
                    if (!Directory.Exists(dir))
                    {
                        testReportPath = dir;
                        break;
                    }
                }
                runDesc.ReportLocation = testReportPath;
                ConsoleWriter.WriteLine($"{DateTime.Now.ToString(Launcher.DateFormat)} Report path is automatically generated: {runDesc.ReportLocation}");
            }
            return true;
        }
        private string GetReportLocationV11(TestInfo testinf, string testPath)
        {
            string rptLocation;
            // use the defined report path if provided
            if (!testinf.ReportPath.IsNullOrWhiteSpace())
                rptLocation = Path.Combine(testinf.ReportPath, REPORT);
            else if (!testinf.ReportBaseDirectory.IsNullOrWhiteSpace())
                rptLocation = Path.Combine(testinf.ReportBaseDirectory, REPORT);
            else
                rptLocation = Path.Combine(testPath, REPORT);

            if (Directory.Exists(rptLocation))
            {
                int lastIndex = rptLocation.IndexOf("\\");
                var location = rptLocation.Substring(0, lastIndex);
                var name = rptLocation.Substring(lastIndex + 1);
                rptLocation = Helper.GetNextResFolder(location, name);
                Directory.CreateDirectory(rptLocation);
            }
            return rptLocation;
        }

        private void SetMobileInfo()
        {
            if (_mcConnection == null || _mcConnection.HostAddress.IsNullOrEmpty())
                return;

            #region Mc connection and other mobile info

            ITDPierToTulip tulip = _qtpApplication.TDPierToTulip;

            // Mc Address, username and password
            tulip.SetTestOptionsVal(MOBILE_HOST_ADDRESS, _mcConnection.HostAddress);
            if (!_mcConnection.HostPort.IsNullOrEmpty())
            {
                tulip.SetTestOptionsVal(MOBILE_HOST_PORT, _mcConnection.HostPort);
            }
            if (_mcConnection.MobileAuthType == AuthType.AuthToken)
            {
                tulip.SetTestOptionsVal(MOBILE_CLIENT_ID, _mcConnection.ClientId);
                tulip.SetTestOptionsVal(MOBILE_SECRET_KEY, _mcConnection.SecretKey);
                tulip.SetTestOptionsVal(MOBILE_AUTH_TYPE, AuthType.AuthToken);
            }
            else if (!_mcConnection.UserName.IsNullOrEmpty())
            {
                tulip.SetTestOptionsVal(MOBILE_USER, _mcConnection.UserName);
                if (!_mcConnection.Password.IsNullOrEmpty())
                {
                    tulip.SetTestOptionsVal(MOBILE_PASSWORD, GetEncryptedPassword(_mcConnection.Password));
                    tulip.SetTestOptionsVal(MOBILE_AUTH_TYPE, AuthType.UsernamePassword);
                }
            }

            if (!_mcConnection.TenantId.IsNullOrEmpty())
            {
                tulip.SetTestOptionsVal(MOBILE_TENANT, _mcConnection.TenantId);
            }

            if (_mcConnection.UseSSL)
                tulip.SetTestOptionsVal(MOBILE_USE_SSL, 1);

            if (_mcConnection.UseProxy)
            {
                tulip.SetTestOptionsVal(MOBILE_USE_PROXY, 1);
                tulip.SetTestOptionsVal(MOBILE_PROXY_SETTING_ADDRESS, _mcConnection.ProxyAddress);
                tulip.SetTestOptionsVal(MOBILE_PROXY_SETTING_PORT, _mcConnection.ProxyPort);
                if (_mcConnection.UseProxyAuth)
                {
                    tulip.SetTestOptionsVal(MOBILE_PROXY_SETTING_AUTHENTICATION, 1);
                    tulip.SetTestOptionsVal(MOBILE_PROXY_SETTING_USERNAME, _mcConnection.ProxyUserName);
                    tulip.SetTestOptionsVal(MOBILE_PROXY_SETTING_PASSWORD, GetEncryptedPassword(_mcConnection.ProxyPassword));
                }
            }

            // Mc info (device, app, launch and terminate data)
            if (!_mobileInfo.IsNullOrEmpty())
            {
                tulip.SetTestOptionsVal(MOBILE_INFO, _mobileInfo);
            }

            #endregion
        }
        private string GetEncryptedPassword(string clearPassword)
        {
            const string DL_OR_PROXY_PASSWORD = "DL Password or DL Proxy Password";
            string encPassword = WinUserNativeMethods.ProtectBSTRToBase64(clearPassword);
            if (encPassword == null)
            {
                ConsoleWriter.WriteLine(string.Format(PROTECT_BstrToBase64_FAILED, DL_OR_PROXY_PASSWORD));
                throw new Exception(string.Format(PROTECT_BstrToBase64_FAILED, DL_OR_PROXY_PASSWORD));
            }
            return encPassword;
        }

        /// <summary>
        /// performs global cleanup code for this type of runner
        /// </summary>
        public void CleanUp()
        {
            try
            {
                //if we don't have a qtp instance, create one
                if (_qtpApplication == null)
                {
                    var type = Type.GetTypeFromProgID(QT_APP);
                    _qtpApplication = Activator.CreateInstance(type) as Application;
                }

                //if the app is running, close it.
                if (_qtpApplication.Launched && !(_qtpApplication.Visible && _leaveUftOpenIfLaunched))
                    _qtpApplication.Quit();
            }
            catch
            {
                //nothing to do. (cleanup code should not throw exceptions, and there is no need to log this as an error in the test)
            }
        }

        static HashSet<string> _colLoadedAddinNames = null;
        /// <summary>
        /// Set the test Addins 
        /// </summary>
        private void LoadNeededAddins(string fileName)
        {
            bool blnNeedToLoadAddins = false;

            //if not launched, we have no addins.
            if (!_qtpApplication.Launched)
                _colLoadedAddinNames = null;

            try
            {
                HashSet<string> colCurrentTestAddins = [];

                var testAddinsObj = _qtpApplication.GetAssociatedAddinsForTest(fileName);
                object[] testAddins = (object[])testAddinsObj;

                foreach (string addin in testAddins.Cast<string>())
                {
                    colCurrentTestAddins.Add(addin);
                }

                if (_colLoadedAddinNames != null)
                {
                    //check if we have a missing addin (and need to quit Qtp, and reload with new addins)
                    foreach (string addin in testAddins.Cast<string>())
                    {
                        if (!_colLoadedAddinNames.Contains(addin))
                        {
                            blnNeedToLoadAddins = true;
                            break;
                        }
                    }

                    //check if there is no extra addins that need to be removed
                    if (_colLoadedAddinNames.Count != colCurrentTestAddins.Count)
                    {
                        blnNeedToLoadAddins = true;
                    }
                }
                else
                {
                    //first time = load addins.
                    blnNeedToLoadAddins = true;
                }

                _colLoadedAddinNames = colCurrentTestAddins;

                //the addins need to be refreshed, load new addins
                if (blnNeedToLoadAddins)
                {
                    if (_qtpApplication.Launched)
                        _qtpApplication.Quit();
                    _qtpApplication.SetActiveAddins(ref testAddinsObj, out object _);
                }

            }
            catch (Exception)
            {
                // Try anyway to run the test
            }
        }

        /// <summary>
        /// Activate all Installed Addins 
        /// </summary>
        private void ActivateAllAddins()
        {
            try
            {
                // Get Addins collection
                Addins qtInstalledAddins = _qtpApplication.Addins;

                if (qtInstalledAddins.Count > 0)
                {
                    string[] qtAddins = new string[qtInstalledAddins.Count];

                    // Addins Object is 1 base order
                    for (int idx = 1; idx <= qtInstalledAddins.Count; ++idx)
                    {
                        // Our list is 0 base order
                        qtAddins[idx - 1] = qtInstalledAddins[idx].Name;
                    }

                    var addinNames = (object)qtAddins;

                    _qtpApplication.SetActiveAddins(ref addinNames, out object erroDescription);
                }
            }
            catch (Exception)
            {
                // Try anyway to run the test
            }
        }

        /// <summary>
        /// runs the given test QTP and returns results
        /// </summary>
        /// <param name="testResults">the test results object containing test info and also receiving run results</param>
        /// <returns></returns>
        private GuiTestRunResult ExecuteQTPRun(TestRunResults testResults)
        {
            GuiTestRunResult result = new() { IsSuccess = true };
            try
            {
                Type runResultsOptionstype = Type.GetTypeFromProgID(QT_RUNRESOPTS);
                var options = (RunResultsOptions)Activator.CreateInstance(runResultsOptionstype);
                options.ResultsLocation = testResults.ReportLocation;
                if (_uftRunMode != null)
                {
                    _qtpApplication.Options.Run.RunMode = _uftRunMode;
                }

                //Check for cancel before executing
                if (_runCancelled())
                {
                    testResults.TestState = TestState.Error;
                    testResults.ErrorDesc = Resources.GeneralTestCanceled;
                    ConsoleWriter.WriteLine(Resources.GeneralTestCanceled);
                    result.IsSuccess = false;
                    return result;
                }
                ConsoleWriter.WriteLine(string.Format(Resources.FsRunnerRunningTest, testResults.TestPath));

                _qtpApplication.Test.Run(options, false, _qtpParameters);

                result.ReportPath = Path.Combine(testResults.ReportLocation, REPORT);
                int slept = 0;
                while (slept < 20000 && _qtpApplication.GetStatus().In([READY, WAITING], true))
                {
                    Thread.Sleep(50);
                    slept += 50;
                }

                while (!_runCancelled() && _qtpApplication.GetStatus().In([RUNNING, BUSY], true))
                {
                    Thread.Sleep(200);
                    if (_timeLeftUntilTimeout - _stopwatch.Elapsed <= TimeSpan.Zero)
                    {
                        _qtpApplication.Test.Stop();
                        testResults.TestState = TestState.Error;
                        testResults.ErrorDesc = Resources.GeneralTimeoutExpired;
                        ConsoleWriter.WriteLine(Resources.GeneralTimeoutExpired);

                        result.IsSuccess = false;
                        return result;
                    }
                }

                if (_runCancelled())
                {
                    QTPTestCleanup();
                    KillQtp();
                    testResults.TestState = TestState.Error;
                    testResults.ErrorDesc = Resources.GeneralTestCanceled;
                    ConsoleWriter.WriteLine(Resources.GeneralTestCanceled);
                    Launcher.ExitCode = Launcher.ExitCodeEnum.Aborted;
                    result.IsSuccess = false;
                    return result;
                }
                string lastError = _qtpApplication.Test.LastRunResults.LastError;

                //read the lastError
                if (!lastError.IsNullOrEmpty())
                {
                    testResults.TestState = TestState.Error;
                    testResults.ErrorDesc = lastError;
                }

                // the way to check the logical success of the target QTP test is: app.Test.LastRunResults.Status == "Passed".
                if (_qtpApplication.Test.LastRunResults.Status.EqualsIgnoreCase(PASSED))
                {
                    testResults.TestState = TestState.Passed;

                }
                else if (_qtpApplication.Test.LastRunResults.Status.EqualsIgnoreCase(WARNING))
                {
                    testResults.TestState = TestState.Passed;
                    testResults.HasWarnings = true;

                    if (Launcher.ExitCode != Launcher.ExitCodeEnum.Failed && Launcher.ExitCode != Launcher.ExitCodeEnum.Aborted)
                        Launcher.ExitCode = Launcher.ExitCodeEnum.Unstable;
                }
                else
                {
                    testResults.TestState = TestState.Failed;
                    testResults.FailureDesc = TEST_FAILED;

                    Launcher.ExitCode = Launcher.ExitCodeEnum.Failed;
                }
            }
            catch (NullReferenceException e)
            {
                ConsoleWriter.WriteLine(string.Format(Resources.GeneralErrorWithStack, e.Message, e.StackTrace));
                testResults.TestState = TestState.Error;
                testResults.ErrorDesc = Resources.QtpRunError;

                result.IsSuccess = false;
                return result;
            }
            catch (SystemException e)
            {
                KillQtp();
                ConsoleWriter.WriteLine(string.Format(Resources.GeneralErrorWithStack, e.Message, e.StackTrace));
                testResults.TestState = TestState.Error;
                testResults.ErrorDesc = Resources.QtpRunError;

                result.IsSuccess = false;
                return result;
            }
            catch (Exception e2)
            {

                ConsoleWriter.WriteLine(string.Format(Resources.GeneralErrorWithStack, e2.Message, e2.StackTrace));
                testResults.TestState = TestState.Error;
                testResults.ErrorDesc = Resources.QtpRunError;

                result.IsSuccess = false;
                return result;
            }


            return result;
        }

        private void KillQtp()
        {
            //if we don't have a qtp instance, create one
            if (_qtpApplication == null)
            {
                var type = Type.GetTypeFromProgID(QT_APP);
                _qtpApplication = Activator.CreateInstance(type) as Application;
            }

            //if the app is running, close it.
            if (_qtpApplication.Launched && _qtpApplication.Visible && _leaveUftOpenIfLaunched)
            {
                //leave UFT open, the user can close it manually if needed
            }
            else
            {
                //error during run, process may have crashed (need to cleanup, close QTP and qtpRemote for next test to run correctly)
                CleanUp();

                //kill the qtp automation, to make sure it will run correctly next time
                Process[] processes = Process.GetProcessesByName(QTPAUTOMATIONAGENT);
                Process qtpAuto = processes.Where(p => p.SessionId == Process.GetCurrentProcess().SessionId).FirstOrDefault();
                qtpAuto?.Kill();
            }
        }

        private bool HandleOutputArguments(ref string errorReason)
        {
            try
            {
                XmlDocument outputArguments = new() { PreserveWhitespace = true };
                outputArguments.LoadXml("<Arguments/>");

                for (int i = 1; i <= _qtpParamDefs.Count; ++i)
                {
                    var pd = _qtpParamDefs[i];
                    if (pd.InOut == qtParameterDirection.qtParamDirOut)
                    {
                        var node = outputArguments.CreateElement(pd.Name);
                        var value = _qtpParameters[pd.Name].Value;
                        if (value != null)
                            node.InnerText = value.ToString();

                        outputArguments.DocumentElement.AppendChild(node);
                    }
                }
            }
            catch
            {
                errorReason = Resources.QtpNotLaunchedError;
                return false;
            }
            return true;
        }
        private bool VerifyParamValueType(object paramValue, qtParameterType type)
        {
            bool legal = type switch
            {
                qtParameterType.qtParamTypeBoolean => paramValue is bool,
                qtParameterType.qtParamTypeDate => paramValue is DateTime,
                qtParameterType.qtParamTypeNumber => paramValue is int || paramValue is long || paramValue is decimal || paramValue is float || paramValue is double,
                qtParameterType.qtParamTypePassword => paramValue is string,
                qtParameterType.qtParamTypeString => paramValue is string,
                _ => true,
            };
            return legal;
        }

        private bool HandleCloudBrowser(Version qtpVersion, ref string errorReason)
        {
            if (_cloudBrowser != null)
            {
                if (qtpVersion < new Version(2023, 4))
                {
                    errorReason = string.Format(Resources.CloudBrowserNotSupported, qtpVersion.ToString(2));
                    return false;
                }
                Launchers launchers = _qtpApplication.Test.Settings.Launchers;
                bool hasWebLauncher = false;
                if (launchers?.Count > 0)
                {
                    foreach (var ln in launchers)
                    {
                        if (ln is WebLauncher webLauncher)
                        {
                            hasWebLauncher = true;
                            try
                            {
                                webLauncher.Active = true;
                                webLauncher.SetLab(CLOUD_BROWSER);
                                if (!_cloudBrowser.Url.IsNullOrWhiteSpace())
                                    webLauncher.Address = _cloudBrowser.Url;
                                webLauncher.CloudBrowser.OS = _cloudBrowser.OS;
                                webLauncher.CloudBrowser.Browser = _cloudBrowser.Browser;
                                webLauncher.CloudBrowser.BrowserVersion = _cloudBrowser.Version;
                                webLauncher.CloudBrowser.Location = _cloudBrowser.Region;
                            }
                            catch (Exception e)
                            {
                                ConsoleWriter.WriteLine(string.Format(Resources.GeneralErrorWithStack, e.Message, e.StackTrace));
                                errorReason = e.Message;
                                return false;
                            }
                            break;
                        }
                    }
                }
                if (!hasWebLauncher)
                {
                    errorReason = WEBLAUNCHER_NOT_FOUND;
                    return false;
                }
            }
            return true;
        }

        private bool HandleInputParameters(string fileName, ref string errorReason, Dictionary<string, object> inputParams, TestInfo testInfo)
        {
            try
            {
                string path = fileName;

                if (_runCancelled())
                {
                    QTPTestCleanup();
                    KillQtp();
                    return false;
                }

                _qtpApplication.Open(path, true, false);
                _qtpParamDefs = _qtpApplication.Test.ParameterDefinitions;
                _qtpParameters = _qtpParamDefs.GetParameters();

                // handle all parameters (index starts with 1 !!!)
                for (int i = 1; i <= _qtpParamDefs.Count; i++)
                {
                    // input parameters
                    if (_qtpParamDefs[i].InOut == qtParameterDirection.qtParamDirIn)
                    {
                        string paramName = _qtpParamDefs[i].Name;
                        qtParameterType type = _qtpParamDefs[i].Type;

                        // if the caller supplies value for a parameter we set it
                        if (inputParams.ContainsKey(paramName))
                        {
                            // first verify that the type is correct
                            object paramValue = inputParams[paramName];
                            if (!VerifyParamValueType(paramValue, type))
                            {
                                ConsoleWriter.WriteErrLine(string.Format("Illegal input parameter type (skipped). param: '{0}'. expected type: '{1}'. actual type: '{2}'", paramName, Enum.GetName(typeof(qtParameterType), type), paramValue.GetType()));
                            }
                            else
                            {
                                _qtpParameters[paramName].Value = paramValue;
                            }
                        }
                    }
                }

                // specify data table path
                if (testInfo.DataTablePath != null)
                {
                    _qtpApplication.Test.Settings.Resources.DataTablePath = testInfo.DataTablePath;
                    ConsoleWriter.WriteLine($"Using external data table: {testInfo.DataTablePath}");
                }

                // specify iteration mode
                if (testInfo.IterationInfo != null)
                {
                    try
                    {
                        IterationInfo ii = testInfo.IterationInfo;
                        if (!IterationInfo.AvailableTypes.Contains(ii.IterationMode))
                        {
                            throw new ArgumentException(string.Format("Illegal iteration mode '{0}'. Available modes are : {1}", ii.IterationMode, string.Join(", ", IterationInfo.AvailableTypes)));
                        }

                        string range = string.Empty;
                        if (IterationInfo.RANGE_ITERATION_MODE == ii.IterationMode)
                        {
                            int start = int.Parse(ii.StartIteration);
                            int end = int.Parse(ii.EndIteration);

                            _qtpApplication.Test.Settings.Run.StartIteration = start;
                            _qtpApplication.Test.Settings.Run.EndIteration = end;
                            range = $"{ii.StartIteration}-{ii.EndIteration}";
                        }

                        _qtpApplication.Test.Settings.Run.IterationMode = ii.IterationMode;

                        ConsoleWriter.WriteLine($"Using iteration mode: {ii.IterationMode} {range}");
                    }
                    catch (Exception e)
                    {
                        string msg = $"Failed to parse 'Iterations' element. Using default iteration settings. Error : {e.Message}";
                        ConsoleWriter.WriteLine(msg);
                    }
                }
            }
            catch
            {
                errorReason = Resources.QtpRunError;
                return false;
            }
            return true;

        }

        /// <summary>
        /// stops and closes qtp test, to make sure nothing is left floating after run.
        /// </summary>
        private void QTPTestCleanup()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_qtpApplication == null)
                    {
                        return;
                    }

                    var qtpTest = _qtpApplication.Test;
                    if (qtpTest != null)
                    {
                        if (qtpTest.IsRunning)
                            qtpTest.Stop();
                        qtpTest.Close();
                    }
                }
            }
            catch { }

            _qtpParameters = null;
            _qtpParamDefs = null;
            _qtpApplication = null;
        }

        #endregion

        /// <summary>
        /// holds the resutls for a GUI test
        /// </summary>
        private class GuiTestRunResult
        {
            public GuiTestRunResult()
            {
                ReportPath = string.Empty;
            }

            public bool IsSuccess { get; set; }
            public string ReportPath { get; set; }
        }
    }
}
