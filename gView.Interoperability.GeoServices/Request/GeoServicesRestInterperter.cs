using gView.Core.Framework.Exceptions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Editor.Core;
using gView.Framework.FDB;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.OGC.GeoJson;
using gView.Framework.system;
using gView.Framework.system.Diagnostics;
using gView.Framework.UI;
using gView.Interoperability.GeoServices.Exceptions;
using gView.Interoperability.GeoServices.Extensions;
using gView.Interoperability.GeoServices.Request.Extensions;
using gView.Interoperability.GeoServices.Rest.Json;
using gView.Interoperability.GeoServices.Rest.Json.Features;
using gView.Interoperability.GeoServices.Rest.Json.FeatureServer;
using gView.Interoperability.GeoServices.Rest.Json.Renderers.SimpleRenderers;
using gView.Interoperability.GeoServices.Rest.Json.Request;
using gView.Interoperability.GeoServices.Rest.Json.Response;
using gView.MapServer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.Interoperability.GeoServices.Request
{
    [gView.Framework.system.RegisterPlugIn("6702B376-8848-435B-8611-6516A9726D3F")]
    public class GeoServicesRestInterperter : IServiceRequestInterpreter
    {
        private IMapServer _mapServer;
        private JsonExportMap _exportMap = null;
        private bool _useTOC = false;

        #region IServiceRequestInterpreter

        public string IdentityName => "geoservices";
        public string IdentityLongName => "OGC/ESRI GeoServices (REST)";

        public int Priority => 100;

        public InterpreterCapabilities Capabilities =>
            new InterpreterCapabilities(new InterpreterCapabilities.Capability[]
            {
                new InterpreterCapabilities.SimpleCapability("Catalog (REST)",InterpreterCapabilities.Method.Post,"{server}/geoservices/rest/services/{folder}","1.0"),
                new InterpreterCapabilities.SimpleCapability("Service (REST)",InterpreterCapabilities.Method.Post,"{server}/geoservices/rest/services/{folder/service}/MapServer","1.0")
            });

        public void OnCreate(IMapServer mapServer)
        {
            _mapServer = mapServer;
        }

        async public Task Request(IServiceRequestContext context)
        {
            switch (context.ServiceRequest.Method.ToLower())
            {
                case "export":
                    await ExportMapRequest(context);
                    break;
                case "query":
                    await Query(context);
                    break;
                case "identify":
                    await Identify(context);
                    break;
                case "legend":
                    await Legend(context);
                    break;
                case "featureserver_query":
                    await Query(context, true);
                    break;
                case "featureserver_addfeatures":
                    await AddFeatures(context);
                    break;
                case "featureserver_updatefeatures":
                    await UpdateFeatures(context);
                    break;
                case "featureserver_deletefeatures":
                    await DeleteFeatures(context);
                    break;
                default:
                    throw new NotImplementedException(context.ServiceRequest.Method + " is not support for geoservices rest");
            }
        }

        public AccessTypes RequiredAccessTypes(IServiceRequestContext context)
        {
            var accessTypes = AccessTypes.None;

            switch (context.ServiceRequest.Method.ToLower())
            {
                case "export":
                    accessTypes |= AccessTypes.Map;
                    break;
                case "query":
                case "identify":
                    accessTypes |= AccessTypes.Query;
                    break;
                case "legend":
                    accessTypes |= AccessTypes.Map;
                    break;
                case "featureserver_query":
                    accessTypes |= AccessTypes.Query;
                    break;
                case "featureserver_addfeatures":
                case "featureserver_updatefeatures":
                case "featureserver_deletefeatures":
                    accessTypes |= AccessTypes.Edit;
                    break;
            }

            return accessTypes;
        }

        #region Export

        async private Task ExportMapRequest(IServiceRequestContext context)
        {
            try
            {
                _exportMap = JsonConvert.DeserializeObject<JsonExportMap>(context.ServiceRequest.Request);

                //Console.WriteLine(_exportMap.BBox);
                //Console.WriteLine(_exportMap.Size);

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    #region SpatialReference

                    if (!String.IsNullOrWhiteSpace(_exportMap.BBoxSRef))
                    {
                        serviceMap.Display.SpatialReference = SRef(_exportMap.BBoxSRef);
                    }

                    #endregion

                    #region Display

                    serviceMap.Display.Dpi = _exportMap.Dpi;
                    //serviceMap.ScaleSymbolFactor = (float)_exportMap.Dpi / 96f;

                    var size = _exportMap.Size.ToSize();
                    serviceMap.Display.ImageWidth = size[0];
                    serviceMap.Display.ImageHeight = size[1];

                    if (_exportMap.Rotation != 0.0)
                    {
                        serviceMap.Display.DisplayTransformation.DisplayRotation = _exportMap.Rotation;
                    }

                    var bbox = _exportMap.BBox.ToBBox();

                    serviceMap.Display.ZoomTo(new Envelope(bbox[0], bbox[1], bbox[2], bbox[3]));

                    #endregion

                    #region ImageFormat / Transparency

                    if (!Enum.TryParse<ImageFormat>(_exportMap.ImageFormat, true, out ImageFormat imageFormat))
                    {
                        throw new MapServerException("Unsuported image format: " + _exportMap.ImageFormat);
                    }

                    var iFormat = imageFormat.ToGraphicsEngineImageFormat();

                    if (_exportMap.Transparent)
                    {
                        serviceMap.Display.MakeTransparent = true;
                        serviceMap.Display.TransparentColor = GraphicsEngine.ArgbColor.White;
                    }
                    else
                    {
                        serviceMap.Display.MakeTransparent = false;
                    }

                    if (serviceMap.Display.MakeTransparent && iFormat == GraphicsEngine.ImageFormat.Png)
                    {
                        // Beim Png sollt dann beim zeichnen keine Hintergrund Rectangle gemacht werden
                        // Darum Farbe mit A=0
                        // Sonst schaut das Bild beim PNG32 und Antialiasing immer zerrupft aus...
                        serviceMap.Display.BackgroundColor = GraphicsEngine.ArgbColor.Transparent;
                    }

                    #endregion

                    serviceMap.BeforeRenderLayers += ServiceMap_BeforeRenderLayers;
                    await serviceMap.Render();

                    if (serviceMap.MapImage != null)
                    {
                        if (_exportMap.OutputFormat?.ToLower() == "image")
                        {
                            context.ServiceRequest.Succeeded = true;

                            MemoryStream ms = new MemoryStream();
                            serviceMap.MapImage.Save(ms, iFormat);

                            context.ServiceRequest.ResponseContentType = _exportMap.GetContentType();
                            context.ServiceRequest.Response = ms.ToArray();

                            // debug
                            //string fileName = serviceMap.Name
                            //    .Replace("/", "_")
                            //    .Replace(",", "_") + "_" + System.Guid.NewGuid().ToString("N") + "." + iFormat.ToString().ToLower();

                            //string path = (_mapServer.OutputPath + @"/" + fileName).ToPlatformPath();
                            //await serviceMap.SaveImage(path, iFormat);
                        }
                        else
                        {
                            string serviceMapName = serviceMap.Name.Replace("/", "_").Replace(",", "_");
                            string fileName =
                               $"{serviceMapName}_{System.Guid.NewGuid().ToString("N")}.{iFormat.ToString().ToLower()}";

                            string path = ($"{_mapServer.OutputPath}/{fileName}").ToPlatformPath();
                            await serviceMap.SaveImage(path, iFormat);

                            context.ServiceRequest.Succeeded = true;
                            context.ServiceRequest.Response = new JsonExportResponse()
                            {
                                Href = $"{context.ServiceRequest.OutputUrl}/{fileName}",
                                Width = serviceMap.Display.ImageWidth,
                                Height = serviceMap.Display.ImageHeight,
                                ContentType = $"image/{iFormat.ToString().ToLower()}",
                                Scale = serviceMap.Display.MapScale,
                                Extent = new JsonExtent()
                                {
                                    Xmin = serviceMap.Display.Envelope.minx,
                                    Ymin = serviceMap.Display.Envelope.miny,
                                    Xmax = serviceMap.Display.Envelope.maxx,
                                    Ymax = serviceMap.Display.Envelope.maxy
                                    // ToDo: SpatialReference
                                },
                                IdleMilliseconds = ContextVariables.UseMetrics ? serviceMap.Metrics : null,
                                Error = serviceMap is IMap && ((IMap)serviceMap).HasRequestExceptions ?
                                    new JsonError.ErrorDef()
                                    {
                                        Code = 999,
                                        Message = "MapExport exeption occured",
                                        Details = ((IMap)serviceMap).RequestExceptions.Select(ex => ex.Message)
                                    } : null
                            };
                        }
                    }
                    else
                    {
                        context.ServiceRequest.Succeeded = false;
                        context.ServiceRequest.Response = new JsonError()
                        {
                            Error = new JsonError.ErrorDef()
                            {
                                Code = -1,
                                Message = "No image data"
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = new JsonError()
                {
                    Error = new JsonError.ErrorDef()
                    {
                        Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : -1,
                        Message = ex.Message + (ex is NullReferenceException ? $" Stacktrace: {ex.StackTrace}" : "")
                    }
                };
            }
        }

        private void ServiceMap_BeforeRenderLayers(Framework.Carto.IServiceMap sender, List<Framework.Data.ILayer> layers)
        {
            var mapLayersString = _exportMap?.Layers.Trim();

            if (!String.IsNullOrWhiteSpace(mapLayersString) &&
                mapLayersString.Contains(":") &&
                mapLayersString.IndexOf(":") < mapLayersString.Length - 1)  // show: ...
            {
                #region Apply Visibility

                string option = _exportMap.Layers.Substring(0, mapLayersString.IndexOf(":")).ToLower();
                int[] layerIds = _exportMap.Layers.Substring(mapLayersString.IndexOf(":") + 1)
                                        .Split(',').Select(l => int.Parse(l)).ToArray();


                foreach (var layer in layers)
                {
                    var tocElement = sender.TOC?.GetTocElementByLayerId(layer.ID);
                    bool layerIdContains = tocElement != null ?
                        LayerOrParentIsInArray(sender, tocElement, layerIds) :    // this is how AGS works: if group is shown -> all layers in group are shown...
                        layerIds.Contains(layer.ID);

                    switch (option)
                    {
                        case "show":
                            layer.Visible = layerIdContains;
                            break;
                        case "hide":
                            layer.Visible = !layerIdContains;
                            break;
                        case "include":
                            if (layerIdContains)
                            {
                                layer.Visible = true;
                            }
                            break;
                        case "exclude":
                            if (layerIdContains)
                            {
                                layer.Visible = false;
                            }

                            break;
                    }
                }

                #endregion
            }

            if (!String.IsNullOrEmpty(_exportMap?.LayerDefs))
            {
                #region Apply Layer Definitions

                Dictionary<string, string> layerDefs = null;

                try
                {
                    layerDefs = JsonConvert.DeserializeObject<Dictionary<string, string>>(_exportMap.LayerDefs);

                    foreach (var layerId in layerDefs.Keys)
                    {
                        var lID = int.Parse(layerId);
                        var layer = layers.Where(l => l.ID == lID).FirstOrDefault();
                        if (layer is IFeatureLayer)
                        {
                            ((IFeatureLayer)layer).FilterQuery = ((IFeatureLayer)layer).FilterQuery.AppendWhereClause(layerDefs[layerId]);

                            //File.WriteAllText("c:\\temp\\filter.txt", ((IFeatureLayer)layer).FilterQuery.WhereClause);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Can't parse layer definitions: {ex.Message}");
                }

                #endregion
            }

            if (!String.IsNullOrWhiteSpace(_exportMap.DynamicLayers))
            {
                #region Apply Dynamic Layers

                var jsonDynamicLayers = JsonConvert.DeserializeObject<JsonDynamicLayer[]>(_exportMap.DynamicLayers);
                foreach (var jsonDynamicLayer in jsonDynamicLayers)
                {
                    if (jsonDynamicLayer.Source != null)
                    {
                        var featureLayers = MapServerHelper.FindMapLayers(sender, _useTOC, jsonDynamicLayer.Source.MapLayerId.ToString());

                        foreach (var featureLayer in featureLayers)
                        {
                            if (!(featureLayer.Class is IFeatureClass))
                            {
                                continue;
                            }

                            IFeatureClass fc = (IFeatureClass)featureLayer.Class;
                            var dynLayer = LayerFactory.Create(featureLayer.Class, featureLayer) as IFeatureLayer;
                            if (dynLayer != null)
                            {
                                if (jsonDynamicLayer.DrawingInfo?.Renderer != null)
                                {
                                    dynLayer.FeatureRenderer = JsonRenderer.FromJsonRenderer(jsonDynamicLayer.DrawingInfo.Renderer);
                                }
                                else
                                {
                                    dynLayer.FeatureRenderer = null;
                                }

                                if (jsonDynamicLayer.DrawingInfo?.LabelingInfo != null && jsonDynamicLayer.DrawingInfo.LabelingInfo.Length == 1)
                                {
                                    var labelRenderer = jsonDynamicLayer.DrawingInfo.LabelingInfo[0].ToLabelRenderer();
                                    dynLayer.LabelRenderer = labelRenderer;
                                }
                                else
                                {
                                    dynLayer.LabelRenderer = null;
                                }
                                dynLayer.FilterQuery = new QueryFilter()
                                {
                                    SubFields = "*",
                                    WhereClause = jsonDynamicLayer.DefinitionExpression
                                };
                                dynLayer.Visible = true;
                                layers.Add(dynLayer);
                            }
                        }
                    }
                }

                #endregion
            }
        }

        private bool LayerAndParentIsInArray(IServiceMap map, ITocElement tocElement, int[] layerIds)
        {
            if (tocElement == null)
            {
                return false;
            }

            while (tocElement != null)
            {
                if (tocElement.Layers != null)
                {
                    foreach (var layer in tocElement.Layers)
                    {
                        if (!layerIds.Contains(layer.ID))
                        {
                            return false;
                        }
                    }
                }
                tocElement = tocElement.ParentGroup;
            }

            return true;
        }

        private bool LayerOrParentIsInArray(IServiceMap map, ITocElement tocElement, int[] layerIds)
        {
            while (tocElement != null)
            {
                if (tocElement.Layers != null)
                {
                    foreach (var layer in tocElement.Layers)
                    {
                        if (layerIds.Contains(layer.ID))
                        {
                            return true;
                        }
                    }
                }
                tocElement = tocElement.ParentGroup;
            }

            return false;
        }

        #endregion

        #region Query

        async private Task Query(IServiceRequestContext context, bool isFeatureServer = false)
        {
            try
            {
                var query = JsonConvert.DeserializeObject<JsonQueryLayer>(context.ServiceRequest.Request);

                int featureCount = 0;
                List<JsonFeature> jsonFeatures = new List<JsonFeature>();
                List<JsonFeatureResponse.Field> jsonFields = new List<JsonFeatureResponse.Field>();
                double idleMilliseconds = 0D;
                string objectIdFieldName = String.Empty;
                EsriGeometryType esriGeometryType = EsriGeometryType.esriGeometryAny;
                JsonSpatialReference featureSref = null;

                int limit = query.ReturnIdsOnly == true ?   // return all Ids!
                                    int.MaxValue :
                                    _mapServer.FeatureQueryLimit;

                #region GeoJson

                bool returnGeoJson = "geojson".Equals(query.OutputFormat, StringComparison.InvariantCultureIgnoreCase) &&
                         query.ReturnCountOnly == false &&
                         query.ReturnIdsOnly == false;
                List<IFeature> returnGeoJsonFeatures = new List<IFeature>();

                if (returnGeoJson && String.IsNullOrEmpty(query.OutSRef))
                {
                    query.OutSRef = "4326";
                }

                Envelope extent = null;

                #endregion

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    string filterQuery;

                    var tableClasses = FindTableClass(serviceMap, query.LayerId.ToString(), out filterQuery);
                    if (isFeatureServer == true && tableClasses.Count > 1)
                    {
                        throw new Exception("FeatureService can't be used with aggregated feature classes");
                    }

                    if (!String.IsNullOrWhiteSpace(query.Where))
                    {
                        query.Where.CheckWhereClauseForSqlInjection();
                    }

                    foreach (var tableClass in tableClasses)
                    {
                        objectIdFieldName = tableClass.IDFieldName;
                        if (tableClass is IFeatureClass)
                        {
                            var geometryType = ((IFeatureClass)tableClass).GeometryType;
                            if (geometryType == GeometryType.Unknown)
                            {
                                var featureLayer = serviceMap.MapElements.Where(l => l.ID == query.LayerId).FirstOrDefault() as IFeatureLayer;
                                if (featureLayer != null)
                                {
                                    geometryType = featureLayer.LayerGeometryType;
                                }
                            }

                            esriGeometryType = JsonLayer.ToGeometryType(geometryType);
                        }

                        IQueryFilter filter;

                        if (!String.IsNullOrWhiteSpace(query.Geometry))
                        {
                            filter = new SpatialFilter();
                            var jsonGeometry = query.Geometry.ToJsonGeometry(); // JsonConvert.DeserializeObject<Rest.Json.Features.Geometry.JsonGeometry>(query.Geometry);
                            var filterGeometry = jsonGeometry.ToGeometry();

                            ((SpatialFilter)filter).Geometry = filterGeometry;
                            ((SpatialFilter)filter).FilterSpatialReference =
                                SRef(query.InSRef) ??
                                (filterGeometry.Srs > 0 ? SpatialReference.FromID($"epsg:{filterGeometry.Srs}") : null);
                        }
                        else if (query.ReturnDistinctValues)
                        {
                            filter = new DistinctFilter(query.OutFields);
                        }
                        else if (!String.IsNullOrWhiteSpace(query.ObjectIds))
                        {
                            filter = new RowIDFilter(tableClass.IDFieldName,
                                query.ObjectIds.Split(',').Select(id => int.Parse(id)).ToList());
                        }
                        else
                        {
                            filter = new QueryFilter();
                        }

                        #region Prepare Where Clause

                        filter.WhereClause = query.Where;

                        if (!String.IsNullOrWhiteSpace(filterQuery))
                        {
                            filter.WhereClause = (!String.IsNullOrWhiteSpace(filter.WhereClause)) ?
                                "(" + filter.WhereClause + ") AND (" + filterQuery + ")" :
                                filterQuery;
                        }

                        #endregion

                        if (query.ReturnCountOnly == true)
                        {
                            if (tableClass is ITableClass2 && !String.IsNullOrWhiteSpace(tableClass.IDFieldName))
                            {
                                #region Fast Count

                                featureCount += await ((ITableClass2)tableClass).ExecuteCount(filter);
                                continue;

                                #endregion
                            }
                            else
                            {
                                filter.SubFields = !String.IsNullOrWhiteSpace(tableClass.IDFieldName) ? tableClass.IDFieldName : "*";
                            }
                        }
                        else if (query.ReturnIdsOnly)
                        {
                            if (String.IsNullOrEmpty(tableClass.IDFieldName))
                            {
                                throw new Exception("Can't query IdsOnly. Table has no ID-Field.");
                            }

                            filter.SubFields = tableClass.IDFieldName;
                        }
                        else if (query.ReturnExtentOnly)
                        {
                            if (tableClass is IFeatureClass && !filter.HasField(((IFeatureClass)tableClass).ShapeFieldName))
                            {
                                filter.SubFields = ((IFeatureClass)tableClass).ShapeFieldName;
                            }
                            else
                            {
                                throw new Exception("ReturnExtentOnly can only applied on tables with shape/geometry field");
                            }
                        }
                        else if (query.ReturnDistinctValues)
                        {
                            if (query.ReturnGeometry)
                            {
                                throw new Exception("Geometry is not supported with DISTINCT.");
                            }
                        }
                        else
                        {
                            var outFields = query.OutFields.FieldsNames().CheckAllowedFunctions(tableClass, isFeatureServer);

                            filter.SubFields = String.Join(",", outFields);
                            if (query.ReturnGeometry)
                            {
                                if (tableClass is IFeatureClass && !filter.HasField(((IFeatureClass)tableClass).ShapeFieldName))
                                {
                                    filter.AddField(((IFeatureClass)tableClass).ShapeFieldName);
                                }
                            }
                        }
                        if (!String.IsNullOrWhiteSpace(query.OutSRef))
                        {
                            filter.FeatureSpatialReference = SRef(query.OutSRef);
                        }
                        else if (tableClass is IFeatureClass)
                        {
                            filter.FeatureSpatialReference = ((IFeatureClass)tableClass).SpatialReference;
                        }
                        if (filter.FeatureSpatialReference != null)
                        {
                            try
                            {
                                featureSref = new JsonSpatialReference()
                                {
                                    Wkid = int.Parse(filter.FeatureSpatialReference.Name.Split(':')[1])
                                };
                            }
                            catch { }
                        }

                        #region Limit/Begin/Order

                        filter.Limit = query.ResultRecordCount > 0 ?
                            Math.Min(query.ResultRecordCount, limit) :
                            limit;

                        filter.BeginRecord = query.ResultOffset + 1;  // Start is 1 by IQueryFilter definition

                        filter.OrderBy = query.OrderByFields;

                        #endregion

                        bool transform = false;
                        using (var geoTransfromer = GeometricTransformerFactory.Create())
                        {
                            if (tableClass is IFeatureClass && ((IFeatureClass)tableClass).SpatialReference == null && filter.FeatureSpatialReference != null && serviceMap.LayerDefaultSpatialReference != null)
                            {
                                geoTransfromer.SetSpatialReferences(serviceMap.LayerDefaultSpatialReference, filter.FeatureSpatialReference);
                                transform = true;

                                #region Transform Filter Geometry

                                if (filter is ISpatialFilter &&
                                    ((ISpatialFilter)filter).FilterSpatialReference != null &&
                                    !serviceMap.LayerDefaultSpatialReference.EpsgCode.Equals(((ISpatialFilter)filter).FilterSpatialReference?.EpsgCode))
                                {
                                    using (var filterTransformer = GeometricTransformerFactory.Create())
                                    {
                                        filterTransformer.SetSpatialReferences(((SpatialFilter)filter).FilterSpatialReference, serviceMap.LayerDefaultSpatialReference);
                                        ((SpatialFilter)filter).Geometry = filterTransformer.Transform2D(((SpatialFilter)filter).Geometry) as IGeometry;
                                    }
                                }

                                #endregion
                            }

                            using (var cursor = await tableClass.Search(filter))
                            {
                                if (cursor == null)
                                {
                                    throw new ExecuteQueryException();
                                }

                                bool firstFeature = true;
                                if (cursor is IFeatureCursor)
                                {
                                    IFeature feature;
                                    IFeatureCursor featureCursor = (IFeatureCursor)cursor;

                                    while ((feature = await featureCursor.NextFeature()) != null)
                                    {
                                        featureCount++;

                                        if (query.ReturnGeometry == false && query.ReturnExtentOnly == false)
                                        {
                                            feature.Shape = null;  // do not geometry
                                        }
                                        else if (transform)
                                        {
                                            feature.Shape = geoTransfromer.Transform2D(feature.Shape) as IGeometry;
                                        }

                                        if (returnGeoJson)
                                        {
                                            returnGeoJsonFeatures.Add(feature);
                                        }
                                        else if (query.ReturnExtentOnly)
                                        {
                                            #region Calculate Envelope

                                            if (feature.Shape != null)
                                            {
                                                if (extent == null)
                                                {
                                                    extent = new Envelope(feature.Shape.Envelope);
                                                }
                                                else
                                                {
                                                    extent.Union(feature.Shape.Envelope);
                                                }
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            var jsonFeature = new JsonFeature();
                                            var attributesDict = (IDictionary<string, object>)jsonFeature.Attributes;

                                            if (feature.Fields != null)
                                            {
                                                foreach (var field in feature.Fields)
                                                {
                                                    object val = field.Value;

                                                    if (val is DateTime)
                                                    {
                                                        val = Convert.ToInt64(((DateTime)val - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
                                                    }

                                                    attributesDict[field.Name] = val;

                                                    if (firstFeature)
                                                    {
                                                        var tableField = tableClass.FindField(field.Name);
                                                        if (tableField != null)
                                                        {
                                                            jsonFields.Add(new JsonFeatureResponse.Field()
                                                            {
                                                                Name = tableField.name,
                                                                Alias = tableField.aliasname,
                                                                Length = tableField.size > 0 ? (int?)tableField.size : null,
                                                                Type = JsonField.ToType(tableField.type).ToString()
                                                            });
                                                        }
                                                    }
                                                }
                                            }

                                            jsonFeature.Geometry = feature.Shape?.ToJsonGeometry();

                                            jsonFeatures.Add(jsonFeature);
                                            firstFeature = false;
                                        }
                                    }
                                }

                                if(cursor is IDiagnostics)
                                {
                                    idleMilliseconds = ((IDiagnostics)cursor).DiagnosticParameters?.IdleMilliseconds ?? 0D;
                                }
                            }
                        }
                    }
                }

                context.ServiceRequest.Succeeded = true;

                if (query.ReturnCountOnly == true)
                {
                    context.ServiceRequest.Response = new JsonFeatureCountResponse()
                    {
                        Count = featureCount  //jsonFeatures.Count()
                    };
                }
                else if (query.ReturnIdsOnly)
                {
                    context.ServiceRequest.Response = new JsonObjectIdResponse()
                    {
                        ObjectIdFieldName = objectIdFieldName,
                        ObjectIds = jsonFeatures.Select(f => Convert.ToInt32(((IDictionary<string, object>)f.Attributes)[objectIdFieldName]))
                    };
                }
                else if (query.ReturnExtentOnly)
                {
                    context.ServiceRequest.Response = new JsonExtentResponse()
                    {
                        Extend = new JsonExtent()
                        {
                            Xmin = extent != null ? extent.minx : double.NaN,
                            Ymin = extent != null ? extent.miny : double.NaN,
                            Xmax = extent != null ? extent.maxx : double.NaN,
                            Ymax = extent != null ? extent.maxy : double.NaN,
                            SpatialReference = featureSref
                        }
                    };
                }
                else if (returnGeoJson)
                {
                    context.ServiceRequest.Response = Encoding.UTF8.GetBytes(GeoJsonHelper.ToGeoJson(returnGeoJsonFeatures));
                    context.ServiceRequest.ResponseContentType = Constants.JsonContentType;
                }
                else if (isFeatureServer)
                {
                    context.ServiceRequest.Response = new JsonFeatureServiceQueryResponse()
                    {
                        ObjectIdFieldName = objectIdFieldName,
                        GeometryType = esriGeometryType.ToString(),
                        SpatialReference = featureSref,
                        Fields = jsonFields.ToArray(),
                        Features = jsonFeatures.ToArray(),
                        ExceededTransferLimit = jsonFeatures.Count() >= limit
                    };
                }
                else
                {
                    context.ServiceRequest.Response = new JsonFeatureResponse()
                    {
                        GeometryType = esriGeometryType.ToString(),
                        SpatialReference = featureSref,
                        Fields = jsonFields.ToArray(),
                        Features = jsonFeatures.ToArray(),
                        ExceededTransferLimit = jsonFeatures.Count() >= limit,
                        IdleMilliseconds = SystemVariables.UseDiagnostic ?
                                new Dictionary<string, double> { ["0"] = idleMilliseconds } :
                                null
                    };
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = new JsonError()
                {
                    Error = new JsonError.ErrorDef()
                    {
                        Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : -1,
                        Message = ex.Message
                    }
                };
            }
        }

        #endregion

        #region Identify

        async private Task Identify(IServiceRequestContext context)
        {
            try
            {
                // https://developers.arcgis.com/rest/services-reference/identify-map-service-.htm

                var identify = JsonConvert.DeserializeObject<JsonIdentify>(context.ServiceRequest.Request);
                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    #region Parameters

                    IGeometry geometry = null;
                    switch (identify.geometryType?.ToLower())
                    {
                        case "esrigeometrypoint":
                        case "esrigeometryenvelope":
                            geometry = identify.Geometry.ToJsonGeometry().ToGeometry();
                            break;
                        default:
                            geometry = JsonConvert.DeserializeObject<Rest.Json.Features.Geometry.JsonGeometry>(identify.Geometry)?.ToGeometry();
                            break;
                    }
                    if (geometry == null)
                    {
                        throw new Exception("Invalid identify geometry");
                    }

                    var bbox = identify.MapExtent?.ToBBox();
                    if (bbox == null || bbox.Length != 4)
                    {
                        throw new Exception("Invalid identify map extent. Use <xmin>, <ymin>, <xmax>, <ymax>");
                    }
                    var mapExtent = new Envelope(bbox[0], bbox[1], bbox[2], bbox[3]);

                    var imageDisplay = identify.ImageDisplay?.Split(',').Select(f => NumberConverter.ToFloat(f)).ToArray();
                    if (imageDisplay == null || imageDisplay.Length != 3)
                    {
                        throw new Exception("Invalid identify image display. Use <width>, <height>, <dpi>");
                    }

                    ISpatialReference sRef = SpatialReference.FromID("epsg:" + identify.SRef);

                    #endregion

                    #region Initialize Display

                    serviceMap.Display.SpatialReference = sRef;
                    serviceMap.Display.ImageWidth = (int)imageDisplay[0];
                    serviceMap.Display.ImageHeight = (int)imageDisplay[1];
                    serviceMap.Display.Dpi = imageDisplay[2];

                    serviceMap.Display.Limit = mapExtent;
                    serviceMap.Display.ZoomTo(mapExtent);

                    //serviceMap.Display.Image2World(geometry);  // the geometry is in world coordinate system!

                    if (geometry is IPoint && mapExtent.Width > 0 && mapExtent.Height > 0)
                    {
                        double tol = identify.PixelTolerance * serviceMap.Display.MapScale / (96 / 0.0254);  // [m]
                        if (sRef != null &&
                            sRef.SpatialParameters.IsGeographic)
                        {
                            tol = (180.0 * tol / Math.PI) / 6370000.0;
                        }

                        geometry = new Envelope(((IPoint)geometry).X - tol, ((IPoint)geometry).Y - tol, ((IPoint)geometry).X + tol, ((IPoint)geometry).Y + tol);
                    }

                    #endregion

                    #region Collect Layers

                    var layers = serviceMap.MapElements?
                                           .Where(l => l is ILayer)
                                           .Select(l => (ILayer)l)
                                           .ToArray() ?? new ILayer[0];

                    if (!String.IsNullOrWhiteSpace(identify?.Layers) && identify.Layers.Contains(":"))
                    {
                        #region Apply Visibility

                        string option = identify.Layers.Substring(0, identify.Layers.IndexOf(":")).ToLower();
                        int[] layerIds = identify.Layers.Substring(identify.Layers.IndexOf(":") + 1)
                                                 .Split(',').Select(l => int.Parse(l)).ToArray();


                        foreach (var layer in layers)
                        {
                            var tocElement = serviceMap.TOC.GetTocElementByLayerId(layer.ID);
                            bool layerIdContains = tocElement != null ?
                                LayerOrParentIsInArray(serviceMap, tocElement, layerIds) :    // this is how AGS works: if group is shown -> all layers in group are shown...
                                layerIds.Contains(layer.ID);

                            switch (option)
                            {
                                case "show":
                                    layer.Visible = layerIdContains;
                                    break;
                                case "hide":
                                    layer.Visible = !layerIdContains;
                                    break;
                                case "include":
                                    if (layerIdContains)
                                    {
                                        layer.Visible = true;
                                    }

                                    break;
                                case "exclude":
                                    if (layerIdContains)
                                    {
                                        layer.Visible = false;
                                    }

                                    break;
                            }
                        }

                        #endregion
                    }

                    if (!String.IsNullOrEmpty(identify?.LayerDefs))
                    {
                        #region Apply Layer Definitions

                        Dictionary<string, string> layerDefs = null;

                        try
                        {
                            layerDefs = JsonConvert.DeserializeObject<Dictionary<string, string>>(identify.LayerDefs);

                            foreach (var layerId in layerDefs.Keys)
                            {
                                var lID = int.Parse(layerId);
                                var layer = layers.Where(l => l.ID == lID).FirstOrDefault();
                                if (layer is IFeatureLayer)
                                {
                                    ((IFeatureLayer)layer).FilterQuery = ((IFeatureLayer)layer).FilterQuery.AppendWhereClause(layerDefs[layerId]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Can't parse layer definitions: {ex.Message}");
                        }

                        #endregion
                    }

                    #endregion

                    #region Sptial Reference

                    var spatialFilter = new SpatialFilter();

                    spatialFilter.FilterSpatialReference = spatialFilter.FeatureSpatialReference = sRef;
                    spatialFilter.Geometry = geometry;
                    spatialFilter.SubFields = "*";

                    #endregion

                    var results = new List<JsonIdentifyResponse.Result>();

                    #region Query Layers

                    foreach (var layer in layers.Where(l => l.Visible))
                    {
                        ICursor cursor = null;

                        if (layer.Class is IFeatureClass)
                        {
                            cursor = await ((IFeatureClass)layer.Class).GetFeatures(spatialFilter);
                        }
                        else if (layer.Class is IPointIdentify)
                        {
                            using (var pointIdentifyContext = ((IPointIdentify)layer.Class).CreatePointIdentifyContext())
                            {
                                cursor = await ((IPointIdentify)layer.Class).PointQuery(serviceMap.Display, geometry.Envelope.Center, sRef, null, pointIdentifyContext);
                            }
                        }

                        if (cursor is IFeatureCursor)
                        {
                            IFeature feature;

                            while ((feature = await ((IFeatureCursor)cursor).NextFeature()) != null)
                            {
                                var result = new JsonIdentifyResponse.Result() { LayerId = layer.ID, LayerName = layer.Title };

                                result.ResultAttributes = feature.AttributesDictionary();

                                if (identify.ReturnGeometry)
                                {
                                    // ToDo: Project?
                                    result.Geometry = feature.Shape?.ToJsonGeometry();
                                }

                                results.Add(result);
                            }
                        }
                        else if (cursor is IRowCursor)
                        {
                            IRow row;
                            while ((row = await ((IRowCursor)cursor).NextRow()) != null)
                            {
                                var result = new JsonIdentifyResponse.Result() { LayerId = layer.ID, LayerName = layer.Title };

                                result.ResultAttributes = row.AttributesDictionary()
                                                             .AppendRasterAttributes();
                                if (identify.ReturnGeometry)
                                {
                                    result.Geometry = geometry?.Envelope?.Center?.ToJsonGeometry();
                                }

                                results.Add(result);
                            }
                        }
                    }

                    #endregion

                    context.ServiceRequest.Response = new JsonIdentifyResponse()
                    {
                        Results = results.ToArray()
                    };
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = new JsonError()
                {
                    Error = new JsonError.ErrorDef()
                    {
                        Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : -1,
                        Message = ex.Message
                    }
                };
            }
        }

        #endregion

        #region Legend

        async private Task Legend(IServiceRequestContext context)
        {
            try
            {
                var legendLayers = new List<Rest.Json.Legend.Layer>();

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    foreach (var layer in serviceMap.MapElements)
                    {
                        if (!(layer is IFeatureLayer) || ((IFeatureLayer)layer).FeatureRenderer == null)
                        {
                            continue;
                        }

                        var featureLayer = (IFeatureLayer)layer;

                        var tocElement = serviceMap.TOC.GetTOCElement(featureLayer);
                        if (tocElement == null)
                        {
                            continue;
                        }

                        using (var tocLegendItems = await serviceMap.TOC.LegendSymbol(tocElement))
                        {
                            if (tocLegendItems.Items == null || tocLegendItems.Items.Count() == 0)
                            {
                                continue;
                            }

                            var legendLayer = new Rest.Json.Legend.Layer()
                            {
                                LayerId = featureLayer.ID,
                                LayerName = tocElement.Name,
                                LayerType = "Feature-Layer",
                                MinScale = Convert.ToInt32(featureLayer.MaximumScale > 1 ? featureLayer.MaximumScale : 0),
                                MaxScale = Convert.ToInt32(featureLayer.MinimumScale > 1 ? featureLayer.MinimumScale : 0)
                            };

                            var legends = new List<Rest.Json.Legend.Legend>();
                            foreach (var tocLegendItem in tocLegendItems.Items)
                            {
                                if (tocLegendItem.Image == null)
                                {
                                    continue;
                                }

                                MemoryStream ms = new MemoryStream();
                                tocLegendItem.Image.Save(ms, GraphicsEngine.ImageFormat.Png);

                                legends.Add(new Rest.Json.Legend.Legend()
                                {
                                    Label = tocLegendItem?.Label,
                                    Url = Guid.NewGuid().ToString("N").ToString(),
                                    ImageData = Convert.ToBase64String(ms.ToArray()),
                                    ContentType = "image/png",
                                    Width = tocLegendItem.Image.Width,
                                    Height = tocLegendItem.Image.Height
                                });
                            }
                            legendLayer.Legend = legends.ToArray();
                            legendLayers.Add(legendLayer);
                        }
                    }
                }

                context.ServiceRequest.Succeeded = true;
                context.ServiceRequest.Response = new Rest.Json.Legend.LegendResponse()
                {
                    Layers = legendLayers.ToArray()
                };
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = new JsonError()
                {
                    Error = new JsonError.ErrorDef()
                    {
                        Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : -1,
                        Message = ex.Message
                    }
                };
            }
        }

        #endregion

        #region FeatureService

        async private Task AddFeatures(IServiceRequestContext context)
        {
            try
            {
                var editRequest = JsonConvert.DeserializeObject<JsonFeatureServerUpdateRequest>(context.ServiceRequest.Request);

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    CheckEditableStatement(serviceMap, editRequest, EditStatements.INSERT);

                    var featureClass = GetFeatureClass(serviceMap, editRequest);
                    var dataset = featureClass.Dataset;
                    var database = dataset?.Database as IFeatureUpdater;
                    if (database == null)
                    {
                        throw new Exception("Featureclass is not editable");
                    }

                    List<IFeature> features = GetFeatures(featureClass, editRequest, true);
                    if (features.Count == 0)
                    {
                        throw new Exception("No features to add");
                    }

                    if (features.Where(f => f.OID > 0).Count() > 0)
                    {
                        throw new Exception("Can't insert features with existing ObjectId");
                    }

                    features.GeometryMakeValid(serviceMap, featureClass);

                    if (!await database.Insert(featureClass, features))
                    {
                        throw new Exception(database.LastErrorMessage);
                    }

                    context.ServiceRequest.Succeeded = true;
                    context.ServiceRequest.Response = JsonConvert.SerializeObject(
                        new JsonFeatureServerResponse()
                        {
                            AddResults = new JsonFeatureServerResponse.JsonResponse[]
                            {
                                new JsonFeatureServerResponse.JsonResponse()
                                {
                                    Success = true
                                }
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = JsonConvert.SerializeObject(new JsonFeatureServerResponse()
                {
                    AddResults = new JsonFeatureServerResponse.JsonResponse[]
                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : 999,
                                Description = ex.Message.Split('\n')[0]
                                //Description = $"{ex.Message}\n{ex.StackTrace}"
                            }
                        }
                    }
                });
            }
        }

        async private Task UpdateFeatures(IServiceRequestContext context)
        {
            try
            {
                var editRequest = JsonConvert.DeserializeObject<JsonFeatureServerUpdateRequest>(context.ServiceRequest.Request);

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    CheckEditableStatement(serviceMap, editRequest, EditStatements.UPDATE);

                    var featureClass = GetFeatureClass(serviceMap, editRequest);
                    var dataset = featureClass.Dataset;
                    var database = dataset?.Database as IFeatureUpdater;
                    if (database == null)
                    {
                        throw new Exception("Featureclass is not editable");
                    }

                    List<IFeature> features = GetFeatures(featureClass, editRequest, true);
                    if (features.Count == 0)
                    {
                        throw new Exception("No features to add");
                    }

                    if (features.Where(f => f.OID <= 0).Count() > 0)
                    {
                        throw new Exception("Can't update features without existing ObjectId");
                    }

                    features.GeometryMakeValid(serviceMap, featureClass);

                    if (!await database.Update(featureClass, features))
                    {
                        throw new Exception(database.LastErrorMessage);
                    }

                    context.ServiceRequest.Succeeded = true;
                    context.ServiceRequest.Response = JsonConvert.SerializeObject(
                        new JsonFeatureServerResponse()
                        {
                            UpdateResults = features.Select(f => f.OID).ToEditJsonResponse(true).ToArray()
                        });
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = JsonConvert.SerializeObject(new JsonFeatureServerResponse()
                {
                    UpdateResults = new JsonFeatureServerResponse.JsonResponse[]
                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : 999,
                                Description = ex.Message.Split('\n')[0]
                                //Description = $"{ex.Message}\n{ex.StackTrace}"
                            }
                        }
                    }
                });
            }
        }

        async private Task DeleteFeatures(IServiceRequestContext context)
        {
            try
            {
                var editRequest = JsonConvert.DeserializeObject<JsonFeatureServerDeleteRequest>(context.ServiceRequest.Request);

                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    CheckEditableStatement(serviceMap, editRequest, EditStatements.DELETE);

                    var featureClass = GetFeatureClass(serviceMap, editRequest);
                    var dataset = featureClass.Dataset;
                    var database = dataset?.Database as IFeatureUpdater;
                    if (database == null)
                    {
                        throw new Exception("Featureclass is not editable");
                    }

                    var objectIds = editRequest.ObjectIds.Split(',').Select(s => int.Parse(s));
                    foreach (int objectId in objectIds)
                    {
                        if (!await database.Delete(featureClass, objectId))
                        {
                            throw new Exception(database.LastErrorMessage);
                        }
                    }

                    context.ServiceRequest.Succeeded = true;
                    context.ServiceRequest.Response = JsonConvert.SerializeObject(
                        new JsonFeatureServerResponse()
                        {
                            DeleteResults = objectIds.ToEditJsonResponse(true).ToArray()
                        });
                }
            }
            catch (Exception ex)
            {
                context.ServiceRequest.Succeeded = false;
                context.ServiceRequest.Response = JsonConvert.SerializeObject(new JsonFeatureServerResponse()
                {
                    DeleteResults = new JsonFeatureServerResponse.JsonResponse[]
                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code = ex is GeoServicesException ? ((GeoServicesException)ex).ErrorCode : 999,
                                Description = ex.Message.Split('\n')[0]
                            }
                        }
                    }
                });
            }
        }

        #region Helper

        private IFeatureLayer GetFeatureLayer(IServiceMap serviceMap, JsonFeatureServerEditRequest editRequest)
        {
            return serviceMap.MapElements.Where(e => e.ID == editRequest.LayerId).FirstOrDefault() as IFeatureLayer;
        }

        private IFeatureClass GetFeatureClass(IServiceMap serviceMap, JsonFeatureServerEditRequest editRequest)
        {
            string filterQuery;

            var tableClasses = FindTableClass(serviceMap, editRequest.LayerId.ToString(), out filterQuery);
            if (tableClasses.Count > 1)
            {
                throw new Exception("FeatureService can't be used with aggregated feature classes");
            }
            if (tableClasses.Count == 0 || !(tableClasses[0] is IFeatureClass))
            {
                throw new Exception("FeatureService can only used with feature classes");
            }

            var featureClass = (IFeatureClass)tableClasses[0];

            return featureClass;
        }

        private List<IFeature> GetFeatures(IFeatureClass featureClass, JsonFeatureServerUpdateRequest editRequest, bool projetToFeatureClassSpatialReference)
        {
            int? fcSrs = featureClass?.SpatialReference?.EpsgCode;

            List<IFeature> features = new List<IFeature>();
            foreach (var jsonFeature in editRequest.Features)
            {
                var feature = ToFeature(featureClass, jsonFeature);

                if (projetToFeatureClassSpatialReference == true)
                {
                    if (feature.Shape != null && feature.Shape.Srs.HasValue && !feature.Shape.Srs.Equals(fcSrs))
                    {
                        using (var transformer = GeometricTransformerFactory.Create())
                        {
                            var fromSrs = SpatialReference.FromID("epsg:" + feature.Shape.Srs.ToString());
                            var toSrs = SpatialReference.FromID("epsg:" + fcSrs.ToString());

                            transformer.SetSpatialReferences(fromSrs, toSrs);

                            var transformedShape = transformer.Transform2D(feature.Shape) as IGeometry;
                            transformedShape.Srs = fcSrs;

                            feature.Shape = transformedShape;
                        }
                    }
                }

                features.Add(feature);
            }

            return features;
        }

        private void CheckEditableStatement(IServiceMap serviceMap, JsonFeatureServerEditRequest editRequest, EditStatements statement)
        {
            var editModule = serviceMap.GetModule<gView.Plugins.Modules.EditorModule>();
            if (editModule == null)
            {
                throw new Exception("No editor module available for service");
            }

            var editLayer = editModule.GetEditLayer(editRequest.LayerId);
            if (editLayer == null)
            {
                throw new Exception("No editable layer found with id=" + editRequest.LayerId);
            }

            if (!editLayer.Statements.HasFlag(statement))
            {
                throw new Exception("Editoperation " + statement + " not allowed for layer with id=" + editRequest.LayerId);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Helper

        private Feature ToFeature(IFeatureClass fc, JsonFeature jsonFeature)
        {
            var feature = new Feature();

            feature.Shape = jsonFeature.Geometry.ToGeometry();
            var attributes = (IDictionary<string, object>)jsonFeature.Attributes;
            if (attributes == null)
            {
                throw new ArgumentException("No features attributes!");
            }

            for (int f = 0, fieldCount = fc.Fields.Count; f < fieldCount; f++)
            {
                var field = fc.Fields[f];

                if (attributes.ContainsKey(field.name))
                {
                    switch (field.type)
                    {
                        case FieldType.ID:
                            feature.Fields.Add(new FieldValue(field.name, attributes[field.name]));
                            feature.OID = Convert.ToInt32(attributes[field.name]);
                            break;
                        case FieldType.Date:
                            object val = attributes[field.name];
                            if (val is string)
                            {
                                if (val.ToString().Contains(" "))
                                {
                                    val = DateTime.ParseExact(val.ToString(),
                                        new string[]{
                                            "dd.MM.yyyy HH:mm:ss",
                                            "dd.MM.yyyy HH:mm",
                                            "yyyy.MM.dd HH:mm:ss",
                                            "yyyy.MM.dd HH:mm",
                                            "yyyy-MM-dd HH:mm:ss",
                                            "yyyy-MM-dd HH:mm"
                                        },
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.None);
                                }
                                else
                                {
                                    val = DateTime.ParseExact(val.ToString(),
                                        new string[]{
                                            "dd.MM.yyyy",
                                            "yyyy.MM.dd",
                                            "yyyy-MM-dd"
                                            },
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.None);
                                }
                            }
                            else if (val is long || val is int)
                            {
                                long esriDate = Convert.ToInt64(val);
                                val = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(esriDate);
                            }
                            feature.Fields.Add(new FieldValue(field.name, val));
                            break;
                        default:
                            feature.Fields.Add(new FieldValue(field.name, attributes[field.name]));
                            break;
                    }
                }
            }

            return feature;
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
        }

        private ISpatialReference SRef(string sref)
        {
            if (String.IsNullOrWhiteSpace(sref))
            {
                return null;
            }

            sref = sref.Trim();
            if (sref.StartsWith("{") && sref.EndsWith("}"))
            {
                var spatialReference = JsonConvert.DeserializeObject<Rest.Json.JsonMapService.SpatialReference>(sref);

                return SpatialReference.FromID("epsg:" + spatialReference.Wkid) ??
                       SpatialReference.FromID("epsg:" + spatialReference.LatestWkid);
            }

            return SpatialReference.FromID("epsg:" + sref);
        }

        private GraphicsEngine.ArgbColor ToColor(int[] col)
        {
            if (col == null)
            {
                return GraphicsEngine.ArgbColor.Transparent;
            }

            if (col.Length == 3)
            {
                return GraphicsEngine.ArgbColor.FromArgb(col[0], col[1], col[2]);
            }

            if (col.Length == 4)
            {
                return GraphicsEngine.ArgbColor.FromArgb(col[3], col[0], col[1], col[2]);
            }

            throw new Exception("Invalid symbol color: [" + String.Join(",", col) + "]");
        }

        #endregion
    }
}
