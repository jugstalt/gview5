using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.Network;
using gView.Framework.Network.Algorthm;
using gView.Framework.system;
using gView.Framework.XML;
using gView.GraphicsEngine;
using gView.Interoperability.ArcXML.Dataset;
using gView.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace gView.Interoperability.ArcXML
{
    [gView.Framework.system.RegisterPlugIn("BB294D9C-A184-4129-9555-398AA70284BC")]
    public class ArcXMLRequest : IServiceRequestInterpreter
    {
        private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
        private IMapServer _mapServer = null;
        protected bool _useTOC;

        public ArcXMLRequest()
        {
            _useTOC = false;
        }

        #region IServiceRequestInterpserviceRequest.Responseer Member

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
                context.ServiceRequest.Response = "<FATALERROR>MapServer Object is not available!</FATALERROR>";
                return;
            }
#if(DEBUG)
            Logger.LogDebug("Start ArcXML Request");
#endif
            string service = context.ServiceRequest.Service;
            string request = context.ServiceRequest.Request;

            try
            {
                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request_detail, request);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(request);

                if (service == "catalog" && doc.SelectSingleNode("//GETCLIENTSERVICES") != null)
                {
                    await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request, "AXL REQUEST: GETCLIENTSERVICES");
                    AXL response = new AXL("ARCXML", "1.1");

                    context.ServiceRequest.Response = await response.GETCLIENTSERVICES(_mapServer, context.ServiceRequest.Identity);
                    await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.request_detail, context.ServiceRequest.ResponseAsString);

                    return;
                }

                XmlNode rNode = doc.SelectSingleNode("//REQUEST");
                XmlNode rType = rNode.FirstChild;

                switch (rType.Name)
                {
                    case "GET_SERVICE_INFO":
#if(DEBUG)
                        Logger.LogDebug("Start ArcXML GET_SERVICE_INFO Request");
#endif
                        await PerformGetServiceInfoRequest(context, rType);
#if(DEBUG)
                        Logger.LogDebug("ArcXML GET_SERVICE_INFO Request Finished");
#endif
                        break;
                    case "GET_IMAGE":
#if(DEBUG)
                        Logger.LogDebug("Start ArcXML GET_IMAGE Request");
#endif
                        await PerformGetImageRequest(context, rType);
#if(DEBUG)
                        Logger.LogDebug("ArcXML GET_IMAGE Request Finished");
#endif
                        break;
                    case "GET_FEATURES":
#if(DEBUG)
                        Logger.LogDebug("Start ArcXML GET_FEATURES Request");
#endif
                        await PerformGetFeatureRequest(context, rType);
#if(DEBUG)
                        Logger.LogDebug("ArcXML GET_FEATURES Request Finished");
#endif
                        break;
                    case "GET_RASTER_INFO":
#if(DEBUG)
                        Logger.LogDebug("Start ArcXML GET_RASTER_INFO Request");
#endif
                        await PerformGetRasterInfoRequest(context, rType);
#if (DEBUG)
                        Logger.LogDebug("ArcXML GET_RASTER_INFO Request Finished");
#endif
                        break;
                    case "gv_CAN_TRACE_NETWORK":
                        await PerformCanTraceNetwork(context, rType);
                        break;
                    case "gv_TRACE_NETWORK":
                        await PerformTraceNetwork(context, rType);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                context.ServiceRequest.Response = CreateException(ex.Message + " " + ex.StackTrace);
                return;
            }
            finally
            {
#if(DEBUG)
                Logger.LogDebug("ArcXML Request Finished");
#endif
            }
        }

        public AccessTypes RequiredAccessTypes(IServiceRequestContext context)
        {
            var accessTypes = AccessTypes.None;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(context.ServiceRequest.Request);

            if (context.ServiceRequest.Service == "catalog" && doc.SelectSingleNode("//GETCLIENTSERVICES") != null)
            {
                return AccessTypes.None;
            }

            XmlNode rNode = doc.SelectSingleNode("//REQUEST");
            XmlNode rType = rNode.FirstChild;

            switch (rType.Name)
            {
                case "GET_IMAGE":
                    accessTypes |= AccessTypes.Map;
                    break;
                case "GET_FEATURES":
                case "GET_RASTER_INFO":
                case "gv_CAN_TRACE_NETWORK":
                case "gv_TRACE_NETWORK":
                    accessTypes |= AccessTypes.Query;
                    break;
            }

            return accessTypes;
        }

        public string IdentityName => "axl";
        public string IdentityLongName => "ESRI ArcXML (deprecated)";

        public int Priority => 0;

        public InterpreterCapabilities Capabilities
        {
            get
            {
                return new InterpreterCapabilities(new InterpreterCapabilities.Capability[]{
                    new InterpreterCapabilities.SimpleCapability("Emulating ServletEngine: Post ArcXML Capabilites ",InterpreterCapabilities.Method.Post,"{server}/servlet/com.esri.esrimap.Esrimap?cmd=capabilities&ServiceName={folder/service}","1.1"),
                    new InterpreterCapabilities.SimpleCapability("Emulating ServletEngine: Post ArcXML Request ",InterpreterCapabilities.Method.Post,"{server}/servlet/com.esri.esrimap.Esrimap?ServiceName={folder/service}","1.1"),
                    new InterpreterCapabilities.LinkCapability("Emulating ServletEngine: IMS Ping",InterpreterCapabilities.Method.Get,"{server}/servlet/com.esri.esrimap.Esrimap?cmd=ping","1.1"),
                    new InterpreterCapabilities.LinkCapability("Emulating ServletEngine: IMS GetVersion",InterpreterCapabilities.Method.Get,"{server}/servlet/com.esri.esrimap.Esrimap?cmd=getversion","1.1")
                    }
                );
            }
        }

        #endregion

        async private Task PerformGetServiceInfoRequest(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetServiceInfoRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request, "ArcXML Request: GET_SERVICE_INFO");

            AXL response = new AXL("ARCXML", "1.1");

            bool fields = NodeAttributeBool(rType, "fields", true);
            bool envelope = NodeAttributeBool(rType, "envelope", true);
            bool renderers = NodeAttributeBool(rType, "renderer", true);
            bool gv_meta = NodeAttributeBool(rType, "gv_meta", false);
            bool toc = false; //NodeAttributeBool(rType, "toc");
            serviceRequest.Response = await response.GET_SERVICE_INFO(context, fields, envelope, renderers, toc, gv_meta, _useTOC);

            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request_detail, serviceRequest.ResponseAsString);
        }

        async private Task PerformGetImageRequest(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetImageRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request, "ArcXML Request: GET_IMAGE");

            float dpi = 96f;
            double displayRotation = 0.0;

            GET_IMAGE_REQUEST getImage = new GET_IMAGE_REQUEST();
            XmlNode properties = rType.SelectSingleNode("PROPERTIES");
            XmlNode Envelope = properties.SelectSingleNode("ENVELOPE");
            if (Envelope != null)
            {
                getImage.Envelope = new Envelope(Envelope);
            }

            XmlNode imageSize = properties.SelectSingleNode("IMAGESIZE");
            if (imageSize != null)
            {
                getImage.iWidth = NodeAttributeInt(imageSize, "width");
                getImage.iHeight = NodeAttributeInt(imageSize, "height");

                if (imageSize.Attributes["dpi"] != null)
                {
                    dpi = float.Parse(imageSize.Attributes["dpi"].Value.Replace(",", "."), _nhi);
                }
                if (imageSize.Attributes["scalesymbols"] != null &&
                    imageSize.Attributes["scalesymbols"].Value.ToLower() == "true" &&
                    dpi != 96f)
                {
                    //getImage.symbolScaleFactor = dpi / 96f;
                    getImage.Dpi = dpi;
                }
            }
            XmlNode displaytransformation = properties.SelectSingleNode("DISPLAYTRANSFORMATION[@rotation]");
            if (displaytransformation != null)
            {
                displayRotation = double.Parse(displaytransformation.Attributes["rotation"].Value.Replace(",", "."), _nhi);
            }

            ISpatialReference sRef = SpatialReferenceFromNode(properties, SpatialreferenceType.Feature);

            XmlNode output = properties.SelectSingleNode("OUTPUT[@type]");
            if (output != null)
            {
                switch (output.Attributes["type"].Value.ToLower())
                {
                    case "gif":
                        getImage.ImageFormat = ImageFormat.Gif;
                        break;
                    case "jpg":
                    case "jpeg":
                        getImage.ImageFormat = ImageFormat.Jpeg;
                        break;
                    case "png":
                    case "png8":
                    case "png24":
                    case "png32":
                        getImage.ImageFormat = ImageFormat.Png;
                        break;
                    default:
                        getImage.ImageFormat = ImageFormat.Png;
                        break;
                }
            }

            getImage.layerDefs = properties.SelectSingleNode("LAYERLIST");
            getImage.LAYERS = rType.SelectNodes("//LAYER");

            using (IServiceMap map = await context.CreateServiceMapInstance())  // _mapServer[context];
            {
                if (map == null || map.Display == null)
                {
                    //((AXLRequest)axlrequest).ResetEvent.Set();
                    serviceRequest.Response = CreateException("Service not available...");
                    return;
                }

                map.Display.Dpi = dpi;
                if (sRef != null)
                {
                    map.Display.SpatialReference = sRef;
                }

                if (displayRotation != 0.0)
                {
                    map.Display.DisplayTransformation.DisplayRotation = displayRotation;
                }

                if (properties.SelectSingleNode("DISPLAY[@refscale]") != null)
                {
                    map.Display.ReferenceScale = double.Parse(properties.SelectSingleNode("DISPLAY[@refscale]").Attributes["refscale"].Value.Replace(",", "."), _nhi);
                }


                XmlNode background = properties.SelectSingleNode("BACKGROUND");
                if (background != null)
                {
                    ArgbColor col = NodeAttributeColor(background, "color");
                    if (!col.Equals(ArgbColor.Empty))
                    {
                        map.Display.BackgroundColor = col;
                    }
                    col = NodeAttributeColor(background, "transcolor");
                    if (!col.Equals(ArgbColor.Empty))
                    {
                        map.Display.TransparentColor = col;
                        map.Display.MakeTransparent = true;
                    }
                    else
                    {
                        map.Display.MakeTransparent = false;
                    }
                }

                if (map.Display.MakeTransparent && getImage.ImageFormat == ImageFormat.Png)
                {
                    // Beim Png sollt dann beim zeichnen keine Hintergrund Rectangle gemacht werden
                    // Darum Farbe mit A=0
                    // Sonst schaut das Bild beim PNG32 und Antialiasing immer zerrupft aus...
                    map.Display.BackgroundColor = ArgbColor.Transparent;
                }

                if (properties.SelectSingleNode("DRAW[@map='false']") != null)
                {
                    XmlNode legend = properties.SelectSingleNode("LEGEND");
                    if (legend == null)
                    {
                        return;
                    }

                    serviceRequest.Response = await getImage.LegendRequest(map, context, _useTOC);
                }
                else
                {
                    serviceRequest.Response = await getImage.ImageRequest(map, context, _useTOC);
                }
                //map.Release();
            }
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request_detail, serviceRequest.ResponseAsString);
        }
        async private Task PerformGetFeatureRequest(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetFeatureRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request, "ArcXML Request: GET_FEATURES");

            try
            {
                GET_FEATURES_REQUEST getFeatures = new GET_FEATURES_REQUEST();

                // LAYER
                XmlNode layer = rType.SelectSingleNode("LAYER");
                if (layer == null)
                {
                    //((AXLRequest)axlrequest).ResetEvent.Set();
                    serviceRequest.Response = CreateException("No Layerdefinition");
                    return;
                }
                string id = layer.Attributes["id"].Value;
                using (IServiceMap map2 = await context.CreateServiceMapInstance()) //  _mapServer[context];
                {
                    if (map2 == null)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("Service not available...");
                        return;
                    }
                    getFeatures.ServiceMap = map2;
                    string filterQuery = String.Empty;
                    getFeatures.Classes = FindTableClass(map2, id, out filterQuery);
                    if (getFeatures.Classes == null || getFeatures.Classes.Count == 0)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("Can't find layer with id='" + id + "'");
                        return;
                    }

                    // QUERY, SPATIALQUERY
                    XmlNode query = rType.SelectSingleNode("QUERY");
                    if (query == null)
                    {
                        query = rType.SelectSingleNode("SPATIALQUERY");
                    }

                    if (query != null)
                    {
                        IArcXMLQueryFilter filter = null;
                        if (query.Name == "QUERY")
                        {
                            filter = new ArcXMLQueryFilter();
                        }
                        else
                        {
                            filter = new ArcXMLSpatialFilter();
                        }

                        if (query.Attributes["where"] != null && !String.IsNullOrWhiteSpace(query.Attributes["where"].Value))
                        {
                            query.Attributes["where"].Value.CheckWhereClauseForSqlInjection();

                            filter.WhereClause = query.Attributes["where"].Value;
                        }
                        if (query.Attributes["subfields"] != null)
                        {
                            foreach (string field in query.Attributes["subfields"].Value.Split(' '))
                            {
                                if (field == "#ALL#")
                                {
                                    filter.AddField("*");
                                }
                                else if (field == "#SHAPE#")
                                {
                                    foreach (ITableClass tClass in getFeatures.Classes)
                                    {
                                        if (!(tClass is IFeatureClass))
                                        {
                                            continue;
                                        }

                                        filter.AddField(((IFeatureClass)tClass).ShapeFieldName);
                                    }
                                }
                                else if (field == "#ID#")
                                {
                                    foreach (ITableClass tClass in getFeatures.Classes)
                                    {
                                        filter.AddField(tClass.IDFieldName);
                                    }
                                }
                                else
                                {
                                    filter.AddField(field);
                                }
                            }
                        }
                        else
                        {
                            filter.SubFields = "*";
                        }

                        XmlNode spatialFilter = query.SelectSingleNode("SPATIALFILTER");
                        if (query.Name == "SPATIALQUERY" && spatialFilter != null)
                        {
                            if (spatialFilter.Attributes["relation"] != null)
                            {
                                ((ISpatialFilter)filter).SpatialRelation =
                                    (spatialFilter.Attributes["relation"].Value == "envelope_intersection") ? spatialRelation.SpatialRelationEnvelopeIntersects : spatialRelation.SpatialRelationIntersects;
                            }
                            else
                            {
                                ((ISpatialFilter)filter).SpatialRelation = spatialRelation.SpatialRelationIntersects;
                            }
                            ((ISpatialFilter)filter).Geometry = ArcXMLGeometry.AXL2Geometry(spatialFilter.InnerXml, false);
                            ((ISpatialFilter)filter).FilterSpatialReference = SpatialReferenceFromNode(query, SpatialreferenceType.Filter);

                            if (((ISpatialFilter)filter).FilterSpatialReference == null)
                            {
                                ((ISpatialFilter)filter).FilterSpatialReference = map2.Display.SpatialReference;
                            }
                        }
                        XmlNode buffer = query.SelectSingleNode("BUFFER[@distance]");
                        if (buffer != null && buffer.SelectSingleNode("TARGETLAYER[@id]") != null)
                        {
                            string targetID = buffer.SelectSingleNode("TARGETLAYER[@id]").Attributes["id"].Value;
                            List<ITableClass> tClass = FindTableClass(map2, targetID);
                            if (tClass == null || tClass.Count == 0)
                            {
                                serviceRequest.Response = CreateException("Layer with ID='" + targetID + "' is not available...");
                                return;
                            }

                            //
                            // TODO:
                            // Hier wird nur die erste Klasse berücksichtigt, was passiert, wenn mehrer Klassen
                            // beteiligt sind???!!!! (Mehrer Layer in einem TOC als Targetlayer!!!)
                            //
                            // Wenn beide 
                            if (tClass.Count > 1)
                            {
                                // wird recht kompliziert, abfrage zwar möglich, aber dann die Darstellung...
                                // darum ERROR 
                                serviceRequest.Response = WriteException("Can't perform buffer operation with merged Featureclasses...");

                                //List<IPolygon> bufferPolygons = new List<IPolygon>();
                                //foreach (ITableClass rootClass in getFeatures.Classes)
                                //{
                                //    IQueryFilter cloned = filter.Clone() as IQueryFilter;
                                //    if (!await MapServerHelper.ModifyFilter(map2, rootClass, cloned))
                                //    {
                                //        continue;
                                //    }

                                //    BufferQueryFilter bFilter = new BufferQueryFilter();
                                //    bFilter.RootFilter = cloned;
                                //    bFilter.RootFeatureClass = rootClass as IFeatureClass;
                                //    bFilter.BufferDistance = Convert.ToDouble(buffer.Attributes["distance"].Value.Replace(".", ","));

                                //    try
                                //    {
                                //        ISpatialFilter sFilter = await BufferQueryFilter.ConvertToSpatialFilter(bFilter);
                                //        IPolygon polygon = sFilter.Geometry as IPolygon;
                                //        if (sFilter.FilterSpatialReference != null &&
                                //            !sFilter.FilterSpatialReference.Equals(map2.Display.SpatialReference))
                                //        {
                                //            polygon = GeometricTransformerFactory.Transform2D(
                                //                polygon,
                                //                sFilter.FilterSpatialReference,
                                //                map2.Display.SpatialReference) as IPolygon;
                                //        }
                                //        bufferPolygons.Add(polygon);
                                //    }
                                //    catch { }

                                //    IPolygon bufferPolygon = gView.Framework.SpatialAlgorithms.Algorithm.MergePolygons(bufferPolygons);
                                //    if (bufferPolygons != null)
                                //    {
                                //        ISpatialFilter newFilter = new SpatialFilter();
                                //        newFilter.SubFields = filter.SubFields;
                                //        newFilter.FilterSpatialReference = map2.Display.SpatialReference;
                                //        newFilter.SpatialRelation = spatialRelation.SpatialRelationIntersects;
                                //        newFilter.Geometry = bufferPolygon;
                                //        newFilter.FeatureSpatialReference = filter.FeatureSpatialReference;
                                //        filter = new ArcXMLSpatialFilter(newFilter);
                                //    }
                                //}
                            }
                            else if (tClass.Count == 1)
                            {
                                if (tClass[0].Dataset is ArcIMSDataset &&
                                    tClass[0].Dataset == getFeatures.Classes[0].Dataset)
                                {
                                    filter.BufferNode = buffer.Clone();
                                    filter.BufferNode.SelectSingleNode("TARGETLAYER[@id]").Attributes["id"].Value =
                                        ArcXMLLayerID(map2, targetID);
                                }
                                else
                                {
                                    ArcXMLBufferQueryFilter bFilter = new ArcXMLBufferQueryFilter(/*filter*/);
                                    bFilter.RootFilter = filter;
                                    bFilter.SubFields = "*";
                                    bFilter.RootFeatureClass = getFeatures.Classes[0] as IFeatureClass;
                                    bFilter.BufferDistance = Convert.ToDouble(buffer.Attributes["distance"].Value.Replace(".", ","));
                                    bFilter.FeatureSpatialReference = filter.FeatureSpatialReference;
                                    filter = bFilter;

                                    getFeatures.Classes = tClass;
                                }
                            }
                        }

                        //   attributes ="true | false"  [true] 
                        //   beginrecord ="integer"  [0] 
                        //   checkesc ="true | false"  [false] 
                        //   compact ="true | false"  [false] 
                        //   dataframe ="string" 
                        //   envelope ="true | false"  [false] 
                        //   featurelimit ="integer"  [all features] 
                        //   geometry ="true | false"  [true] 
                        //   globalenvelope ="true | false"  [false] 
                        //   outputmode ="binary | xml | newxml"  [binary] 
                        //   skipfeatures ="true | false"  [false] 

                        if (rType.Attributes["beginrecord"] != null)
                        {
                            filter.beginrecord = Convert.ToInt32(rType.Attributes["beginrecord"].Value);
                        }

                        if (rType.Attributes["featurelimit"] != null && Convert.ToInt32(rType.Attributes["featurelimit"].Value) > 0)
                        {
                            filter.featurelimit = Math.Min(Convert.ToInt32(rType.Attributes["featurelimit"].Value), _mapServer.FeatureQueryLimit);
                        }

                        if (rType.Attributes["envelope"] != null)
                        {
                            filter.envelope = Convert.ToBoolean(rType.Attributes["envelope"].Value);
                        }

                        if (rType.Attributes["geometry"] != null)
                        {
                            filter.geometry = Convert.ToBoolean(rType.Attributes["geometry"].Value);
                        }

                        if (rType.Attributes["attributes"] != null)
                        {
                            filter.attributes = Convert.ToBoolean(rType.Attributes["attributes"].Value);
                        }

                        if (rType.Attributes["checkesc"] != null)
                        {
                            filter.checkesc = Convert.ToBoolean(rType.Attributes["checkesc"].Value);
                        }

                        if (rType.Attributes["compact"] != null)
                        {
                            filter.compact = Convert.ToBoolean(rType.Attributes["compact"].Value);
                        }

                        if (rType.Attributes["globalenvelope"] != null)
                        {
                            filter.globalenvelope = Convert.ToBoolean(rType.Attributes["globalenvelope"].Value);
                        }

                        if (rType.Attributes["skipfeatures"] != null)
                        {
                            filter.skipfeatures = Convert.ToBoolean(rType.Attributes["skipfeatures"].Value);
                        }

                        if (rType.Attributes["dataframe"] != null)
                        {
                            filter.dataframe = rType.Attributes["dataframe"].Value;
                        }

                        if (rType.Attributes["outputmode"] != null)
                        {
                            switch (rType.Attributes["outputmode"].Value)
                            {
                                case "binary":
                                    filter.outputmode = OutputMode.binary;
                                    break;
                                case "xml":
                                    filter.outputmode = OutputMode.xml;
                                    break;
                                case "newxml":
                                    filter.outputmode = OutputMode.newxml;
                                    break;
                            }
                        }

                        if (filter.geometry)
                        {
                            foreach (ITableClass tClass in getFeatures.Classes)
                            {
                                if (!(tClass is IFeatureClass))
                                {
                                    continue;
                                }

                                filter.AddField(((IFeatureClass)tClass).ShapeFieldName);
                            }
                        }

                        filter.ContextLayerDefaultSpatialReference = map2 is IMap ? ((IMap)map2).LayerDefaultSpatialReference : null;
                        getFeatures.Filter = filter;

                        if (!String.IsNullOrWhiteSpace(filterQuery))
                        {
                            filter.WhereClause = (!String.IsNullOrWhiteSpace(filter.WhereClause)) ?
                                "(" + filter.WhereClause + ") AND " + filterQuery :
                                filterQuery;
                        }

                        ISpatialReference sRef2 = this.SpatialReferenceFromNode(query, SpatialreferenceType.Filter);
                        if (sRef2 != null)
                        {
                            map2.Display.SpatialReference = sRef2;
                            if (getFeatures.Filter is ISpatialFilter)
                            {
                                ((ISpatialFilter)getFeatures.Filter).FilterSpatialReference = sRef2;
                            }
                        }
                        ISpatialReference sRef3 = this.SpatialReferenceFromNode(query, SpatialreferenceType.Feature);
                        if (sRef3 != null)
                        {
                            getFeatures.Filter.FeatureSpatialReference = sRef3;
                            if (getFeatures.Filter is IBufferQueryFilter &&
                                ((IBufferQueryFilter)getFeatures.Filter).RootFilter != null)
                            {
                                ((IBufferQueryFilter)getFeatures.Filter).RootFilter.FeatureSpatialReference = sRef3;
                            }
                        }
                    }

                    getFeatures.Filter.SetUserData("IServiceRequestContext", context);
                    if (getFeatures.Filter is IBufferQueryFilter)
                    {
                        ((IBufferQueryFilter)getFeatures.Filter).RootFilter.SetUserData("IServiceRequestContext", context);
                    }

                    serviceRequest.Response = await getFeatures.Request();
                }

                await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request_detail, serviceRequest.ResponseAsString);
                return;
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                serviceRequest.Response = CreateException(ex.Message);
                return;
            }
        }
        async private Task PerformGetRasterInfoRequest(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetRasterInfoRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            if (rType.Attributes["layerid"] == null)
            {
                serviceRequest.Response = CreateException("missing layerid attribute");
                return;
            }
            using (IServiceMap map3 = await context.CreateServiceMapInstance()) // _mapServer[context];
            {
                if (map3 == null)
                {
                    //((AXLRequest)axlrequest).ResetEvent.Set();
                    serviceRequest.Response = CreateException("Service not available...");
                    return;
                }

                List<IClass> tables = FindClass(map3, rType.Attributes["layerid"].Value);
                if (tables == null || tables.Count == 0)
                {
                    serviceRequest.Response = CreateException("no layer with id=" + rType.Attributes["layerid"].Value);
                    return;
                }
                foreach (IClass table in ListOperations<IClass>.Clone(tables))
                {
                    if (/*table is IRasterClass && */table is IPointIdentify)
                    {
                        continue;
                    }

                    tables.Remove(table);
                }
                if (tables.Count == 0)
                {
                    serviceRequest.Response = CreateException("rasterinfo is not implemented for this layer");
                    return;
                }
                List<IPoint> points = new List<IPoint>();
                bool compact = false, grid = false;
                int rows = 0, cols = 0;
                double gridDx = 1, gridDy = 1;

                if (rType.Attributes["x"] != null && rType.Attributes["y"] != null)
                {
                    points.Add(new Point(double.Parse(rType.Attributes["x"].Value),
                                         double.Parse(rType.Attributes["y"].Value)));
                }
                else if (rType.Attributes["minx"] != null && rType.Attributes["miny"] != null &&
                         rType.Attributes["maxx"] != null && rType.Attributes["maxy"] != null &&
                         rType.Attributes["dx"] != null && rType.Attributes["dy"] != null)
                {
                    double minx = double.Parse(rType.Attributes["minx"].Value);
                    double miny = double.Parse(rType.Attributes["miny"].Value);
                    double maxx = double.Parse(rType.Attributes["maxx"].Value);
                    double maxy = double.Parse(rType.Attributes["maxy"].Value);
                    double dx = double.Parse(rType.Attributes["dx"].Value);
                    double dy = double.Parse(rType.Attributes["dy"].Value);

                    for (double y = miny; y <= maxy; y += dy)
                    {
                        cols++; rows = 0;
                        for (double x = minx; x <= maxx; x += dx)
                        {
                            rows++;
                            points.Add(new Point(x, y));
                        }
                    }
                    compact = true;
                }
                else if (rType.Attributes["grid_p0_x"] != null && rType.Attributes["grid_p0_y"] != null &&
                         rType.Attributes["grid_p1_x"] != null && rType.Attributes["grid_p1_y"] != null &&
                         rType.Attributes["grid_p2_x"] != null && rType.Attributes["grid_p2_y"] != null &&
                         rType.Attributes["dx"] != null && rType.Attributes["dy"] != null)
                {
                    points.Add(new Point(double.Parse(rType.Attributes["grid_p0_x"].Value),
                                         double.Parse(rType.Attributes["grid_p0_y"].Value)));
                    points.Add(new Point(double.Parse(rType.Attributes["grid_p1_x"].Value),
                                         double.Parse(rType.Attributes["grid_p1_y"].Value)));
                    points.Add(new Point(double.Parse(rType.Attributes["grid_p2_x"].Value),
                                         double.Parse(rType.Attributes["grid_p2_y"].Value)));
                    gridDx = double.Parse(rType.Attributes["dx"].Value);
                    gridDy = double.Parse(rType.Attributes["dy"].Value);
                    grid = true;
                }
                else
                {
                    foreach (XmlNode pointNode in rType.SelectNodes("POINT"))
                    {
                        if (pointNode.Attributes["x"] != null && pointNode.Attributes["y"] != null)
                        {
                            points.Add(new Point(double.Parse(pointNode.Attributes["x"].Value),
                                                 double.Parse(pointNode.Attributes["y"].Value)));
                        }
                    }
                }
                if (points.Count == 0)
                {
                    serviceRequest.Response = CreateException("no coordinates in rasterinfo");
                    return;
                }

                GET_RASTER_INFO raster_request = new GET_RASTER_INFO();

                XmlNode coordsysNode = rType.SelectSingleNode("COORDSYS");
                raster_request.SpatialReference = SpatialReferenceFromNode(rType, SpatialreferenceType.Coordsys);
                if (raster_request.SpatialReference == null)
                {
                    raster_request.SpatialReference = map3.Display.SpatialReference;
                }

                XmlNode dispNode = rType.SelectSingleNode("gv_display");
                if (dispNode != null)
                {
                    Display display = new Display(map3 as IMap);

                    if (dispNode.Attributes["iwidth"] == null || dispNode.Attributes["iheight"] == null)
                    {
                        serviceRequest.Response = CreateException("invalid gv_display node (no iwidth/iheight attribute)");
                        return;
                    }
                    display.ImageWidth = Convert.ToInt32(dispNode.Attributes["iwidth"].Value);
                    display.ImageHeight = Convert.ToInt32(dispNode.Attributes["iheight"].Value);

                    XmlNode dispEnvNode = dispNode.SelectSingleNode("ENVELOPE");
                    if (dispNode == null)
                    {
                        serviceRequest.Response = CreateException("invalid gv_display node (no ENVELOPE node)");
                        return;
                    }
                    display.Envelope = new Envelope(dispEnvNode);

                    raster_request.Display = display;
                }

                raster_request.Classes = tables;
                raster_request.Points = points;
                raster_request.UserData = new UserData();
                raster_request.UserData.SetUserData("IServiceRequestContext", context);
                raster_request.Compact = compact;
                raster_request.Grid = grid;
                raster_request.GridDx = gridDx;
                raster_request.GridDy = gridDy;
                raster_request.CompactRows = rows;
                raster_request.CompactCols = cols;

                serviceRequest.Response = await raster_request.Request();
            }
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request_detail, serviceRequest.ResponseAsString);
        }

        #region Network Methods

        async private Task PerformCanTraceNetwork(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetFeatureRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request, "ArcXML Request: gv_CAN_TRACE_NETWORK");

            try
            {
                var result = await GetNetworkTracerInputCollection(context, rType);

                INetworkTracer tracer = result.tracer;
                INetworkFeatureClass netFc = result.netFc;

                var networkInput = result.inputCollection;

                int counter = 0;
                bool hasMore = false;
                XmlWriter xWriter = GetNetworkFeaturesWriter();

                if (networkInput != null && netFc != null && tracer != null)
                {
                    foreach (var inputItem in networkInput)
                    {
                        if (inputItem is NetworkSourceInput)
                        {
                            counter++;
                            IFeature node = await netFc.GetNodeFeature(((NetworkSourceInput)inputItem).NodeId);
                            WriteNetworkNodeFeature(xWriter, ((NetworkSourceInput)inputItem).NodeId, node.Shape as IPoint, null);
                        }
                        else if (inputItem is NetworkSourceEdgeInput)
                        {
                            counter++;
                            IFeature edge = await netFc.GetEdgeFeature(((NetworkSourceEdgeInput)inputItem).EdgeId);
                            WriteNetworkEdgeFeature(xWriter, ((NetworkSourceEdgeInput)inputItem).EdgeId, edge.Shape as IPolyline, null);
                        }
                    }
                }

                serviceRequest.Response = GetNetworkFeaturesReturnString(xWriter, counter, hasMore);
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                serviceRequest.Response = CreateException(ex.Message);
                return;
            }
        }

        async private Task PerformTraceNetwork(IServiceRequestContext context, XmlNode rType)
        {
            if (context == null || context.ServiceRequest == null)
            {
                await _mapServer.LogAsync(context, "PerformGetFeatureRequest", loggingMethod.error, "no context or servicerequest");
                return;
            }

            ServiceRequest serviceRequest = context.ServiceRequest;
            await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.request, "ArcXML Request: gv_CAN_TRACE_NETWORK");

            try
            {
                var result = await GetNetworkTracerInputCollection(context, rType);

                INetworkTracer tracer = result.tracer;
                INetworkFeatureClass netFc = result.netFc;

                var input = result.inputCollection;

                int counter = 0;
                bool hasMore = false;
                XmlWriter xWriter = GetNetworkFeaturesWriter();

                if (input != null && netFc != null && tracer != null)
                {
                    var networkOutput = await tracer.Trace(netFc, input, new CancelTracker());

                    foreach (var outputItem in networkOutput)
                    {
                        if (outputItem is NetworkEdgeCollectionOutput)
                        {
                            using (IFeatureCursor cursor = await NetworkPathEdges(netFc, (NetworkEdgeCollectionOutput)outputItem))
                            {
                                IFeature feature;
                                while ((feature = await cursor.NextFeature()) != null)
                                {
                                    if (!(feature.Shape is IPolyline))
                                    {
                                        continue;
                                    }

                                    counter++;
                                    if (feature.FindField("FDB_OID") == null)
                                    {
                                        feature.Fields.Add(new FieldValue("FDB_OID", feature.OID));
                                    }

                                    int edgeId = feature.FindField("_eid") != null ? int.Parse(feature["_eid"].ToString()) : feature.OID;
                                    WriteNetworkEdgeFeature(xWriter, edgeId, feature.Shape as IPolyline, feature);
                                }
                            }
                        }
                        else if (outputItem is NetworkPolylineOutput)
                        {
                            counter++;
                            WriteNetworkEdgeFeature(xWriter, -1, ((NetworkPolylineOutput)outputItem).Polyline, null);
                        }
                        else if (outputItem is NetworkFlagOutput)
                        {
                            counter++;

                            int nodeId = -1;
                            IFeature nodeFeature = null;
                            var nfo = (NetworkFlagOutput)outputItem;
                            if (nfo.UserData is NetworkFlagOutput.NodeFeatureData)
                            {
                                nodeId = ((NetworkFlagOutput.NodeFeatureData)nfo.UserData).NodeId;
                            }

                            if (nodeId > 0)
                            {
                                nodeFeature = await netFc.GetNodeFeatureAttributes(nodeId, null);
                                if (nodeFeature != null)
                                {
                                    if (nodeFeature.FindField("FDB_OID") == null)
                                    {
                                        nodeFeature.Fields.Add(new FieldValue("FDB_OID", nodeId));
                                    }
                                    else
                                    {
                                        nodeFeature["FDB_OID"] = nodeId;
                                    }
                                }
                            }
                            nodeFeature.Fields.Add(new FieldValue("FDB_NW_NODEID", nodeId));

                            WriteNetworkNodeFeature(xWriter, nodeId, nfo.Location, nodeFeature);
                        }
                    }
                }

                serviceRequest.Response = GetNetworkFeaturesReturnString(xWriter, counter, hasMore);
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                serviceRequest.Response = CreateException(ex.Message);
                return;
            }
        }

        #region Network Helper

        async private Task<(NetworkTracerInputCollection inputCollection, INetworkFeatureClass netFc, INetworkTracer tracer)>
            GetNetworkTracerInputCollection(IServiceRequestContext context, XmlNode rType)
        {
            ServiceRequest serviceRequest = context.ServiceRequest;

            INetworkFeatureClass netFc = null;
            INetworkTracer tracer = null;

            try
            {
                #region Get Layer and Tracer

                // LAYER
                XmlNode layer = rType.SelectSingleNode("LAYER");
                if (layer == null)
                {
                    //((AXLRequest)axlrequest).ResetEvent.Set();
                    serviceRequest.Response = CreateException("No Layerdefinition");
                    return (null, netFc, tracer);
                }

                string id = layer.Attributes["id"].Value;
                using (IServiceMap map2 = await context.CreateServiceMapInstance()) // _mapServer[context];
                {
                    if (map2 == null)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("Service not available...");
                        return (null, netFc, tracer);
                    }

                    string filterQuery = String.Empty;
                    var classes = FindTableClass(map2, id, out filterQuery);
                    if (classes == null || classes.Count == 0 || !(classes[0] is INetworkFeatureClass))
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("Can't find network layer with id='" + id + "'");
                        return (null, netFc, tracer);
                    }
                    netFc = (INetworkFeatureClass)classes[0];

                    // TRACER
                    XmlNode tracerNode = rType.SelectSingleNode("gv_TRACER");
                    if (tracerNode == null)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("No Tracer definition");
                        return (null, netFc, tracer);
                    }
                    string tracerguid = tracerNode.Attributes["id"].Value;

                    if (new Guid(tracerguid) == Guid.Empty)
                    {
                        tracer = new NetworkElementTracer();
                    }
                    else
                    {
                        tracer = PlugInManager.Create(new Guid(tracerguid)) as INetworkTracer;
                    }

                    if (tracer == null)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("No Tracer-Plugin with guid: " + tracerguid);
                        return (null, netFc, tracer);
                    }

                    #endregion

                    #region Queryfilter

                    XmlNode queryNode = rType.SelectSingleNode("QUERY");
                    if (queryNode == null)
                    {
                        queryNode = rType.SelectSingleNode("SPATIALQUERY");
                    }

                    IQueryFilter filter = ObjectFromAXLFactory.Query(queryNode, netFc as ITableClass);
                    if (filter == null)
                    {
                        //((AXLRequest)axlrequest).ResetEvent.Set();
                        serviceRequest.Response = CreateException("No Filter!");
                        return (null, netFc, tracer);
                    }

                    #endregion

                    NetworkTracerInputCollection input = new NetworkTracerInputCollection();

                    #region Barriers

                    XmlNode barriersNode = rType.SelectSingleNode("gv_NW_BARRIERS");
                    if (barriersNode != null)
                    {
                        foreach (XmlNode barrierNode in barriersNode.SelectNodes("gv_NW_BARRIER[@nodeid]"))
                        {
                            input.Add(new NetworkBarrierNodeInput(Convert.ToInt32(barrierNode.Attributes["nodeid"].Value)));
                        }
                    }

                    #endregion

                    #region Closest Node

                    double dist = double.MaxValue;
                    int n1 = -1;
                    IPoint p1 = null, p = null;
                    if (filter is ISpatialFilter && ((ISpatialFilter)filter).Geometry != null)
                    {
                        p = ((ISpatialFilter)filter).Geometry.Envelope.Center;
                    }

                    using (IFeatureCursor cursor = await netFc.GetNodeFeatures(filter))
                    {
                        IFeature feature;
                        while ((feature = await cursor.NextFeature()) != null)
                        {
                            if (p != null)
                            {
                                double d = p.Distance(feature.Shape as IPoint);
                                if (d < dist)
                                {
                                    dist = d;
                                    n1 = feature.OID;
                                    p1 = feature.Shape as IPoint;
                                }
                            }
                            else
                            {
                                n1 = feature.OID;
                                p1 = feature.Shape as IPoint;
                                break;
                            }
                        }
                    }

                    #endregion

                    if (p1 != null && n1 >= 0)
                    {

                        input.Add(new NetworkSourceInput(n1));

                        if (tracer.CanTrace(input))
                        {
                            return (input, netFc, tracer);
                        }
                    }

                    return (null, netFc, tracer);
                }
            }
            catch (Exception ex)
            {
                await _mapServer.LogAsync(context, "Service:" + serviceRequest.Service, loggingMethod.error, ex.Message + "\r\n" + ex.StackTrace);
                serviceRequest.Response = CreateException(ex.Message);
                return (null, netFc, tracer);
            }
        }

        private XmlWriter GetNetworkFeaturesWriter()
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xWriter = new XmlTextWriter(ms, Encoding.UTF8);

            xWriter.WriteStartDocument();
            xWriter.WriteStartElement("ARCXML");
            xWriter.WriteAttributeString("version", "1.1");

            xWriter.WriteStartElement("RESPONSE");
            xWriter.WriteStartElement("FEATURES");

            return xWriter;
        }

        private string GetNetworkFeaturesReturnString(XmlWriter xWriter, int counter, bool hasmore)
        {
            StringBuilder axl = new StringBuilder();
            xWriter.WriteStartElement("FEATURECOUNT");
            xWriter.WriteAttributeString("count", counter.ToString());
            xWriter.WriteAttributeString("hasmore", hasmore.ToString().ToLower());
            xWriter.WriteEndElement(); // FEATURECOUNT

            xWriter.WriteEndElement();
            xWriter.WriteEndDocument();
            xWriter.Flush();

            Stream ms = ((XmlTextWriter)xWriter).BaseStream;
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            axl.Append(sr.ReadToEnd());
            sr.Close();
            ms.Close();
            xWriter.Close();

            return axl.ToString();
        }

        private void WriteNetworkNodeFeature(XmlWriter xWriter, int nodeId, IPoint point, IFeature nodeFeature)
        {
            xWriter.WriteStartElement("FEATURE");
            xWriter.WriteAttributeString("type", "network_node");
            xWriter.WriteAttributeString("id", nodeId.ToString());

            if (nodeFeature != null)
            {
                xWriter.WriteStartElement("FIELDS");
                foreach (var fieldValue in nodeFeature.Fields)
                {
                    xWriter.WriteStartElement("FIELD");
                    xWriter.WriteAttributeString("name", fieldValue.Name);
                    xWriter.WriteAttributeString("value", fieldValue.Value != null ? fieldValue.Value.ToString() : String.Empty);
                    xWriter.WriteEndElement(); //FIELD
                }
                xWriter.WriteEndElement(); // FIELDS
            }

            if (point != null)
            {
                xWriter.WriteRaw(AXLFromObjectFactory.Geometry(point));
            }

            xWriter.WriteEndElement();  // FEATURE
        }

        private void WriteNetworkEdgeFeature(XmlWriter xWriter, int edgeId, IPolyline line, IFeature edgeFeature)
        {
            xWriter.WriteStartElement("FEATURE");
            xWriter.WriteAttributeString("type", "network_edge");
            xWriter.WriteAttributeString("id", edgeId.ToString());

            if (edgeFeature != null)
            {
                xWriter.WriteStartElement("FIELDS");
                foreach (var fieldValue in edgeFeature.Fields)
                {
                    xWriter.WriteStartElement("FIELD");
                    xWriter.WriteAttributeString("name", fieldValue.Name);
                    xWriter.WriteAttributeString("value", fieldValue.Value != null ? fieldValue.Value.ToString() : String.Empty);
                    xWriter.WriteEndElement(); //FIELD
                }
                xWriter.WriteEndElement(); // FIELDS
            }

            if (line != null)
            {
                xWriter.WriteRaw(AXLFromObjectFactory.Geometry(line));
            }

            xWriter.WriteEndElement();  // FEATURE
        }

        async private Task<IFeatureCursor> NetworkPathEdges(INetworkFeatureClass nfc, NetworkEdgeCollectionOutput edgeCollection)
        {
            if (nfc == null)
            {
                return null;
            }

            RowIDFilter filter = new RowIDFilter(String.Empty);
            filter.SubFields = "*";
            foreach (NetworkEdgeOutput edge in edgeCollection)
            {
                filter.IDs.Add(edge.EdgeId);
            }

            return await nfc.GetEdgeFeatures(filter);
        }

        async private Task<IFeatureCursor> NetworkNodes(INetworkFeatureClass nfc, int[] nodeIds)
        {
            if (nfc == null)
            {
                return null;
            }

            RowIDFilter filter = new RowIDFilter(String.Empty);
            filter.IDs.AddRange(nodeIds);

            return await nfc.GetNodeFeatures(filter);
        }

        private class NetworkElementTracer : INetworkTracer
        {
            #region INetworkTracer Members

            public string Name
            {
                get { return "Network Identify"; }
            }

            public bool CanTrace(NetworkTracerInputCollection input)
            {
                return input.Collect(NetworkTracerInputType.SourceNode).Count > 0 /* ||
                       input.Collect(NetworkTracerInputType.SoruceEdge).Count > 0*/;
            }

            async public Task<NetworkTracerOutputCollection> Trace(INetworkFeatureClass network, NetworkTracerInputCollection input, ICancelTracker cancelTraker)
            {
                NetworkTracerOutputCollection outputCollection = new NetworkTracerOutputCollection();
                GraphTable gt = new GraphTable(network.GraphTableAdapter());

                foreach (NetworkSourceInput nodeInput in input.Collect(NetworkTracerInputType.SourceNode))
                {
                    int fcId = gt.GetNodeFcid(nodeInput.NodeId);

                    IFeature nodeFeature = await network.GetNodeFeature(nodeInput.NodeId);
                    if (nodeFeature != null && nodeFeature.Shape is IPoint)
                    {
                        outputCollection.Add(new NetworkFlagOutput(nodeFeature.Shape as IPoint,
                            new NetworkFlagOutput.NodeFeatureData(nodeInput.NodeId, fcId, Convert.ToInt32(nodeFeature["OID"]), String.Empty)));
                    }
                }

                //foreach (NetworkSourceEdgeInput edgeInput in input.Collect(NetworkTracerInputType.SoruceEdge))
                //{
                //    int fcId = gt.GetNodeFcid(edgeInput.NodeId);

                //    IFeature nodeFeature = network.GetNodeFeature(edgeInput.NodeId);
                //    if (nodeFeature != null && nodeFeature.Shape is IPoint)
                //    {
                //        outputCollection.Add(new NetworkFlagOutput(nodeFeature.Shape as IPoint,
                //            new NetworkFlagOutput.NodeFeatureData(edgeInput.NodeId, fcId, Convert.ToInt32(nodeFeature["OID"]), String.Empty)));
                //    }
                //}

                return outputCollection;
            }

            #endregion
        }

        #endregion

        #endregion

        private string WriteException(string message)
        {
            AXL response = new AXL("ARCXML", "1.1");

            return response.ErrorMessage(message);
        }
        private List<ITableClass> FindTableClass(IServiceMap map, string id)
        {
            string dummy;
            return FindTableClass(map, id, out dummy);
        }
        private List<ITableClass> FindTableClass(IServiceMap map, string id, out string filterQuery)
        {
            filterQuery = String.Empty;
            if (map == null)
            {
                return null;
            }

            List<ITableClass> classes = new List<ITableClass>();
            foreach (ILayer element in MapServerHelper.FindMapLayers(map, _useTOC, id))
            {
                if (element.Class is ITableClass)
                {
                    classes.Add(element.Class as ITableClass);
                }

                if (element is IFeatureLayer)
                {
                    if (((IFeatureLayer)element).FilterQuery != null)
                    {
                        string fquery = ((IFeatureLayer)element).FilterQuery.WhereClause;
                        if (String.IsNullOrWhiteSpace(filterQuery))
                        {
                            filterQuery = fquery;
                        }
                        else if (filterQuery != fquery)
                        {
                            filterQuery = "(" + filterQuery + ") AND (" + fquery + ")";
                        }
                    }
                }
            }
            return classes;

            //foreach (IDatasetElement element in map.MapElements)
            //{
            //    if (!(element is ILayer)) continue;
            //    if (element.ID.ToString() == id)
            //    {
            //        if (element is IFeatureLayer)
            //        {
            //            if (((IFeatureLayer)element).FilterQuery != null)
            //                filterQuery = ((IFeatureLayer)element).FilterQuery.WhereClause;

            //            return ((IFeatureLayer)element).FeatureClass as ITableClass;
            //        }
            //    }
            //    if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null && ((IWebServiceLayer)element).WebServiceClass.Themes != null)
            //    {
            //        foreach (IWebServiceTheme theme in ((IWebServiceLayer)element).WebServiceClass.Themes)
            //        {
            //            if (theme.Class is IFeatureClass && theme.ID.ToString() == id)
            //            {
            //                return theme.FeatureClass as ITableClass;
            //            }
            //        }
            //    }
            //}
        }

        private List<IClass> FindClass(IServiceMap map, string id)
        {
            if (map == null)
            {
                return null;
            }

            List<IClass> classes = new List<IClass>();
            foreach (ILayer element in MapServerHelper.FindMapLayers(map, _useTOC, id))
            {
                if (element.Class is IClass)
                {
                    classes.Add(element.Class as IClass);
                }
            }
            return classes;
        }

        private string ArcXMLLayerID(IServiceMap map, string id)
        {
            if (map == null)
            {
                return "";
            }

            foreach (IDatasetElement element in map.MapElements)
            {
                if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null && ((IWebServiceLayer)element).WebServiceClass.Themes != null)
                {
                    foreach (IWebServiceTheme theme in ((IWebServiceLayer)element).WebServiceClass.Themes)
                    {
                        if (theme.Class is IFeatureClass && theme.ID.ToString() == id)
                        {
                            return theme.LayerID;
                        }
                    }
                }
            }
            return "";
        }
        private string CreateException(string msg)
        {
            return "<ERROR>" + msg + "</ERROR>";
        }

        private bool NodeAttributeBool(XmlNode node, string attr)
        {
            return NodeAttributeBool(node, attr, false);
        }
        private bool NodeAttributeBool(XmlNode node, string attr, bool defaultValue)
        {
            try
            {
                if (node.Attributes[attr] == null)
                {
                    return defaultValue;
                }

                return Convert.ToBoolean(node.Attributes[attr].Value);
            }
            catch
            {
                return false;
            }
        }
        private int NodeAttributeInt(XmlNode node, string attr)
        {
            try
            {
                if (node.Attributes[attr] == null)
                {
                    return 0;
                }

                return Convert.ToInt32(node.Attributes[attr].Value);
            }
            catch
            {
                return 0;
            }
        }
        private double NodeAttributeDouble(XmlNode node, string attr)
        {
            try
            {
                if (node.Attributes[attr] == null)
                {
                    return 0.0;
                }

                return Convert.ToDouble(node.Attributes[attr].Value.Replace(".", ","));
            }
            catch
            {
                return 0.0;
            }
        }
        private ArgbColor NodeAttributeColor(XmlNode node, string attr)
        {
            try
            {
                if (node.Attributes[attr] == null)
                {
                    return ArgbColor.Empty;
                }

                if (node.Attributes[attr].Value.ToLower() == "transparent")
                {
                    return ArgbColor.Transparent;
                }

                string[] rgb = node.Attributes[attr].Value.Split(',');
                return ArgbColor.FromArgb(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2]));
            }
            catch
            {
                return ArgbColor.Empty;
            }
        }

        private enum SpatialreferenceType { Filter = 0, Feature = 1, Coordsys = 2 }
        private ISpatialReference SpatialReferenceFromNode(XmlNode node, SpatialreferenceType type)
        {
            if (node == null)
            {
                return null;
            }

            XmlNode spatialreference = node.SelectSingleNode("SPATIALREFERENCE[@param]");
            if (spatialreference != null)
            {
                SpatialReference sRef = new SpatialReference();
                SpatialReference.FromProj4(sRef, spatialreference.Attributes["param"].Value);
                if (spatialreference.Attributes["name"] != null)
                {
                    sRef.Name = spatialreference.Attributes["name"].Value;
                }

                return sRef;
            }

            string nodeName = String.Empty;
            switch (type)
            {
                case SpatialreferenceType.Coordsys:
                    nodeName = "COORDSYS";
                    break;
                case SpatialreferenceType.Feature:
                    nodeName = "FEATURECOORDSYS";
                    break;
                case SpatialreferenceType.Filter:
                    nodeName = "FILTERCOORDSYS";
                    break;
            }
            XmlNode featureCoordSys = node.SelectSingleNode(nodeName);
            return ArcXMLGeometry.AXL2SpatialReference(featureCoordSys);
        }
    }

    /*
    [gView.Framework.system.RegisterPlugIn("DC58F436-9E1E-444b-8E09-EA2D1E0F5C30")]
    public class ArcXMLRequest2 : ArcXMLRequest
    {
        public ArcXMLRequest2()
            : base()
        {
            _useTOC = true;
        }

        public override string IntentityName
        {
            get
            {
                return "axl2";
            }
        }
    }
     * */
}
