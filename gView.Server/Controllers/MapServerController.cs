using gView.Core.Framework.Exceptions;
using gView.Framework.system;
using gView.MapServer;
using gView.Server.AppCode;
using gView.Server.AppCode.Extensions;
using gView.Server.Models;
using gView.Server.Services.MapServer;
using gView.Server.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.Server.Controllers
{
    public class MapServerController : BaseController
    {
        private readonly MapServiceManager _mapServiceManager;
        private readonly MapServiceDeploymentManager _mapServiceDeploymentMananger;
        private readonly LoginManager _loginMananger;
        private readonly AccessControlService _accessControl;

        public MapServerController(
            MapServiceManager mapServiceMananger,
            MapServiceDeploymentManager mapServiceDeploymentManager,
            LoginManager loginMananger,
            AccessControlService accessControl,
            EncryptionCertificateService encryptionCertificateService)
            : base(mapServiceMananger, loginMananger, encryptionCertificateService)
        {
            _mapServiceManager = mapServiceMananger;
            _mapServiceDeploymentMananger = mapServiceDeploymentManager;
            _loginMananger = loginMananger;
            _accessControl = accessControl;
        }

        public IActionResult Index()
        {
            return View();
        }

        async public Task<IActionResult> Catalog(string format)
        {
            try
            {
                //string user, pwd;
                //var request = Request(out user, out pwd);


                #region Security

                var credentials = GetRequestCredentials();
                IIdentity identity = String.IsNullOrEmpty(credentials.user) ?
                    Identity.FromFormattedString(credentials.user) :
                    _accessControl.GetIdentity(credentials.user, credentials.password);

                #endregion

                #region Collect Services

                var servicesModel = new ServicesModel() { Services = new List<ServiceModel>() };

                foreach (var service in await _mapServiceManager.Instance.Maps(identity))
                {
                    if (service.Type == MapServiceType.Folder)  // if accessable for current user... => ToDo!!!
                    {
                        _mapServiceManager.ReloadServices(service.Name);
                        foreach (var folderService in _mapServiceManager.MapServices.Where(s => s.Folder == service.Name))
                        {
                            if (await folderService.HasAnyAccess(identity))
                            {
                                servicesModel.Services.Add(new ServiceModel() { Name = folderService.Fullname, Type = folderService.Type.ToInvariantString() });
                            }
                        }
                    }
                    else if (service.Type == MapServiceType.MXL && String.IsNullOrWhiteSpace(service.Folder))
                    {
                        servicesModel.Services.Add(new ServiceModel() { Name = service.Fullname, Type = service.Type.ToInvariantString() });
                    }
                }

                #endregion

                if ("xml".Equals(format, StringComparison.InvariantCultureIgnoreCase))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<RESPONSE><SERVICES>");
                    foreach (var serviceModel in servicesModel.Services)
                    {
                        sb.Append("<SERVICE ");
                        sb.Append($"NAME='{serviceModel.Name}' ");
                        sb.Append($"name='{serviceModel.Name}' ");
                        sb.Append($"type='{serviceModel.Type.ToString()}' ");
                        sb.Append("/>");
                    }
                    sb.Append("</SERVICES></RESPONSE>");

                    return Result(sb.ToString(), "text/xml");
                }
                else if ("json".Equals(format, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new JsonResult(servicesModel);
                }
                else
                {
                    throw new Exception("Unknown format");
                }
            }
            catch (NotAuthorizedException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                if ("xml".Equals(format, StringComparison.InvariantCultureIgnoreCase))
                {
                    return WriteError(ex.Message);
                }
            }

            return null;
        }

        async public Task<IActionResult> MapRequest(string guid, string name, string folder)
        {
            try
            {
                if (IfMatch())
                {
                    return NotModified();
                }

                #region Security

                Identity identity = Identity.FromFormattedString(_loginMananger.GetAuthToken(this.Request).Username);

                #endregion

                if (!String.IsNullOrWhiteSpace(folder))
                {
                    name = folder + "/" + name;
                }

                //DateTime td = DateTime.Now;
                //Console.WriteLine("Start Map Request " + td.ToLongTimeString() + "." + td.Millisecond + " (" + name + ")");
                //System.Threading.Thread.Sleep(10000);

                //string user, pwd;
                //var request = Request(out user, out pwd);

                #region Request

                string input = await GetBody();
                if (String.IsNullOrEmpty(input))
                {
                    input = this.Request.QueryString.ToString();
                }

                if (input.StartsWith("?"))
                {
                    input = input.Substring(1);
                }

                ServiceRequest serviceRequest = new ServiceRequest(name.ServiceName(), name.FolderName(), input)
                {
                    OnlineResource = _mapServiceManager.Options.OnlineResource + "/MapRequest/" + guid + "/" + name,
                    OutputUrl = _mapServiceManager.Options.OutputUrl,
                    Identity = identity
                };

                #endregion

                IServiceRequestInterpreter interpreter =
                    _mapServiceManager.GetInterpreter(new Guid(guid));

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServiceManager.Instance,
                    interpreter,
                    serviceRequest);

                await _mapServiceManager.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                string ret = serviceRequest.ResponseAsString;

                return Result(ret, "text/xml");
            }
            catch (MapServerException mse)
            {
                return WriteError(mse.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return WriteUnauthorized();
            }
        }

        #region Manage

        [HttpPost]
        async public Task<IActionResult> AddMap(string name, string folder)
        {
            try
            {
                string input = await GetBody();

                var credentials = GetRequestCredentials();

                name = String.IsNullOrWhiteSpace(folder) ? name : folder + "/" + name;

                bool ret = await _mapServiceDeploymentMananger.AddMap(name, input, credentials.user, credentials.password);

                return Result(ret.ToString(), "text/plain");
            }
            catch (MapServerException mse)
            {
                return WriteError(mse.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return WriteUnauthorized();
            }
        }

        [HttpPost]
        async public Task<IActionResult> RemoveMap(string name, string folder)
        {
            try
            {
                string user = String.Empty, pwd = String.Empty;
                // var request = Request(out user, out pwd);

                name = String.IsNullOrWhiteSpace(folder) ? name : folder + "/" + name;

                bool ret = await _mapServiceDeploymentMananger.RemoveMap(name, user, pwd);

                return Result(ret.ToString(), "text/plain");
            }
            catch (MapServerException mse)
            {
                return WriteError(mse.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return WriteUnauthorized();
            }
        }

        async public Task<IActionResult> GetMetadata(string name, string folder)
        {
            try
            {
                string user = String.Empty, pwd = String.Empty;
                // var request = Request(out user, out pwd);

                name = String.IsNullOrWhiteSpace(folder) ? name : folder + "/" + name;

                string ret = await _mapServiceDeploymentMananger.GetMetadata(name, user, pwd);

                return Result(ret, "text/xml");
            }
            catch (MapServerException mse)
            {
                return WriteError(mse.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return WriteUnauthorized();
            }
        }

        [HttpPost]
        async public Task<IActionResult> SetMetadata(string name, string folder)
        {
            try
            {
                string input = await GetBody();

                name = String.IsNullOrWhiteSpace(folder) ? name : folder + "/" + name;

                string user = String.Empty, pwd = String.Empty;
                // var request = Request(out user, out pwd);

                bool ret = await _mapServiceDeploymentMananger.SetMetadata(name, input, user, pwd);

                return Result(ret.ToString(), "text/plain");
            }
            catch (MapServerException mse)
            {
                return WriteError(mse.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return WriteUnauthorized();
            }
        }

        #endregion

        #region Helper

        private IActionResult WriteUnauthorized()
        {
            return WriteError("Unauthorized");
        }

        private IActionResult WriteError(string message)
        {
            return Result("<ERROR>" + message + "</ERROR>", "text/xml");
        }

        async private Task<string> GetBody()
        {
            if (Request.Body.CanRead)
            {
                using (var reader = new StreamReader(Request.Body))
                {
                    var body = await reader.ReadToEndAsync();
                    return body;
                }
            }

            return String.Empty;
        }

        private IActionResult Result(string response, string contentType)
        {
            byte[] data = null;
            if (response.StartsWith("base64:"))
            {
                response = response.Substring("base64:".Length);
                if (response.Contains(":"))
                {
                    int pos = response.IndexOf(":");
                    contentType = response.Substring(0, pos);
                    response = response.Substring(pos + 1);
                }
                data = Convert.FromBase64String(response);
            }
            else if (response.StartsWith("{"))
            {
                try
                {
                    var mapServerResponse = gView.Framework.system.MapServerResponse.FromString(response);

                    if (mapServerResponse.Expires != null)
                    {
                        AppendEtag((DateTime)mapServerResponse.Expires);
                    }

                    return Result(mapServerResponse.Data, mapServerResponse.ContentType);
                }
                catch { }
            }
            else
            {
                data = Encoding.UTF8.GetBytes(response);
            }
            return Result(data, contentType);
        }

        private IActionResult Result(byte[] data, string contentType)
        {
            //ViewData["content-type"] = contentType;
            //ViewData["data"] = data;

            //return View("_binary");
            return File(data, contentType);
        }

        private (string user, string password) GetRequestCredentials()
        {
            string user = String.Empty, password = String.Empty;
            string auth64 = this.Request.Headers["Authorization"];

            if (auth64 != null)
            {
                if (auth64.ToLower().StartsWith("basic "))
                {
                    auth64 = auth64.Substring("basic ".Length);
                }

                if (!String.IsNullOrEmpty(auth64))
                {
                    string auth = Encoding.ASCII.GetString(Convert.FromBase64String(auth64));
                    int index = auth.IndexOf(":");
                    if (index > 0)
                    {
                        user = auth.Substring(0, index);
                        password = auth.Substring(index + 1);
                    }
                }
            }

            return (user, password);
        }

        #region ETag

        private new bool HasIfNonMatch()
        {
            return this.Request.Headers["If-None-Match"].ToString() != null;
        }

        private new bool IfMatch()
        {
            try
            {
                if (HasIfNonMatch() == false)
                {
                    return false;
                }

                var etag = long.Parse(this.Request.Headers["If-None-Match"].ToString());

                DateTime etagTime = new DateTime(etag, DateTimeKind.Utc);
                if (DateTime.UtcNow > etagTime)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private new void AppendEtag(DateTime expires)
        {
            this.Request.Headers.Append("ETag", expires.Ticks.ToString());
            this.Request.Headers.Append("Last-Modified", DateTime.UtcNow.ToString("R"));
            this.Request.Headers.Append("Expires", expires.ToString("R"));

            //this.Response.CacheControl = "private";
            //this.Response.Cache.SetMaxAge(new TimeSpan(24, 0, 0));
        }

        protected new IActionResult NotModified()
        {
            Response.StatusCode = 304;
            return Content(String.Empty);
        }

        #endregion

        #endregion
    }
}