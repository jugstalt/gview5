﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gView.Framework.system;
using gView.Interoperability.ArcXML;
using gView.MapServer;
using gView.Server.AppCode;
using Microsoft.AspNetCore.Mvc;

namespace gView.Server.Controllers
{
    public class ArcIMSController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public Task<IActionResult> EsriMap(string cmd, string ServiceName)
        {
            return EsriMap(cmd, ServiceName, String.Empty);
        }

        [HttpPost]
        async public Task<IActionResult> EsriMap(string cmd, string ServiceName, string content)
        {
            #region Security

            Identity identity = Identity.FromFormattedString(base.GetAuthToken().Username);

            #endregion

            if (cmd == "ping")
            {
                return Result("gView MapServer Instance v" + gView.Framework.system.SystemVariables.gViewVersion.ToString(), "text/plain");
            }
            if (cmd == "getversion")
            {
                return Result(gView.Framework.system.SystemVariables.gViewVersion.ToString(), "text/plain");
            }
            if(cmd=="capabilities")
            {
                content = @"<?xml version=""1.0"" encoding=""UTF-8""?><ARCXML version=""1.1""><REQUEST><GET_SERVICE_INFO fields=""true"" envelope=""true"" renderer=""true"" extensions=""true"" /></REQUEST></ARCXML>";
            }

            var interpreter = InternetMapServer.GetInterpreter(typeof(ArcXMLRequest));

            #region Request 

            if(String.IsNullOrEmpty(content) && Request.Body.CanRead)
            {
                MemoryStream ms = new MemoryStream();

                byte[] bodyData = new byte[1024];
                int bytesRead;
                while ((bytesRead = Request.Body.Read(bodyData, 0, bodyData.Length)) > 0)
                {
                    ms.Write(bodyData, 0, bytesRead);
                }
                content = Encoding.UTF8.GetString(ms.ToArray());
            }

            ServiceRequest serviceRequest = new ServiceRequest(ServiceName.ServiceName(), ServiceName.FolderName(), content)
            {
                Identity = identity,
                OnlineResource = InternetMapServer.OnlineResource
            };

            #endregion

            #region Queue & Wait

            IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                InternetMapServer.Instance,
                interpreter,
                serviceRequest);

            await InternetMapServer.TaskQueue.AwaitRequest(interpreter.Request, context);

            #endregion

            return Result(serviceRequest.Response, "text/xml");
        }

        #region Helper

        private IActionResult Result(string response, string contentType)
        {
            ViewData["content-type"] = contentType;
            ViewData["data"] = Encoding.UTF8.GetBytes(response);

            return View("_binary");
        }

        #endregion
    }
}