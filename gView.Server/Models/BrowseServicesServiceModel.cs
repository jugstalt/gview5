using gView.MapServer;
using System;
using System.Collections.Generic;

namespace gView.Server.Models
{
    public class BrowseServicesServiceModel
    {
        public string Server { get; set; }
        public string OnlineResource { get; set; }
        public IMapService MapService { get; set; }
        public IEnumerable<IServiceRequestInterpreter> Interpreters { get; set; }
    }
}
