﻿using gView.Core.Framework.Exceptions;
using gView.Framework.Security;
using gView.Security.Framework;
using gView.Server.AppCode;
using gView.Server.Extensions;
using gView.Server.Services.MapServer;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace gView.Server.Services.Security
{
    public class LoginManager
    {
        private readonly MapServiceManager _mapServerService;
        private readonly EncryptionCertificateService _encryptionCertService;

        public LoginManager(MapServiceManager mapServerService, EncryptionCertificateService encryptionCertService)
        {
            _mapServerService = mapServerService;
            _encryptionCertService = encryptionCertService;
        }

        #region Manager Logins

        public AuthToken GetManagerAuthToken(string username, string password, int exipreMinutes = 30, bool createIfFirst = false)
        {
            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/manage");
            if (createIfFirst && di.GetFiles().Count() == 0)
            {
                CreateLogin(di.FullName, username.ToLower(), password);
                _encryptionCertService.GetCertificate("crypto0");  // Create the Service if not exits
            }

            return CreateAuthToken(di.FullName, username, password, AppCode.AuthToken.AuthTypes.Manage, exipreMinutes);
        }

        public bool HasManagerLogin()
        {
            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/manage");
            return di.Exists && di.GetFiles("*.lgn").Length > 0;
        }

        public IEnumerable<string> GetMangeUserNames()
        {
            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/manage");

            if (di.Exists)
            {
                return di.GetFiles("*.lgn").Select(f => f.Name.Substring(0, f.Name.Length - f.Extension.Length));
            }

            return new string[0];
        }

        #endregion

        #region Token Logins

        public void CreateTokenLogin(string username, string password)
        {
            if (GetManageAndTokenUsernames().Where(u => username.Equals(u, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
            {
                throw new MapServerException("User '" + username + "' already exists");
            }

            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/token");
            var fi = new FileInfo(di.FullName + "/" + username + ".lgn");
            if (fi.Exists)
            {
                throw new MapServerException("User '" + username + "' already exists");
            }

            CreateLogin(di.FullName, username, password);
        }

        public void ChangeTokenUserPassword(string username, string newPassword)
        {
            newPassword.ValidatePassword();

            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/token");
            var fi = new FileInfo(di.FullName + "/" + username + ".lgn");
            if (!fi.Exists)
            {
                throw new MapServerException("User '" + username + "' do not exists");
            }

            var hashedPassword = SecureCrypto.Hash64(newPassword, username);
            File.WriteAllText(fi.FullName, hashedPassword);
        }

        public IEnumerable<string> GetTokenUsernames()
        {

            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/token");
            if (di.Exists)
            {
                return di.GetFiles("*.lgn").Select(f => f.Name.Substring(0, f.Name.Length - f.Extension.Length));
            }

            return new string[0];
        }

        public AuthToken GetAuthToken(string username, string password, int expireMinutes = 30)
        {
            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/token");
            return CreateAuthToken(di.FullName, username, password, AppCode.AuthToken.AuthTypes.Tokenuser, expireMinutes);
        }

        public AuthToken CreateUserAuthTokenWithoutPasswordCheck(string username, int expireMinutes = 30)
        {
            var di = new DirectoryInfo(_mapServerService.Options.LoginManagerRootPath + "/token");
            return CreateAuthTokenWithoutPasswordCheck(di.FullName, username, AppCode.AuthToken.AuthTypes.Tokenuser, expireMinutes);
        }

        #endregion

        public IEnumerable<string> GetManageAndTokenUsernames()
        {
            List<string> names = new List<string>();

            names.AddRange(GetMangeUserNames());
            names.AddRange(GetTokenUsernames());

            return names;
        }

        #region Request

        public string LoginUsername(HttpRequest request)
        {
            try
            {
                return LoginAuthToken(request).Username;
            }
            catch (Exception)
            {
                return String.Empty;
            }
        }

        public bool IsManageUser(HttpRequest request)
        {
            try
            {
                var loginAuthToken = LoginAuthToken(request);
                return loginAuthToken != null &&
                       loginAuthToken.AuthType == AuthToken.AuthTypes.Manage;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public AuthToken LoginAuthToken(HttpRequest request)
        {
            AuthToken authToken = null;

            try
            {
                #region From Token

                string token = request.Query["token"];
                if (String.IsNullOrWhiteSpace(token) && request.HasFormContentType)
                {
                    try
                    {
                        token = request.Form["token"];
                    }
                    catch { }
                }
                if (!String.IsNullOrEmpty(token))
                {
                    return authToken = _encryptionCertService.FromToken(token);
                }

                #endregion

                #region From Cookie

                string cookie = request.Cookies[Globals.AuthCookieName];
                if (!String.IsNullOrWhiteSpace(cookie))
                {
                    try
                    {
                        return authToken = _encryptionCertService.FromToken(cookie);
                    }
                    catch (System.Security.Cryptography.CryptographicException)
                    {
                        return authToken = new AuthToken()
                        {
                            Username = String.Empty
                        };
                    }
                }

                #endregion

                #region Authorization Header

                if (!String.IsNullOrEmpty(request.Headers["Authorization"]))
                {
                    var userPwd = request.Headers["Authorization"].ToString().FromAuthorizationHeader();
                    var path = _mapServerService.Options.LoginManagerRootPath + "/token";

                    return authToken = CreateAuthToken(path, userPwd.username, userPwd.password, AuthToken.AuthTypes.Tokenuser);
                }

                #endregion

                return authToken = new AuthToken()
                {
                    Username = String.Empty
                };
            }
            finally
            {
                if (authToken == null || authToken.IsExpired)
                {
                    throw new InvalidTokenException();
                }
            }
        }

        public AuthToken GetAuthToken(HttpRequest request)
        {
            return LoginAuthToken(request);
        }



        #endregion

        #region Helper

        private AuthToken CreateAuthToken(string path, string username, string password, AuthToken.AuthTypes authType, int expireMiniutes = 30)
        {
            var fi = new FileInfo(path + "/" + username + ".lgn");
            if (fi.Exists)
            {
                if (SecureCrypto.VerifyPassword(password, File.ReadAllText(fi.FullName), username))
                {
                    return new AuthToken(username, authType, new DateTimeOffset(DateTime.UtcNow.Ticks, new TimeSpan(0, 30, 0)));
                }
            }

            return null;
        }



        private AuthToken CreateAuthTokenWithoutPasswordCheck(string path, string username, AuthToken.AuthTypes authType, int expireMiniutes = 30)
        {
            var fi = new FileInfo(path + "/" + username + ".lgn");
            if (fi.Exists)
            {
                return new AuthToken(username, authType, new DateTimeOffset(DateTime.UtcNow.Ticks, new TimeSpan(0, 30, 0)));
            }

            return null;
        }

        private void CreateLogin(string path, string username, string password)
        {
            username.ValidateUsername();
            password.ValidatePassword();

            var hashedPassword = SecureCrypto.Hash64(password, username);

            File.WriteAllText(path + "/" + username + ".lgn", hashedPassword);
        }

        #endregion
    }
}
