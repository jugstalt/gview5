﻿using Newtonsoft.Json;

namespace gView.Interoperability.GeoServices.Rest.Json.Response
{
    public class JsonExportResponse : JsonStopWatch
    {
        [JsonProperty("href", NullValueHandling = NullValueHandling.Ignore)]
        public string Href { get; set; }

        [JsonProperty("imageData", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageData { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("extent")]
        public JsonExtent Extent { get; set; }

        [JsonProperty("scale")]
        public double Scale { get; set; }
    }
}
