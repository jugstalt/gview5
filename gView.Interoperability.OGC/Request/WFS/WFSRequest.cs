using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.OGC.GML;
using gView.Framework.OGC.WFS;
using gView.Framework.system;
using gView.Interoperability.OGC.Dataset.GML;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace gView.Interoperability.OGC
{
    [gView.Framework.system.RegisterPlugIn("391FA941-3E31-456e-8A3A-703E07962BA6")]
    public class WFSRequest : IServiceRequestInterpreter
    {
        private IMapServer _mapServer = null;
        protected bool _useTOC;

        public WFSRequest()
        {
            _useTOC = false;
        }

        #region IServiceRequestInterpreter Member

        public void OnCreate(IMapServer mapServer)
        {
            _mapServer = mapServer;
        }

        async public Task Request(IServiceRequestContext context)
        {
            if (context == null || context.ServiceRequest == null)
            {
                return;
            }

            if (_mapServer == null)
            {
                return;
            }

            WFSParameterDescriptor parameters = new WFSParameterDescriptor();
            string requestString = context.ServiceRequest.Request.Trim();
            if (requestString[0] == '<')
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(requestString);

                if (!parameters.ParseParameters(doc))
                {
                    return;
                }
            }
            else
            {
                if (!parameters.ParseParameters(context.ServiceRequest.Request.Split('&')))
                {
                    return;
                }
            }

            //ServiceRequestContext context = new ServiceRequestContext(_mapServer, this, request);
            switch (parameters.Request)
            {
                case WFSRequestType.GetCapabilities:
                    context.ServiceRequest.Response = await WFS_GetCapabilities(context.ServiceRequest.OnlineResource, context.ServiceRequest.Service, parameters, context);
                    context.ServiceRequest.ResponseContentType = "text/xml";
                    break;
                case WFSRequestType.DescribeFeatureType:
                    context.ServiceRequest.Response = await WFS_DescribeFeatureType(context.ServiceRequest.Service, parameters, context);
                    context.ServiceRequest.ResponseContentType = "text/xml";
                    break;
                case WFSRequestType.GetFeature:
                    context.ServiceRequest.Response = await WFS_GetFeature(context.ServiceRequest.OnlineResource, context.ServiceRequest.Service, parameters, context);
                    context.ServiceRequest.ResponseContentType = "text/xml";
                    break;
            }
        }

        public AccessTypes RequiredAccessTypes(IServiceRequestContext context)
        {
            var accessTypes = AccessTypes.Query;

            return accessTypes;
        }

        public string IdentityName => "wfs";
        public string IdentityLongName => "OGC Web Feature Service";

        public int Priority => 50;

        public InterpreterCapabilities Capabilities
        {
            get
            {
                return new InterpreterCapabilities(new InterpreterCapabilities.Capability[]{
                    new InterpreterCapabilities.LinkCapability("GetCapabilities","{server}/ogc/{folder@service}?VERSION=1.0.0&SERVICE=WFS&REQUEST=GetCapabilities","1.0.0"),
                    //new InterpreterCapabilities.SimpleCapability("DescribeFeatureType","{server}/ogc/{service}?VERSION=1.0.0&SERVICE=WFS&REQUEST=DescribeFeatureType&...","1.0.0"),
                    //new InterpreterCapabilities.SimpleCapability("GetFeature","{onlineresource}VERSION=1.0.0&SERVICE=WFS&REQUEST=GetFeatureInfo&...","1.0.0"),     
                    new InterpreterCapabilities.LinkCapability("GetCapabilities","{server}/ogc/{folder@service}?VERSION=1.1.0&SERVICE=WFS&REQUEST=GetCapabilities","1.1.0"),
                    //new InterpreterCapabilities.SimpleCapability("DescribeFeatureType","{server}/ogc/{service}?VERSION=1.1.0&SERVICE=WFS&REQUEST=DescribeFeatureType&...","1.1.0"),
                    //new InterpreterCapabilities.SimpleCapability("GetFeature","{onlineresource}VERSION=1.1.0&SERVICE=WFS&REQUEST=GetFeatureInfo&...","1.1.0"),
                    //new InterpreterCapabilities.SimpleCapability("DescribeFeatureType",InterpreterCapabilities.Method.Post, "{onlineresource}SERVICE=WFS","1.0.0, 1.1.0"),
                    //new InterpreterCapabilities.SimpleCapability("GetFeature",InterpreterCapabilities.Method.Post, "{onlineresource}SERVICE=WFS","1.0.0, 1.1.0")
                }
                );
            }
        }

        #endregion

        async private Task<string> WFS_GetCapabilities(string OnlineResource, string service, WFSParameterDescriptor parameters, IServiceRequestContext context)
        {
            try
            {
                if (_mapServer == null || parameters == null)
                {
                    return "";
                }

                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "WFS GetCapabilities");

                //if (parameters != null)
                //    _mapServer.Log(loggingMethod.request_detail, "WMS: " + Concat(parameters, "&"));

                using (IServiceMap map = await context.CreateServiceMapInstance()) // _mapServer[context];
                {
                    if (map == null)
                    {
                        return "";
                    }

                    WFS_Export_Metadata exMetadata = map.MetadataProvider(new Guid("A2342AC5-9FD6-47a4-A25A-F684C375C895")) as WFS_Export_Metadata;
                    WFS_Export_Metadata.Metadata metadata = ((exMetadata != null) ? exMetadata.Data : null);
                    if (metadata == null)
                    {
                        metadata = new WFS_Export_Metadata.Metadata(
                                ((map.Display != null && map.Display.SpatialReference != null) ?
                                map.Display.SpatialReference.Name : String.Empty));
                    }

                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    string sOnlineResource = OnlineResource /*MapServerConnectors.OnlineResource(PlugInManager.PlugInID(this))*/ + "?SERVICE=WFS&";
                    sOnlineResource = sOnlineResource.Replace("??", "?");

                    if (parameters.Version == "1.0.0")
                    {
                        #region Version 1.0.0
                        gView.Framework.OGC.WFS.Version_1_0_0.WFS_CapabilitiesType caps = new Framework.OGC.WFS.Version_1_0_0.WFS_CapabilitiesType();
                        caps.Service = new Framework.OGC.WFS.Version_1_0_0.ServiceType();
                        caps.Service.Name = caps.Service.Title = caps.Service.Abstract = map.Name;
                        caps.Service.OnlineResource = sOnlineResource;
                        caps.Capability = new gView.Framework.OGC.WFS.Version_1_0_0.CapabilityType();
                        caps.Capability.Request = new gView.Framework.OGC.WFS.Version_1_0_0.RequestType();
                        caps.Capability.Request.Items = new object[]{
                        new gView.Framework.OGC.WFS.Version_1_0_0.GetCapabilitiesType(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.DescribeFeatureTypeType(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.GetFeatureTypeType()
                    };
                        caps.Capability.Request.ItemsElementName = new Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType4[] {
                        Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType4.GetCapabilities,
                        Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType4.DescribeFeatureType,
                        Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType4.GetFeature
                    };

                        gView.Framework.OGC.WFS.Version_1_0_0.DCPTypeType dcpType = new Framework.OGC.WFS.Version_1_0_0.DCPTypeType();
                        dcpType.HTTP = new object[]{
                        new gView.Framework.OGC.WFS.Version_1_0_0.GetType(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.PostType(),
                    };
                        ((gView.Framework.OGC.WFS.Version_1_0_0.GetType)dcpType.HTTP[0]).onlineResource = sOnlineResource;
                        ((gView.Framework.OGC.WFS.Version_1_0_0.PostType)dcpType.HTTP[1]).onlineResource = sOnlineResource;
                        ((gView.Framework.OGC.WFS.Version_1_0_0.GetFeatureTypeType)caps.Capability.Request.Items[2]).DCPType =
                            new gView.Framework.OGC.WFS.Version_1_0_0.DCPTypeType[] { dcpType };

                        caps.FeatureTypeList = new Framework.OGC.WFS.Version_1_0_0.FeatureTypeListType();
                        caps.FeatureTypeList.Operations = new object[] { new gView.Framework.OGC.WFS.Version_1_0_0.QueryType() };

                        List<gView.Framework.OGC.WFS.Version_1_0_0.FeatureTypeType> fTypes = new List<Framework.OGC.WFS.Version_1_0_0.FeatureTypeType>();
                        foreach (MapServerHelper.Layers layers in MapServerHelper.MapLayers(map, _useTOC))
                        {
                            IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                            if (fc == null)
                            {
                                continue;
                            }

                            gView.Framework.OGC.WFS.Version_1_0_0.FeatureTypeType fType = new Framework.OGC.WFS.Version_1_0_0.FeatureTypeType();
                            fType.Name = "c" + layers.ID;
                            fType.Title = layers.Title;
                            IEnvelope env4326 = GetEPSG4326Envelope(map, fc);
                            if (env4326 != null)
                            {
                                fType.LatLongBoundingBox = new Framework.OGC.WFS.Version_1_0_0.LatLongBoundingBoxType[] { new Framework.OGC.WFS.Version_1_0_0.LatLongBoundingBoxType() };
                                fType.LatLongBoundingBox[0].minx = env4326.minx.ToString(nfi);
                                fType.LatLongBoundingBox[0].miny = env4326.miny.ToString(nfi);
                                fType.LatLongBoundingBox[0].maxx = env4326.maxx.ToString(nfi);
                                fType.LatLongBoundingBox[0].maxy = env4326.maxy.ToString(nfi);
                            }
                            if (map.Display.SpatialReference != null)
                            {
                                fType.SRS = map.Display.SpatialReference.Name.ToUpper();
                            }

                            fTypes.Add(fType);
                        }
                        caps.FeatureTypeList.FeatureType = fTypes.ToArray();

                        caps.Filter_Capabilities = new Framework.OGC.WFS.Version_1_0_0.Filter_Capabilities();
                        caps.Filter_Capabilities.Spatial_Capabilities = new Framework.OGC.WFS.Version_1_0_0.Spatial_CapabilitiesType();
                        caps.Filter_Capabilities.Spatial_Capabilities.Spatial_Operators = new Framework.OGC.WFS.Version_1_0_0.Spatial_OperatorsType();
                        caps.Filter_Capabilities.Spatial_Capabilities.Spatial_Operators.Items = new object[]{
                        new gView.Framework.OGC.WFS.Version_1_0_0.BBOXType(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.Intersect()
                    };
                        caps.Filter_Capabilities.Spatial_Capabilities.Spatial_Operators.ItemsElementName = new gView.Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType3[]{
                        gView.Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType3.BBOX,
                        gView.Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType3.Intersect
                    };
                        caps.Filter_Capabilities.Scalar_Capabilities = new Framework.OGC.WFS.Version_1_0_0.Scalar_CapabilitiesType();
                        caps.Filter_Capabilities.Scalar_Capabilities.Items = new object[]{
                        new gView.Framework.OGC.WFS.Version_1_0_0.Logical_Operators(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.Comparison_OperatorsType()
                    };
                        caps.Filter_Capabilities.Scalar_Capabilities.ItemsElementName = new Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType2[]{
                        Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType2.Logical_Operators,
                        Framework.OGC.WFS.Version_1_0_0.ItemsChoiceType2.Comparison_Operators
                    };
                        ((gView.Framework.OGC.WFS.Version_1_0_0.Comparison_OperatorsType)caps.Filter_Capabilities.Scalar_Capabilities.Items[1]).Items = new object[]{
                        new gView.Framework.OGC.WFS.Version_1_0_0.Simple_Comparisons(),
                        new gView.Framework.OGC.WFS.Version_1_0_0.Like()
                    };

                        XsdSchemaSerializer<gView.Framework.OGC.WFS.Version_1_0_0.WFS_CapabilitiesType> ser = new XsdSchemaSerializer<Framework.OGC.WFS.Version_1_0_0.WFS_CapabilitiesType>();
                        string xml = ser.Serialize(caps);
                        return xml;
                        #endregion
                    }
                    else if (parameters.Version == "1.1.0")
                    {
                        #region Version 1.1.0
                        gView.Framework.OGC.WFS.Version_1_1_0.WFS_CapabilitiesType caps = new Framework.OGC.WFS.Version_1_1_0.WFS_CapabilitiesType();

                        #region ows:ServiceIdentification
                        caps.ServiceIdentification = new Framework.OGC.WFS.Version_1_1_0.ServiceIdentification();
                        caps.ServiceIdentification.Title = map.Name;
                        caps.ServiceIdentification.Abstract = "gView Gis OS Map: " + map.Name;
                        caps.ServiceIdentification.ServiceTypeVersion = new string[] { "1.1.0", "1.0.0" };
                        caps.ServiceIdentification.ServiceType = new gView.Framework.OGC.WFS.Version_1_1_0.CodeType1();
                        caps.ServiceIdentification.ServiceType.Value = "WFS";
                        #endregion

                        #region Operations - Online Resources

                        caps.OperationsMetadata = new Framework.OGC.WFS.Version_1_1_0.OperationsMetadata();
                        caps.OperationsMetadata.Operation = new Framework.OGC.WFS.Version_1_1_0.Operation[]{
                        new Framework.OGC.WFS.Version_1_1_0.Operation(), // GetCapabilities
                        new Framework.OGC.WFS.Version_1_1_0.Operation(), // DescribeFeatureType
                        new Framework.OGC.WFS.Version_1_1_0.Operation(), // GetFeature
                    };
                        caps.OperationsMetadata.Operation[0].name = "GetCapabilities";
                        caps.OperationsMetadata.Operation[0].DCP = new Framework.OGC.WFS.Version_1_1_0.DCP[]{
                        new Framework.OGC.WFS.Version_1_1_0.DCP() // Get
                    };
                        caps.OperationsMetadata.Operation[0].DCP[0].Item = new Framework.OGC.WFS.Version_1_1_0.HTTP();
                        caps.OperationsMetadata.Operation[0].DCP[0].Item.Items = new Framework.OGC.WFS.Version_1_1_0.RequestMethodType[]{
                        new gView.Framework.OGC.WFS.Version_1_1_0.RequestMethodType()
                    };
                        caps.OperationsMetadata.Operation[0].DCP[0].Item.Items[0].href = sOnlineResource;
                        caps.OperationsMetadata.Operation[0].DCP[0].Item.ItemsElementName = new Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13[]{
                        Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13.Get
                    };
                        caps.OperationsMetadata.Operation[1].name = "DescribeFeatureType";
                        caps.OperationsMetadata.Operation[1].DCP = new Framework.OGC.WFS.Version_1_1_0.DCP[]{
                        new Framework.OGC.WFS.Version_1_1_0.DCP()
                    };
                        caps.OperationsMetadata.Operation[1].DCP[0].Item = new Framework.OGC.WFS.Version_1_1_0.HTTP();
                        caps.OperationsMetadata.Operation[1].DCP[0].Item.Items = new Framework.OGC.WFS.Version_1_1_0.RequestMethodType[]{
                        new gView.Framework.OGC.WFS.Version_1_1_0.RequestMethodType(), // get
                        new gView.Framework.OGC.WFS.Version_1_1_0.RequestMethodType()  // post
                    };
                        caps.OperationsMetadata.Operation[1].DCP[0].Item.Items[0].href = sOnlineResource;
                        caps.OperationsMetadata.Operation[1].DCP[0].Item.Items[1].href = sOnlineResource;
                        caps.OperationsMetadata.Operation[1].DCP[0].Item.ItemsElementName = new Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13[]{
                        Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13.Get,
                        Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13.Post
                    };
                        caps.OperationsMetadata.Operation[2].name = "GetFeature";
                        caps.OperationsMetadata.Operation[2].DCP = new Framework.OGC.WFS.Version_1_1_0.DCP[]{
                        new Framework.OGC.WFS.Version_1_1_0.DCP()
                    };
                        caps.OperationsMetadata.Operation[2].DCP[0].Item = new Framework.OGC.WFS.Version_1_1_0.HTTP();
                        caps.OperationsMetadata.Operation[2].DCP[0].Item.Items = new Framework.OGC.WFS.Version_1_1_0.RequestMethodType[]{
                        new gView.Framework.OGC.WFS.Version_1_1_0.RequestMethodType(), // get
                        new gView.Framework.OGC.WFS.Version_1_1_0.RequestMethodType()  // post
                    };
                        caps.OperationsMetadata.Operation[2].DCP[0].Item.Items[0].href = sOnlineResource;
                        caps.OperationsMetadata.Operation[2].DCP[0].Item.Items[1].href = sOnlineResource;
                        caps.OperationsMetadata.Operation[2].DCP[0].Item.ItemsElementName = new Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13[]{
                        Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13.Get,
                        Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType13.Post
                    };
                        #endregion

                        #region FeatureTypes
                        caps.FeatureTypeList = new Framework.OGC.WFS.Version_1_1_0.FeatureTypeListType();
                        List<gView.Framework.OGC.WFS.Version_1_1_0.FeatureTypeType> fTypes = new List<Framework.OGC.WFS.Version_1_1_0.FeatureTypeType>();
                        foreach (MapServerHelper.Layers layers in MapServerHelper.MapLayers(map, _useTOC))
                        {
                            IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                            if (fc == null)
                            {
                                continue;
                            }

                            gView.Framework.OGC.WFS.Version_1_1_0.FeatureTypeType fType = new Framework.OGC.WFS.Version_1_1_0.FeatureTypeType();
                            fType.Name = new XmlQualifiedName("c" + layers.ID, "http://www.opengis.net/wfs");
                            fType.Title = layers.Title;

                            fType.Items = new object[] { (map.Display.SpatialReference != null ? map.Display.SpatialReference.Name.ToUpper() : (fc.SpatialReference != null ? fc.SpatialReference.Name.ToUpper() : String.Empty)) };
                            fType.ItemsElementName = new Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType14[]{
                            (map.Display.SpatialReference!=null || fc.SpatialReference!=null ? Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType14.DefaultSRS : Framework.OGC.WFS.Version_1_1_0.ItemsChoiceType14.NoSRS)
                        };
                            IEnvelope env4326 = GetEPSG4326Envelope(map, fc);
                            if (env4326 != null)
                            {
                                fType.WGS84BoundingBox = new Framework.OGC.WFS.Version_1_1_0.WGS84BoundingBoxType[] { new Framework.OGC.WFS.Version_1_1_0.WGS84BoundingBoxType() };
                                fType.WGS84BoundingBox[0].LowerCorner = env4326.minx.ToString(nfi) + " " + env4326.miny.ToString(nfi);
                                fType.WGS84BoundingBox[0].UpperCorner = env4326.maxx.ToString(nfi) + " " + env4326.maxy.ToString(nfi);
                            }
                            fType.OutputFormats = new Framework.OGC.WFS.Version_1_1_0.OutputFormatListType();
                            fType.OutputFormats.Format = new string[] { "text/xml; subtype=gml/3.1.1" };
                            fTypes.Add(fType);
                        }
                        caps.FeatureTypeList.FeatureType = fTypes.ToArray();
                        #endregion

                        #region Filter Capabilities
                        caps.Filter_Capabilities = new Framework.OGC.WFS.Version_1_1_0.Filter_Capabilities();
                        caps.Filter_Capabilities.Spatial_Capabilities = new Framework.OGC.WFS.Version_1_1_0.Spatial_CapabilitiesType();
                        caps.Filter_Capabilities.Spatial_Capabilities.SpatialOperators = new Framework.OGC.WFS.Version_1_1_0.SpatialOperatorType[]{
                        new Framework.OGC.WFS.Version_1_1_0.SpatialOperatorType(),
                        new Framework.OGC.WFS.Version_1_1_0.SpatialOperatorType()
                    };
                        caps.Filter_Capabilities.Spatial_Capabilities.SpatialOperators[0].name = gView.Framework.OGC.WFS.Version_1_1_0.SpatialOperatorNameType.BBOX;
                        caps.Filter_Capabilities.Spatial_Capabilities.SpatialOperators[1].name = gView.Framework.OGC.WFS.Version_1_1_0.SpatialOperatorNameType.Intersects;

                        caps.Filter_Capabilities.Spatial_Capabilities.GeometryOperands = new XmlQualifiedName[]{
                        new XmlQualifiedName("Envelope","http://www.opengis.net/gml"),
                        new XmlQualifiedName("Point","http://www.opengis.net/gml"),
                        new XmlQualifiedName("LineString","http://www.opengis.net/gml"),
                        new XmlQualifiedName("Polygon","http://www.opengis.net/gml")
                    };

                        caps.Filter_Capabilities.Scalar_Capabilities = new Framework.OGC.WFS.Version_1_1_0.Scalar_CapabilitiesType();
                        caps.Filter_Capabilities.Scalar_Capabilities.LogicalOperators = new Framework.OGC.WFS.Version_1_1_0.LogicalOperators();
                        caps.Filter_Capabilities.Scalar_Capabilities.ComparisonOperators = new Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorsType();
                        caps.Filter_Capabilities.Scalar_Capabilities.ComparisonOperators.ComparisonOperator = new Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType[]{
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.Between,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.EqualTo,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.GreaterThan,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.GreaterThanEqualTo,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.LessThan,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.LessThanEqualTo,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.Like,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.NotEqualTo,
                        Framework.OGC.WFS.Version_1_1_0.ComparisonOperatorType.NullCheck
                    };

                        #endregion

                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("gml", "http://www.opengis.net/gml");
                        ns.Add("ows", "http://www.opengis.net/ows");
                        ns.Add("xlink", "http://www.w3.org/1999/xlink");
                        ns.Add("ogc", "http://www.opengis.net/ogc");
                        ns.Add("wfs", "http://www.opengis.net/wfs");
                        ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                        XsdSchemaSerializer<gView.Framework.OGC.WFS.Version_1_1_0.WFS_CapabilitiesType> ser = new XsdSchemaSerializer<gView.Framework.OGC.WFS.Version_1_1_0.WFS_CapabilitiesType>();
                        string xml = ser.Serialize(caps, ns);
                        return xml;
                        #endregion
                    }

                    return String.Empty;
                }
                #region Müll
                /*
                MemoryStream ms = new MemoryStream();
                StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

                sw.WriteLine(@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no"" ?>");
                sw.WriteLine(@"
<WFS_Capabilities 
   version=""1.0.0""
   updateSequence=""0"" 
   xmlns=""http://www.opengis.net/wfs"" 
   xmlns:ogc=""http://www.opengis.net/ogc"" 
   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
   xsi:schemaLocation=""http://www.opengis.net/wfs ../wfs/1.0.0/WFS-capabilities.xsd"">");

                sw.WriteLine(@"
<Service>
  <Name>" + map.Name + @" WFS</Name>
  <Title>" + map.Name + @"</Title>
  <Abstract>" + map.Name + @" provided by gView GIS</Abstract>
  <OnlineResource>" + sOnlineResource + @"</OnlineResource>
  <AccessConstraints>none</AccessConstraints>
</Service>");

                sw.WriteLine(@"
<Capability>
  <Request>
    <GetCapabilities>
      <DCPType>
        <HTTP>
          <Get onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
      <DCPType>
        <HTTP>
          <Post onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
    </GetCapabilities>
    <DescribeFeatureType>
      <SchemaDescriptionLanguage>
        <XMLSCHEMA/>
      </SchemaDescriptionLanguage>
      <DCPType>
        <HTTP>
          <Get onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
      <DCPType>
        <HTTP>
          <Post onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
    </DescribeFeatureType>
    <GetFeature>
      <ResultFormat>
        <GML2/>
      </ResultFormat>
      <DCPType>
        <HTTP>
          <Get onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
      <DCPType>
        <HTTP>
          <Post onlineResource=""" + sOnlineResource + @""" />
        </HTTP>
      </DCPType>
    </GetFeature>
  </Request>
</Capability>");

                sw.Write(@"
<FeatureTypeList>
  <Operations>
    <Query/>
  </Operations>");

                // FeatureTypes
                foreach (MapServerHelper.Layers layers in MapServerHelper.MapLayers(map, _useTOC))
                {
                    IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                    if (fc == null) continue;

                    AppendFeatureType(sw, map, fc, 'c' + layers.ID, layers.Title, metadata);
                }
                //foreach (IDatasetElement element in map.MapElements)
                //{
                //    IFeatureClass fc = null;
                //    if (element is IFeatureLayer &&
                //        element.Class is IFeatureClass)
                //    {
                //        fc = element.Class as IFeatureClass;
                //    }
                //    else if (element is IWebServiceLayer &&
                //        element.Class is IWebServiceClass &&
                //        ((IWebServiceClass)element.Class).Themes != null)
                //    {
                //        foreach (IWebServiceTheme theme in ((IWebServiceClass)element.Class).Themes)
                //        {
                //            if (theme == null || !(theme.Class is IFeatureClass)) continue;

                //            AppendFeatureType(sw, map, theme.Class as IFeatureClass);
                //        }
                //    }

                //    if (fc == null) continue;
                //    AppendFeatureType(sw, map, fc);
                //}

                sw.Write(@"
</FeatureTypeList>

<ogc:Filter_Capabilities>
  <ogc:Spatial_Capabilities>
    <ogc:Spatial_Operators>
      <ogc:Intersects/>
      <ogc:Within/>
      <ogc:Contains/>
      <ogc:BBOX/>
    </ogc:Spatial_Operators>
  </ogc:Spatial_Capabilities>
  <ogc:Scalar_Capabilities>
    <ogc:Logical_Operators />
    <ogc:Comparison_Operators>
      <ogc:Simple_Comparisons />
      <ogc:Like />
      <ogc:Between />
      <ogc:EqualTo />
      <ogc:NotEqualTo />
    </ogc:Comparison_Operators>
  </ogc:Scalar_Capabilities>
</ogc:Filter_Capabilities>

</WFS_Capabilities>");

                sw.Flush();

                ms.Position = 0;
                byte[] bytes = new byte[ms.Length];
                ms.Read(bytes, 0, (int)ms.Length);
                sw.Close();

                string ret = Encoding.UTF8.GetString(bytes);
                _mapServer.Log("Service:" + service, loggingMethod.request_detail, ret);
                return ret;
 * */
                #endregion
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.error,
                    gView.Framework.IO.ExceptionConverter.ToString(ex));
                return "";
            }
        }

        async private Task<string> WFS_DescribeFeatureType(string service, WFSParameterDescriptor parameters, IServiceRequestContext context)
        {
            using (IServiceMap map = await context.CreateServiceMapInstance()) // _mapServer[context];
            {
                if (map == null)
                {
                    return "";
                }

                if (_mapServer == null || parameters == null)
                {
                    return "";
                }

                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "WFS DescribeFeatureType");

                try
                {
                    /*
                    XmlSchema schema = new XmlSchema();

                    foreach (string fcname in parameters.TypeName.Split(','))
                    {
                        if (fcname == string.Empty || fcname[0] != 'c') continue;
                        string fcID = fcname.Substring(1, fcname.Length - 1);

                        MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, fcID);
                        IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                        if (fc == null) continue;

                        XmlSchemaElement element = new XmlSchemaElement();
                        element.Name = fc.Name;
                        //element.QualifiedName = new XmlQualifiedName(fc.Name, "http://www.gViewGIS.com/server");
                        element.SubstitutionGroup = new XmlQualifiedName("_Feature", "http://www.opengis.net/gml");
                        schema.Items.Add(element);
                    }

                    XmlSchemaSet schemaSet = new XmlSchemaSet();
                    //schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallbackOne);
                    schemaSet.Add(schema);
                    schemaSet.Compile();

                    XmlSchema compiledSchema = null;

                    foreach (XmlSchema schema1 in schemaSet.Schemas())
                    {
                        compiledSchema = schema1;
                    }

                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
                    nsmgr.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
                    nsmgr.AddNamespace("gml", "http://www.opengis.net/gml");
                    return compiledSchema.ToString();
                    //compiledSchema.Write(Console.Out, nsmgr);
                    */


                    //gView.Framework.OGC.w3org.schema schema = new Framework.OGC.w3org.schema();
                    ////schema.annotation = new Framework.OGC.w3org.annotation[0];

                    //Framework.OGC.w3org.topLevelElement[] elements = new Framework.OGC.w3org.topLevelElement[0];

                    //foreach (string fcname in parameters.TypeName.Split(','))
                    //{
                    //    if (fcname == string.Empty || fcname[0] != 'c') continue;
                    //    string fcID = fcname.Substring(1, fcname.Length - 1);

                    //    MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, fcID);
                    //    IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                    //    if (fc == null) continue;

                    //    Framework.OGC.w3org.topLevelElement element = new Framework.OGC.w3org.topLevelElement();
                    //    element.name = fc.Name;
                    //    element.type = new XmlQualifiedName(fc.Name + "Type"/*, "http://www.gViewGIS.com/server"*/);
                    //    element.substitutionGroup = new XmlQualifiedName("_Feature"/*, "http://www.opengis.net/gml"*/);

                    //    Array.Resize<Framework.OGC.w3org.topLevelElement>(ref elements, elements.Length + 1);
                    //    elements[elements.Length - 1] = element;
                    //}
                    //schema.element = elements;

                    //XsdSchemaSerializer<gView.Framework.OGC.w3org.schema> ser = new XsdSchemaSerializer<gView.Framework.OGC.w3org.schema>();
                    //string xml = ser.Serialize(schema);
                    //return xml;

                }
                catch (Exception ex)
                {
                    await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.error,
                        gView.Framework.IO.ExceptionConverter.ToString(ex));
                    return "";
                }

                List<XmlSchemaWriter.FeatureClassSchema> fcs = CollectFeatureClassSchemas(map, parameters);

                //MemoryStream ms = new MemoryStream();
                //StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);

                XmlSchemaWriter schema = new XmlSchemaWriter(fcs);
                //sw.WriteLine(schema.Write());

                //sw.Flush();

                //ms.Position = 0;
                //byte[] bytes = new byte[ms.Length];
                //ms.Read(bytes, 0, (int)ms.Length);
                //sw.Close();

                //string ret = Encoding.UTF8.GetString(bytes);
                string ret = schema.Write();
                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request_detail, ret);
                return ret.Trim();
            }
        }

        async private Task<string> WFS_GetFeature(string OnlineResource, string service, WFSParameterDescriptor parameters, IServiceRequestContext context)
        {
            if (_mapServer == null || parameters == null)
            {
                return "";
            }

            await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "WFS GetFeature");

            using (IServiceMap map = await context.CreateServiceMapInstance()) // _mapServer[context];
            {
                if (map == null)
                {
                    return "";
                }

                string sOnlineResource = OnlineResource /*MapServerConnectors.OnlineResource(PlugInManager.PlugInID(this))*/ + "?SERVICE=WFS&amp;";
                sOnlineResource = sOnlineResource.Replace("??", "?");

                GmlVersion gmlVersion = GmlVersion.v1;
                if (parameters.Version == "1.1.0")
                {
                    gmlVersion = GmlVersion.v3;
                }

                StringBuilder sb = new StringBuilder();

                sb.Append(@"
<?xml version=""1.0"" encoding=""UTF-8"" ?>
<wfs:FeatureCollection
   xmlns:gv=""http://www.gViewGIS.com/server""
   xmlns:wfs=""http://www.opengis.net/wfs""
   xmlns:gml=""http://www.opengis.net/gml""
   xmlns:ogc=""http://www.opengis.net/ogc""
   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
   xsi:schemaLocation=""http://www.opengis.net/wfs http://schemas.opengeospatial.net/wfs/1.0.0/WFS-basic.xsd 
                        http://www.gViewGIS.com/server " + sOnlineResource + @"VERSION=" + parameters.Version + @"&amp;REQUEST=DescribeFeatureType&amp;TYPENAME=" + parameters.TypeName + @"&amp;OUTPUTFORMAT=XMLSCHEMA"">");

                // TODO: Bounding Box einfügen !!!

                foreach (string fcid in parameters.TypeName.Split(','))
                {
                    if (fcid == String.Empty || fcid[0] != 'c')
                    {
                        continue;
                    }

                    string fcID = fcid.Substring(1, fcid.Length - 1);

                    MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, fcID);
                    foreach (ILayer layer in layers)
                    {
                        if (!(layer.Class is IFeatureClass))
                        {
                            continue;
                        }

                        IFeatureClass fc = (IFeatureClass)layer.Class;

                        if (fc == null)
                        {
                            continue;
                        }

                        IQueryFilter filter = Filter.FromWFS(fc, parameters.Filter, gmlVersion);
                        Filter.AppendSortBy(filter, parameters.SortBy);

                        if (filter == null)
                        {
                            continue;
                        }

                        filter.SubFields = parameters.SubFields;

                        if (layer is IFeatureLayer)
                        {
                            var featureLayer = (IFeatureLayer)layer;

                            #region Append Filter Query

                            if (featureLayer.FilterQuery != null && !String.IsNullOrWhiteSpace(featureLayer.FilterQuery?.WhereClause))
                            {
                                var filterQuery = featureLayer.FilterQuery.WhereClause;

                                filter.WhereClause = (!String.IsNullOrWhiteSpace(filter.WhereClause)) ?
                                "(" + filter.WhereClause + ") AND (" + filterQuery + ")" :
                                filterQuery;
                            }

                            #endregion
                        }

                        IGeometricTransformer transformer = null;
                        string srsName = parameters.SrsName;
                        if (srsName != String.Empty &&
                            fc.SpatialReference != null)
                        {
                            ISpatialReference sRef = SpatialReference.FromID(srsName);

                            if (fc.SpatialReference != null &&
                                !fc.SpatialReference.Equals(sRef))
                            {
                                transformer = GeometricTransformerFactory.Create();
                                transformer.SetSpatialReferences(fc.SpatialReference, sRef);
                            }
                        }
                        else if (filter is ISpatialFilter &&
                            fc.SpatialReference != null &&
                            ((ISpatialFilter)filter).FilterSpatialReference != null &&
                            !fc.SpatialReference.Equals(((ISpatialFilter)filter).FilterSpatialReference))
                        {

                            transformer = GeometricTransformerFactory.Create();
                            transformer.SetSpatialReferences(fc.SpatialReference, ((ISpatialFilter)filter).FilterSpatialReference);
                            srsName = ((ISpatialFilter)filter).FilterSpatialReference.Name;
                        }
                        else if (fc.SpatialReference != null)
                        {
                            srsName = fc.SpatialReference.Name;
                        }

                        //if (parameters.MaxFeatures < 0 || parameters.MaxFeatures > 10000)
                        //    parameters.MaxFeatures = 10000;

                        //sb.Append("WHERE " + filter.WhereClause + " ORDER BY " + filter.OrderBy);

                        filter.SetUserData("IServiceRequestContext", context);
                        using (IFeatureCursor cursor = await fc.GetFeatures(filter))
                        {
                            if (cursor == null)
                            {
                                continue;
                            }

                            await FeatureTranslator.Features2GML(cursor, fc, fcid, sb, srsName, transformer, gmlVersion, parameters.MaxFeatures);
                        }

                        //if(fc is IDebugging && ((IDebugging)fc).LastException!=null)
                        //{
                        //    sb.Append(((IDebugging)fc).LastException.Message);
                        //    sb.Append(((IDebugging)fc).LastException.StackTrace);
                        //}

                        if (transformer != null)
                        {
                            transformer.Release();
                        }
                    }
                }
                sb.Append(@"</wfs:FeatureCollection>");
                string ret = sb.ToString().Trim();

                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request_detail, ret);

                return ret;
            }
        }

        #region Helper
        private void AppendFeatureType(StreamWriter sw, IServiceMap map, IFeatureClass fc, string name, string title, WFS_Export_Metadata.Metadata metadata)
        {
            if (fc == null)
            {
                return;
            }

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";

            //string title = fc.Name;
            //if (map!=null && map.TOC != null)
            //{
            //    ITOCElement tocElement = map.TOC.GetTOCElement(fc);
            //    if (tocElement != null) title = tocElement.name;
            //}

            sw.WriteLine(@"
<FeatureType>
        <Name>" + name + @"</Name>
        <Title>" + title + @"</Title>");
            IEnvelope env4326 = GetEPSG4326Envelope(map, fc);
            if (env4326 != null)
            {
                StringBuilder sb_env = new StringBuilder();
                foreach (string s in metadata.EPSGCodes)
                {
                    IEnvelope env = TransFormEPSG4326Envelope(env4326, s);
                    if (env == null)
                    {
                        continue;
                    }

                    sw.WriteLine("<SRS>" + s.ToUpper() + "</SRS>");
                    //sb_env.Append("\r\n         <BoundingBox SRS=\"" + s.ToUpper() + "\" minx=\"");
                    //sb_env.Append(env.minx.ToString(nfi));
                    //sb_env.Append("\" miny=\"");
                    //sb_env.Append(env.miny.ToString(nfi));
                    //sb_env.Append("\" maxx=\"");
                    //sb_env.Append(env.maxx.ToString(nfi));
                    //sb_env.Append("\" maxy=\"");
                    //sb_env.Append(env.maxy.ToString(nfi));
                    //sb_env.Append("\" />");
                }
                sw.Write("\r\n         <LatLonBoundingBox minx=\"");
                sw.Write(env4326.minx.ToString(nfi));
                sw.Write("\" miny=\"");
                sw.Write(env4326.miny.ToString(nfi));
                sw.Write("\" maxx=\"");
                sw.Write(env4326.maxx.ToString(nfi));
                sw.Write("\" maxy=\"");
                sw.Write(env4326.maxy.ToString(nfi));
                sw.WriteLine("\" />");
                sw.WriteLine(sb_env.ToString());
            }
            sw.WriteLine(@"
    </FeatureType>");
        }

        private IEnvelope GetEPSG4326Envelope(IServiceMap map, IFeatureClass fc)
        {
            if (map == null || fc == null || fc.Envelope == null)
            {
                return null;
            }

            ISpatialReference sRef = fc.SpatialReference;
            ISpatialReference sRef_4326 = SpatialReference.FromID("epsg:4326");

            IEnvelope env = GeometricTransformerFactory.Transform2D(fc.Envelope, sRef, sRef_4326).Envelope;

            return env;
        }

        private IEnvelope TransFormEPSG4326Envelope(IEnvelope env4326, string projID)
        {
            if (projID.ToLower() == "epsg:4326")
            {
                return env4326;
            }

            if (env4326 == null)
            {
                return null;
            }

            ISpatialReference from = SpatialReference.FromID("epsg:4326");
            ISpatialReference to = SpatialReference.FromID(projID);
            if (from == null || to == null || from.Equals(to))
            {
                return env4326;
            }

            return GeometricTransformerFactory.Transform2D(env4326, from, to).Envelope;
        }

        private List<XmlSchemaWriter.FeatureClassSchema> CollectFeatureClassSchemas(IServiceMap map, WFSParameterDescriptor parameters)
        {
            if (map == null || parameters == null)
            {
                return null;
            }

            List<XmlSchemaWriter.FeatureClassSchema> fcs = new List<XmlSchemaWriter.FeatureClassSchema>();
            foreach (string fcname in parameters.TypeName.Split(','))
            {
                if (fcname == string.Empty || fcname[0] != 'c')
                {
                    continue;
                }

                string fcID = fcname.Substring(1, fcname.Length - 1);

                MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, fcID);
                IFeatureClass fc = MapServerHelper.GetProtoFeatureClass(layers);
                if (fc == null)
                {
                    continue;
                }

                fcs.Add(new XmlSchemaWriter.FeatureClassSchema('c' + layers.ID, fc));
            }
            return fcs;
        }
        private List<IFeatureClass> CollectFeatureclasses(IServiceMap map, WFSParameterDescriptor parameters)
        {
            if (map == null || parameters == null)
            {
                return null;
            }

            List<IFeatureClass> fcs = new List<IFeatureClass>();
            foreach (string fcname in parameters.TypeName.Split(','))
            {
                if (fcname == string.Empty || fcname[0] != 'c')
                {
                    continue;
                }

                string fcID = fcname.Substring(1, fcname.Length - 1);

                MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, fcID);
                foreach (ILayer layer in layers)
                {
                    if (layer.Class is IFeatureClass)
                    {
                        fcs.Add((IFeatureClass)layer.Class);
                    }
                }
            }

            //List<string> fcnames=new List<string>();
            //foreach(string fcname in parameters.TypeName.Split(',')) 
            //{
            //    fcnames.Add(fcname);
            //}
            //foreach (IDatasetElement element in map.MapElements)
            //{
            //    if (element == null) continue;

            //    if (element.Class is IFeatureClass &&
            //        fcnames.Contains(element.Class.Name))
            //    {
            //        fcs.Add(element.Class as IFeatureClass);
            //    }

            //    if (element.Class is IWebServiceClass)
            //    {
            //        foreach (IWebServiceTheme theme in ((IWebServiceClass)element.Class).Themes)
            //        {
            //            if (theme == null) continue;

            //            if (theme.Class is IFeatureClass &&
            //                fcnames.Contains(theme.Class.Name))
            //            {
            //                fcs.Add(theme.Class as IFeatureClass);
            //            }
            //        }
            //    }
            //}

            return fcs;
        }
        #endregion
    }
}
