# Micro Focus UFT One CI Utilities
**Micro Focus UFT One CI Utilities** contains tools that you can use to run automation tests by launching Micro Focus functional testing applications such as **UFT One** (formerly **Unified Functional Testing**) and **LoadRunner**, and so on.

The following tools are available:
- [FTToolsLauncher](#fttools-launcher)
- [FTToolsAborter](#fttools-aborter)
- [LRAnalysisLauncher](#fttools-lr-analysis-launcher)
- [ReportConverter](#report-converter)

## <a name="fttools-launcher"></a>FTToolsLauncher
The **FTToolsLauncher** is a command-line tool that launches the functional testing application and runs tests.

This tool lets you run one or more of the following test types:
- **UFT One** tests:
    * GUI/API tests stored in the file system
    * GUI/API/BPT tests and test sets stored in **Micro Focus Application Lifecycle Management** (**ALM**)
    * GUI tests in parallel mode stored in the file system
- **LoadRunner** tests

### <a name="fttools-launcher-toc"></a>Table Of Contents
- [Command Line References](#cmd-line-refs)
- [Parameter File References](#params-file-refs)
    * [Basic Parameters](#basic-params-refs)
    * [ALM Parameters](#alm-params-refs)
    * [File System Parameters](#filesystem-params-refs)
    * [Test Rerun Parameters (File System Only)](#test-rerun-params-refs)
    * [LoadRunner Parameters (File System Only)](#lr-params-refs)
    * [UFT Mobile Parameters](#mc-params-refs)
    * [Parallel Runner Parameters (File System Only)](#parallel-runner-params-refs)
    * [Non-public Parameters](#non-public-params-refs)
- [.mtb File References](#mtb-file-refs)
- [.mtbx File References](#mtbx-file-refs)
- [Parallel Runner Variables](#parallel-runner-vars)
- [Samples: FTToolsLauncher Parameters File](#fttools-launcher-samples)
    * [Sample 1: Run one GUITest (File System)](#fttools-launcher-sample-1)
    * [Sample 2: Run multiple tests (File System)](#fttools-launcher-sample-2)
    * [Sample 3: Run test (File System) with multiple environments in parallel](#fttools-launcher-sample-3)
    * [Sample 4: Run ALM test sets](#fttools-launcher-sample-4)
    * [Sample 5: Run mobile test](#fttools-launcher-sample-5)
    * [Sample 6: Run multiple test with .mtb file (File System)](#fttools-launcher-sample-6)
    * [Sample 7: Run multiple test with .mtbx file (File System)](#fttools-launcher-sample-7)
- [Exit Code](#fttools-exit-code)
- [Limitations](#fttools-launcher-limit)

### <a name="cmd-line-refs"></a>Command Line References
> Go to [Table Of Contents](#fttools-launcher-toc)

```batch
FTToolsLauncher.exe -paramfile <a file in key=value format>
```

### <a name="params-file-refs"></a>Parameter File References
> Go to [Table Of Contents](#fttools-launcher-toc)

The **FTToolsLauncher** command-line tool requires a parameter file that describes parameters in `key=value` format, one parameter per line. A good example is the **Java Properties** file.

Special characters need to be escaped to enable the tool to properly load the paramters file. For example, if there is a parameter `Test1=C:\\tests\\GUITest1`, the path delimeters need to be escaped with `\`.

The follwoing types of parameters are supported:
* [Basic Parameters](#basic-params-refs)
* [ALM Parameters](#alm-params-refs)
* [File System Parameters](#filesystem-params-refs)
* [Test Rerun Parameters (File System Only)](#test-rerun-params-refs)
* [Load Runner Parameters (File System Only)](#lr-params-refs)
* [UFT Mobile Parameters](#mc-params-refs)
* [ParallelRunner Parameters (File System Only)](#parallel-runner-params-refs)
* [Non-public Parameters](#non-public-params-refs)

#### <a name="basic-params-refs"></a>Basic Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`runType`** | string | `FileSystem` _or_ `Alm` | [**Mandatory**] The test asset location type.<br/><br/>`FileSystem` for UFT GUI/API and LoadRunner tests stored in the file system.<br/>`Alm` for UFT GUI/API tests stored on a **Micro Focus Application Lifecycle Management** (**ALM**) server. |
| **`resultsFilename`** | string | file name _or_ file path | [**Mandatory**] The file name or file path in which to save the test results summary. If the file name is a relative path, the path is relative to the current workspace. |
| `resultFormatLanguage` | string | *`Default`*<br/>-or-<br/>`System`<br/>-or-<br/>&lt;language-tag&gt; | **Introduced in `v1.0.22.4723` (`v1.0-beta-rev6`)**.<br/><br/>(*Optional*) The language used to format numbers, dates, and times in the test results summary file. For example, the number `12.34` is generated as is in English (language tag `en-US`) while it is `12,34` in German (language tag `de-DE`). The default language is English.<br/><br/>If the value is `System`, the application will automatically detect the language used in the system and use that language for localization.<br/><br/>The value can also be one of the valid case-insensitive language tag names such as `en-US`, `de-DE` and so on. For a list of predefined language tag names on Windows systems, see the **Language tag** column in the [list of language/region names supported by Windows][msdoc-list-of-langauge-region-names-supported-by-windows]. The names follow the standard defined by [BCP 47][bcp47-url]. In addition, starting with **Windows 10**, name can be any valid BCP-47 language tag. |
| `resultTestNameOnly` | boolean | `true` _or_ *`false`* | **Introduced in `v1.0.23.2025` (`v1.0-beta-rev7`)**.<br/><br/>(*Optional*) Indicates whether the test results summary file should include only test names for **UFT One** tests, rather than full paths. The **LoadRunner** tests are not affected. Default = `false`.<br/><br/>By default the test results summary file includes the full path of the **UFT One** test. For example: `<testcase name="C:\tests\GUITest1"`.<br/>When this parameter is set to `true`, the results show the test name only: `<testcase name="GUITest1"`. |
| `unstableAsFailure` | boolean | `true` _or_ *`false`* | **Introduced in `v1.0.29.221` (`v1.0-beta-rev9`)**.<br/><br/>(*Optional*) Indicates whether to treat an unstable test as a failure and return a non-zero exit code.<br/>`true` – The exit code for an unstable test is **Unstable**.<br/>`false` – The exit code for an unstable test is **Passed**.<br/>See more details in the [Exit Code](#fttools-exit-code) section. |  

#### <a name="alm-params-refs"></a>ALM Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The ALM parameters are used to launch tests stored in **Micro Focus Application Lifecycle Management** (**ALM**). The ALM parameters listed in the table below take effect only when the `runType` parameter is set to `Alm`.

Some additional actions are required before running ALM test sets:
1. Download and install the **ALM Connectivity Tool** from `http://{alm-server-hostname-or-ip}:{alm-server-port}/qcbin/TDConnectivity_index.html`.
2. Open the link `http://{alm-server-hostname-or-ip}:{alm-server-port}/qcbin/start_a.jsp?common=true` to install mandatory components.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`almServerUrl`** | string | http(s)://{hostname-or-ip}[:{port}]/qcbin | [**Mandatory**] The URL of the ALM server to connect to and run tests from.<br/><br/>For example, `http://10.105.32.108:8080/qcbin`. |
| **`almUsername`** | string | ALM user name | [**Mandatory**] The user name to use for the ALM connection. |
| `almPasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. Use secure means to transmit the parameter file to prevent sensitive information from being exposed.**<br/><br/>(*Optional*) The ALM connection password, encoded in base64 format. |
| **`almDomain`** | string | ALM domain name | [**Mandatory**] The domain name in which the ALM projects can be found on the ALM server. |
| **`almProject`** | string | ALM project name | [**Mandatory**] The project (under the specified domain) to open once connected to the ALM server. |
| `SSOEnabled` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether to enable the SSO mode when login to the ALM server. Default = `false`. |
| `almClientID` | string | ALM SSO client ID | [**Mandatory** if `SSOEnabled` is `true`] The client ID used together with `almApiKeySecretBasicAuth` parameter as the identifier when login in SSO mode.<br/><br/>See the online topic [API Key Management][alm-api-key-management-url] for the details to manage API keys. |
| `almApiKeySecretBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. Use secure means to transmit the parameter file to prevent sensitive information from being exposed.**<br/><br/>[**Mandatory** if `SSOEnabled` is `true`] The API key secret used together with `almClientID` parameter as the identifier when login in SSO mode, encoded in base64 format.<br/><br/>See the online topic [API Key Management][alm-api-key-management-url] for the details to manage API keys. |
| `almRunMode` | string | *`RUN_LOCAL`* _or_ `RUN_REMOTE` _or_ `RUN_PLANNED_HOST` | (*Optional*) Indicated where to run the ALM tests, on the local machine or on remote machines. Default = `RUN_LOCAL`.<br/><br/>`RUN_LOCAL`: ALM tests run on the local machine where this tool is running;<br/>`RUN_REMOTE`: ALM tests run on a the machine specified in the `almRunHost` parameter;<br/>`RUN_PLANNED_HOST` runs ALM tests on the machines configured in the ALM test set. Only the **Default** test sets are supported. |
| `almRunHost` | string | hostname _or_ IP address | [**Mandatory** if `almRunMode` is `RUN_REMOTE`] The hostname or IP address of the machine on which to run the ALM tests. Takes effect only if `almRunMode` is `RUN_REMOTE`. |
| `almTimeout` | integer | `0` to `2147483647` | (*Optional*) The number of seconds before the ALM test run times out. Default = `2147483647` (around 68 years). |
| **`TestSet{i}`** | string | The path to an ALM test set _or_<br/>an ALM folder that contains test sets | [**Mandatory**] A list of ALM paths that refer to the ALM test set or ALM folder that contains the test sets.<br/><br/>Specify multiple test sets by increasing the `{i}` which starts from `1`. For example, `TestSet1=path1`, `TestSet2=folder2`. |
| `FilterTests` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether filters need to be applied on the ALM test sets. Default = `false`. |
| `FilterByName` | string | name to filter | (*Optional*) The test name or part of a test name to use to filter the ALM test sets. Only takes effect when the `FilterTests` parameter is set to `true`. |
| `FilterByStatus` | string | {status1},{status2}, ... | (*Optional*) A comma-separated list containing the statuses to use to filter the ALM test sets. Only takes effect when the `FilterTests` parameter is set to `true`. |

#### <a name="filesystem-params-refs"></a>File System Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The File System parameters are used to launch tests stored in the file system. All the following File System parameters take effect only when the `runType` parameter is set to `FileSystem`.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`Test{i}`** | string | path to:<br/>a test folder _or_<br/>a folder contains test folders _or_<br/>a LoadRunner test file (`.lrs`) _or_<br/>a batch file that describes test folders (`.mtb`) _or_<br/>a batch file that describes tests with additional settings (`.mtbx`) | [**Mandatory**] A list of file system paths that refer to the test folders that contain the tests.<br/><br/>Specify multiple tests by increasing the `{i}` which starts from `1`. For example, `Test1=testpath1`, `Test2=folder2`, `Test3=test3.lrs`, `Test4=tests4.mtb`, `Test5=tests5.mtbx`.<br/><br/>See [.mtb File References](#mtb-file-refs) and [.mtbx File References](#mtbx-file-refs) for details. |
| `fsTimeout` | integer | `0` to `9223372036854775807` | (*Optional*) The number of seconds before the test run times out. Default = `9223372036854775807` (around 29,247 years). |
| `fsReportPath` | string | directory path | (*Optional*) The location under which to save all test reports. A dynamic subdirectory will be created for each test under this location when running the tests.<br/><br/>Default = for each test, use its own test report location. |
| `fsReportPath{i}` | string | directory path | (*Optional*) The explicit location in which to save the test report for the test specified in `Test{i}`. If both the `fsReportPath` and `fsReportPath{i}` are specified, the `fsReportPath{i}` takes precedence over the `fsReportPath`.<br/><br/>This parameter is ignored if a `.mtb` or `.mtbx` batch file is specified in `Test{i}`. |
| `fsUftRunMode` | string | `Normal` _or_ _`Fast`_ | (*Optional*) Specifies the run mode when running UFT tests. Default = `Fast` run mode. |

#### <a name="test-rerun-params-refs"></a>Test Rerun Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The Test Rerun parameters determine how to rerun the failed tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `onCheckFailedTest` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether to rerun tests if any tests failed. If set to `true`, this tool will rerun tests according to the other Test Rerun parameters. Default = `false`: don't rerun if any tests failed. |
| `testType` | string | `Rerun the entire set of tests` _or_<br/>`Rerun specific tests in the build` _or_<br/>`Rerun only failed tests` | (*Optional*) Specifies how to rerun tests. Default: No tests will be rerun if any tests failed.<br/><br/>If the value is `Rerun the entire set of tests` and any tests failed, all of the tests will be run again `x` times where `x` is the value of the `Reruns1` parameter specified. In this case, only `Reruns1` parameter takes effect and all the other `Reruns{i}` parameters are ignored.<br/><br/>If the value is `Rerun specific tests in the build` and any tests failed, the specific tests will be run `x` times where `x` is the value of the `Reruns1` parameter specified. The rerun tests are specified by the `FailedTest{i}` parameter where `i` starts from `1`. In this case, only `Reruns1` parameter takes effect and all the other `Reruns{i}` parameters are ignored.<br/><br/>If the value is `Rerun only failed tests` and any tests failed, only the failed test(s) will be rerun. The rerun times is decided by the value of the `Reruns{i}` accordingly. In this case, the `Reruns{i}` defines the rerun times for each test, respectively. For example, `Reruns1=2`, `Reruns2=1` means rerun `Test1` twice if `Test1` failed and rerun `Test2` once if `Test2` failed.  |
| `Reruns{i}` | integer | number of rerun times | (*Optional*) A list of numbers specifying how many times to rerun test(s). See the remarks of the `testType` parameter for more details.<br/><br/>*NOTE:* Currently, if the rerun times is larger than one (1), rerun will not check the test results until all reruns finish. For example, if `Reruns1=2` and `Test1` failed, the test will always rerun twice even though the first rerun passed. |
| `FailedTest{i}` | string | same as `Test{i}` parameter | (*Optional*) A list of paths specifying the test folders that contain the tests to be run when any `Test{i}` tests fail. See the remarks of the `testType` parameter for more details. |
| `CleanupTest{i}` | string | same as `Test{i}` parameter | (*Optional*) A list of paths specifying the test folders that contain the tests that perform the cleanup actions before rerunning the tests.<br/><br/>The basic logic is that if any `Test{i}` tests failed, all the `CleanupTest{i}` tests will be executed (no relationships with `Test{i}`), followed by the rerun tests. |

#### <a name="lr-params-refs"></a>LoadRunner Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The following parameters are used for **LoadRunner** tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `displayController` | integer | *`0`* _or_ `1` | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Indicates whether the controller is displayed when running LoadRunner tests. Set `1` to show the controller. Default = `0`: Do not show the controller. |
| `controllerPollingInterval` | integer | `0` to `2147483647` | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Indicates the controller polling interval, in seconds. Default = `30` seconds. |
| `PerScenarioTimeOut` | integer | `0` to `9223372036854775807` | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Indicates the timeout for each scenario, in minutes. Default = `9223372036854775807` (around 17,548,272,520,652 years) |
| `analysisTemplate` | string | file path | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) The file path to the analysis template file  used by the `LRAnalysisLauncher` tool when running the LoadRunner tests. |
| `ignoreErrorStrings` | string | multi-lines string | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) One or more error texts to ignore when running LoadRunner tests. One error string per line. |
| `SummaryDataLog` | string | `0`\|`1`;`0`\|`1`;`0`\|`1`;{num} | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Specifies the configuration of summary data log.<br/><br/>Format: Four components separated by semicolons (`;`). The first three components are all `0` or `1` which enables (`1`) or disables (`0`) _logVusersStates_, _logErrorCount_, _logTransactionStatistics_ respectively. The fourth component is a positive number represents the polling interval, in seconds.<br/><br/>For example, the value `1;0;0;30` enables _logVusersStates_, disables _logErrorCount_ and _logTransactionStatistics_, and sets polling interval to 30 seconds. |
| `ScriptRTS{i}` | string | script name | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Defines a list of scripts for which the runtime settings (attributes) are set. The placeholder `{i}` is used to define multiple scripts, starting from `1`, for example, `ScriptRTS1=sc1`, `ScriptRTS2=demo`. |
| `AdditionalAttribute{i}` | string | {script-name};{attr-name};{attr-value};{attr-description} | (*Optional*) (**FOR LOADRUNNER TESTS ONLY**) Defines a list of runtime settings (attributes) for scripts set by `ScriptRTS{i}` parameters.<br/><br/>The value consists of four components separated by semicolons (`;`). The first one spedifies the script for which the attributes are used; the next three components are: attribute name, attribute value, and attribute description.<br/><br/>For example, the value `sc1;a1;valx;this is a demo attribute` represents an attribute to be set for the script `sc1` with attribute name `a1`, value `valx`, and description `this is a demo attribute`. |

#### <a name="mc-params-refs"></a>UFT Mobile Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The following parameters are used for connecting to **Micro Focus UFT Mobile** (formerly **Mobile Center**) when running tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`MobileHostAddress`** | string | http(s)://{hostname-or-ip}[:{port}] | [**Mandatory**] The host address URL of the UFT Mobile server. |
| **`MobileUserName`** | string | user name | [**Mandatory** if MobileClientId is missing] The user name used to connect to the UFT Mobile server. |
| `MobilePasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. Use secure means to transmit the parameter file to prevent sensitive information from being exposed.**<br/><br/>(*Optional*) The password encoded in base64 format which is used to connect to the UFT Mobile server. |
| **`MobileClientId`** | string | client ID | [**Mandatory** if MobileUserName is missing] The client ID used to connect to the UFT Mobile server. |
| **`MobileSecretKeyBasicAuth`** | string | base64-encoded string | [**Mandatory** if MobileClientId is provided] **CAUTION: This key is simply encoded in base64 format which can be easily decoded by anyone. Use secure means to transmit the parameter file to prevent sensitive information from being exposed.**<br/><br/>The secret key encoded in base64 format which is used to connect to the UFT Mobile server. |
| **`MobileTenantId`** | string | MC tenant ID | [**Mandatory**] The tenant ID used to connect to the UFT Mobile server with multi-tenant mode enabled. If the multi-tenant mode is disabled on the UFT Mobile server, specify `999999999` instead. |
| `MobileUseSSL` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether to use SSL (`https` protocol) when connecting to the UFT Mobile server.<br/><br/>Specify `0` to use http protocol or `1` to use https. Default = `0` (http). |
| `MobileUseProxy` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether to use a proxy when connecting to the UFT Mobile server.<br/><br/>Specify `0` to use direct connection (no-proxy mode) or `1` to use proxy. Default = `0` (no-proxy mode). |
| `MobileProxyType` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates the type of proxy to use when connecting to the UFT Mobile server, if the `MobileUseProxy` is set to `1`.<br/><br/>Specify `0` to use an http proxy or `1` to use the system proxy. Default = `0` (http proxy).<br/><br/>For the system proxy type, proxy settings are detected by reading the system proxy settings; for the http proxy type, the proxy settings are explicitly specified by the `MobileProxySetting_`xxx parameters. |
| `MobileProxySetting_Address` | string | [http(s)://]{hostname-or-ip}[:{port}] | [**Mandatory** if `MobileUseProxy` is set to `1` and `MobileProxyType` is set to `0`] The address of the proxy.<br/><br/>Takes effect only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy). |
| `MobileProxySetting_Authentication` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether the proxy requires authentication.<br/><br/>Specify `1` to enable proxy authentication. Default = `0` (no proxy authentication).<br/><br/>Relevant only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy). |
| `MobileProxySetting_UserName` | string | proxy user name | [**Mandatory** if `MobileUseProxy` is set to `1` and `MobileProxyType` is set to `0` and `MobileProxySetting_Authentication` is set to `1`] The user name to use when connecting to the proxy server.<br/><br/>Takes effect only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy) and `MobileProxySetting_Authentication` parameter is set to `1`. |
| `MobileProxySetting_PasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. Use secure means to transmit the parameter file to prevent sensitive information from being exposed.**<br/><br/>(*Optional*) The password encoded in base64 format which is used to connect to the proxy server. |
| `mobileinfo` | string | data in JSON format | (*Optional*) This parameter correpsonds to the mobile configurations set via the **Record and Run Settings** dialog box in UFT One.<br/><br/>This parameter is optional in most cases, however, in some circumstances it might be required in order to instrcut UFT One to launch a specific mobile deivce and application before running the mobile test.<br/><br/>Although it is possible to compose the JSON string manually, it is strongly recommended to fetch the JSON string by first setting up the mobile configuration in UFT One's **Record and Run Settings** dialog box and then getting the data from the registry at `HKEY_CURRENT_USER\SOFTWARE\Mercury Interactive\QuickTest Professional\MicTest\AddIn Manager\Mobile\Startup Settings\JOB_SETTINGS`, value name `_default`. A typical JSON string could start from text `{"RnRType":-1,`... |

#### <a name="parallel-runner-params-refs"></a>ParallelRunner Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

> *Notes*: In order to run tests in parallel, ensure you have reviewed the online user guide: [Before starting parallel testing][parallel-runner-before-start].

The following parameters are used for tests run by the Micro Focus ParallelRunner.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `parallelRunnerMode` | boolean | `true` _or_ _`false`_ | (*Optional*) Indicates whether to run the tests in parallel mode with parallel runner tool. Default = `false`.<br/><br/>Once enabled (`true`), this tool launches the ParallelRunner for each test set by the `Test{i}` parameter with multiple environments (set by the `ParallelTest{i}Env{j}` parameter). For example, given `Test1=pathA`, `Test2=pathB`, this tool first launches the ParallelRunner to run `Test1` and when `Test1` is finished, this tool launches the ParallelRunner again to run `Test2`. Currently, this tool does **NOT** support running multiple tests in parallel. |
| `ParallelTest{i}Env{j}` | string | {key1}:{value1},{key2}:{value2}, ... | (*Optional*) The variables passed to the ParallelRunner in order to run tests in parallel mode.<br/><br/>A ParallelRunner variable consists of a key and the corresponding value, separated by colon (`:`). Separate multiple variables by commas (`,`).<br/><br/>The placeholder `{i}` is used to identify the test to set variables which starts from `1`, corresponding to the `Test{i}` parameter. The placeholder `{j}` is used to define more than one variable, also starts from `1`.<br/><br/>For example, the parameter `ParallelTest3Env1=browser:CHROME` defines a variable `browser:CHROME` (key=`browser`; value=`CHROME`) for the test `Test3` run by the ParallelRunner.<br/><br/>For all supported ParallelRunner variables, see the [**ParallelRunner Variables**](#parallel-runner-vars) section.<br/><br/>To define the ParallelRunner environment for a test, increase `{j}`. For example, `ParallelTest3Env1=browser:CHROME`, `ParallelTest3Env2=browser:IE` tells this tool to launch the ParallelRunner to run the `Test3` with two browser environments in parallel. |

#### <a name="non-public-params-refs"></a>Non-public Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The non-public parameters are dedicated and only used by some Micro Focus tools. These parameters are generally supported for backward compatibility. Some parameters may be obsolete in future versions.

In most cases, do not use these parameters when you run this tool with your own parameter file.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `JenkinsEnv` | string | | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameter file, set the environment variable before running this tool instead. |
| `almPassword` | string | encoded string | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameter file, use the `almPasswordBasicAuth` parameter instead.<br/><br/>If both the `almPassword` and `almPasswordBasicAuth` parameters are provided, the `almPasswordBasicAuth` parameter takes precedence over the `almPassword` parameter. |
| `almApiKeySecret` | string | encoded string | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameter file, use the `almApiKeySecretBasicAuth` parameter instead.<br/><br/>If both the `almApiKeySecret` and `almApiKeySecretBasicAuth` parameters are provided, the `almApiKeySecretBasicAuth` parameter takes precedence over the `almApiKeySecret` parameter. |
| `MobilePassword` | string | encoded string | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameter file, use the `MobilePasswordBasicAuth` parameter instead.<br/><br/>If both the `MobilePassword` and `MobilePasswordBasicAuth` parameters are provided, the `MobilePasswordBasicAuth` parameter takes precedence over the `MobilePassword` parameter. |
| `MobileSecretKey` | string | encoded string | [**Used by Micro Focus Azure DevOps plugin**] When running this tool with your own parameter file, use the `MobileSecretKeyBasicAuth` parameter instead.<br/><br/>If both the `MobileSecretKey` and `MobileSecretKeyBasicAuth` parameters are provided, the `MobileSecretKeyBasicAuth` parameter takes precedence over the `MobileSecretKey` parameter. |
| `MobileProxySetting_Password` | string | encoded string | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameter file, use the `MobileProxySetting_PasswordBasicAuth` parameter instead .<br/><br/>If both the `MobileProxySetting_Password` and `MobileProxySetting_PasswordBasicAuth` parameters are provided, the `MobileProxySetting_PasswordBasicAuth` parameter takes precedence over the `MobileProxySetting_Password` parameter. |


### <a name="mtb-file-refs"></a>.mtb File References
> Go to [Table Of Contents](#fttools-launcher-toc)

An `.mtb` file is an initialization (ini) file which describes the test paths to run. The `Test{i}` parameter is typically used to specify multiple tests in one batch file.

The initialization file includes one `[Files]` section under which a unique `NumberOfFiles` key indicates how many tests are involved, followed by one or more keys `File{i}` representing the test paths.

For example, given the parameter `Test1=tests.mtb` with the following content of `tests.mtb` file, this tool will read the content of the file and parse two test paths:

```ini
[Files]
NumberOfFiles=2
File1=C:\\tests\\test1
File2=C:\\tests\\test2
```

### <a name="mtbx-file-refs"></a>.mtbx File References
> Go to [Table Of Contents](#fttools-launcher-toc)

An `.mtbx` file is an XML file that describes the tests to run. Since this file is written in XML format, it provides more flexibility to specify the test paths together with additional settings such as test input parameters, data table, and iterations.

This tool supports expanding the string interpolations in the `.mtbx` file by replacing the well-known interpolated syntax `%xxx%` (Windows batch) and `${xxx}` (Unix shell) with the environment variables. For example, in the following sample, the interpolated string `%TEST_FOLDER%` will be expanded before reading the XML content with the value of the environment variable `TEST_FOLDER` if the environment variable is set and the real value could be `C:\tests\GUITest1` when the value of the environment variable `TEST_FOLDER` is `C:\tests`.

There are two attributes that can specify the report location: `reportPath` and `reportExactPath`. The former is used to specify a base report directory in which a dynamic directory will be created for each test. The latter explicitly defines the report directory for that test which means the report assets will be saved to that exact path. If neither is specified, the `fsReportPath` parameter in the parameter file takes effect.

If the `Iterations` XML element is specified in the `.mtbx` file, the iteration **mode** is required and shall be one of the following values: `rngIterations`, `rngAll` and `oneIteration`. If the mode is set to `rngIterations`, the `start` and `end` attributes might be also specified to define the range. See the sample below for the usage of the test iterations.

```xml
<Mtbx>
    <Test name="test1" path="%TEST_FOLDER%\GUITest1" reportPath="${REPORT_FOLDER}\GUITest1">
        <Parameter name="p1" value="123" type="int"/>
        <Parameter name="p4" value="123.4" type="float"/>
        <Parameter name="A" value="abc" type="string"/>
    </Test>
    <Test name="test2" path="%TEST_FOLDER%\GUITest2" reportExactPath="${REPORT_FOLDER}\my_reports\test2">
        <DataTable path="%TEST_FOLDER%\GUITest2\params.xlsx"/>
        <!-- run iteration 1 to iteration 3 -->
        <Iterations mode="rngIterations" start="1" end="3"/>
    </Test>
    <Test name="test3" path="%TEST_FOLDER%\GUITest3">
        <!-- run all iterations -->
        <Iterations mode="rngAll"/>
    </Test>
    <Test name="test4" path="%TEST_FOLDER%\GUITest4">
        <!-- run only one iteration -->
        <Iterations mode="oneIteration"/>
    </Test>
</Mtbx>
```

#### .mtbx XML Schema
```xml
<xs:schema attributeFormDefault="unqualified" elementFormDefault="qualified" xmlns:xs="http://www.w3.org/2001/XMLSchema">
  <xs:simpleType name="varType">
    <xs:restriction base="xs:string">
      <xs:enumeration value="Float" />
      <xs:enumeration value="String" />
      <xs:enumeration value="Any" />
      <xs:enumeration value="Boolean" />
      <xs:enumeration value="Bool" />
      <xs:enumeration value="Int" />
      <xs:enumeration value="Integer" />
      <xs:enumeration value="Number" />
      <xs:enumeration value="Password" />
      <xs:enumeration value="DateTime" />
      <xs:enumeration value="Date" />
      <xs:enumeration value="Long" />
      <xs:enumeration value="Double" />
      <xs:enumeration value="Decimal" />
      <xs:enumeration value="float" />
      <xs:enumeration value="string" />
      <xs:enumeration value="any" />
      <xs:enumeration value="boolean" />
      <xs:enumeration value="bool" />
      <xs:enumeration value="int" />
      <xs:enumeration value="integer" />
      <xs:enumeration value="number" />
      <xs:enumeration value="password" />
      <xs:enumeration value="dateTime" />
      <xs:enumeration value="datetime" />
      <xs:enumeration value="date" />
      <xs:enumeration value="long" />
      <xs:enumeration value="double" />
      <xs:enumeration value="decimal" />
    </xs:restriction>
  </xs:simpleType>
  <xs:element name="Mtbx">
    <xs:complexType>
      <xs:sequence>
        <xs:element name="Test" maxOccurs="unbounded" minOccurs="0">
          <xs:complexType>
            <xs:sequence>
              <xs:element name="Parameter" maxOccurs="unbounded" minOccurs="0">
                <xs:complexType>
                  <xs:simpleContent>
                    <xs:extension base="xs:string">
                      <xs:attribute type="xs:string" name="name" use="required"/>
                      <xs:attribute type="xs:string" name="value" use="required"/>
                      <xs:attribute type="varType" name="type" use="optional"/>
                    </xs:extension>
                  </xs:simpleContent>
                </xs:complexType>
              </xs:element>
              <xs:element name="DataTable" maxOccurs="1" minOccurs="0">
                <xs:complexType>
                  <xs:attribute type="xs:string" name="path" use="required"/>
                </xs:complexType>
              </xs:element>
              <xs:element name="Iterations" maxOccurs="1" minOccurs="0">
                <xs:complexType>
                  <xs:attribute type="xs:string" name="mode" use="required"/>
                  <xs:attribute type="xs:integer" name="start" use="optional"/>
                  <xs:attribute type="xs:integer" name="end" use="optional"/>
                </xs:complexType>
              </xs:element>
            </xs:sequence>
            <xs:attribute type="xs:string" name="name" use="optional"/>
            <xs:attribute type="xs:string" name="path" use="required"/>
            <xs:attribute type="xs:string" name="reportPath" use="optional"/>
            <xs:attribute type="xs:string" name="reportExactPath" use="optional"/>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
```

### <a name="parallel-runner-vars"></a>ParallelRunner Variables
> Go to [Table Of Contents](#fttools-launcher-toc)

In order to run tests in parallel mode, the ParallelRunner requires some settings for every test it runs. These settings are specified as one or more ParallelRunner variables by setting the `ParallelTest{i}Env{j}` parameter. For details of the `ParallelTest{i}Env{j}` parameter, see the remarks of that parameter.

#### ParallelRunner Variables For Web Tests
The parallel runner for web tests is supported in UFT One 14.50 and later.

| Variable | Values | Remarks |
| ---- | ---- | ---- |
| `browser` | Supported in UFT One **14.50** and later:<br/>`CHROME`, `IE`, `IE64`, `FIREFOX`, `FIREFOX64`<br/><br/>Supported in UFT One **14.51** and later:<br/>`SAFARI`, `EDGE`, `CHROME_HEADLESS`<br/><br/>Supported in UFT One **15.0.1** and later:<br/>`CHROMIUMEDGE` | One of the browsers to launch when running the web test. |

#### ParallelRunner Variables For Mobile Tests
The parallel runner for mobile tests is suppored in UFT One **14.03** and later.

| Variable | Values | Remarks |
| ---- | ---- | ---- |
| `deviceId` | Mobile device ID | The device ID in UFT Mobile. For example, `TA99217E5A`. |
| `manufacturerAndModel` | {manufacturer} {model} | The device manufacturer and model, separated by whitespace, for example, `motorola XT1096`. |
| `osType` | `Android`, `iOS`, `Windows Phone` | One of the values represents the device operating system. |
| `osVersion` | \[\>\|\>=\|\<\|\<=\]{version} | The device operating system version. Can be a specific version like `10.0` or a range of versions like `>=10.0`. For example: `osVersion:>=10.0`. |

### <a name="fttools-launcher-samples"></a>Samples: FTToolsLauncher Parameters File
#### <a name="fttools-launcher-sample-1"></a>Sample 1: Run one GUITest (File System)
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020162709300.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\GUITest1

# Rerun parameters
onCheckFailedTest=true
testType=Rerun only failed tests
Reruns1=1
```

#### <a name="fttools-launcher-sample-2"></a>Sample 2: Run multiple tests (File System)
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020163345091.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\GUITest1
Test2=C:\\tests\\APITest3

# Rerun parameters
onCheckFailedTest=true
testType=Rerun only failed tests
Reruns1=1
Reruns2=1
```

#### <a name="fttools-launcher-sample-3"></a>Sample 3: Run test (File System) with multiple environments in parallel
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020164009776.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\GUITest1

# Rerun parameters
onCheckFailedTest=false

# Parallel Runner parameters
parallelRunnerMode=true
ParallelTest1Env1=browser:FIREFOX
ParallelTest1Env2=browser:IE
```

#### <a name="fttools-launcher-sample-4"></a>Sample 4: Run ALM test sets
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=Alm
resultsFilename=Results18112020164301206.xml

# ALM parameters
almServerUrl=http://10.105.32.108:8080/qcbin
almUsername=john
almPasswordBasicAuth=UEBzc1dvckQ=
almDomain=DEFAULT
almProject=myproj1
almRunMode=RUN_LOCAL
almTimeout=3600
TestSet1=Root\\mydemo\\testset1
```

#### <a name="fttools-launcher-sample-5"></a>Sample 5: Run mobile test
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020165019361.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\GUITest5

# Rerun parameters
onCheckFailedTest=true
testType=Rerun only failed tests
Reruns1=1

# Mobile Center parameters
MobileHostAddress=http://10.52.108.99:8080
MobileUserName=admin@default.com
MobilePasswordBasicAuth=UEBzc1dvckQ=
MobileTenantId=999999999
```

#### <a name="fttools-launcher-sample-6"></a>Sample 6: Run multiple test with .mtb file (File System)
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020165526188.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\config\\tests1.mtb

# Rerun parameters
onCheckFailedTest=true
testType=Rerun only failed tests
Reruns1=1
```

##### tests1.mtb File
```ini
[Files]
NumberOfFiles=2
File1=C:\\tests\\GUITest1
File2=C:\\tests\\GUITest2
```

#### <a name="fttools-launcher-sample-7"></a>Sample 7: Run multiple test with .mtbx file (File System)
> Go to [Table Of Contents](#fttools-launcher-toc)

```ini
# Basic parameters
runType=FileSystem
resultsFilename=Results18112020170251990.xml

# File System parameters
fsTimeout=3600
Test1=C:\\tests\\config\\tests2.mtbx

# Rerun parameters
onCheckFailedTest=true
testType=Rerun only failed tests
Reruns1=1
```

##### tests2.mtbx File
```xml
<Mtbx>
    <Test name="test1" path="C:\tests\GUITest1">
        <Parameter name="p1" value="123" type="int"/>
        <Parameter name="p4" value="123.4" type="float"/>
    </Test>
    <Test name="test2" path="C:\tests\GUITest2">
        <DataTable path="C:\tests\GUITest2\Defaults.xls"/>
        <!-- run only one iteration -->
        <Iterations mode="oneIteration"/>
    </Test>
</Mtbx>
```

### <a name="fttools-exit-code"></a>Exit Code
Starting from `v1.0.29.221` (`v1.0-beta-rev9`), the **FTToolsLauncher** tool returns one of the following exit codes:

- **Passed: `0`**    
Indicates that the **FTToolsLauncher** tool exited without error.
- **Failed: `-1` (`0xFFFFFFFF`)**    
Indicates that either a test run failed or an error occurred.
- **PartialFailed: `-2` (`0xFFFFFFFE`)**    
Indicates that some of the tests are **Failed** while others are **Passed** or **Warning**.
- **Aborted: `-3` (`0xFFFFFFFD`)**    
Indicates that the **FTToolsLauncher** tool was aborted.
- **Unstable: `-4` (`0xFFFFFFFC`)**    
Indicates that the final result is unstable. See details below.
- **AlmNotConnected: `-5` (`0xFFFFFFFB`)** *- Introduced in `v1.0.30.1919` (`v1.0-beta-rev10`)*    
Indicates that the **FTToolsLauncher** tool failed to connect to the **ALM** server.

A test is considered unstable if:
- At least one test result is **Warning** and all others are **Passed** _or_
- One or more test results are **Failed** and after rerun all the rerun tests are **Passed** or **Warning**.

By default, the **FTToolsLauncher** tool returns the exit code **Passed** (`0`) when a test is unstable. This behavior can be changed by setting the `unstableAsFailure` parameter to `true` to return exit code **Unstable** (`-4`) which is an error code to the operating system.


### <a name="fttools-launcher-limit"></a>Limitations
In this release, the **FTToolsLauncher** tool has the following limitations:

1. When setting the rerun times to more than one, the specified number of reruns is carried out even if one rerun passes. For example, assume that Test1 is configured to be rerun twice when it failed. The test will always rerun twice, even though the first rerun of Test1 already passed.

2. When running tests in parallel mode, this tool only supports running one test with multiple environment settings in parallel and does not support running multiple tests in parallel. For example, you can run Test1 with two browsers like IE and Chrome in parallel, however, you cannot run Test1 and Test2 in parallel.


## <a name="fttools-aborter"></a>FTToolsAborter
The **FTToolsAborter** is a command-line tool that terminates any functional testing applications that are currently running tests on the same machine as this aborter tool.

This tool enables terminating the following Micro Focus functional testing applications:
- **UFT One** (formerly **Unified Functional Testing**)
- **LoadRunner** (**LR**)
- UFT ParallelRunner

This tool shall be used together with the [FTToolsLauncher](#fttools-launcher) tool. Make sure these two executable files are put in the same directory and the file name of the [FTToolsLauncher](#fttools-launcher) tool is **FTToolsLauncher.exe**.

### <a name="aborter-cmd-line-refs"></a>Command Line References
```batch
FTToolsAborter.exe <parameters file in key=value format>
```

### <a name="aborter-params-file-refs"></a>Parameter File References
The **FTToolsAborter** command-line tool requires a parameter file that describes parameters in `key=value` format, one parameter per line. A good example is the **Java Properties** file.

Special characters need to be escaped to enable the tool to properly load the paramters file. For example, if there is a parameter `Test1=C:\\tests\\GUITest1`, the path delimeters need to be escaped with `\`.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`runType`** | string | `FileSystem` _or_ `Alm` | [**Mandatory**] The type of test run to terminate. Use the same value as you did when launching tests with the **FTToolsLauncher** tool. |
| **`almRunMode`** | string | *`RUN_LOCAL`* _or_ `RUN_REMOTE` _or_ `RUN_PLANNED_HOST` | [**Mandatory** if `runType` is set to `Alm`] The machine on which the tests are running. Use the same value as you did when launching tests with **FTToolsLauncher** tool.<br/><br/>If the mode is `RUN_REMOTE` or `RUN_PLANNED_HOST`, this aborter tool will **NOT** terminate any applications since tests are running remotely. |

### <a name="aborter-params-file-samples"></a>Samples: FTToolsAborter Parameter File
#### <a name="aborter-params-file-sample-1"></a>Sample 1: Abort tests (File System)
```ini
runType=FileSystem
```

#### <a name="aborter-params-file-sample-2"></a>Sample 2: Abort ALM tests
```ini
runType=Alm
almRunMode=RUN_LOCAL
```


## <a name="fttools-lr-analysis-launcher"></a>LRAnalysisLauncher
The **LRAnalysisLauncher** is a command-line tool that analyzes the LoadRunner scenario run results (`.lrr`) and generates the analysis files (`.lra`) and HTML result file (`.html`).

This tool shall be used together with the [FTToolsLauncher](#fttools-launcher) tool and it is commonly triggered by [FTToolsLauncher](#fttools-launcher) while running LoadRunner tests.

Make sure the LRAnalysisLauncher tool and the [FTToolsLauncher](#fttools-launcher) tool are put in the same directory and the file name of the LRAnalysisLauncher tool is **LRAnalysisLauncher.exe**.

In order to run LoadRunner scenario successfully,  the LoadRunner **bin** folder (ie. *C:\Program Files (x86)\Micro Focus\LoadRunner\bin*) shall be added to system **Path** environment variable if it is not properly set automatically.


## <a name="report-converter"></a>ReportConverter
The **ReportConverter** command-line tool is used to convert the UFT One test reports to other report formats like JUnit XML.

### <a name="report-converter-notes"></a>Important Notes
- This tool can only convert the report XML file `run_results.xml` which is generated in UFT One with the **HTML Report** report format option enabled.
- This tool doesn't require the installation of UFT One.

### <a name="report-converter-cmd-line-refs"></a>Command Line References
```batch
ReportConverter <options> <input-folder>
```

To convert the UFT One report XML file to the JUnit XML report:
```batch
ReportConverter -j <output-junit-file> <uft-test-report-folder>
```

As of ReportConverter `1.0.20.2003`, you can convert multiple UFT One report XML files to an aggregate JUnit XML report:
```batch
ReportConverter -j <output-junit-file> --aggregate <uft-test-report-folder-1> <uft-test-report-folder-2> ...
```

Run `ReportConverter -h` for the details of the command usages.

### <a name="report-converter-samples"></a>Samples
Sample 1: In this example, ReportConverter reads the `run_results.xml` file in the specified folder `GUITest1\Res2\Report` and converts the report to the JUnit XML file at `out\junit_report.xml`.

```batch
ReportConverter -j "out\junit_report.xml" "GUITest1\Res2\Report"
```

Sample 2: In this example, ReportConverter reads the `run_results.xml` files in the two specified folders and converts those two reports to one JUnit XML file at `out\junit_report.xml`.

```batch
ReportConverter -j "out\junit_report.xml" --aggregate "GUITest1\Res2\Report" "APITest5\Report3"
```


[alm-api-key-management-url]: https://admhelp.microfocus.com/alm/en/latest/online_help/Content/Admin/api_keys_toc.htm
[parallel-runner-before-start]: https://admhelp.microfocus.com/uft/en/latest/UFT_Help/Content/User_Guide/parallel-test-runs.htm#mt-item-1
[msdoc-list-of-langauge-region-names-supported-by-windows]: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/a9eac961-e77d-41a6-90a5-ce1a8b0cdb9c
[bcp47-url]: https://tools.ietf.org/html/bcp47
