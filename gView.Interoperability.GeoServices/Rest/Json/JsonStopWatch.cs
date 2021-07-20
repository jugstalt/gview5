﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Interoperability.GeoServices.Rest.Json
{
    public class JsonStopWatch : JsonError
    {
        [JsonProperty("_duration_ms")]
        public double DurationMilliseconds { get; set; }

        [JsonProperty("_size_bytes", NullValueHandling = NullValueHandling.Ignore)]
        public int? SizeBytes { get; set; }
    }
}
