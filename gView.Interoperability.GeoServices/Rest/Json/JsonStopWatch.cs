﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Interoperability.GeoServices.Rest.Json
{
    public class JsonStopWatch
    {
        [JsonProperty("_duration_ms")]
        public double DurationMilliseconds { get; set; }
    }
}
