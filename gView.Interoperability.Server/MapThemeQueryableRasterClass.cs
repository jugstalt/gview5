﻿using gView.Framework.Data;
using gView.Framework.IO;
using gView.Framework.system;
using gView.MapServer;
using gView.Server.Connector;
using System;
using System.Threading.Tasks;

namespace gView.Interoperability.Server
{
    internal class MapThemeQueryableRasterClass : gView.Framework.XML.AXLQueryableRasterClass
    {
        public MapThemeQueryableRasterClass(IDataset dataset, string id)
            : base(dataset, id)
        {
        }

        protected override Task<string> SendRequest(IUserData userData, string axlRequest)
        {
            if (_dataset == null)
            {
                return Task.FromResult(String.Empty);
            }

            string server = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "server");
            string service = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "service");

            IServiceRequestContext context = (userData != null) ? userData.GetUserData("IServiceRequestContext") as IServiceRequestContext : null;
            string user = ConfigTextStream.ExtractValue(_dataset.ConnectionString, "user");
            string pwd = Identity.HashPassword(ConfigTextStream.ExtractValue(_dataset.ConnectionString, "pwd"));

            if ((user == "#" || user == "$") &&
                    context != null && context.ServiceRequest != null && context.ServiceRequest.Identity != null)
            {
                string roles = String.Empty;
                if (user == "#" && context.ServiceRequest.Identity.UserRoles != null)
                {
                    foreach (string role in context.ServiceRequest.Identity.UserRoles)
                    {
                        if (String.IsNullOrEmpty(role))
                        {
                            continue;
                        }

                        roles += "|" + role;
                    }
                }
                user = context.ServiceRequest.Identity.UserName + roles;
                // ToDo:
                //pwd = context.ServiceRequest.Identity.HashedPassword;
            }

            ServerConnection conn = new ServerConnection(server);
            try
            {
                return Task.FromResult(conn.Send(service, axlRequest, "BB294D9C-A184-4129-9555-398AA70284BC", user, pwd));
            }
            catch (Exception ex)
            {
                MapServerClass.ErrorLog(context, "Query", server, service, ex);
                return Task.FromResult(String.Empty);
            }
        }
    }
}