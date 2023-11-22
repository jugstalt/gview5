using gView.Framework.system;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace gView.Web.Framework.Web.Authorization
{
    public class WebAuthorizationCredentials
    {
        const string AuthTypeDefault = "Basic";
        const string GrantTypeDefault = "client_credentials";
        const string ScopeDefault = "openid";

        private static ConcurrentDictionary<string, Token> _globalTokenCache = new ConcurrentDictionary<string, Token>();

        public WebAuthorizationCredentials(
            string username,
            string password,
            AuthorizationType authType = AuthorizationType.Basic,
            string accessTokenServiceUrl = "",
            string grantType = "",
            string scope = "")
        {
            this.Username = username;
            this.Password = password;
            this.AuthType = authType;
            this.AccessTokenTokenServiceUrl = accessTokenServiceUrl;
            this.GrantType = grantType.OrTake(GrantTypeDefault);
            this.Scope = scope.OrTake(ScopeDefault);
        }

        public WebAuthorizationCredentials(string connectionString)
        {
            this.Username = connectionString.ExtractConnectionStringParameter("wa-user");
            this.Password = connectionString.ExtractConnectionStringParameter("wa-pwd");

            if (Enum.TryParse<AuthorizationType>(
                String.IsNullOrWhiteSpace(connectionString.ExtractConnectionStringParameter("wa-authtype")) ?
                AuthTypeDefault :
                connectionString.ExtractConnectionStringParameter("wa-authtype"), out AuthorizationType authType))
            {
                this.AuthType = authType;
            }
            else
            {
                this.AuthType = AuthorizationType.Basic;
            }

            AccessTokenTokenServiceUrl = connectionString.ExtractConnectionStringParameter("wa-access-token-service");
            GrantType = connectionString.ExtractConnectionStringParameter("wa-granttype").OrTake(GrantTypeDefault);
            Scope = connectionString.ExtractConnectionStringParameter("wa-scope").OrTake(ScopeDefault);
        }

        public string ConnectionString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (!String.IsNullOrWhiteSpace(this.Username))
                {
                    sb.AddConnectionStringParameter("wa-user", this.Username);
                }

                if (!String.IsNullOrWhiteSpace(this.Password))
                {
                    sb.AddConnectionStringParameter("wa-pwd", this.Password);
                }

                if (!String.IsNullOrWhiteSpace(this.Username) ||
                    !this.AuthType.ToString().Trim().Equals(AuthTypeDefault, StringComparison.InvariantCultureIgnoreCase))
                {
                    sb.AddConnectionStringParameter("wa-authtype", this.AuthType.ToString());
                }

                if (!String.IsNullOrWhiteSpace(this.AccessTokenTokenServiceUrl))
                {
                    sb.AddConnectionStringParameter("wa-access-token-service", AccessTokenTokenServiceUrl);
                }

                if (!String.IsNullOrWhiteSpace(this.AccessTokenTokenServiceUrl) ||
                    !GrantTypeDefault.Equals(this.GrantType?.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    sb.AddConnectionStringParameter($"wa-granttype", this.GrantType);
                }

                if (!String.IsNullOrWhiteSpace(this.AccessTokenTokenServiceUrl) ||
                    !ScopeDefault.Equals(this.Scope?.Trim(), StringComparison.InvariantCultureIgnoreCase))
                {
                    sb.AddConnectionStringParameter($"wa-scope", this.Scope);
                }

                return sb.ToString();
            }
        }

        public string Username { get; set; }
        public string Password { get; set;  }

        public AuthorizationType AuthType { get; set; }

        public string AccessTokenTokenServiceUrl { get; set; }

        public string GrantType { get; set; }
        public string Scope { get; set; }

        async public Task AddAuthorizationHeaders(HttpClient httpClient)
        {
            if (String.IsNullOrEmpty(this.Username))
            {
                return;
            }

            try
            {
                Token token = null;
                _globalTokenCache.TryGetValue(CacheKey, out token);

                if (token == null || token.IsExpired())
                {
                    if (this.AuthType == AuthorizationType.Basic)
                    {
                        #region Basic Authentication

                        _globalTokenCache[CacheKey] = token = new Token()
                        {
                            AuthToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.Username}:{this.Password}")),
                            Expires = DateTime.Now.AddDays(365)
                        };

                        #endregion
                    }
                    if (this.AuthType == AuthorizationType.Bearer)
                    {
                        if (!String.IsNullOrEmpty(AccessTokenTokenServiceUrl))
                        {
                            #region OAuth2

                            var authServiceClient = httpClient; // use same...
                            //using (var authServiceClient = new HttpClient())
                            {
                                authServiceClient.DefaultRequestHeaders.Authorization =
                                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{this.Username}:{this.Password}")));
                                //authServiceClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

                                var nvc = new List<KeyValuePair<string, string>>();
                                nvc.Add(new KeyValuePair<string, string>("grant_type", this.GrantType));
                                nvc.Add(new KeyValuePair<string, string>("scope", this.Scope));
                                var client = new HttpClient();

                                using (var req = new HttpRequestMessage(HttpMethod.Post, AccessTokenTokenServiceUrl) { Content = new FormUrlEncodedContent(nvc) })
                                {
                                    var httpResponseMessage = await authServiceClient.SendAsync(req);
                                    if (httpResponseMessage.IsSuccessStatusCode)
                                    {
                                        var tokenResonseString = await httpResponseMessage.Content.ReadAsStringAsync();
                                        var accessToken = JsonConvert.DeserializeObject<AccessTokenResponse>(tokenResonseString);

                                        if (!String.IsNullOrEmpty(accessToken.error))
                                        {
                                            throw new Exception(RemoveSecrets($"AccessToken POST request returns error: {accessToken.error} - {accessToken.error_description ?? String.Empty}"));
                                        }

                                        _globalTokenCache[CacheKey] = token = new Token()
                                        {
                                            AuthToken = accessToken.access_token, //Convert.ToBase64String(Encoding.UTF8.GetBytes(accessToken.access_token)),
                                            Expires = DateTime.Now.AddSeconds(accessToken.expires_in)
                                        };
                                    }
                                    else
                                    {
                                        throw new Exception($"AccessToken POST request returns statuscode {httpResponseMessage.StatusCode}");
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Username => access_token

                            _globalTokenCache[CacheKey] = token = new Token()
                            {
                                AuthToken = this.Username,
                                Expires = DateTime.Now.AddDays(365)
                            };

                            #endregion
                        }
                    }
                }

                if (token != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue(AuthType.ToString(),
                        token.AuthToken);
                }
            }
            catch (Exception ex)
            {
                _globalTokenCache[CacheKey] = null;
                throw new Exception("WebAuthoriation Exception", ex);
            }
        }

        private string CacheKey => $"{this.AccessTokenTokenServiceUrl}:{this.AuthType.ToString()}:{this.Username}";

        private string RemoveSecrets(string msg)
        {
            if (!String.IsNullOrEmpty(Username))
            {
                msg = msg.Replace(Username, $"{Username.Substring(0, Math.Min(Username.Length, 2))}******");
            }

            if (!String.IsNullOrEmpty(Password))
            {
                msg = msg.Replace(Username, $"********");
            }

            if (!String.IsNullOrEmpty(AccessTokenTokenServiceUrl))
            {
                msg = msg.Replace(AccessTokenTokenServiceUrl, $"{AccessTokenTokenServiceUrl.Substring(0, Math.Min(AccessTokenTokenServiceUrl.Length, 24))}******");
            }

            return msg;
        }

        #region Classes

        class Token
        {
            public string AuthToken { get; set; }
            public DateTime Expires { get; set; }

            public bool IsExpired()
            {
                return DateTime.Now > Expires;
            }
        }


        class AccessTokenResponse
        {
            public string access_token { get; set; }
            public string scope { get; set; }
            public string id_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }

            public string error { get; set; }
            public string error_description { get; set; }
        }

        #endregion
    }
}
