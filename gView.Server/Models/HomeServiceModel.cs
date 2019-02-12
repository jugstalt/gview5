﻿using gView.Framework.Carto;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.Server.Models
{
    public class HomeServiceModel
    {
        public string Server { get; set; }
        public string OnlineResource { get; set; }
        public IMapService MapService { get; set; }
        public IEnumerable<IServiceRequestInterpreter> Interpreters { get; set; }

        public string ReplaceRequest(string request)
        {
            return request?
                .Replace("{server}", Server)
                .Replace("{onlineresource}", OnlineResource)
                .Replace("{service}", MapService.Name)
                .Replace("{folder}", MapService.Folder)
                .Replace("{folder/service}", (String.IsNullOrWhiteSpace(MapService.Folder) ? "" : MapService.Folder + "/") + MapService.Name)
                .Replace("{folder@service}", (String.IsNullOrWhiteSpace(MapService.Folder) ? "" : MapService.Folder + "@") + MapService.Name);
        }
    }
}
