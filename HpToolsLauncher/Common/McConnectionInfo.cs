using HpToolsLauncher.Properties;
using HpToolsLauncher.Utils;
using System;
using System.ComponentModel;
using System.Text;

namespace HpToolsLauncher.Common
{
    public class McConnectionInfo
    {
        private const string EQ = "=";
        private const string SEMI_COLON = ";";
        private const string YES = "Yes";
        private const string NO = "No";
        private const string SYSTEM = "System";
        private const string HTTP = "Http";
        private const string HTTPS = "Https";
        private const string PORT_8080 = "8080";
        private const string PORT_443 = "443";
        private const string CLIENT = "client";
        private const string SECRET = "secret";
        private const string TENANT = "tenant";
        private const int ZERO = 0;
        private const int ONE = 1;
        private static readonly char[] SLASH = ['/'];
        private static readonly char[] COLON = [':'];
        private static readonly char[] DBL_QUOTE = ['"'];

        private const string MOBILEHOSTADDRESS = "MobileHostAddress";
        private const string MOBILEUSESSL = "MobileUseSSL";
        private const string MOBILEUSERNAME = "MobileUserName";
        private const string MOBILEPASSWORD = "MobilePassword";
        private const string MOBILEPASSWORDBASICAUTH = "MobilePasswordBasicAuth";
        private const string MOBILETENANTID = "MobileTenantId";
        private const string MOBILECLIENTID = "MobileClientId";
        private const string MOBILESECRETKEYBASICAUTH = "MobileSecretKeyBasicAuth";
        private const string MOBILESECRETKEY = "MobileSecretKey";
        private const string DIGITALLABTYPE = "DigitalLabType";
        private const string MOBILEUSEPROXY = "MobileUseProxy";
        private const string MOBILEPROXYTYPE = "MobileProxyType";
        private const string MOBILEPROXYSETTING_ADDRESS = "MobileProxySetting_Address";
        private const string MOBILEPROXYSETTING_AUTH = "MobileProxySetting_Authentication";
        private const string MOBILEPROXYSETTING_USERNAME = "MobileProxySetting_UserName";
        private const string MOBILEPROXYSETTING_PASSWORD = "MobileProxySetting_Password";
        private const string MOBILEPROXYSETTING_PASSWORDBASICAUTH = "MobileProxySetting_PasswordBasicAuth";

        public enum AuthType
        {
            [Description("Username Password")]
            UsernamePassword = 0,
            [Description("Access Key")]
            AuthToken = 1
        }

        public enum DigitalLabType
        {
            UFT = 0,
            Lite = 1,
            ValueEdge = 2
        }

        private readonly string _userName;
        private readonly string _password;
        private readonly string _clientId;
        private readonly string _secretKey;
        private readonly string _tenantId;
        private readonly string _hostAddress;
        private readonly string _hostPort;
        private readonly int _proxyType;
        private readonly string _proxyAddress;
        private readonly int _proxyPort;
        private readonly string _proxyUserName;
        private readonly string _proxyPassword;

        private readonly bool _useSSL;
        private readonly bool _useProxy;
        private readonly bool _useProxyAuth;

        private readonly AuthType _authType = AuthType.UsernamePassword;
        private readonly DigitalLabType _labType = DigitalLabType.UFT;

        public AuthType MobileAuthType => _authType;
        public string UserName => _userName;
        public string Password => _password;
        public string ClientId => _clientId;
        public string SecretKey => _secretKey;
        public string TenantId => _tenantId;
        public string HostAddress => _hostAddress;
        public string HostPort => _hostPort;

        public bool UseSSL => _useSSL;
        public bool UseProxy => _useProxy;

        public int ProxyType => _proxyType;
        public string ProxyAddress => _proxyAddress;
        public int ProxyPort => _proxyPort;
        public bool UseProxyAuth => _useProxyAuth;
        public string ProxyUserName => _proxyUserName;
        public string ProxyPassword => _proxyPassword;
        public DigitalLabType LabType => _labType;

        public McConnectionInfo(string host = "", string port = "", string username = "", string password = "", bool useSSL = false, string proxyAddr = "", int proxyPort = 0, bool useProxyAuth = false, string proxyUserName = "", string proxyPassword = "")
        {
            _hostAddress = host;
            _hostPort = port;
            _userName = username;
            _password = password;
            _useSSL = useSSL;
            _proxyAddress = proxyAddr;
            _proxyPort = proxyPort;
            _useProxyAuth = useProxyAuth;
            _proxyPassword = proxyPassword;
            _proxyUserName = proxyUserName;
            _proxyPassword = proxyPassword;
        }

