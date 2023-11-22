using gView.Core.Framework.Exceptions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.OGC.GeoJson;
using gView.Framework.OGC.GML;
using gView.Framework.OGC.WMS;
using gView.Framework.system;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using gView.Interoperability.OGC.SLD;
using gView.MapServer;
using gView.OGC.Framework.OGC.Exceptions;
using gView.Web.Framework.Web.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace gView.Interoperability.OGC
{
    [gView.Framework.system.RegisterPlugIn("C4892F49-446C-4e22-BCC7-76F033F1F03B")]
    public class WMSRequest : IServiceRequestInterpreter
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

        private IMapServer _mapServer = null;
        //private IServiceRequestInterpreter _arcXML = null;
        protected bool _useTOC;
        private static Guid _tilemetaprovider = new Guid("D33D3DD2-DD63-4a47-9F84-F840FE0D01C0");

        public WMSRequest()
        {
            _useTOC = false;
        }

        #region IServiceRequestInterpreter Member

        public void OnCreate(IMapServer mapServer)
        {
            _mapServer = mapServer;

            //ComponentManager compMan = new ComponentManager();
            //_arcXML = compMan.getComponent(new Guid("BB294D9C-A184-4129-9555-398AA70284BC")) as IServiceRequestInterpreter;
            //if (_arcXML != null) _arcXML.OnCreate(mapServer);
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

            try
            {
                WMSParameterDescriptor parameters = ParseParameters(context);

                //ServiceRequestContext context = new ServiceRequestContext(_mapServer, this, request);
                switch (parameters.Request)
                {
                    case WMSRequestType.GetCapabilities:
                        var getCapabilitiesResponse = await WMS_GetCapabilities(context.ServiceRequest.OnlineResource, context.ServiceRequest.Service, parameters, context);
                        context.ServiceRequest.Response = getCapabilitiesResponse.body;
                        context.ServiceRequest.ResponseContentType = getCapabilitiesResponse.contentType;
                        break;
                    case WMSRequestType.GetMap:
                        var getMapResponse = await WMS_GetMap(context.ServiceRequest.Service, parameters, context);
                        context.ServiceRequest.Response = getMapResponse.body;
                        context.ServiceRequest.ResponseContentType = getMapResponse.contentType;
                        break;
                    case WMSRequestType.GetFeatureInfo:
                        var featureInfoResponse = await WMS_FeatureInfo(context.ServiceRequest.Service, parameters, context);
                        context.ServiceRequest.Response = featureInfoResponse.body;
                        context.ServiceRequest.ResponseContentType = featureInfoResponse.contentType;
                        break;
                    case WMSRequestType.GetLegendGraphic:
                        var getLegendResponse = await WMS_GetLegendGraphic(context.ServiceRequest.Service, parameters, context);
                        context.ServiceRequest.Response = getLegendResponse.body;
                        context.ServiceRequest.ResponseContentType = getLegendResponse.contentType;
                        break;

                        //case WMSRequestType.DescriptTiles:
                        //    context.ServiceRequest.Response = await WMSC_DescriptTiles(context.ServiceRequest.Service, parameters, context);
                        //    context.ServiceRequest.ResponseContentType = "text/xml";
                        //    break;
                        //case WMSRequestType.GetTile:
                        //    context.ServiceRequest.Response = await WMSC_GetTile(context.ServiceRequest.Service, parameters, context);
                        //    break;
                        //case WMSRequestType.GenerateTiles:
                        //    context.ServiceRequest.Response = await WMS_GenerateTiles(context.ServiceRequest.Service, parameters, context);
                        //    break;
                }
            }
            catch (ParseParametersException ppe)
            {
                context.ServiceRequest.Response = ppe.MessageData;
                context.ServiceRequest.ResponseContentType = ppe.ContentType;
            }
        }

        public AccessTypes RequiredAccessTypes(IServiceRequestContext context)
        {
            var accessTypes = AccessTypes.None;

            try
            {
                WMSParameterDescriptor parameters = ParseParameters(context);

                switch (parameters.Request)
                {
                    case WMSRequestType.GetMap:
                    case WMSRequestType.GetTile:
                        accessTypes |= AccessTypes.Map;
                        break;
                    case WMSRequestType.GetFeatureInfo:
                        accessTypes |= AccessTypes.Query;
                        break;
                }
            }
            catch (ParseParametersException) { }

            return accessTypes;
        }

        public string IdentityName => "wms";
        public string IdentityLongName => "OGC Web Map Service";

        public int Priority => 80;

        public InterpreterCapabilities Capabilities
        {
            get
            {
                return new InterpreterCapabilities(new InterpreterCapabilities.Capability[]
                {
                    new InterpreterCapabilities.LinkCapability("Recommended: GetCapabilities (GeoServices - with Token)","{server}/geoservices/rest/services/{folder/service}/MapServer/WmsServer?VERSION=1.1.1&SERVICE=WMS&REQUEST=GetCapabilities","1.1.1"),
                    new InterpreterCapabilities.LinkCapability("Recommended: GetCapabilities (GeoServices - with Token)","{server}/geoservices/rest/services/{folder/service}/MapServer/WmsServer?VERSION=1.3.0&SERVICE=WMS&REQUEST=GetCapabilities","1.3.0"),
                    new InterpreterCapabilities.LinkCapability("GetCapabilities (with Basic Auth)","{server}/ogc/{folder@service}?VERSION=1.1.1&SERVICE=WMS&REQUEST=GetCapabilities","1.1.1"),
                    new InterpreterCapabilities.LinkCapability("GetCapabilities (with Basic Auth)","{server}/ogc/{folder@service}?VERSION=1.3.0&SERVICE=WMS&REQUEST=GetCapabilities","1.3.0")
                });
            }
        }

        private WMSParameterDescriptor ParseParameters(IServiceRequestContext context)
        {
            var queryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(context?.ServiceRequest?.Request);

            WMSParameterDescriptor parameters = new WMSParameterDescriptor();

            parameters.ParseParameters(queryString);

            return parameters;
        }

        #endregion

        async private Task<(string body, string contentType)> WMS_GetCapabilities(string OnlineResource, string service, WMSParameterDescriptor parameters, IServiceRequestContext context)
        {
            try
            {
                if (_mapServer == null || parameters == null)
                {
                    return (String.Empty, String.Empty);
                }

                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "WMS GetCapabilities");

                //if (parameters != null)
                //    _mapServer.Log(loggingMethod.request_detail, "WMS: " + Concat(parameters, "&"));

                using (IServiceMap map = await context.CreateServiceMapInstance())// _mapServer[context];
                {
                    if (map == null)
                    {
                        return (String.Empty, String.Empty);
                    }

                    WMS_Export_Metadata exMetadata = map.MetadataProvider(new Guid("0F6317BC-38FD-41d3-8E1A-82AB1873C526")) as WMS_Export_Metadata;
                    WMS_Export_Metadata.Metadata metadata = ((exMetadata != null) ? exMetadata.Data : null);
                    if (metadata == null)
                    {
                        metadata = new WMS_Export_Metadata.Metadata(
                            ((map.Display != null && map.Display.SpatialReference != null) ?
                            map.Display.SpatialReference.Name : String.Empty));
                    }

                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.NumberDecimalSeparator = ".";

                    string sOnlineResource = $"{OnlineResource.AddParameterToUrl("SERVICE", "WMS")}&";

                    if (parameters.Version == "1.1.1")
                    {
                        gView.Framework.OGC.WMS.Version_1_1_1.WMT_MS_Capabilities caps = new Framework.OGC.WMS.Version_1_1_1.WMT_MS_Capabilities();

                        #region Service
                        caps.Service = new Framework.OGC.WMS.Version_1_1_1.Service();
                        caps.Service.Name = map.Name;
                        caps.Service.Title = "gView Gis OS WMS: " + map.Name;
                        caps.Service.OnlineResource = new Framework.OGC.WMS.Version_1_1_1.OnlineResource();
                        caps.Service.OnlineResource.href = sOnlineResource;
                        caps.Service.Fees = "none";
                        caps.Service.ContactInformation = new Framework.OGC.WMS.Version_1_1_1.ContactInformation();
                        caps.Service.AccessConstraints = "none";
                        #endregion

                        #region Capability
                        caps.Capability = new Framework.OGC.WMS.Version_1_1_1.Capability();

                        #region Request
                        caps.Capability.Request = new Framework.OGC.WMS.Version_1_1_1.Request();

                        #region GetCapabilities
                        caps.Capability.Request.GetCapabilities = new Framework.OGC.WMS.Version_1_1_1.GetCapabilities();
                        caps.Capability.Request.GetCapabilities.Format = new string[] { "application/vnd.ogc.wms_xml" };
                        caps.Capability.Request.GetCapabilities.DCPType = new Framework.OGC.WMS.Version_1_1_1.DCPType[]{
                        new Framework.OGC.WMS.Version_1_1_1.DCPType()
                    };
                        caps.Capability.Request.GetCapabilities.DCPType[0].HTTP = new object[]{
                        new gView.Framework.OGC.WMS.Version_1_1_1.Get()
                    };
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetCapabilities.DCPType[0].HTTP[0]).OnlineResource = new Framework.OGC.WMS.Version_1_1_1.OnlineResource();
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetCapabilities.DCPType[0].HTTP[0]).OnlineResource.href = sOnlineResource;
                        #endregion

                        #region GetMap
                        caps.Capability.Request.GetMap = new Framework.OGC.WMS.Version_1_1_1.GetMap();
                        caps.Capability.Request.GetMap.Format = new string[] { "image/png", "image/jpeg" };
                        caps.Capability.Request.GetMap.DCPType = new Framework.OGC.WMS.Version_1_1_1.DCPType[]{
                        new Framework.OGC.WMS.Version_1_1_1.DCPType()
                    };
                        caps.Capability.Request.GetMap.DCPType[0].HTTP = new object[]{
                        new gView.Framework.OGC.WMS.Version_1_1_1.Get()
                    };
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetMap.DCPType[0].HTTP[0]).OnlineResource = new Framework.OGC.WMS.Version_1_1_1.OnlineResource();
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetMap.DCPType[0].HTTP[0]).OnlineResource.href = sOnlineResource;
                        #endregion

                        #region GetFeatureInfo

                        List<string> gfiFormats = new List<string>(new string[] { "text/plain", "text/html", "text/xml", "application/vnd.ogc.gml" });
                        DirectoryInfo di = new DirectoryInfo(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/misc/wms/GetFeatureInfo/xsl");
                        if (di.Exists)
                        {
                            foreach (FileInfo fi in di.GetFiles("*.xsl"))
                            {
                                gfiFormats.Add("xsl/" + fi.Name);
                            }
                        }

                        caps.Capability.Request.GetFeatureInfo = new Framework.OGC.WMS.Version_1_1_1.GetFeatureInfo();
                        caps.Capability.Request.GetFeatureInfo.Format = gfiFormats.ToArray();
                        caps.Capability.Request.GetFeatureInfo.DCPType = new Framework.OGC.WMS.Version_1_1_1.DCPType[]{
                        new Framework.OGC.WMS.Version_1_1_1.DCPType()
                    };
                        caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP = new object[]{
                        new gView.Framework.OGC.WMS.Version_1_1_1.Get()
                    };
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP[0]).OnlineResource = new Framework.OGC.WMS.Version_1_1_1.OnlineResource();
                        ((gView.Framework.OGC.WMS.Version_1_1_1.Get)caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP[0]).OnlineResource.href = sOnlineResource;
                        #endregion

                        #endregion

                        #region Exception
                        caps.Capability.Exception = new string[] { "application/vnd.ogc.se_xml", "application/vnd.ogc.se_inimage", "application/vnd.ogc.se_blank" };
                        #endregion

                        #region UserDefinedSymbolization
                        caps.Capability.UserDefinedSymbolization = new Framework.OGC.WMS.Version_1_1_1.UserDefinedSymbolization();
                        caps.Capability.UserDefinedSymbolization.SupportSLD = Framework.OGC.WMS.Version_1_1_1.UserDefinedSymbolizationSupportSLD.Item1;
                        caps.Capability.UserDefinedSymbolization.RemoteWFS = Framework.OGC.WMS.Version_1_1_1.UserDefinedSymbolizationRemoteWFS.Item0;
                        caps.Capability.UserDefinedSymbolization.UserStyle = Framework.OGC.WMS.Version_1_1_1.UserDefinedSymbolizationUserStyle.Item1;
                        caps.Capability.UserDefinedSymbolization.UserLayer = Framework.OGC.WMS.Version_1_1_1.UserDefinedSymbolizationUserLayer.Item0;
                        #endregion

                        #region Layer

                        caps.Capability.Layer = new Framework.OGC.WMS.Version_1_1_1.Layer();
                        caps.Capability.Layer.Name = map.Name;
                        caps.Capability.Layer.SRS = metadata.EPSGCodes;

                        IEnvelope env4326 = GetEPSG4326Envelope(map);
                        if (env4326 != null)
                        {
                            Framework.OGC.WMS.Version_1_1_1.BoundingBox[] bboxes = new Framework.OGC.WMS.Version_1_1_1.BoundingBox[0];
                            foreach (string s in caps.Capability.Layer.SRS)
                            {
                                IEnvelope env = TransFormEPSG4326Envelope(env4326, s, parameters.Version);
                                if (env == null)
                                {
                                    continue;
                                }

                                Array.Resize<Framework.OGC.WMS.Version_1_1_1.BoundingBox>(ref bboxes, bboxes.Length + 1);
                                bboxes[bboxes.Length - 1] = new Framework.OGC.WMS.Version_1_1_1.BoundingBox();
                                bboxes[bboxes.Length - 1].SRS = s;
                                bboxes[bboxes.Length - 1].minx = env.minx.ToString(_nhi);
                                bboxes[bboxes.Length - 1].miny = env.miny.ToString(_nhi);
                                bboxes[bboxes.Length - 1].maxx = env.maxx.ToString(_nhi);
                                bboxes[bboxes.Length - 1].maxy = env.maxy.ToString(_nhi);
                            }
                            caps.Capability.Layer.BoundingBox = bboxes;
                            caps.Capability.Layer.LatLonBoundingBox = new Framework.OGC.WMS.Version_1_1_1.LatLonBoundingBox();
                            caps.Capability.Layer.LatLonBoundingBox.minx = env4326.minx.ToString(_nhi);
                            caps.Capability.Layer.LatLonBoundingBox.miny = env4326.miny.ToString(_nhi);
                            caps.Capability.Layer.LatLonBoundingBox.maxx = env4326.maxx.ToString(_nhi);
                            caps.Capability.Layer.LatLonBoundingBox.maxy = env4326.maxy.ToString(_nhi);
                        }

                        List<Framework.OGC.WMS.Version_1_1_1.Layer> fTypes = new List<Framework.OGC.WMS.Version_1_1_1.Layer>();
                        foreach (MapServerHelper.Layers layers in MapServerHelper.MapLayers(map, _useTOC))
                        {
                            if (layers == null || layers.FirstLayer == null || layers.FirstClass == null)
                            {
                                continue;
                            }

                            Framework.OGC.WMS.Version_1_1_1.Layer fType = new Framework.OGC.WMS.Version_1_1_1.Layer();
                            fType.Name = "c" + layers.ID;
                            fType.Title = layers.Title;
                            IClass c = layers.FirstClass;
                            fType.queryable = c is IFeatureClass ? Framework.OGC.WMS.Version_1_1_1.LayerQueryable.Item1 : Framework.OGC.WMS.Version_1_1_1.LayerQueryable.Item0;

                            if (layers.MinScale > 1.0 || layers.MaxScale > 1.0)
                            {
                                fType.ScaleHint = new Framework.OGC.WMS.Version_1_1_1.ScaleHint();
                                if (layers.MinScale > 1.0)
                                {
                                    fType.ScaleHint.min = (layers.MinScale / 2004.4).ToString(_nhi);
                                }

                                if (layers.MaxScale > 1.0)
                                {
                                    fType.ScaleHint.max = (layers.MaxScale / 2004.4).ToString(_nhi);
                                }
                            }
                            fTypes.Add(fType);
                        }
                        caps.Capability.Layer.Layer1 = fTypes.ToArray();
                        #endregion

                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("xlink", "http://www.w3.org/1999/xlink");
                        XsdSchemaSerializer<gView.Framework.OGC.WMS.Version_1_1_1.WMT_MS_Capabilities> ser = new XsdSchemaSerializer<gView.Framework.OGC.WMS.Version_1_1_1.WMT_MS_Capabilities>();
                        string xml = ser.Serialize(caps, ns);
                        return (xml, "");

                        #endregion

                    }
                    else if (parameters.Version == "1.3.0")
                    {
                        gView.Framework.OGC.WMS.Version_1_3_0.WMS_Capabilities caps = new Framework.OGC.WMS.Version_1_3_0.WMS_Capabilities();

                        #region Service

                        caps.Service = new Framework.OGC.WMS.Version_1_3_0.Service();
                        caps.Service.Name = new Framework.OGC.WMS.Version_1_3_0.ServiceName();
                        caps.Service.Title = map.Name;
                        caps.Service.OnlineResource = new Framework.OGC.WMS.Version_1_3_0.OnlineResource();
                        caps.Service.OnlineResource.href = sOnlineResource;
                        caps.Service.ContactInformation = new Framework.OGC.WMS.Version_1_3_0.ContactInformation();
                        caps.Service.Fees = "none";
                        caps.Service.AccessConstraints = "none";
                        caps.Service.MaxHeight = caps.Service.MaxWidth = "8000";

                        #endregion

                        #region Capability

                        caps.Capability = new Framework.OGC.WMS.Version_1_3_0.Capability();

                        #region Request

                        caps.Capability.Request = new Framework.OGC.WMS.Version_1_3_0.Request();

                        #region GetCapabilities
                        caps.Capability.Request.GetCapabilities = new Framework.OGC.WMS.Version_1_3_0.OperationType();
                        caps.Capability.Request.GetCapabilities.Format = new string[] { "text/xml" };
                        caps.Capability.Request.GetCapabilities.DCPType = new Framework.OGC.WMS.Version_1_3_0.DCPType[]{
                        new Framework.OGC.WMS.Version_1_3_0.DCPType()
                    };
                        caps.Capability.Request.GetCapabilities.DCPType[0].HTTP = new Framework.OGC.WMS.Version_1_3_0.HTTP();
                        caps.Capability.Request.GetCapabilities.DCPType[0].HTTP.Get = new Framework.OGC.WMS.Version_1_3_0.Get();
                        caps.Capability.Request.GetCapabilities.DCPType[0].HTTP.Get.OnlineResource = new Framework.OGC.WMS.Version_1_3_0.OnlineResource();
                        caps.Capability.Request.GetCapabilities.DCPType[0].HTTP.Get.OnlineResource.href = sOnlineResource;
                        #endregion

                        #region GetMap
                        caps.Capability.Request.GetMap = new Framework.OGC.WMS.Version_1_3_0.OperationType();
                        caps.Capability.Request.GetMap.Format = new string[] { "image/png", "image/jpeg" };
                        caps.Capability.Request.GetMap.DCPType = new Framework.OGC.WMS.Version_1_3_0.DCPType[]{
                            new Framework.OGC.WMS.Version_1_3_0.DCPType()
                        };
                        caps.Capability.Request.GetMap.DCPType[0].HTTP = new Framework.OGC.WMS.Version_1_3_0.HTTP();
                        caps.Capability.Request.GetMap.DCPType[0].HTTP.Get = new Framework.OGC.WMS.Version_1_3_0.Get();
                        caps.Capability.Request.GetMap.DCPType[0].HTTP.Get.OnlineResource = new Framework.OGC.WMS.Version_1_3_0.OnlineResource();
                        caps.Capability.Request.GetMap.DCPType[0].HTTP.Get.OnlineResource.href = sOnlineResource;
                        #endregion

                        #region GetFeatureInfo

                        List<string> gfiFormats = new List<string>(new string[] { "text/plain", "text/html", "text/xml", "application/vnd.ogc.gml", "application/json" });
                        DirectoryInfo di = new DirectoryInfo($"{_mapServer.EtcPath}/wms/getfeatureinfo/xsl");
                        if (di.Exists)
                        {
                            foreach (FileInfo fi in di.GetFiles("*.xsl"))
                            {
                                gfiFormats.Add("xsl/" + fi.Name);
                            }
                        }

                        caps.Capability.Request.GetFeatureInfo = new Framework.OGC.WMS.Version_1_3_0.OperationType();
                        caps.Capability.Request.GetFeatureInfo.Format = gfiFormats.ToArray();
                        caps.Capability.Request.GetFeatureInfo.DCPType = new Framework.OGC.WMS.Version_1_3_0.DCPType[]{
                            new Framework.OGC.WMS.Version_1_3_0.DCPType()
                        };
                        caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP = new Framework.OGC.WMS.Version_1_3_0.HTTP();
                        caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP.Get = new Framework.OGC.WMS.Version_1_3_0.Get();
                        caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP.Get.OnlineResource = new Framework.OGC.WMS.Version_1_3_0.OnlineResource();
                        caps.Capability.Request.GetFeatureInfo.DCPType[0].HTTP.Get.OnlineResource.href = sOnlineResource;

                        #endregion

                        #region GetLegendGraphic

                        caps.Capability.Request.GetLegendGraphic = new Framework.OGC.WMS.Version_1_3_0.OperationType();
                        caps.Capability.Request.GetLegendGraphic.DCPType = new Framework.OGC.WMS.Version_1_3_0.DCPType[]{
                            new Framework.OGC.WMS.Version_1_3_0.DCPType()
                        };
                        caps.Capability.Request.GetLegendGraphic.Format = new string[] { "image/png" };
                        caps.Capability.Request.GetLegendGraphic.DCPType[0].HTTP = new Framework.OGC.WMS.Version_1_3_0.HTTP();
                        caps.Capability.Request.GetLegendGraphic.DCPType[0].HTTP.Get = new Framework.OGC.WMS.Version_1_3_0.Get();
                        caps.Capability.Request.GetLegendGraphic.DCPType[0].HTTP.Get.OnlineResource = new Framework.OGC.WMS.Version_1_3_0.OnlineResource();
                        caps.Capability.Request.GetLegendGraphic.DCPType[0].HTTP.Get.OnlineResource.href = sOnlineResource;

                        #endregion

                        #region Exception
                        caps.Capability.Exception = new string[] { "XML", "INIMAGE", "BLANK" };
                        #endregion

                        #region UserDefinedSymbolization

                        #endregion

                        #region Layer
                        caps.Capability.Layer = new Framework.OGC.WMS.Version_1_3_0.Layer[]{
                        new Framework.OGC.WMS.Version_1_3_0.Layer()
                    };
                        caps.Capability.Layer[0].Name = map.Name;
                        caps.Capability.Layer[0].Title = map.Name;
                        caps.Capability.Layer[0].CRS = metadata.EPSGCodes;

                        IEnvelope env4326 = GetEPSG4326Envelope(map);
                        if (env4326 != null)
                        {
                            Framework.OGC.WMS.Version_1_3_0.BoundingBox[] bboxes = new Framework.OGC.WMS.Version_1_3_0.BoundingBox[0];
                            foreach (string s in caps.Capability.Layer[0].CRS)
                            {
                                IEnvelope env = TransFormEPSG4326Envelope(env4326, s, parameters.Version);
                                if (env == null)
                                {
                                    continue;
                                }

                                Array.Resize<Framework.OGC.WMS.Version_1_3_0.BoundingBox>(ref bboxes, bboxes.Length + 1);
                                bboxes[bboxes.Length - 1] = new Framework.OGC.WMS.Version_1_3_0.BoundingBox();
                                bboxes[bboxes.Length - 1].CRS = s;
                                bboxes[bboxes.Length - 1].minx = env.minx;
                                bboxes[bboxes.Length - 1].miny = env.miny;
                                bboxes[bboxes.Length - 1].maxx = env.maxx;
                                bboxes[bboxes.Length - 1].maxy = env.maxy;
                            }
                            caps.Capability.Layer[0].BoundingBox = bboxes;

                            caps.Capability.Layer[0].EX_GeographicBoundingBox = new Framework.OGC.WMS.Version_1_3_0.EX_GeographicBoundingBox();
                            caps.Capability.Layer[0].EX_GeographicBoundingBox.westBoundLongitude = env4326.minx;
                            caps.Capability.Layer[0].EX_GeographicBoundingBox.southBoundLatitude = env4326.miny;
                            caps.Capability.Layer[0].EX_GeographicBoundingBox.eastBoundLongitude = env4326.maxx;
                            caps.Capability.Layer[0].EX_GeographicBoundingBox.northBoundLatitude = env4326.maxy;
                        }

                        List<Framework.OGC.WMS.Version_1_3_0.Layer> fTypes = new List<Framework.OGC.WMS.Version_1_3_0.Layer>();
                        foreach (MapServerHelper.Layers layers in MapServerHelper.MapLayers(map, _useTOC))
                        {
                            if (layers == null || layers.FirstLayer == null || layers.FirstClass == null)
                            {
                                continue;
                            }

                            Framework.OGC.WMS.Version_1_3_0.Layer fType = new Framework.OGC.WMS.Version_1_3_0.Layer();
                            fType.Name = "c" + layers.ID;
                            fType.Title = layers.Title;
                            IClass c = layers.FirstClass;
                            fType.queryable = (c is IFeatureClass ? 1 : 0);

                            if (layers.MinScale > 0.0)
                            {
                                fType.MinScaleDenominator = layers.MinScale;
                                fType.MinScaleDenominatorSpecified = true;
                            }
                            if (layers.MaxScale > 0.0)
                            {
                                fType.MaxScaleDenominator = layers.MaxScale;
                                fType.MaxScaleDenominatorSpecified = true;
                            }

                            //fType.Style = new Framework.OGC.WMS.Version_1_3_0.Style[] {
                            //};

                            fTypes.Add(fType);
                        }
                        caps.Capability.Layer[0].Layer1 = fTypes.ToArray();
                        #endregion

                        #endregion

                        #endregion

                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add("xlink", "http://www.w3.org/1999/xlink");
                        ns.Add("sld", "http://www.opengis.net/sld");
                        ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                        XsdSchemaSerializer<gView.Framework.OGC.WMS.Version_1_3_0.WMS_Capabilities> ser = new XsdSchemaSerializer<gView.Framework.OGC.WMS.Version_1_3_0.WMS_Capabilities>();
                        string xml = ser.Serialize(caps, ns);
                        return (xml, "text/xml");
                    }

                    return (String.Empty, String.Empty); ;
                }
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                return (String.Empty, String.Empty); ;
            }
        }

        async virtual public Task<(byte[] body, string contentType)> WMS_GetMap(string service, WMSParameterDescriptor parameters, IServiceRequestContext context)
        {
            // Immer neue Requestklasse erzeugen, damit Request multithreadfähig
            // ist.
            // Parameter wie Layer müssen erhalten bleiben, wenn ServicMap später
            // BeforeRenderlayers aufruft...

            await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "WMS GetMap");

            WMS_GetMapRequest request = new WMS_GetMapRequest(_mapServer, service, parameters, _useTOC, context);

            return await request.Request();
        }

        private static object thisLock = new object();
        private static Dictionary<string, object> lockers = new Dictionary<string, object>();

        private class WMS_GetMapRequest
        {
            private string _service;
            private WMSParameterDescriptor _parameters;
            private IMapServer _mapServer;
            private bool _useTOC;
            private IServiceRequestContext _context;

            public WMS_GetMapRequest(IMapServer mapServer, string service, WMSParameterDescriptor parameters, bool useTOC, IServiceRequestContext context)
            {
                _mapServer = mapServer;
                _service = service;
                _parameters = parameters;
                _useTOC = useTOC;
                _context = context;
            }

            async public Task<(byte[] body, string contentType)> Request()
            {
                if (_mapServer == null)
                {
                    return (null, String.Empty);
                }

                using (IServiceMap map = await _context.CreateServiceMapInstance()) //_mapServer[_context]
                {
                    if (map == null)
                    {
                        return (null, String.Empty);
                    }

                    ISpatialReference sRef = SpatialReference.FromID("epsg:" + _parameters.SRS);
                    map.Display.SpatialReference = sRef;

                    if (_parameters.Version == "1.3.0" && map.Display.SpatialReference != null)
                    {
                        IPoint ll = _parameters.BBOX.LowerLeft;
                        IPoint ur = _parameters.BBOX.UpperRight;

                        ll = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ll, map.Display.SpatialReference);
                        ur = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ur, map.Display.SpatialReference);

                        _parameters.BBOX = new Envelope(ll, ur);
                    }

                    map.Display.Dpi = _parameters.dpi;
                    map.Display.ImageWidth = _parameters.Width;
                    map.Display.ImageHeight = _parameters.Height;
                    map.Display.Limit = _parameters.BBOX;
                    map.Display.ZoomTo(_parameters.BBOX);

                    if (_parameters.Transparent)
                    {
                        map.Display.MakeTransparent = true;
                        map.Display.TransparentColor = GraphicsEngine.ArgbColor.White;
                    }
                    map.BeforeRenderLayers += new BeforeRenderLayersEvent(map_BeforeRenderLayers);
                    await map.Render();

                    if (map.MapImage != null)
                    {
                        //
                        // Zurück in das richtige Koordinatenfenster verzerren
                        //
                        double minx, miny, maxx, maxy;
                        minx = _parameters.BBOX.minx; miny = _parameters.BBOX.miny;
                        maxx = _parameters.BBOX.maxx; maxy = _parameters.BBOX.maxy;

                        map.Display.World2Image(ref minx, ref miny);
                        map.Display.World2Image(ref maxx, ref maxy);

                        if ((int)Math.Round(minx) != 0 || (int)Math.Round(maxy) != 0 ||
                            (int)Math.Round(maxx) != _parameters.Width || (int)Math.Round(miny) != _parameters.Height)
                        {
                            using (var bitmap = GraphicsEngine.Current.Engine.CreateBitmap(_parameters.Width, _parameters.Height))
                            {
                                using (var canvas = bitmap.CreateCanvas())
                                {
                                    var sourceRect = new GraphicsEngine.CanvasRectangleF((float)minx, (float)maxy, (float)Math.Abs(maxx - minx), (float)Math.Abs(miny - maxy));
                                    canvas.DrawBitmap(map.MapImage,
                                        new GraphicsEngine.CanvasRectangleF(0f, 0f, bitmap.Width, bitmap.Height),
                                        sourceRect);
                                }

                                if (_parameters.Transparent)
                                {
                                    bitmap.MakeTransparent(GraphicsEngine.ArgbColor.White);
                                }

                                MemoryStream ms = new MemoryStream();
                                bitmap.Save(ms, _parameters.GetImageFormat());
                                return (ms.ToArray(), _parameters.GetContentType());
                            }
                        }
                        else
                        {
                            MemoryStream ms = new MemoryStream();
                            if (await map.SaveImage(ms, _parameters.GetImageFormat()) >= 0)
                            {
                                return (ms.ToArray(), _parameters.GetContentType());
                            }
                        }
                    }
                    return (null, String.Empty);
                }
            }

            void map_BeforeRenderLayers(IServiceMap sender, List<ILayer> layers)
            {
                // if layers=MapName => Top Layer in Capabilites => all Layers visible (or default?)
                var mapLayerVisible = _parameters.Layers.Length == 1 && _parameters.Layers[0] == sender.Name;

                if (mapLayerVisible == false)  // otherwise -> default
                {
                    foreach (ILayer layer in layers)
                    {
                        if (layer == null)
                        {
                            continue;
                        }

                        layer.Visible = false;
                    }

                    foreach (string id in _parameters.Layers)
                    {
                        if (id == String.Empty || id[0] != 'c')
                        {
                            continue;
                        }

                        foreach (ILayer layer in MapServerHelper.FindMapLayers(sender, _useTOC, id.Substring(1, id.Length - 1), layers))
                        {
                            layer.Visible = true;
                        }
                    }
                }

                // SLD
                XmlDocument sld = null;
                if (_parameters.SLD_BODY != String.Empty)
                {
                    try
                    {
                        sld = new XmlDocument();
                        sld.LoadXml(_parameters.SLD_BODY);

                        foreach (XmlNode namedLayer in sld.SelectNodes("StyledLayerDescriptor/NamedLayer"))
                        {
                            XmlNode nameNode = namedLayer.SelectSingleNode("Name");
                            if (nameNode == null)
                            {
                                continue;
                            }

                            string id = nameNode.InnerText;
                            if (id == String.Empty || id[0] != 'c')
                            {
                                continue;
                            }

                            SLDRenderer renderer = new SLDRenderer(namedLayer.OuterXml);
                            foreach (ILayer layer in MapServerHelper.FindMapLayers(sender, _useTOC, id.Substring(1, id.Length - 1), layers))
                            {
                                if (layer is IFeatureLayer)
                                {
                                    ((IFeatureLayer)layer).FeatureRenderer = renderer;
                                }
                            }
                        }
                    }
                    catch /*(Exception ex)*/
                    {
                        //string err = ex.Message;
                    }
                }
            }
        }

        private string MapName(IServiceRequestContext context)
        {
            return context.ServiceRequest.Service;
        }

        async public Task<(string body, string contentType)> WMS_FeatureInfo(string service, WMSParameterDescriptor parameters, IServiceRequestContext context)
        {
            if (parameters == null || _mapServer == null)
            {
                return (String.Empty, String.Empty);
            }

            using (IServiceMap map = await context.CreateServiceMapInstance()) // _mapServer[context];
            {
                if (map == null || map.TOC == null)
                {
                    return (String.Empty, String.Empty);
                }

                ISpatialReference sRef = SpatialReference.FromID("epsg:" + parameters.SRS);

                IPoint featureInfoPoint = new gView.Framework.Geometry.Point(parameters.FeatureInfoX, parameters.FeatureInfoY);
                if (parameters.Version == "1.3.0" && sRef != null)
                {
                    IPoint ll = parameters.BBOX.LowerLeft;
                    IPoint ur = parameters.BBOX.UpperRight;

                    ll = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ll, sRef);
                    ur = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ur, sRef);

                    parameters.BBOX = new Envelope(ll, ur);
                    //featureInfoPoint = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(featureInfoPoint, sRef);
                }

                map.Display.SpatialReference = sRef;
                map.Display.ImageWidth = parameters.Width;
                map.Display.ImageHeight = parameters.Height;
                map.Display.Limit = parameters.BBOX;
                map.Display.ZoomTo(parameters.BBOX);

                double x = featureInfoPoint.X;
                double y = featureInfoPoint.Y;
                map.Display.Image2World(ref x, ref y);

                double tol = 5.0 * map.Display.MapScale / (96 / 0.0254);  // [m]
                if (sRef != null &&
                    sRef.SpatialParameters.IsGeographic)
                {
                    tol = (180.0 * tol / Math.PI) / 6370000.0;
                }

                SpatialFilter filter = new SpatialFilter();
                filter.Geometry = new Envelope(x - tol, y - tol, x + tol, y + tol);
                filter.FilterSpatialReference = sRef;
                filter.SubFields = "*";

                // Get Layers
                List<ILayer> queryLayers = new List<ILayer>();
                foreach (string l in parameters.QueryLayers)
                {
                    if (l == String.Empty || l[0] != 'c')
                    {
                        continue;
                    }

                    MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(map, _useTOC, l.Substring(1, l.Length - 1));
                    if (layers == null)
                    {
                        continue;
                    }

                    foreach (ILayer layer in layers)
                    {
                        queryLayers.Add(layer);
                    }
                }


                // Query Layers
                StringBuilder response = new StringBuilder();
                List<FeatureType> features = new List<FeatureType>();

                foreach (ILayer layer in queryLayers)
                {
                    if (layer is IFeatureLayer && ((IFeatureLayer)layer).FeatureClass != null)
                    {
                        using (IFeatureCursor cursor = await ((IFeatureLayer)layer).FeatureClass.Search(filter) as IFeatureCursor)
                        {
                            IFeature feature = null;
                            while ((feature = await cursor.NextFeature()) != null)
                            {
                                features.Add(new FeatureType("c" + layer.SID, ((IFeatureLayer)layer).FeatureClass, feature));
                            }
                        }
                    }
                }

                string contentType;
                GetFeatureResponse(features, response, parameters.InfoFormat, parameters.InfoFormatXsl, out contentType);

                return (response.ToString(), contentType);
            }
        }

        async public Task<(byte[] body, string contentType)> WMS_GetLegendGraphic(string service, WMSParameterDescriptor parameters, IServiceRequestContext context)
        {
            if (parameters == null || _mapServer == null)
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrWhiteSpace(parameters.Layer))
            {
                throw new MapServerException("Parameter layer missing");
            }

            using (var serviceMap = await context.CreateServiceMapInstance())
            {
                int imageWidth, imageHeight;
                using (var dummy = Current.Engine.CreateBitmap(1, 1))
                {
                    var size = await DrawLegend(serviceMap, parameters, dummy);
                    imageWidth = size.Width;
                    imageHeight = size.Height;
                }
                using (var legendBitmap = Current.Engine.CreateBitmap(imageWidth, imageHeight))
                {
                    await DrawLegend(serviceMap, parameters, legendBitmap);

                    MemoryStream ms = new MemoryStream();
                    legendBitmap.Save(ms, parameters.GetImageFormat());
                    return (ms.ToArray(), parameters.GetContentType());
                }
            }
        }

        async private Task<CanvasSize> DrawLegend(IServiceMap serviceMap, WMSParameterDescriptor parameters, IBitmap bitmap)
        {
            if (serviceMap?.TOC == null)
            {
                throw new MapServerException("Map has not table of content (TOC)");
            }

            float offsetY = 5f, padding = 5f, lineSpan = 3f;
            float width = 0, height = 0;

            #region Layers

            bool defaultLayerVisibility = parameters.Layer == serviceMap.Name;
            var layerIds = parameters.Layer.Split(',');

            using (var canvas = bitmap.CreateCanvas())
            using (var fontBold = Current.Engine.CreateFont("Arial", 9, FontStyle.Bold))
            using (var fontRegular = Current.Engine.CreateFont("Arial", 8, FontStyle.Regular))
            using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            {
                canvas.SmoothingMode = SmoothingMode.AntiAlias;
                canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

                foreach (var element in serviceMap.MapElements)
                {
                    if (!defaultLayerVisibility && !layerIds.Contains("c" + element.ID))
                    {
                        continue;
                    }

                    if (!(element.Class is IFeatureClass))
                    {
                        continue;
                    }

                    MapServerHelper.Layers layers = MapServerHelper.FindMapLayers(serviceMap, _useTOC, element.ID.ToString());
                    if (layers == null)
                    {
                        continue;
                    }

                    foreach (IFeatureLayer featureLayer in layers.Where(l => l is IFeatureLayer && ((IFeatureLayer)l).FeatureRenderer != null))
                    {

                        var tocElement = serviceMap.TOC.GetTOCElement(featureLayer);
                        if (tocElement == null)
                        {
                            continue;
                        }

                        if (defaultLayerVisibility == true && tocElement.LayerVisible == false)
                        {
                            continue;
                        }

                        using (var tocLegendItems = await serviceMap.TOC.LegendSymbol(tocElement))
                        {
                            if (tocLegendItems.Items == null || tocLegendItems.Items.Count() == 0)
                            {
                                continue;
                            }

                            if (tocLegendItems.Items.Count() == 1)
                            {
                                var tocLegendItem = tocLegendItems.Items.First();
                                if (tocLegendItem == null && tocLegendItem.Image == null)
                                {
                                    continue;
                                }

                                if (bitmap.Width > 1)
                                {
                                    canvas.DrawBitmap(tocLegendItem.Image, new CanvasPointF(padding, offsetY));
                                    canvas.DrawText(tocElement.Name, fontBold, blackBrush, new CanvasPointF(padding * 2f + tocLegendItem.Image.Width, offsetY + 3f));
                                }

                                var labelSize = canvas.MeasureText(tocElement.Name, fontBold);
                                width = Math.Max(width, padding * 2f + tocLegendItem.Image.Width + labelSize.Width + padding);
                                height = Math.Max(height, offsetY + Math.Max(tocLegendItem.Image.Height, labelSize.Height) + padding);

                                offsetY += Math.Max(tocLegendItem.Image.Height, labelSize.Height) + lineSpan;
                            }
                            else
                            {
                                if (bitmap.Width > 1)
                                {
                                    canvas.DrawText(tocElement.Name, fontBold, blackBrush, new CanvasPointF(padding, offsetY));
                                }
                                var titleSize = canvas.MeasureText(tocElement.Name, fontBold);
                                width = Math.Max(width, padding * 2f + titleSize.Width);
                                height = Math.Max(height, offsetY + titleSize.Height + padding);

                                offsetY += titleSize.Height + lineSpan;

                                foreach (var tocLegendItem in tocLegendItems.Items)
                                {
                                    if (tocLegendItem.Image == null)
                                    {
                                        continue;
                                    }

                                    if (bitmap.Width > 1)
                                    {
                                        canvas.DrawBitmap(tocLegendItem.Image, new CanvasPointF(padding, offsetY));
                                        if (!String.IsNullOrWhiteSpace(tocLegendItem.Label))
                                        {
                                            canvas.DrawText(tocLegendItem.Label, fontRegular, blackBrush, new CanvasPointF(padding * 2f + tocLegendItem.Image.Width, offsetY + 3f));
                                        }
                                    }

                                    var labelSize = String.IsNullOrEmpty(tocLegendItem.Label) ? new CanvasSizeF(0f, 0f) : canvas.MeasureText(tocLegendItem.Label, fontRegular);
                                    width = Math.Max(width, padding * 2f + tocLegendItem.Image.Width + labelSize.Width + padding);
                                    height = Math.Max(height, Math.Max(tocLegendItem.Image.Height, labelSize.Height) + padding);

                                    offsetY += Math.Max(tocLegendItem.Image.Height, labelSize.Height) + lineSpan;
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            return new CanvasSize((int)width, (int)height);
        }

        #region Helper

        //private string getValue(string parameter, string[] parameters)
        //{
        //    foreach (string p in parameters)
        //    {
        //        if (p.ToLower().IndexOf(parameter.ToLower() + "=") == 0)
        //        {
        //            return p.Substring(parameter.Length + 1, p.Length - parameter.Length - 1);
        //        }
        //    }
        //    return null;
        //}

        //private string Concat(string[] parameters, string divider)
        //{
        //    if (parameters == null) return "";

        //    StringBuilder sb = new StringBuilder();
        //    foreach (string str in parameters)
        //    {
        //        if (sb.Length > 0) sb.Append(divider);
        //        sb.Append(str);
        //    }
        //    return sb.ToString();
        //}

        private string TransformXML(string xml, string xslPath)
        {
            try
            {
                XslCompiledTransform transformer = new XslCompiledTransform();
                transformer.Load(xslPath);

                StringReader xmlStream = new StringReader(xml);
                XPathDocument xpathdoc = new XPathDocument(xmlStream);

                MemoryStream ms = new MemoryStream();
                XmlTextWriter xWriter = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
                transformer.Transform(xpathdoc, xWriter);

                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                string ret = sr.ReadToEnd();
                sr.Close();
                ms.Close();
                xWriter.Close();

                return ret;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private IEnvelope GetEPSG4326Envelope(IServiceMap map)
        {
            if (map == null)
            {
                return null;
            }

            ISpatialReference sRef_map = map.Display.SpatialReference;
            ISpatialReference sRef_4326 = SpatialReference.FromID("epsg:4326");

            IEnvelope envelope = null;
            foreach (IDatasetElement layer in map.MapElements)
            {
                if (layer == null)
                {
                    continue;
                }

                IEnvelope env = null;
                ISpatialReference sRef = null;
                if (layer.Class is IFeatureClass)
                {
                    sRef = ((IFeatureClass)layer.Class).SpatialReference;
                    if (sRef == null)
                    {
                        sRef = sRef_map;
                    }

                    env = ((IFeatureClass)layer.Class).Envelope;
                }
                else if (layer.Class is IRasterClass && ((IRasterClass)layer.Class).Polygon != null)
                {
                    sRef = ((IRasterClass)layer.Class).SpatialReference;
                    if (sRef == null)
                    {
                        sRef = sRef_map;
                    }

                    env = ((IRasterClass)layer.Class).Polygon.Envelope;
                }
                else if (layer is IWebServiceLayer && ((IWebServiceLayer)layer).WebServiceClass != null && ((IWebServiceLayer)layer).WebServiceClass.Themes != null)
                {
                    IWebServiceLayer wLayer = (IWebServiceLayer)layer;
                    if (wLayer.WebServiceClass.Envelope != null)
                    {
                        sRef = wLayer.WebServiceClass.SpatialReference;
                        if (sRef == null)
                        {
                            sRef = sRef_map;
                        }

                        env = wLayer.WebServiceClass.Envelope;
                    }
                    else
                    {
                        foreach (IWebServiceTheme theme in wLayer.WebServiceClass.Themes)
                        {
                            if (theme == null)
                            {
                                continue;
                            }

                            if (theme.FeatureClass is IFeatureClass &&
                                theme.FeatureClass.Envelope != null)
                            {
                                sRef = theme.FeatureClass.SpatialReference;
                                if (sRef == null)
                                {
                                    sRef = sRef_map;
                                }

                                env = theme.FeatureClass.Envelope;
                            }
                        }
                    }
                }
                if (env == null || sRef == null)
                {
                    continue;
                }

                env = GeometricTransformerFactory.Transform2D(env, sRef, sRef_4326).Envelope;
                if (envelope == null)
                {
                    envelope = env;
                }
                else
                {
                    envelope.Union(env);
                }
            }

            return (envelope != null) ? envelope : new Envelope(-180, -90, 180, 90);
        }

        private IEnvelope TransFormEPSG4326Envelope(IEnvelope env4326, string projID, string version)
        {
            if (env4326 == null)
            {
                return null;
            }

            IEnvelope envelope = null;
            ISpatialReference to = null;
            if (projID.ToLower() == "epsg:4326")
            {
                to = SpatialReference.FromID("epsg:4326");
                envelope = new Envelope(env4326);
            }
            else
            {

                ISpatialReference from = SpatialReference.FromID("epsg:4326");
                to = SpatialReference.FromID(projID);

                if (from == null || to == null || from.Equals(to))
                {
                    envelope = new Envelope(env4326);
                }
                else
                {
                    envelope = GeometricTransformerFactory.Transform2D(env4326, from, to).Envelope;
                }
            }

            if (version == "1.3.0" && envelope != null && to != null)
            {
                IPoint ll = envelope.LowerLeft;
                IPoint ur = envelope.UpperRight;

                ll = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ll, to);
                ur = gView.Framework.OGC.GML.GeometryTranslator.Gml3Point(ur, to);

                envelope = new Envelope(ll, ur);
            }

            return envelope;
        }

        private void GetFeatureResponse(List<FeatureType> features, StringBuilder sb, WMSInfoFormat format, string formatXsl, out string contentType)
        {
            contentType = String.Empty;

            switch (format)
            {
                case WMSInfoFormat.html:
                    GetFeatureResponseHTML(features, sb);
                    contentType = "text/html";
                    break;
                case WMSInfoFormat.xml:
                    GetFeatureResponseXML(features, sb);
                    contentType = "text/xml";
                    break;
                case WMSInfoFormat.gml:
                    GetFeatureResponseGML(features, sb);
                    contentType = "text/html";
                    break;
                case WMSInfoFormat.text:
                    GetFeatureResponseText(features, sb);
                    contentType = "application/vnd.ogc.gml";
                    break;
                case WMSInfoFormat.geojson:
                    GetFeatureResponseGeoJson(features, sb);
                    contentType = "application/json";
                    break;
                case WMSInfoFormat.xsl:
                    StringBuilder xml = new StringBuilder();
                    GetFeatureResponseXML(features, xml);

                    string output = String.Empty;
                    try
                    {
                        output = this.TransformXML(xml.ToString(), $"{_mapServer.EtcPath}/wms/getfeatureinfo/{formatXsl}");

                        /*
                        var xslTrans = new XslCompiledTransform();
                        xslTrans.Load(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/misc/wms/GetFeatureInfo/" + formatXsl.Replace("\\", "/"));
                        using (StringReader xmlStrReader = new StringReader(xml.ToString()))
                        using (XmlReader xmlReader = XmlReader.Create(xmlStrReader))
                        using (StringWriter strWriter = new StringWriter())
                        using (XmlWriter xmlWriter = XmlWriter.Create(strWriter, xslTrans.OutputSettings))
                        {
                            xslTrans.Transform(xmlReader, xmlWriter);
                            output = strWriter.ToString();
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        output = "Exception: " + ex.Message;
                    }

                    sb.Append(output);
                    contentType = "text/xml";
                    break;
            }
        }

        private void GetFeatureResponseHTML(List<FeatureType> features, StringBuilder sb)
        {
            // Table
            sb.Append("<html><body><table style=\"border: solid 1px black;\">");

            // Header
            List<string> Columns = new List<string>();
            foreach (FeatureType feature in features)
            {
                foreach (FieldValue fv in feature.Feature.Fields)
                {
                    if (!Columns.Contains(fv.Name))
                    {
                        Columns.Add(fv.Name);
                    }
                }
            }
            sb.Append("<tr>");
            foreach (string col in Columns)
            {
                sb.Append("<th>"); sb.Append(col); sb.Append("</th>");
            }
            sb.Append("</tr>");

            foreach (FeatureType feature in features)
            {
                sb.Append("<tr>");
                foreach (string col in Columns)
                {
                    sb.Append("<td>");

                    object obj = feature.Feature[col];
                    if (obj == null)
                    {
                        sb.Append("&nbsp;");
                    }
                    else
                    {
                        sb.Append(obj.ToString());
                    }

                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</table></body></html>");
        }

        private void GetFeatureResponseXML(List<FeatureType> features, StringBuilder sb)
        {
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>");
            sb.Append(@"<gview_wms:FeatureInfoResponse version=""1.1.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:gview_wms=""http://www.gviewgis.com/wms"" xmlns=""http://www.gviewgis.com/wms"">");

            string layername = null;

            bool closeFeatureCollectionTag = false;

            foreach (FeatureType feature in features)
            {
                if (feature == null)
                {
                    continue;
                }

                if (layername != feature.FeatureClass.Name)
                {
                    layername = feature.FeatureClass.Name;
                    if (closeFeatureCollectionTag)
                    {
                        sb.Append("</gview_wms:FeatureInfoCollection>");
                    }

                    sb.Append("<gview_wms:FeatureInfoCollection layername=\"" + layername + "\">");
                    closeFeatureCollectionTag = true;
                }

                sb.Append("<gview_wms:FeatureInfo>");
                foreach (FieldValue fv in feature.Feature.Fields)
                {
                    sb.Append("<gview_wms:Field>");

                    string val = fv.Value != null ? fv.Value.ToString() : String.Empty;
                    // Encode for XML
                    val = System.Security.SecurityElement.Escape(val);

                    sb.Append("<gview_wms:FieldName>" + fv.Name + "</gview_wms:FieldName>");
                    sb.Append("<gview_wms:FieldValue>" + val + "</gview_wms:FieldValue>");

                    sb.Append("</gview_wms:Field>");
                }
                sb.Append("</gview_wms:FeatureInfo>");
            }
            if (closeFeatureCollectionTag)
            {
                sb.Append(@"</gview_wms:FeatureInfoCollection>");
            }

            sb.Append(@"</gview_wms:FeatureInfoResponse>");
            /*
            sb.Append("<GETFEATURERESPONSE>");
            sb.Append("<FEATURES>");
            foreach (FeatureType feature in features)
            {
                if (feature == null) continue;

                sb.Append("<FEATURE>");
                sb.Append("<FIELDS>");
                foreach (FieldValue fv in feature.Feature.Fields)
                {
                    sb.Append("<FIELD name=\"" + fv.Name + "\" value=\"" + fv.Value + "\" />");
                }
                sb.Append("</FIELDS>");
                sb.Append("</FEATURE>");
            }
            sb.Append("</FEATURES>");
            sb.Append("</GETFEATURERESPONSE>");
             * */
        }

        private void GetFeatureResponseGML(List<FeatureType> features, StringBuilder sb)
        {
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<wfs:FeatureCollection
   xmlns=""http://www.gViewGIS.com/server""
   xmlns:gv=""http://www.gViewGIS.com/server""
   xmlns:wfs=""http://www.opengis.net/wfs""
   xmlns:gml=""http://www.opengis.net/gml""
   xmlns:ogc=""http://www.opengis.net/ogc""
   xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">");

            // TODO: Bounding Box einfügen !!!

            foreach (FeatureType feature in features)
            {
                FeatureTranslator.Feature2GML(
                    feature.Feature,
                    feature.FeatureClass,
                    feature.Id,
                    sb,
                    (feature.FeatureClass.SpatialReference != null) ? feature.FeatureClass.SpatialReference.Name : String.Empty,
                    null, GmlVersion.v1);
            }

            sb.Append(@"</wfs:FeatureCollection>");
        }

        private void GetFeatureResponseText(List<FeatureType> features, StringBuilder sb)
        {
            if (features == null || features.Count == 0)
            {
                return;
            }

            sb.Append("GetFeatureInfo results:\n\n");
            foreach (FeatureType feature in features)
            {
                if (feature == null ||
                    feature.FeatureClass == null ||
                    feature.Feature == null)
                {
                    continue;
                }

                sb.Append("Layer '" + feature.FeatureClass.Name + "'\n");
                sb.Append("  Feature " + feature.Feature.OID + "\n");
                foreach (FieldValue fv in feature.Feature.Fields)
                {
                    if (fv.Value == null)
                    {
                        //sb.Append("    " + fv.Name + " = <null>\n");
                    }
                    else
                    {
                        sb.Append("    " + fv.Name + " = '" + fv.Value.ToString() + "'\n");
                    }
                }
            }
        }

        private void GetFeatureResponseGeoJson(List<FeatureType> features, StringBuilder sb)
        {
            sb.Append(GeoJsonHelper.ToGeoJson(features.Select(f => f.Feature)));
        }

        private class FeatureType
        {
            public string Id;
            public IFeatureClass FeatureClass;
            public IFeature Feature;

            public FeatureType(string id, IFeatureClass fc, IFeature feat)
            {
                Id = id;
                FeatureClass = fc;
                Feature = feat;
            }
        }
        #endregion
    }
}
