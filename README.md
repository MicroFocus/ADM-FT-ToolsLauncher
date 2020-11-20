# Micro Focus Automation Tools
The **Micro Focus Automation Tools** (**FTTools**) contains the tools which are used to run the automation tests by launching the Micro Focus functional testing applications such as **Unified Functional Testing** (**UFT**) and **Load Runner** etc.

The following tools are available:
- [FTToolsLauncher](#fttools-launcher)
- [FTToolsAborter](#fttools-aborter)

## <a name="fttools-launcher"></a>FTToolsLauncher
The **FTToolsLauncher** is a command-line tool which launches the functional testing application and run tests.

This tool allows to run one or more tests stored in the one of the following storages:
- **Unified Functional Testing** (**UFT**) tests:
    * GUI/API tests stored in the file system
    * GUI/API tests and test sets stored in **Micro Focus Application Lifecycle Management** (**ALM**)
    * GUI tests in parallel mode stored in the file system
- **Load Runner** (**LR**) tests

### <a name="fttools-launcher-toc"></a>Table Of Contents
- [Command Line References](#cmd-line-refs)
- [Parameters File References](#params-file-refs)
    * [Basic Parameters](#basic-params-refs)
    * [ALM Parameters](#alm-params-refs)
    * [File System Parameters](#filesystem-params-refs)
    * [Test Rerun Parameters (File System Only)](#test-rerun-params-refs)
    * [Load Runner Parameters (File System Only)](#lr-params-refs)
    * [Mobile Center Parameters (File System Only)](#mc-params-refs)
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
    * [Sample 5: Run mobile test (File System)](#fttools-launcher-sample-5)
    * [Sample 6: Run multiple test with .mtb file (File System)](#fttools-launcher-sample-6)
    * [Sample 7: Run multiple test with .mtbx file (File System)](#fttools-launcher-sample-7)

### <a name="cmd-line-refs"></a>Command Line References
> Go to [Table Of Contents](#fttools-launcher-toc)

```batch
FTToolsLauncher.exe -paramfile <a file in key=value format>
```

### <a name="params-file-refs"></a>Parameters File References
> Go to [Table Of Contents](#fttools-launcher-toc)

The **FTToolsLauncher** command-line tool requires a parameters file which describes in `key=value` format in lines. A good example is the **Java Properties** file.

The characters shall be escaped so that the parameters file can be loaded by this tool properly. For example, if there is a parameter `Test1=C:\\tests\\GUITest1`, the path delimeters need to be escaped with `\`.

All the parameters are devided into several groups:
* [Basic Parameters](#basic-params-refs)
* [ALM Parameters](#alm-params-refs)
* [File System Parameters](#filesystem-params-refs)
* [Test Rerun Parameters (File System Only)](#test-rerun-params-refs)
* [Load Runner Parameters (File System Only)](#lr-params-refs)
* [Mobile Center Parameters (File System Only)](#mc-params-refs)
* [Parallel Runner Parameters (File System Only)](#parallel-runner-params-refs)
* [Non-public Parameters](#non-public-params-refs)

#### <a name="basic-params-refs"></a>Basic Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`runType`** | string | `FileSystem` _or_ `Alm` | [**Mandatory**] Specify the storage that the test assets are stored.<br/><br/>`FileSystem` for UFT GUI/API and Load Runner tests stored in the file system.<br/>`Alm` for UFT GUI/API tests stored in the **Micro Focus Application Lifecycle Management** (**ALM**) server. |
| **`resultsFilename`** | string | file name _or_ file path | [**Mandatory**] The file name or file path in which the test(s) results shall be saved. If the file name is a relative path, the current workspace is assumed to prepend when creating this results file.<br/><br/>Although the file name can consist of arbitrary characters, there is a file name convention which can be parsed by this tool to automatically detect the value of the `uniqueTimeStamp` parameter.<br/>The name convention is: `Results<time-stamp>.xml`. For example, given the file name `Results27112019073540752.xml` the uniqueTimeStamp is detected as `27112019073540752`. See the remarks of `uniqueTimeStamp` parameter for more details. |
| `uniqueTimeStamp` | string | local date time in `ddMMyyyyHHmmssfff` format | (*Optional*) The time stamp used to create additional unique files by this tool.<br/><br/>The date time is formatted in a particular pattern `ddMMyyyyHHmmssfff` where `dd` is two-digit day (`01`-`31`), `MM` is two-digit month (`01`-`12`), `yyyy` is 4-digit year (ie. `2020`), `HH` is 24-hour (`00`-`23`), `mm` is two-digit minute (`00`-`59`), `ss` is two-digit second (`00`-`60`, `60` is dedicated for leap second) and `fff` is three-digit millisecond (`000`-`999`).<br/><br/>If this parameter is not given, the tool will first try to parse the `resultsFilename` parameter to detect the time stamp and if failed the current local date time will be used by default. |

#### <a name="alm-params-refs"></a>ALM Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The ALM parameters are used to launch the tests which are stored in the **Micro Focus Application Lifecycle Management** (**ALM**) server. All the following ALM parameters take effect only when the `runType` parameter is set to `Alm`.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`almServerUrl`** | string | http(s)://{hostname-or-ip}[:{port}]/qcbin | [**Mandatory**] The ALM server URL to connect and run tests.<br/><br/>For example, `http://10.105.32.108:8080/qcbin`. |
| **`almUsername`** | string | ALM user name | [**Mandatory**] The user name to connect to the ALM server. |
| `almPassword` | string | encoded string with AES algorithm | (*Optional*) (**FOR CI PLUGINS ONLY**) This parameter is encoded by the AES cryptographic algorithm (*AES/CBC/PKCS5Padding*) with a special key dedicated for CI plugins in order to connect to the ALM server.<br/><br/>It is not possible to generate the encrypted password without the secret key, please use `almPasswordBasicAuth` parameter instead if you need to run this tool with your own parameters file.<br/><br/>If both the `almPassword` and `almPasswordBasicAuth` parameters are given, the `almPasswordBasicAuth` parameter takes precedence over the `almPassword` parameter. |
| `almPasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. You need to transmit the parameters file by all secure means to prevent the sensitive information from being exposed.**<br/><br/>(*Optional*) The password encoded in base64 format which is used to connect to ALM server.<br/><br/>If both the `almPassword` and `almPasswordBasicAuth` parameters are given, the `almPasswordBasicAuth` parameter takes precedence over the `almPassword` parameter. |
| **`almDomain`** | string | ALM domain name | [**Mandatory**] The domain name in which the ALM projects can be found on the ALM server. |
| **`almProject`** | string | ALM project name | [**Mandatory**] The project (under the specific domain) to be opened once connected to the ALM server. |
| `SSOEnabled` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether enables the SSO mode when login to the ALM server. If omitted, defaults to `false`. |
| `almClientID` | string | ALM SSO client ID | [**Mandatory** if `SSOEnabled` is `true`] The client ID used together with `almApiKeySecret` parameter as the identifier when login in SSO mode.<br/><br/>See the online topic [API Key Management][alm-api-key-management-url] |
| `almApiKeySecret` | string | ALM SSO API key secret | [**Mandatory** if `SSOEnabled` is `true`] The API key secret used together with `almClientID` parameter as the identifier when login in SSO mode.<br/><br/>See the online topic [API Key Management][alm-api-key-management-url] |
| `almRunMode` | string | *`RUN_LOCAL`* _or_ `RUN_REMOTE` _or_ `RUN_PLANNED_HOST` | (*Optional*) Decides where the ALM tests shall be run, on the local machine or remote machines. If omitted, defaults to `RUN_LOCAL`.<br/><br/>`RUN_LOCAL` means ALM tests run on the local machine where this tool is running;<br/>`RUN_REMOTE` indicates the ALM tests run on a specific machine decided by the `almRunHost` parameter;<br/>`RUN_PLANNED_HOST` runs ALM tests on the machine(s) configured in the ALM server. |
| `almRunHost` | string | hostname _or_ IP address | [**Mandatory** if `almRunMode` is `RUN_REMOTE`] The hostname or IP address of the machine on which the ALM tests shall be run. |
| `almTimeout` | integer | `0` to `2147483647` | (*Optional*) The number of seconds before the ALM tests run timeout. If omitted, defaults to `2147483647` which is around 68 years. |
| **`TestSet{i}`** | string | a ALM test set path _or_<br/>ALM folder path contains test sets | [**Mandatory**] A list of ALM paths refer to the ALM test set or ALM folder that contains the test set(s).<br/><br/>Multiple test sets are given by increasing the `{i}` which starts from `1`. For example, `TestSet1=path1`, `TestSet2=folder2`. |
| `FilterTests` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether the filter(s) need to be applied on the ALM test sets. If omitted, defaults to `false`. |
| `FilterByName` | string | name to filter | (*Optional*) A string represents the name or portion of the name of a test to filter the ALM test sets. Only takes effect when the `FilterTests` parameter is set to `true`. |
| `FilterByStatus` | string | {status1},{status2}, ... | (*Optional*) A comma-separated list contains the status(es) to filter the ALM test sets. Only takes effect when the `FilterTests` parameter is set to `true`. |

#### <a name="filesystem-params-refs"></a>File System Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The File System parameters are used to launch the tests which are stored in the file system. All the following File System parameters take effect only when the `runType` parameter is set to `FileSystem`.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`Test{i}`** | string | path to:<br/>a test folder _or_<br/>a folder contains test folders _or_<br/>a Load Runner test file (`.lrs`) _or_<br/>a batch file describes test folders (`.mtb`) _or_<br/>a batch file describes tests with additional settings (`.mtbx`) | [**Mandatory**] A list of file system paths refer to the test folder(s) that contains the test(s).<br/><br/>Multiple tests are given by increasing the `{i}` which starts from `1`. For example, `Test1=testpath1`, `Test2=folder2`, `Test3=test3.lrs`, `Test4=tests4.mtb`, `Test5=tests5.mtbx`.<br/><br/>See [.mtb File References](#mtb-file-refs) and [.mtbx File References](#mtbx-file-refs) for details. |
| `fsTimeout` | integer | `0` to `9223372036854775807` | (*Optional*) The number of seconds before the tests run timeout. If omitted, defaults to `9223372036854775807` which is around 29,247 years. |
| `fsReportPath` | string | directory path | (*Optional*) The custom location in which all the test reports are saved. If omitted, use default test report location for each test. |
| `fsUftRunMode` | string | `Normal` _or_ _`Fast`_ | (*Optional*) Decides the run mode when running UFT tests. If omitted, defaults to `Fast` run mode. |

#### <a name="test-rerun-params-refs"></a>Test Rerun Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The Test Rerun parameters decide how to rerun the failed tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `onCheckFailedTest` | boolean | `true` _or_ *`false`* | (*Optional*) Indicates whether reruns test(s) if any tests failed. If set to `true`, this tool will rerun test(s) according to the other Test Rerun parameters. If omitted, defaults to `false` which means don't rerun if any tests failed. |
| `testType` | string | `Rerun the entire set of tests` _or_<br/>`Rerun specific tests in the build` _or_<br/>`Rerun only failed tests` | (*Optional*) Decides how to rerun tests. If omitted, no tests will be rerun if any tests failed.<br/><br/>If the value is `Rerun the entire set of tests` and any tests failed, all of the tests will be run again `x` times where `x` is the value of the `Reruns1` parameter specified. In this case, only `Reruns1` parameter takes effect and all the other `Reruns{i}` parameters are ignored.<br/><br/>If the value is `Rerun specific tests in the build` and any tests failed, the specific test(s) will be run `x` times where `x` is the value of the `Reruns1` parameter specified. The rerun tests are specified by the `FailedTest{i}` parameter where `i` starts from `1`. In this case, only `Reruns1` parameter takes effect and all the other `Reruns{i}` parameters are ignored.<br/><br/>If the value is `Rerun only failed tests` and any tests failed, only the failed test(s) will be rerun. The rerun times is decided by the value of the `Reruns{i}` accordingly. In this case, the `Reruns{i}` defines the rerun times for each test, respectively. For example, `Reruns1=2`, `Reruns2=1` means rerun `Test1` twice if `Test1` failed and rerun `Test2` once if `Test2` failed.  |
| `Reruns{i}` | integer | number of rerun times | (*Optional*) A list of numbers represents how many times shall rerun test(s). See the remarks of the `testType` parameter for more details.<br/><br/>*NOTE:* Currently, if the rerun times is larger than one (1), rerun will not check the tests result before all rerun finished. For example, if `Reruns1=2` and `Test1` failed, the test will always rerun twice even though the first rerun passed. |
| `FailedTest{i}` | string | same as `Test{i}` parameter | (*Optional*) A list of paths refer to the test folder that contains the test(s) to be run when any `Test{i}` tests failed. See the remarks of the `testType` parameter for more details. |
| `CleanupTest{i}` | string | same as `Test{i}` parameter | (*Optional*) A list of paths refer to the test folder that contains the test(s) which performs the cleanup actions before rerunning the tests.<br/><br/>The basic logic is that if any `Test{i}` tests failed, all the `CleanupTest{i}` tests will be executed (no relationships with `Test{i}`), followed by the rerun tests. |

#### <a name="lr-params-refs"></a>Load Runner Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The following parameters are dedicated for **Load Runner** tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `displayController` | integer | *`0`* _or_ `1` | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Indicates whether shows the controller when running Load Runner tests. Set `1` to show the controller. If omitted, defaults to `0` which does not show the controller. |
| `controllerPollingInterval` | integer | `0` to `2147483647` | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Indicates the controller polling interval, in seconds. If omitted, defaults to `30` seconds. |
| `PerScenarioTimeOut` | integer | `0` to `9223372036854775807` | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Indicates the timeout for each scenario, in minutes. If omitted, defaults to `9223372036854775807` which is around 17,548,272,520,652 years. |
| `analysisTemplate` | string | file path | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) The file path to the analysis template file which is used by the `LRAnalysisLauncher` tool when running the Load Runner tests. |
| `ignoreErrorStrings` | string | multi-lines string | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) One or more error texts to be ignored when running Load Runner tests. One error string each line. |
| `SummaryDataLog` | string | `0`\|`1`;`0`\|`1`;`0`\|`1`;{num} | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Specifies the configuration of summary data log.<br/><br/>The valid format of the value shall be four components separated by semicolon (`;`). The first three components are all `0` or `1` which enables (`1`) or disables (`0`) _logVusersStates_, _logErrorCount_, _logTransactionStatistics_ respectively. The fourth component is a positive number represents the polling interval, in seconds.<br/><br/>For example, given value `1;0;0;30` indicates that enables _logVusersStates_, disables _logErrorCount_ and _logTransactionStatistics_, sets polling interval to 30 seconds. |
| `ScriptRTS{i}` | string | script name | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Defines a list of scripts for which the runtime settings (attributes) are set. The placeholder `{i}` is used to define multiple scripts, starts from `1`, for example, `ScriptRTS1=sc1`, `ScriptRTS2=demo`. |
| `AdditionalAttribute{i}` | string | {script-name};{attr-name};{attr-value};{attr-description} | (*Optional*) (**FOR LOAD RUNNER TESTS ONLY**) Defines a list of runtime settings (attributes) for scripts set by `ScriptRTS{i}` parameters.<br/><br/>The value consists of four components separated by semicolon (`;`). The first one identifies the script for which the attributes shall be applied; the next three components are: attribute name, attribute value and attribute description.<br/><br/>For example, given value `sc1;a1;valx;this is demo attribute` represents an attribute to be set for the script `sc1` with attribute name `a1`, value `valx` and description `this is demo attribute`. |

#### <a name="mc-params-refs"></a>Mobile Center Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The following parameters are dedicated for connecting to **Micro Focus UFT Mobile** (**Mobile Center**) server when running tests.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`MobileHostAddress`** | string | http(s)://{hostname-or-ip}[:{port}] | [**Mandatory**] The host address URL of the Mobile Center server. |
| **`MobileUserName`** | string | user name | [**Mandatory**] The user name used to connect to the Mobile Center server. |
| `MobilePassword` | string | encoded string in AES algorithm | (*Optional*) (**FOR CI PLUGINS ONLY**) This parameter is encoded by the AES cryptographic algorithm (*AES/CBC/PKCS5Padding*) with a special key dedicated for CI plugins in order to connect to the Mobile Center server.<br/><br/>It is not possible to generate the encrypted password without the secret key, please use `MobilePasswordBasicAuth` parameter instead if you need to run this tool with your own parameters file.<br/><br/>If both the `MobilePassword` and `MobilePasswordBasicAuth` parameters are given, the `MobilePasswordBasicAuth` parameter takes precedence over the `MobilePassword` parameter. |
| `MobilePasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. You need to transmit the parameters file by all secure means to prevent the sensitive information from being exposed.**<br/><br/>(*Optional*) The password encoded in base64 format which is used to connect to the Mobile Center server.<br/><br/>If both the `MobilePassword` and `MobilePasswordBasicAuth` parameters are given, the `MobilePasswordBasicAuth` parameter takes precedence over the `MobilePassword` parameter. |
| **`MobileTenantId`** | string | MC tenant ID | [**Mandatory**] The tenant ID which is used to connect to the Mobile Center server. |
| `MobileUseSSL` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether uses SSL (`https` protocol) when connecting to the Mobile Center server.<br/><br/>Specify `0` to use default `http` protocol or `1` to use `https`. If omitted, defaults to `0` which is `http`. |
| `MobileUseProxy` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether uses proxy when connecting to the Mobile Center server.<br/><br/>Specify `0` to use direct connection (no-proxy mode) or `1` to use proxy. If omitted, defaults to `0` which is no-proxy mode. |
| `MobileProxyType` | integer | _`0`_ _or_ `1` | (*Optional*) Decides the type of the proxy to be used when connecting to the Mobile Center server, if the `MobileUseProxy` is set to `1`.<br/><br/>Specify `0` to use `http` proxy or `1` to use `system` proxy. If omitted, defaults to `0` which is `http` proxy.<br/><br/>If the proxy type is `system`, the proxy settings are detected by reading the system proxy settings; if the proxy type is `http`, the proxy settings are explicitly specified by the `MobileProxySetting_`xxx parameters. |
| `MobileProxySetting_Address` | string | [http(s)://]{hostname-or-ip}[:{port}] | [**Mandatory** if `MobileUseProxy` is set to `1` and `MobileProxyType` is set to `0`] The address of the proxy.<br/><br/>Takes effect only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy). |
| `MobileProxySetting_Authentication` | integer | _`0`_ _or_ `1` | (*Optional*) Indicates whether the proxy requires authentication.<br/><br/>Specify `1` to enable proxy authentication. If omitted, defaults to `0` which disables the proxy authentication.<br/><br/>Takes effect only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy). |
| `MobileProxySetting_UserName` | string | proxy user name | [**Mandatory** if `MobileUseProxy` is set to `1` and `MobileProxyType` is set to `0` and `MobileProxySetting_Authentication` is set to `1`] The user name used to authenticate when connecting to the proxy server.<br/><br/>Takes effect only when the `MobileUseProxy` parameter is set to `1` (use proxy) and `MobileProxyType` parameter is set to `0` (http proxy) and `MobileProxySetting_Authentication` parameter is set to `1`. |
| `MobileProxySetting_Password` | string | encoded string in AES algorithm | (*Optional*) (**FOR CI PLUGINS ONLY**) This parameter is encoded by the AES cryptographic algorithm (*AES/CBC/PKCS5Padding*) with a special key dedicated for CI plugins in order to connect to the proxy server.<br/><br/>It is not possible to generate the encrypted password without the secret key, please use `MobileProxySetting_PasswordBasicAuth` parameter instead if you need to run this tool with your own parameters file.<br/><br/>If both the `MobileProxySetting_Password` and `MobileProxySetting_PasswordBasicAuth` parameters are given, the `MobileProxySetting_PasswordBasicAuth` parameter takes precedence over the `MobileProxySetting_Password` parameter. |
| `MobileProxySetting_PasswordBasicAuth` | string | base64-encoded string | **CAUTION: This password is simply encoded in base64 format which can be easily decoded by anyone. You need to transmit the parameters file by all secure means to prevent the sensitive information from being exposed.**<br/><br/>(*Optional*) The password encoded in base64 format which is used to connect to the proxy server.<br/><br/>If both the `MobileProxySetting_Password` and `MobileProxySetting_PasswordBasicAuth` parameters are given, the `MobileProxySetting_PasswordBasicAuth` parameter takes precedence over the `MobileProxySetting_Password` parameter. |

#### <a name="parallel-runner-params-refs"></a>Parallel Runner Parameters (File System Only)
> Go to [Table Of Contents](#fttools-launcher-toc)

The following parameters are dedicated for tests run by the Micro Focus parallel runner tool.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `parallelRunnerMode` | boolean | `true` _or_ _`false`_ | (*Optional*) Indicates whether runs the test(s) in parallel mode with parallel runner tool. If omitted, defaults to `false`.<br/><br/>Once enabled (`true`), this tool launches the parallel runner tool for each test set by the `Test{i}` parameter with multiple environments (set by the `ParallelTest{i}Env{j}` parameter). For example, given `Test1=pathA`, `Test2=pathB`, this tool first launches the parallel runner tool to run `Test1` and once `Test1` finished this tool then launches the parallel runner tool again to run `Test2`. Currently, this tool does **NOT** support running multiple tests in parallel. |
| `ParallelTest{i}Env{j}` | string | {key1}:{value1},{key2}:{value2}, ... | (*Optional*) The variable(s) passed to the parallel runner tool in order to run tests in parallel mode.<br/><br/>A parallel runner variable consists of a key and the corresponding value, separated by colon (`:`). If there are multiple variables, splits with comma (`,`).<br/><br/>The placeholder `{i}` is used to identify the test to set variables which starts from `1`, corresponding to the `Test{i}` parameter. The placeholder `{j}` is used to define more than one variable, also starts from `1`.<br/><br/>For example, the parameter `ParallelTest3Env1=browser:CHROME` defines a variable `browser:CHROME` (key=`browser`; value=`CHROME`) for the test `Test3` run by the parallel runner tool.<br/><br/>For all supported parallel runner variables, see the [**Parallel Runner Variables**](#parallel-runner-vars) section.<br/><br/>To define the parallel runner environment for a test, increase `{j}`. For example, given `ParallelTest3Env1=browser:CHROME`, `ParallelTest3Env2=browser:IE` tells this tool to launch the parallel runner tool to run the `Test3` with two browser environments in parallel. |

#### <a name="non-public-params-refs"></a>Non-public Parameters
> Go to [Table Of Contents](#fttools-launcher-toc)

The non-public parameters are dedicated and only used by some Micro Focus tools. These parameters are generally supported for backward compatibility. Some parameters may be obsoleted in future versions.

In most cases, do not use these parameters when you need to run this tool standalone with your own parameters file.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| `JenkinsEnv` | string | | [**Used by Micro Focus Jenkins plugin**] When running this tool with your own parameters file, set environment variable before running this tool instead. |
| `mobileinfo` | string | data in JSON format | [**Used by Micro Focus CI plugins**] When running this tool with your own parameters file, set `MobileInfo_`xxx parameters instead. |


### <a name="mtb-file-refs"></a>.mtb File References
> Go to [Table Of Contents](#fttools-launcher-toc)

A `.mtb` file is an initialization (ini) file which describes the test paths to be run. When setting in the `Test{i}` parameter, it is typically used to specify multiple tests in one batch file.

The content of the initialization file shall include one section `[Files]` under which there is a unique key `NumberOfFiles` indicates how many tests are involved, followed by one or more keys `File{i}` represents the test path(s).

For example, given the parameter `Test1=tests.mtb` with the following content of `tests.mtb` file, this tool will read the content of the file and parse two test paths:

```ini
[Files]
NumberOfFiles=2
File1=C:\\tests\\test1
File2=C:\\tests\\test2
```

### <a name="mtbx-file-refs"></a>.mtbx File References
> Go to [Table Of Contents](#fttools-launcher-toc)

A `.mtbx` file is a XML file which describes the tests to be run. Since this file is written in XML format, it provides more flexibilities to specify the test paths together additional settings such as test input parameters, data table and iterations.

This tool supports expanding the string interpolations in the `.mtbx` file by replacing the well-known interpolated syntax `%xxx%` (Windows batch) and `${xxx}` (Unix shell) with the environment variables. For example, in the following sample, the interpolated string `%TEST_FOLDER%` will be expanded before reading the XML content with the value of the environment variable `TEST_FOLDER` if the environment variable is set and the real value could be `C:\tests\GUITest1` when the value of the environment variable `TEST_FOLDER` is `C:\tests`.

If the `Iterations` XML element is specified in the `.mtbx` file, the iteration **mode** is required and shall be one of the following values: `rngIterations`, `rngAll` and `oneIteration`. If the mode is set to `rngIterations`, the `start` and `end` attributes might be also specified to define the range. See the sample below for the usage of the test iterations.

```xml
<Mtbx>
    <Test name="test1" path="%TEST_FOLDER%\GUITest1" reportPath="${REPORT_FOLDER}\GUITest1">
        <Parameter name="p1" value="123" type="int"/>
        <Parameter name="p4" value="123.4" type="float"/>
        <Parameter name="A" value="abc" type="string"/>
    </Test>
    <Test name="test2" path="%TEST_FOLDER%\GUITest2">
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
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
```

### <a name="parallel-runner-vars"></a>Parallel Runner Variables
> Go to [Table Of Contents](#fttools-launcher-toc)

In order to run test(s) in parallel mode, the parallel runner tool requires some mandatory settings for every test to be run. These settings are specified as one or more parallel runner variables by setting the `ParallelTest{i}Env{j}` parameter. For details of the `ParallelTest{i}Env{j}` parameter, see the remarks of that parameter.

#### Parallel Runner Variables For Web Tests
| Variable | Values | Remarks |
| ---- | ---- | ---- |
| `browser` | `IE`, `IE64`, `CHROME`, `FIREFOX`, `FIREFOX64` | One of the browsers to be launched when running the test. |

#### Parallel Runner Variables For Mobile Tests
| Variable | Values | Remarks |
| ---- | ---- | ---- |
| `deviceId` | Mobile device ID | The device ID in Mobile Center. For example, `TA99217E5A`. |
| `manufacturerAndModel` | {manufacturer} {model} | The device manufacturer and model, separated by whitespace, for example, `motorola XT1096`. |
| `osType` | `Android`, `iOS`, `Windows Phone` | One of the values represents the device operating system. |
| `osVersion` | \[\>\|\>=\|\<\|\<=\]{version} | The device operating system version. Could be a specific version like `10.0` or a range of versions like `>=10.0`. For example: `osVersion:>=10.0`. |

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
almDomain=default
almProject=myproj1
almRunMode=RUN_LOCAL
almTimeout=3600
TestSet1=Root/mydemo/testset1
```

#### <a name="fttools-launcher-sample-5"></a>Sample 5: Run mobile test (File System)
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
MobileHostAddress=http://10.55.192.7:12355
MobileUserName=mcuser
MobilePasswordBasicAuth=UEBzc1dvckQ=
MobileTenantId=TN30592177
MobileUseSSL=1
MobileUseProxy=1
MobileProxyType=1
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


## <a name="fttools-aborter"></a>FTToolsAborter
The **FTToolsAborter** is a command-line tool which terminates the running functional testing applications that are still executing the tests on the machine where this aborter tool is running.

This tool allows to terminate the following Micro Focus functional testing applications:
- **Unified Functional Testing** (**UFT**)
- **Load Runner** (**LR**)
- UFT parallel runner tool

### <a name="aborter-cmd-line-refs"></a>Command Line References
```batch
FTToolsAborter.exe -paramfile <a file in key=value format>
```

### <a name="aborter-params-file-refs"></a>Parameters File References
The **FTToolsAborter** command-line tool requires a parameters file which describes in `key=value` format in lines. A good example is the **Java Properties** file.

The characters shall be escaped so that the parameters file can be loaded by this tool properly. For example, if there is a parameter `Test1=C:\\tests\\GUITest1`, the path delimeters need to be escaped with `\`.

| Name | Type | Value | Remarks |
| ---- | ---- | ---- | ---- |
| **`runType`** | string | `FileSystem` _or_ `Alm` | [**Mandatory**] The same value when launching tests by using **FTToolsLauncher** tool so that this aborter tool knows which application shall be terminated. |
| **`almRunMode`** | string | *`RUN_LOCAL`* _or_ `RUN_REMOTE` _or_ `RUN_PLANNED_HOST` | [**Mandatory** if `runType` is set to `Alm`] The same value when launching tests by using **FTToolsLauncher** tool so that this aborter tool knows which application shall be terminated.<br/><br/>If the mode is `RUN_REMOTE` or `RUN_PLANNED_HOST`, this aborter tool will **NOT** terminate any applications since tests are running remotely. |

### <a name="aborter-params-file-samples"></a>Samples: FTToolsAborter Parameters File
#### <a name="aborter-params-file-sample-1"></a>Sample 1: Abort tests (File System)
```ini
runType=FileSystem
```

#### <a name="aborter-params-file-sample-2"></a>Sample 2: Abort ALM tests
```ini
runType=Alm
almRunMode=RUN_LOCAL
```


[alm-api-key-management-url]: https://admhelp.microfocus.com/alm/en/latest/online_help/Content/Admin/api_keys_toc.htm
