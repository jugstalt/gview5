﻿using Newtonsoft.Json;

namespace gView.Interoperability.GeoServices.Rest.Json.Renderers.SimpleRenderers
{
    public class SimpleMarkerSymbol
    {
        public SimpleMarkerSymbol()
        {
            this.Type = "esriSMS";
        }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("color")]
        public int[] Color { get; set; }

        [JsonProperty("size")]
        public float Size { get; set; }

        [JsonProperty("angle")]
        public float Angle { get; set; }

        [JsonProperty("xoffset")]
        public float Xoffset { get; set; }

        [JsonProperty("yoffset")]
        public float Yoffset { get; set; }


        [JsonProperty("outline")]
        public SimpleLineSymbol Outline { get; set; }
    }

    /*
    Simple Marker Symbol
    Simple marker symbols can be used to symbolize point geometries. The type property for simple marker symbols is esriSMS. The angle property defines the number of degrees (0 to 360) that a marker symbol is rotated. The rotation is from East in a counter-clockwise direction where East is the 0° axis.
    New in 10.1

    Support for esriSMSTriangle was added.

    JSON Syntax
    {
    "type" : "esriSMS",
    "style" : "< esriSMSCircle | esriSMSCross | esriSMSDiamond | esriSMSSquare | esriSMSX | esriSMSTriangle >",
    "color" : <color>,
    "size" : <size>,
    "angle" : <angle>,
    "xoffset" : <xoffset>,
    "yoffset" : <yoffset>,
    "outline" : { //if outline has been specified
      "color" : <color>,
      "width" : <width>
    }
    }

    JSON Example
    {
    "type": "esriSMS",
     "style": "esriSMSSquare",
     "color": [76,115,0,255],
     "size": 8,
     "angle": 0,
     "xoffset": 0,
     "yoffset": 0,
     "outline": 
      {
      "color": [152,230,0,255],
       "width": 1
      }
    }    
    */

}