        public McConnectionInfo(JavaProperties ciParams)
        {
            if (ciParams.ContainsKey(MOBILEHOSTADDRESS))
            {
                string mcServerUrl = ciParams[MOBILEHOSTADDRESS].Trim();
                if (string.IsNullOrEmpty(mcServerUrl))
                {
                    throw new NoMcConnectionException();
                }

                if (!mcServerUrl.IsNullOrEmpty())
                {
                    //ssl
                    if (ciParams.ContainsKey(MOBILEUSESSL))
                    {
                        string strUseSSL = ciParams[MOBILEUSESSL];
                        if (!strUseSSL.IsNullOrEmpty())
                        {
                            int.TryParse(ciParams[MOBILEUSESSL], out int intUseSSL);
                            _useSSL = intUseSSL == ONE;
                        }
                    }

                    //url is something like http://xxx.xxx.xxx.xxx:8080
                    string[] arr = mcServerUrl.Split(COLON, StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length == 1)
                    {
                        if (arr[0].Trim().In(true, HTTP, HTTPS))
                            throw new ArgumentException(string.Format(Resources.McInvalidUrl, mcServerUrl));
                        _hostAddress = arr[0].TrimEnd(SLASH);
                        _hostPort = _useSSL ? PORT_443 : PORT_8080;
                    }
                    else if (arr.Length == 2)
                    {
                        if (arr[0].Trim().In(true, HTTP, HTTPS))
                        {
                            _hostAddress = arr[1].Trim(SLASH);
                            _hostPort = _useSSL ? PORT_443 : PORT_8080;
                        }
                        else
                        {
                            _hostAddress = arr[0].Trim(SLASH);
                            _hostPort = arr[1].TrimEnd(SLASH);
                        }
                    }
                    else if (arr.Length == 3)
                    {
                        _hostAddress = arr[1].Trim(SLASH);
                        _hostPort = arr[2].TrimEnd(SLASH);
                    }

                    if (_hostAddress.Trim() == string.Empty)
                    {
                        throw new ArgumentException(Resources.McEmptyHostAddress);
                    }

                    //mc username
                    if (ciParams.ContainsKey(MOBILEUSERNAME))
                    {
                        string mcUsername = ciParams[MOBILEUSERNAME];
                        if (!mcUsername.IsNullOrEmpty())
                        {
                            _userName = mcUsername;
                        }
                    }

                    //mc password
                    if (ciParams.ContainsKey(MOBILEPASSWORDBASICAUTH))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(ciParams[MOBILEPASSWORDBASICAUTH]);
                        _password = Encoding.Default.GetString(data);
                    }
                    else if (ciParams.ContainsKey(MOBILEPASSWORD))
                    {
                        string mcPassword = ciParams[MOBILEPASSWORD];
                        if (!mcPassword.IsNullOrEmpty())
                        {
                            _password = Encrypter.Decrypt(mcPassword);
                        }
                    }

                    //mc tenantId
                    _tenantId = ciParams.GetOrDefault(MOBILETENANTID).Trim();

                    if (ciParams.ContainsKey(MOBILECLIENTID))
                    {
                        string mcClientId = ciParams[MOBILECLIENTID];
                        if (!mcClientId.IsNullOrEmpty())
                        {
                            _clientId = mcClientId;
                            if (ciParams.ContainsKey(MOBILESECRETKEYBASICAUTH))
                            {
                                // base64 decode
                                byte[] data = Convert.FromBase64String(ciParams[MOBILESECRETKEYBASICAUTH]);
                                _secretKey = Encoding.Default.GetString(data);
                                _authType = AuthType.AuthToken;
                            }
                            else if (ciParams.ContainsKey(MOBILESECRETKEY))
                            {
                                string mcSecretKey = ciParams[MOBILESECRETKEY];
                                if (!mcSecretKey.IsNullOrEmpty())
                                {
                                    _secretKey = Encrypter.Decrypt(mcSecretKey);
                                    _authType = AuthType.AuthToken;
                                }
                            }
                        }
                    }

                    if (ciParams.ContainsKey(DIGITALLABTYPE))
                    {
                        var dlLabType = ciParams[DIGITALLABTYPE];
                        if (!string.IsNullOrEmpty(dlLabType))
                        {
                            Enum.TryParse(dlLabType, true, out _labType);
                        }
                    }

                    //Proxy enabled flag
                    if (ciParams.ContainsKey(MOBILEUSEPROXY))
                    {
                        string strUseProxy = ciParams[MOBILEUSEPROXY];
                        if (!strUseProxy.IsNullOrEmpty())
                        {
                            _useProxy = int.Parse(strUseProxy) == ONE;
                        }
                    }

                    //Proxy type
                    if (ciParams.ContainsKey(MOBILEPROXYTYPE))
                    {
                        string proxyType = ciParams[MOBILEPROXYTYPE];
                        if (!proxyType.IsNullOrEmpty())
                        {
                            _proxyType = int.Parse(proxyType);
                        }
                    }

                    //proxy address
                    if (ciParams.ContainsKey(MOBILEPROXYSETTING_ADDRESS))
                    {
                        string proxyAddress = ciParams[MOBILEPROXYSETTING_ADDRESS];
                        if (!proxyAddress.IsNullOrEmpty())
                        {
                            // data is something like "16.105.9.23:8080"
                            string[] strArrayForProxyAddress = proxyAddress.Split(COLON);

                            if (strArrayForProxyAddress.Length == 2)
                            {
                                _proxyAddress = strArrayForProxyAddress[0];
                                _proxyPort = int.Parse(strArrayForProxyAddress[1]);
                            }
                        }
                    }

                    //Proxy authentication
                    if (ciParams.ContainsKey(MOBILEPROXYSETTING_AUTH))
                    {
                        string proxyAuthentication = ciParams[MOBILEPROXYSETTING_AUTH];
                        if (!proxyAuthentication.IsNullOrEmpty())
                        {
                            _useProxyAuth = int.Parse(proxyAuthentication) == ONE;
                        }
                    }

                    //Proxy username
                    if (ciParams.ContainsKey(MOBILEPROXYSETTING_USERNAME))
                    {
                        string proxyUsername = ciParams[MOBILEPROXYSETTING_USERNAME];
                        if (!proxyUsername.IsNullOrEmpty())
                        {
                            _proxyUserName = proxyUsername;
                        }
                    }

                    //Proxy password
                    if (ciParams.ContainsKey(MOBILEPROXYSETTING_PASSWORDBASICAUTH))
                    {
                        // base64 decode
                        byte[] data = Convert.FromBase64String(ciParams[MOBILEPROXYSETTING_PASSWORDBASICAUTH]);
                        _proxyPassword = Encoding.Default.GetString(data);
                    }
                    else if (ciParams.ContainsKey(MOBILEPROXYSETTING_PASSWORD))
                    {
                        string proxyPassword = ciParams[MOBILEPROXYSETTING_PASSWORD];
                        if (!proxyPassword.IsNullOrEmpty())
                        {
                            _proxyPassword = Encrypter.Decrypt(proxyPassword);
                        }
                    }
                }
            }
        }

