﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace gView.Interoperability.GeoServices.Rest.Json
{
    public class JsonStopWatch : JsonError
    {
        [JsonProperty("_duration_ms")]
        public double DurationMilliseconds { get; set; }

        [JsonProperty("_idle_ms", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string,double> IdleMilliseconds { get; set; }

        [JsonProperty("_size_bytes", NullValueHandling = NullValueHandling.Ignore)]
        public int? SizeBytes { get; set; }
    }
}
