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

using HpToolsLauncher.Common;
using HpToolsLauncher.ParallelTestRunConfiguraion;
using HpToolsLauncher.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace HpToolsLauncher.ParallelRunner
{
    [Serializable]
    public class ParallelRunnerConfigurationException : Exception
    {
        public ParallelRunnerConfigurationException(string message) : base(message)
        {
        }

        public ParallelRunnerConfigurationException(string message, Exception innerException) : base(message, innerException)
        { }
    };

    public enum EnvironmentType
    {
        WEB,
        MOBILE,
        UNKNOWN
    }

    public class ParallelRunnerEnvironmentUtil
    {
        // the list of supported browsers
        private static readonly IList<string> BrowserNames = new List<string>
        {
            "CHROME",
            "IE",
            "IE64",
            "FIREFOX",
            "FIREFOX64",
            "SAFARI",           // UFT versions 14.51 and later
            "EDGE",             // UFT versions 14.51 and later
            "CHROME_HEADLESS",  // UFT versions 14.51 and later
            "CHROMIUMEDGE"      // UFT One 15.0.1 or later
        }.AsReadOnly();

        // environment specific constants
        private const char EnvironmentTokenSeparator = ',';
        private const char EnvironmentKeyValueSeparator = ':';

        // environment keys
        private const string DeviceIdKey = "deviceId";
        private const string ManufacturerAndModelKey = "manufacturerAndModel";
        private const string ManufacturerKey = "manufacturer";
        private const string ModelKey = "model";
        private const string OsVersionKey = "osVersion";
        private const string OsTypeKey = "osType";
        private const string BrowserKey = "browser";

        // parallel runner config specific constants
        private const string WebLab = "LocalBrowser";
        private const string MobileCenterLab = "MobileCenter";
        private const string ACCESS_KEY_FORMAT = "client={0};secret={1};tenant={2}";

        // the list of mobile properties
        private static readonly IList<string> MobileProperties = new List<string>
        {
            DeviceIdKey,
            ManufacturerAndModelKey,
            ManufacturerKey,
            ModelKey,
            OsVersionKey,
            OsTypeKey,
        }.AsReadOnly();

        /// <summary>
        /// Parse the environment string and return a dictionary containing the values.
        /// </summary>
        /// <param name="environment"> The jenkins environment string.</param>
        /// <returns> the dictionary containing the key/value pairs</returns>
        public static Dictionary<string, string> GetEnvironmentProperties(string environment)
        {
            Dictionary<string, string> dictionary = [];

            if (string.IsNullOrEmpty(environment))
                return dictionary;

            // the environment could be : "deviceId : 12412"
            // or "manufacturerAndModel: Samsung S6, osVersion: > 6"
            // or for web: "browser: Chrome"

            // the environment string is case-sensitive
            environment = environment.Trim();

            // we get the key value pairs by splitting them
            string[] tokens = environment.Split(EnvironmentTokenSeparator);

            foreach (var token in tokens)
            {
                string[] keyValuePair = token.Split(EnvironmentKeyValueSeparator);

                // invalid setting, there should be at least two values
                if (keyValuePair.Length <= 1) continue;

                string key = keyValuePair[0].Trim().ToLower();

                if (string.IsNullOrEmpty(key)) continue;

                // we will also consider the case when we have something like
                // "manufacturerAndModel : some:string:separated"
                // so the first one will be the key and the rest will be the value
                string value = string.Join("", keyValuePair.Skip(1).ToArray())
                    .Trim();
                
                // must have a value
                if (string.IsNullOrEmpty(value)) continue;

                dictionary[key] = value;
            }

            return dictionary;
        }

        /// <summary>
        /// Parse the mobile environment provided in jenkins.
        /// </summary>
        /// <param name="environment"> the environment string</param>
        /// <returns>the MobileEnvironment for ParallelRunner </returns>
        public static MobileEnvironment ParseMobileEnvironment(string environment)
        {
            var dictionary = GetEnvironmentProperties(environment);

            // invalid environment string
            if (dictionary.Count == 0) return null;

            var device = new Device();

            var mobileEnvironment = new MobileEnvironment
            {
                device = device,
                lab = MobileCenterLab
            };

            if (dictionary.ContainsKey(DeviceIdKey.ToLower()))
            {
                if (!string.IsNullOrEmpty(dictionary[DeviceIdKey.ToLower()]))
                {
                    device.deviceID = dictionary[DeviceIdKey.ToLower()];
                }
            }

            if (dictionary.ContainsKey(ManufacturerAndModelKey.ToLower()) && !string.IsNullOrEmpty(dictionary[ManufacturerAndModelKey.ToLower()]))
            {
                device.manufacturer = dictionary[ManufacturerAndModelKey.ToLower()];
            }
            else
            {
                if (dictionary.ContainsKey(ManufacturerKey.ToLower()) && !string.IsNullOrEmpty(dictionary[ManufacturerKey.ToLower()]))
                {
                    device.manufacturer = dictionary[ManufacturerKey.ToLower()];
                }
                if (dictionary.ContainsKey(ModelKey.ToLower()) && !string.IsNullOrEmpty(dictionary[ModelKey.ToLower()]))
                {
                    device.model = dictionary[ModelKey.ToLower()];
                }
            }

            if (dictionary.ContainsKey(OsVersionKey.ToLower()))
            {
                if (!string.IsNullOrEmpty(dictionary[OsVersionKey.ToLower()]))
                {
                    device.osVersion = dictionary[OsVersionKey.ToLower()];
                }
            }

            if (dictionary.ContainsKey(OsTypeKey.ToLower()))
            {
                if (!string.IsNullOrEmpty(dictionary[OsTypeKey.ToLower()]))
                {
                    device.osType = dictionary[OsTypeKey.ToLower()];
                }
            }

            // the environment string should contain at least a valid property
            // in order for PrallelRunner to be able to query MC for the specific device
            if(device.deviceID == null && (device.osType == null && device.osVersion == null && device.manufacturer == null && device.model == null))
            {
                return null;
            }

            return mobileEnvironment;
        }

        /// <summary>
        /// Parse the web environment provided.
        /// </summary>
        /// <param name="environment"> the environment string</param>
        /// <returns> the WebEnvironmnet for ParallelRunner</returns>
        public static WebEnvironment ParseWebEnvironment(string environment)
        {
            // example of environment string for web
            // "browser: Chrome"

            var dictionary = GetEnvironmentProperties(environment);

            if (!dictionary.ContainsKey(BrowserKey.ToLower())) return null;

            WebEnvironment webEnvironment = new WebEnvironment { lab = WebLab };

            var browser = dictionary[BrowserKey.ToLower()].Trim();

            // try to find a browser that matches the one provided
            foreach(var browserName in BrowserNames)
            {
                if(string.Equals(browserName,browser,StringComparison.CurrentCultureIgnoreCase))
                {
                    webEnvironment.browser = browserName;
                    return webEnvironment;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if a given propery is part of the mobile properties.
        /// </summary>
        /// <param name="property"> the property to check </param>
        /// <returns>true if the given property is a mobile prop, false otherwise </returns>
        public static bool IsKnownMobileProperty(string property)
        {
            foreach (var knownProp in MobileProperties)
            {
                if (knownProp.ToLower() == property.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return the environment type based on the environment string provided.
        /// </summary>
        /// <param name="environment">the environment string</param>
        /// <returns>the environment type</returns>
        public static EnvironmentType GetEnvironmentType(string environment)
        {
            if (string.IsNullOrEmpty(environment)) return EnvironmentType.UNKNOWN;

            environment = environment.Trim().ToLower();

            Dictionary<string, string> props = GetEnvironmentProperties(environment);

            // no valid property found
            if(props.Count == 0)
            {
                return EnvironmentType.UNKNOWN;
            }

            // web environment only contains the browser key
            if (props.ContainsKey(BrowserKey.ToLower()) && props.Count == 1)
            {
                return EnvironmentType.WEB;
            }

            // check if it's a mobile environment
            foreach(var prop in props)
            {
                if(!IsKnownMobileProperty(prop.Key))
                {
                    return EnvironmentType.UNKNOWN;
                }
            }

            return EnvironmentType.MOBILE;
        }

        /// <summary>
        /// Parse all of the provided environment strings for this test.
        /// </summary>
        /// <param name="environments">the environments list</param>
        /// <param name="testInfo">the test information </param>
        /// <returns>the parallel test run configuration</returns>
        public static ParallelTestRunConfiguration ParseEnvironmentStrings(IEnumerable<string> environments, TestInfo testInfo)
        {
            var parallelTestRunConfiguration = new ParallelTestRunConfiguration
            {
                reportPath = !string.IsNullOrWhiteSpace(testInfo.ReportPath) ? testInfo.ReportPath : testInfo.ReportBaseDirectory
            };

            List<ParallelTestRunConfigurationItem> items = [];

            foreach (var env in environments)
            {
                var environment = new ParallelTestRunConfiguraion.Environment();

                // try to determine the environment type
                var type = GetEnvironmentType(env);

                if (type == EnvironmentType.MOBILE)
                {
                    environment.mobile = ParseMobileEnvironment(env);

                    if(environment.mobile == null)
                    {
                        throw new ParallelRunnerConfigurationException("Invalid mobile configuration provided: " + env);
                    }
                }
                else if(type == EnvironmentType.WEB)
                {
                    environment.web = ParseWebEnvironment(env);

                    if(environment.web == null)
                    {
                        throw new ParallelRunnerConfigurationException("Invalid web configuration provided: " + env);
                    }
                }
                else
                {
                    // environment might be an empty string, just ignore it
                    continue;
                }

                var item = new ParallelTestRunConfigurationItem
                {
                    test = testInfo.TestPath,
                    env = environment,
                    reportPath = testInfo.TestPath
                };

                items.Add(item);
            }

            parallelTestRunConfiguration.parallelRuns = items.ToArray();

            return parallelTestRunConfiguration;
        }

        /// <summary>
        /// Return the proxy settings.
        /// </summary>
        /// <param name="mcConnectionInfo">the mc connection info</param>
        /// <returns></returns>
        public static ProxySettings GetMCProxySettings(McConnectionInfo mcConnectionInfo)
        {
            if (String.IsNullOrEmpty(mcConnectionInfo.ProxyAddress))
                return null;

            AuthenticationSettings authenticationSettings = null;

            if (!string.IsNullOrEmpty(mcConnectionInfo.ProxyUserName)
                && !string.IsNullOrEmpty(mcConnectionInfo.ProxyPassword))
            {
                authenticationSettings = new AuthenticationSettings
                {
                    username = mcConnectionInfo.ProxyUserName,
                    password = WinUserNativeMethods.
                        ProtectBSTRToBase64(mcConnectionInfo.ProxyPassword)
                };
            }

            ProxySettings proxySettings = new ProxySettings
            {
                authentication = authenticationSettings,
                hostname = mcConnectionInfo.ProxyAddress,
                port = mcConnectionInfo.ProxyPort,
                type = mcConnectionInfo.ProxyType == 1 ? "system" : "http",
            };

            return proxySettings;
        }

        /// <summary>
        /// Parses the MC settings and returns the corresponding UFT settings.
        /// </summary>
        /// <param name="info"> the mc settings</param>
        /// <returns> the parallel runner uft settings </returns>
        public static UFTSettings ParseMCSettings(McConnectionInfo info)
        {
            if (info.HostAddress.IsNullOrEmpty() ||
                (info.UserName.IsNullOrEmpty() && info.ClientId.IsNullOrEmpty()) ||
                (info.Password.IsNullOrEmpty() && info.SecretKey.IsNullOrEmpty()) ||
                info.HostPort.IsNullOrEmpty())
                return null;

            MCSettings mcSettings = new()
            {
                authType = (int)info.MobileAuthType,
                hostname = info.HostAddress,
                port = Convert.ToInt32(info.HostPort),
                protocol = info.UseSSL ? "https" : "http"
            };
            if (info.MobileAuthType == McConnectionInfo.AuthType.UsernamePassword)
            {
                mcSettings.username = info.UserName;
                mcSettings.password = WinUserNativeMethods.ProtectBSTRToBase64(info.Password);
                mcSettings.tenantId = info.TenantId;
            }
            else if (info.MobileAuthType == McConnectionInfo.AuthType.AuthToken)
            {
                string accessKey = string.Format(ACCESS_KEY_FORMAT, info.ClientId, info.SecretKey, info.TenantId);
                mcSettings.accessKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(accessKey));
            }
            else
            {
                throw new ParallelRunnerConfigurationException("Incorrect type of credentials provided for Digital Lab authentication.");
            }

            var proxy = GetMCProxySettings(info);

            // set the proxy information if we have it
            if(proxy != null)
            {
                mcSettings.proxy = proxy;
            }

            UFTSettings uftSettings = new UFTSettings
            {
                mc = mcSettings
            };

            return uftSettings;
        }

        /// <summary>
        /// Creates the json config file needed by the parallel runner and returns the path.
        /// </summary>
        /// <param name="testInfo"> The test information. </param>
        /// <returns>
        /// the path of the newly generated config file 
        /// </returns>
        public static string GetConfigFilePath(TestInfo testInfo, McConnectionInfo mcConnectionInfo,Dictionary<string, List<string>> environments)
        {
            // no environment defined for this test
            if (!environments.ContainsKey(testInfo.TestId))
            {
                throw new ParallelRunnerConfigurationException("No parallel runner environments found for this test!");
            }

            // get the parallel run configuration
            var config = ParseEnvironmentStrings(environments[testInfo.TestId], testInfo);

            // there were no valid environments provided
            if (config.parallelRuns.Length == 0)
            {
                throw new ParallelRunnerConfigurationException("No valid environments found for this test!");
            }

            var mcSettings = ParseMCSettings(mcConnectionInfo);

            // set the Digital Lab settings if provided
            if (mcSettings != null)
            {
                config.settings = mcSettings;
            }

            var configFilePath = Path.Combine(testInfo.TestPath, testInfo.TestId + ".json");

            JavaScriptSerializer serializer = new JavaScriptSerializer();

            string configJson = null;

            try
            {
                configJson = serializer.Serialize(config);
            }
            catch(InvalidOperationException e)
            {
                throw new ParallelRunnerConfigurationException("Invalid json confguration provided: ",e);
            }
            catch(ArgumentException e)
            {
                throw new ParallelRunnerConfigurationException("Configuration serialization recursion limit exceeded: ", e);
            }

            try
            {
                File.WriteAllText(configFilePath, configJson);
            }
            catch (Exception e)
            {
                throw new ParallelRunnerConfigurationException("Could not write configuration file: ", e);
            }

            return configFilePath;
        }
    }
}