        private string ProxyTypeAsString => ProxyType == ONE ? SYSTEM : HTTP;
        private string UseProxyAuthAsString => _useProxyAuth ? YES : NO;
        private string UseSslAsString => _useSSL ? YES : NO;

        public override string ToString()
        {
            string usernameOrClientId = string.Empty;
            if (_authType == AuthType.AuthToken)
            {
                usernameOrClientId = $"ClientId: {ClientId}";
            }
            else if (_authType == AuthType.UsernamePassword)
            {
                usernameOrClientId = $"Username: {UserName}";
            }
            string strProxy = $"UseProxy: {(_useProxy ? YES: NO)}";
            if (_useProxy)
            {
                strProxy += $", ProxyType: {ProxyTypeAsString}, ProxyAddress: {_proxyAddress}, ProxyPort: {_proxyPort}, ProxyAuth: {UseProxyAuthAsString}, ProxyUser: {_proxyUserName}";
            }
            return
                 $"Digital Lab HostAddress: {_hostAddress}, Port: {_hostPort}, AuthType: {_authType}, {usernameOrClientId}, TenantId: {_tenantId}, UseSSL: {UseSslAsString}, {strProxy}";
        }
    }

    public class NoMcConnectionException : Exception
    {
    }

    public class CloudBrowser(string url, string os, string type, string version, string location)
    {
        private const string EQ = "=";
        private const string SEMI_COLON = ";";
        private const string URL = "url";
        private const string TYPE = "type";
        private const string _OS = "os";
        private const string VERSION = "version";
        private const string REGION = "region";

        private static readonly char[] DBL_QUOTE = ['"'];

        public string Url => url;
        public string OS => os;
        public string Browser => type;
        public string Version => version;
        public string Region => location;

        public static bool TryParse(string strCloudBrowser, out CloudBrowser cloudBrowser)
        {
            cloudBrowser = null;
            try
            {
                string[] arrKeyValPairs = strCloudBrowser.Trim().Trim(DBL_QUOTE).Split(SEMI_COLON.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string url = null, os = null, type = null, version = null, region = null;

                // key-values are separated by =, we need its value, the key is known
                foreach (var pair in arrKeyValPairs)
                {
                    string[] arrKVP = pair.Split(EQ.ToCharArray(), 2);

                    if (arrKVP.Length < 2)
                        continue;

                    var key = arrKVP[0].Trim();
                    var value = arrKVP[1].Trim();
                    switch (key.ToLower())
                    {
                        case URL:
                            url = value; break;
                        case _OS:
                            os = value; break;
                        case TYPE:
                            type = value; break;
                        case VERSION:
                            version = value; break;
                        case REGION:
                            region = value; break;
                        default:
                            break;
                    }
                }
                cloudBrowser = new CloudBrowser(url, os, type, version, region);
                return true;
            }
            catch (Exception ex)
            {
                ConsoleWriter.WriteErrLine(ex.Message);
                return false;
            }
        }
    }

    public class DigitalLab
    {
        private McConnectionInfo _connInfo;
        private string _mobileInfo;
        private CloudBrowser _cloudBrowser;
        public DigitalLab(McConnectionInfo mcConnInfo, string mobileInfo, CloudBrowser cloudBrowser)
        {
            _connInfo = mcConnInfo;
            _mobileInfo = mobileInfo;
            _cloudBrowser = cloudBrowser;
        }
        public McConnectionInfo ConnectionInfo { get { return _connInfo; } }
        public string MobileInfo { get { return _mobileInfo; } }
        public CloudBrowser CloudBrowser { get { return _cloudBrowser; } }
    }
}
