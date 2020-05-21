﻿using gView.Core.Framework.Exceptions;
using gView.Framework.system;
using gView.MapServer;
using gView.Server.AppCode;
using gView.Server.Models;
using gView.Server.Services.Logging;
using gView.Server.Services.MapServer;
using gView.Server.Services.Security;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Server.Controllers
{
    public class ManageController : BaseController
    {
        private readonly InternetMapServerService _mapServerService;
        private readonly MapServerDeployService _mapServerDeployService;
        private readonly LoginManagerService _loginManagerService;
        private readonly MapServicesEventLogger _logger;

        public ManageController(InternetMapServerService mapServerService,
                                MapServerDeployService mapServerDeployService,
                                LoginManagerService loginManagerService,
                                MapServicesEventLogger logger,
                                EncryptionCertificateService encryptionCertificateService)
                                : base(loginManagerService, encryptionCertificateService)
        {
            _mapServerService = mapServerService;
            _mapServerDeployService = mapServerDeployService;
            _loginManagerService = loginManagerService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var authToken = _loginManagerService.GetAuthToken(this.Request);
                if (!authToken.IsManageUser)
                {
                    return RedirectToAction("Login");
                }

                ViewData["mainMenuItems"] = "mainMenuItemsPartial";
                return View();
            }
            catch (Exception)
            {
                return RedirectToAction("Login");
            }
        }

        #region Login

        [HttpGet]
        public IActionResult Login()
        {
            if (Globals.AllowFormsLogin == false)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new ManageLoginModel());
        }

        [HttpPost]
        public IActionResult Login(ManageLoginModel model)
        {
            try
            {
                if (Globals.AllowFormsLogin == false)
                {
                    return RedirectToAction("Index", "Home");
                }

                //Console.WriteLine("UN: "+model.Username);
                //Console.WriteLine("PW: "+model.Password);

                var authToken = _loginManagerService.GetManagerAuthToken(model.Username, model.Password, createIfFirst: true);

                if (authToken == null)
                {
                    throw new Exception("Unknown user or password");
                }

                base.SetAuthCookie(authToken);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
                return View(model);
            }
        }

        #endregion



        public IActionResult Collect()
        {
            var mem1 = (double)GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            GC.Collect();
            var mem2 = (double)GC.GetTotalMemory(true) / 1024.0 / 1024.0;
            return Json(new { succeeded = true, mem1 = mem1, mem2 = mem2 });
        }

        #region Services

        async public Task<IActionResult> Folders()
        {
            return await SecureApiCall(async () =>
            {
                var folderServices = _mapServerService.MapServices
                                    .Where(s => s.Type == MapServiceType.Folder)
                                    .Select(s => s)
                                    .OrderBy(s => s.Name)
                                    .Distinct();

                List<object> mapServiceJson = new List<object>();
                foreach (var folderService in folderServices)
                {
                    mapServiceJson.Add(MapService2Json(folderService, await folderService.GetSettingsAsync()));
                }

                return Json(new
                {
                    success = true,
                    folders = mapServiceJson.ToArray()
                });
            });
        }

        async public Task<IActionResult> Services(string folder)
        {
            folder = folder ?? String.Empty;
            _mapServerService.ReloadServices(folder, true);

            return await SecureApiCall(async () =>
            {
                var servicesInFolder = _mapServerService.MapServices
                    .Where(s => s.Type != MapServiceType.Folder &&
                                    s.Folder == folder);

                List<object> mapServiceJson = new List<object>();
                foreach (var serviceInFolder in servicesInFolder)
                {
                    mapServiceJson.Add(MapService2Json(serviceInFolder, await serviceInFolder.GetSettingsAsync()));
                }

                return Json(new
                {
                    success = true,
                    services = mapServiceJson.ToArray()
                });
            });
        }

        async public Task<IActionResult> SetServiceStatus(string service, string status)
        {
            return await SecureApiCall(async () =>
            {
                var mapService = _mapServerService.MapServices.Where(s => s.Fullname == service).FirstOrDefault();
                if (mapService == null)
                {
                    throw new MapServerException("Unknown service: " + service);
                }

                var settings = await mapService.GetSettingsAsync();
                switch (status.ToLower())
                {
                    case "running":
                        if (settings.Status != MapServiceStatus.Running)
                        {
                            // start
                            settings.Status = MapServiceStatus.Running;
                            await mapService.SaveSettingsAsync();
                            // reload
                            await _mapServerService.Instance.GetServiceMapAsync(service.ServiceName(), service.FolderName());
                        }
                        break;
                    case "stopped":
                        settings.Status = MapServiceStatus.Stopped;
                        _mapServerDeployService.MapDocument.RemoveMap(mapService.Fullname);
                        await mapService.SaveSettingsAsync();
                        break;
                    case "refresh":
                        settings.RefreshService = DateTime.UtcNow;

                        // start
                        settings.Status = MapServiceStatus.Running;
                        await mapService.SaveSettingsAsync();
                        // reload
                        await _mapServerService.Instance.GetServiceMapAsync(service.ServiceName(), service.FolderName());
                        break;
                }

                return Json(new
                {
                    success = true,
                    service = MapService2Json(mapService, settings)
                });
            });
        }

        public IActionResult ServiceErrorLogs(string service, string last = "0")
        {
            return SecureApiCall(() =>
            {
                var errorsResult = _logger.ErrorLogs(service, Framework.system.loggingMethod.error, long.Parse(last));

                return Json(new
                {
                    errors = errorsResult.errors,
                    ticks = errorsResult.ticks > 0 ? errorsResult.ticks.ToString() : null
                });
            });
        }

        [HttpGet]
        async public Task<IActionResult> ServiceSecurity(string service)
        {
            return await SecureApiCall(async () =>
            {
                service = service?.ToLower() ?? String.Empty;

                var mapService = _mapServerService.MapServices.Where(s => s.Fullname?.ToLower() == service).FirstOrDefault();
                if (mapService == null)
                {
                    throw new MapServerException("Unknown service: " + service);
                }

                var settings = await mapService.GetSettingsAsync();

                List<string> allTypes = new List<string>(Enum.GetNames(typeof(AccessTypes)).Select(n => n.ToLower()).Where(n => n != "none"));
                allTypes.Add("_all");

                var accessRules = settings.AccessRules;

                foreach (var interpreterType in _mapServerService.Interpreters)
                {
                    allTypes.Add("_" + ((IServiceRequestInterpreter)Activator.CreateInstance(interpreterType)).IdentityName.ToLower());
                }

                return Json(new
                {
                    allTypes = allTypes.ToArray(),
                    accessRules = accessRules,
                    allUsers = _loginManagerService.GetTokenUsernames(),
                    anonymousUsername = Identity.AnonyomousUsername
                });
            });
        }

        [HttpPost]
        async public Task<IActionResult> ServiceSecurity()
        {
            return await SecureApiCall(async () =>
            {
                var service = Request.Query["service"].ToString().ToLower();

                var mapService = _mapServerService.MapServices.Where(s => s.Fullname?.ToLower() == service).FirstOrDefault();
                if (mapService == null)
                {
                    throw new MapServerException("Unknown service: " + service);
                }

                var settings = await mapService.GetSettingsAsync();
                settings.AccessRules = null;  // Remove all

                if (Request.Form != null)
                {
                    var form = Request.Form;
                    foreach (var key in form.Keys)
                    {
                        if (key.Contains("~"))   // username~accesstype or username~_interpreter
                        {
                            var username = key.Substring(0, key.IndexOf("~"));

                            var accessRule = settings.AccessRules?.Where(a => a.Username.ToLower() == username.ToLower()).FirstOrDefault();
                            if (accessRule == null)
                            {
                                accessRule = new MapServiceSettings.MapServiceAccess();
                                var rules = new List<IMapServiceAccess>();
                                if (settings.AccessRules != null)
                                {
                                    rules.AddRange(settings.AccessRules);
                                }

                                rules.Add(accessRule);
                                settings.AccessRules = rules.ToArray();
                            }
                            accessRule.Username = username;


                            var rule = key.Substring(key.IndexOf("~") + 1, key.Length - key.IndexOf("~") - 1);

                            string serviceType = String.Empty;

                            if (rule == "_all")
                            {
                                serviceType = rule;
                            }
                            else if (rule.StartsWith("_") && _mapServerService.Interpreters
                                            .Select(t => new Framework.system.PlugInManager().CreateInstance<IServiceRequestInterpreter>(t))
                                            .Where(i => "_" + i.IdentityName.ToLower() == rule.ToLower())
                                            .Count() == 1)  // Interpreter
                            {
                                serviceType = rule.ToLower();
                            }
                            else if (Enum.TryParse<AccessTypes>(rule, true, out AccessTypes accessType))
                            {
                                serviceType = accessType.ToString();
                            }

                            if (!String.IsNullOrWhiteSpace(serviceType))
                            {
                                if (Convert.ToBoolean(form[key]) == true)
                                {
                                    accessRule.AddServiceType(serviceType);
                                }
                                else
                                {
                                    accessRule.RemoveServiceType(serviceType);
                                }
                            }
                        }
                    }
                }

                await mapService.SaveSettingsAsync();

                return Json(new
                {
                    success = true,
                    service = MapService2Json(mapService, settings)
                });
            });
        }

        [HttpGet]
        async public Task<IActionResult> FolderSecurity(string folder)
        {
            return await SecureApiCall(async () =>
            {
                folder = folder?.ToLower() ?? String.Empty;

                var mapService = _mapServerService.MapServices.Where(s => s.Type== MapServiceType.Folder && s.Fullname?.ToLower() == folder).FirstOrDefault();
                if (mapService == null)
                {
                    throw new MapServerException("Unknown folder: " + folder);
                }

                var settings = await mapService.GetSettingsAsync();

                List<string> allTypes = new List<string>(Enum.GetNames(typeof(FolderAccessTypes)).Select(n => n.ToLower()).Where(n => n != "none"));
                allTypes.Add("_all");

                var accessRules = settings.AccessRules;

                foreach (var interpreterType in _mapServerService.Interpreters)
                {
                    allTypes.Add("_" + ((IServiceRequestInterpreter)Activator.CreateInstance(interpreterType)).IdentityName.ToLower());
                }

                return Json(new
                {
                    allTypes = allTypes.ToArray(),
                    accessRules = accessRules,
                    allUsers = _loginManagerService.GetTokenUsernames(),
                    anonymousUsername = Identity.AnonyomousUsername,

                    onlineResource = settings.OnlineResource,
                    outputUrl = settings.OutputUrl
                });
            });
        }

        [HttpPost]
        async public Task<IActionResult> FolderSecurity()
        {
            return await SecureApiCall(async () =>
            {
                var folder = Request.Query["folder"].ToString().ToLower();

                var mapService = _mapServerService.MapServices.Where(s => s.Type== MapServiceType.Folder && s.Fullname?.ToLower() == folder).FirstOrDefault();
                if (mapService == null)
                {
                    throw new MapServerException("Unknown folder: " + folder);
                }

                var settings = await mapService.GetSettingsAsync();
                settings.AccessRules = null;  // Remove all

                if (Request.Form != null)
                {
                    var form = Request.Form;
                    foreach (var key in form.Keys)
                    {
                        if (key.Contains("~"))   // username~accesstype or username~_interpreter
                        {
                            var username = key.Substring(0, key.IndexOf("~"));

                            var accessRule = settings.AccessRules?.Where(a => a.Username.ToLower() == username.ToLower()).FirstOrDefault();
                            if (accessRule == null)
                            {
                                accessRule = new MapServiceSettings.MapServiceAccess();
                                var rules = new List<IMapServiceAccess>();
                                if (settings.AccessRules != null)
                                {
                                    rules.AddRange(settings.AccessRules);
                                }

                                rules.Add(accessRule);
                                settings.AccessRules = rules.ToArray();
                            }
                            accessRule.Username = username;


                            var rule = key.Substring(key.IndexOf("~") + 1, key.Length - key.IndexOf("~") - 1);

                            string serviceType = String.Empty;

                            if (rule == "_all")
                            {
                                serviceType = rule;
                            }
                            else if (rule.StartsWith("_") && _mapServerService.Interpreters
                                            .Select(t => new Framework.system.PlugInManager().CreateInstance<IServiceRequestInterpreter>(t))
                                            .Where(i => "_" + i.IdentityName.ToLower() == rule.ToLower())
                                            .Count() == 1)  // Interpreter
                            {
                                serviceType = rule.ToLower();
                            }
                            else if (Enum.TryParse<FolderAccessTypes>(rule, true, out FolderAccessTypes accessType))
                            {
                                serviceType = accessType.ToString();
                            }

                            if (!String.IsNullOrWhiteSpace(serviceType))
                            {
                                if (Convert.ToBoolean(form[key]) == true)
                                {
                                    accessRule.AddServiceType(serviceType);
                                }
                                else
                                {
                                    accessRule.RemoveServiceType(serviceType);
                                }
                            }
                        }
                        else 
                        {
                            switch(key)
                            {
                                case "advancedsettings_onlineresource":
                                    settings.OnlineResource = String.IsNullOrWhiteSpace(form[key]) ? null : form[key].ToString();
                                    break;
                                case "advancedsettings_outputurl":
                                    settings.OutputUrl = String.IsNullOrWhiteSpace(form[key]) ? null : form[key].ToString();
                                    break;
                            }
                        }
                    }
                }

                await mapService.SaveSettingsAsync();

                return Json(new
                {
                    success = true,
                    folder = MapService2Json(mapService, settings)
                });
            });
        }

        #endregion

        #region Security

        public IActionResult TokenUsers()
        {
            return SecureApiCall(() =>
            {
                return Json(new { users = _loginManagerService.GetTokenUsernames() });
            });
        }

        [HttpPost]
        public IActionResult CreateTokenUser(CreateTokenUserModel model)
        {
            return SecureApiCall(() =>
            {
                model.NewUsername = model.NewUsername?.Trim() ?? String.Empty;
                model.NewPassword = model.NewPassword?.Trim() ?? String.Empty;

                _loginManagerService.CreateTokenLogin(model.NewUsername, model.NewPassword);

                return Json(new { success = true });
            });
        }

        [HttpPost]
        public IActionResult ChangeTokenUserPassword(ChangeTokenUserPasswordModel model)
        {
            return SecureApiCall(() =>
            {
                model.Username = model.Username?.Trim() ?? String.Empty;
                model.NewPassword = model.NewPassword?.Trim() ?? String.Empty;

                _loginManagerService.ChangeTokenUserPassword(model.Username, model.NewPassword);

                return Json(new { success = true });
            });
        }

        #endregion

        #region Helper

        private IActionResult SecureApiCall(Func<IActionResult> func)
        {
            try
            {
                var authToken = _loginManagerService.GetAuthToken(this.Request);
                if (!authToken.IsManageUser)
                {
                    throw new Exception("Not allowed");
                }

                return func.Invoke();
            }
            catch (NotAuthorizedException)
            {
                return Json(new { success = false, error = "not authorized" });
            }
            catch (MapServerException mse)
            {
                return Json(new { success = false, error = mse.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, error = "unknown error" });
            }
        }

        async private Task<IActionResult> SecureApiCall(Func<Task<IActionResult>> func)
        {
            try
            {
                var authToken = _loginManagerService.GetAuthToken(this.Request);
                if (!authToken.IsManageUser)
                {
                    throw new Exception("Not allowed");
                }

                return await func.Invoke();
            }
            catch (NotAuthorizedException)
            {
                return Json(new { success = false, error = "not authorized" });
            }
            catch (MapServerException mse)
            {
                return Json(new { success = false, error = mse.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, error = "unknown error" });
            }
        }

        private object MapService2Json(IMapService mapService, IMapServiceSettings settings)
        {
            return new
            {
                name = mapService.Name,
                folder = mapService.Folder,
                status = (settings?.Status ?? MapServer.MapServiceStatus.Running).ToString().ToLower(),
                hasSecurity = settings?.AccessRules != null && settings.AccessRules.Length > 0,
                runningSince = settings?.Status == MapServiceStatus.Running && mapService.RunningSinceUtc.HasValue ?
                    mapService.RunningSinceUtc.Value.ToShortDateString() + " " + mapService.RunningSinceUtc.Value.ToLongTimeString() + " (UTC)" :
                    String.Empty,
                hasErrors = _logger.LogFileExists(mapService.Fullname, Framework.system.loggingMethod.error)
            };
        }

        #endregion
    }
}