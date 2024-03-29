﻿using gView.Interoperability.GeoServices.Rest.Reflection;
using Newtonsoft.Json;
using System;

namespace gView.Interoperability.GeoServices.Rest.Json
{
    [ServiceMethod("ExportMap", "export")]
    [ServiceMethod("All Layers", "layers")]
    [ServiceMethod("Legend", "legend")]
    [ServiceMethod("Identify", "identify")]
    public class JsonMapService
    {
        public JsonMapService()
        {
            this.Layers = new Layer[0];
            this.Tables = new Table[0];
            this.DocumentInfo = new DocumentInfoClass()
            {
                Title = String.Empty,
                Author = String.Empty,
                Comments = String.Empty,
                Category = String.Empty,
                Subject = String.Empty,
                Keywords = String.Empty
            };
            SupportedImageFormats = "PNG32,PNG24,PNG,JPG";
            Capabilities = "Map,Query";
            SupportedQueryFormats = "JSON";
            DatumTransformations = new DatumTransformationsClass[0];
            SupportsDatumTransformation = true;
            Units = "esriMeters";

            MaxRecordCount = 1000;
            MaxImageWidth = MaxImageHeight = 4096;
            SupportedExtensions = String.Empty;

            this.MapName = "Layers";
        }

        [JsonProperty(PropertyName = "currentVersion")]
        public double CurrentVersion { get; set; }

        [JsonProperty(PropertyName = "mapName")]
        public string MapName { get; set; }

        [JsonProperty(PropertyName = "serviceDescription")]
        public string ServiceDescription { get; set; }

        [JsonProperty(PropertyName = "copyrightText")]
        public string CopyrightText { get; set; }

        [JsonProperty(PropertyName = "supportsDynamicLayers")]
        public bool SupportsDynamicLayers => true;

        [JsonProperty(PropertyName = "layers")]
        public Layer[] Layers { get; set; }

        [JsonProperty(PropertyName = "tables")]
        public Table[] Tables { get; set; }

        [JsonProperty(PropertyName = "singleFusedMapCache")]
        public bool SingleFusedMapCache => false;

        [JsonProperty(PropertyName = "spatialReference")]
        public SpatialReference SpatialReferenceInstance { get; set; }

        [JsonProperty(PropertyName = "fullExtent")]
        public Extent FullExtent { get; set; }

        [JsonProperty(PropertyName = "initialExtent")]
        public JsonMapService.Extent InitialExtend { get; set; }

        [JsonProperty(PropertyName = "minScale")]
        public int MinScale { get; set; }

        [JsonProperty(PropertyName = "maxScale")]
        public int MaxScale { get; set; }

        [JsonProperty(PropertyName = "units")]
        public string Units { get; set; }

        [JsonProperty(PropertyName = "supportedImageFormatTypes")]
        public string SupportedImageFormats { get; set; }

        [JsonProperty(PropertyName = "documentInfo")]
        public DocumentInfoClass DocumentInfo { get; set; }

        [JsonProperty(PropertyName = "capabilities")]
        public string Capabilities { get; set; }

        [JsonProperty(PropertyName = "supportedQueryFormats")]
        public string SupportedQueryFormats { get; set; }

        [JsonProperty(PropertyName = "exportTilesAllowed")]
        public bool exportTilesAllowed => false;

        [JsonProperty(PropertyName = "datumTransformations")]
        public DatumTransformationsClass[] DatumTransformations { get; set; }

        [JsonProperty(PropertyName = "supportsDatumTransformation")]
        public bool SupportsDatumTransformation { get; set; }

        [JsonProperty(PropertyName = "maxRecordCount")]
        public int MaxRecordCount { get; set; }

        [JsonProperty(PropertyName = "maxImageHeight")]
        public int MaxImageHeight { get; set; }

        [JsonProperty(PropertyName = "maxImageWidth")]
        public int MaxImageWidth { get; set; }

        [JsonProperty(PropertyName = "supportedExtensions")]
        public string SupportedExtensions { get; set; }

        #region Classes

        [YamlGroupBy("ParentLayerId")]
        public class Layer : JsonIdName
        {
            public Layer()
            {
                this.ParentLayerId = -1;
            }


            [JsonProperty(PropertyName = "parentLayerId")]
            public int ParentLayerId { get; set; }

            [JsonProperty(PropertyName = "defaultVisibility")]
            public bool DefaultVisibility { get; set; }

            [JsonProperty(PropertyName = "subLayerIds")]
            public int[] SubLayersIds { get; set; }

            [JsonProperty(PropertyName = "minScale")]
            public double MinScale { get; set; }

            [JsonProperty(PropertyName = "maxScale")]
            public double MaxScale { get; set; }

            [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
            public string LayerType { get; set; }

            [JsonProperty(PropertyName = "geometryType", NullValueHandling = NullValueHandling.Ignore)]
            public string GeometryType { get; set; }
        }

        public class Table { }

        public class Extent
        {
            [JsonProperty(PropertyName = "xmin")]
            public double XMin { get; set; }

            [JsonProperty(PropertyName = "ymin")]
            public double YMin { get; set; }

            [JsonProperty(PropertyName = "xmax")]
            public double XMax { get; set; }

            [JsonProperty(PropertyName = "ymax")]
            public double YMax { get; set; }

            [JsonProperty(PropertyName = "spatialReference")]
            public SpatialReference SpatialReference { get; set; }
        }

        public class SpatialReference
        {
            public SpatialReference()
            {

            }

            public SpatialReference(int wkid)
            {
                this.Wkid = this.LatestWkid = wkid;
                //this.Wkt = "PROJCS[\"Austria_Gauss_Krueger_M34_Nord_5Mio\",GEOGCS[\"GCS_BESSEL_AUT\",DATUM[\"D_BESSEL_AUT\",SPHEROID[\"Bessel_1841\",6377397.155,299.1528128]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"false_easting\",0.0],PARAMETER[\"false_northing\",-5000000.0],PARAMETER[\"central_meridian\",16.33333333],PARAMETER[\"scale_factor\",1.0],PARAMETER[\"latitude_of_origin\",0.0],UNIT[\"Meter\",1.0]]";
            }

            [JsonProperty("wkt", NullValueHandling = NullValueHandling.Ignore)]
            public string Wkt { get; set; }

            [JsonProperty(PropertyName = "wkid")]
            public int Wkid { get; set; }

            [JsonProperty(PropertyName = "latestWkid")]
            public int LatestWkid { get; set; }
        }

        public class DocumentInfoClass
        {
            [JsonProperty(PropertyName = "Title")]
            public string Title { get; set; }

            [JsonProperty(PropertyName = "Author")]
            public string Author { get; set; }

            [JsonProperty(PropertyName = "Comments")]
            public string Comments { get; set; }

            [JsonProperty(PropertyName = "Subject")]
            public string Subject { get; set; }

            [JsonProperty(PropertyName = "Category")]
            public string Category { get; set; }

            [JsonProperty(PropertyName = "AntialiasingMode")]
            public string AntialiasingMode => "None";

            [JsonProperty(PropertyName = "TextAntialiasingMode")]
            public string TextAliasingMode => "Force";

            [JsonProperty(PropertyName = "Keywords")]
            public string Keywords { get; set; }
        }

        public class DatumTransformationsClass
        {
            [JsonProperty(PropertyName = "geoTransforms")]
            public GeoTransformation[] GeoTransforms { get; set; }
        }

        public class GeoTransformation : SpatialReference
        {
            [JsonProperty(PropertyName = "geoTransforms")]
            public bool TransformForward { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }

        #endregion
    }
}
