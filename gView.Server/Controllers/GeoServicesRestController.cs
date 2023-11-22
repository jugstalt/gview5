using gView.Core.Framework.Exceptions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.system;
using gView.Interoperability.GeoServices.Request;
using gView.Interoperability.GeoServices.Rest.Json;
using gView.Interoperability.GeoServices.Rest.Json.Features;
using gView.Interoperability.GeoServices.Rest.Json.FeatureServer;
using gView.Interoperability.GeoServices.Rest.Json.Renderers.SimpleRenderers;
using gView.Interoperability.GeoServices.Rest.Json.Request;
using gView.Interoperability.GeoServices.Rest.Reflection;
using gView.Interoperability.OGC;
using gView.MapServer;
using gView.Server.AppCode;
using gView.Server.AppCode.Extensions;
using gView.Server.Extensions;
using gView.Server.Services.Hosting;
using gView.Server.Services.Logging;
using gView.Server.Services.MapServer;
using gView.Server.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static gView.Interoperability.GeoServices.Rest.Json.JsonServices;

namespace gView.Server.Controllers
{
    public class GeoServicesRestController : BaseController
    {
        private readonly MapServiceManager _mapServerService;
        private readonly UrlHelperService _urlHelperService;
        private readonly LoginManager _loginManagerService;
        private readonly EncryptionCertificateService _encryptionCertificate;
        private readonly PerformanceLoggerService _performanceLogger;

        public GeoServicesRestController(
            MapServiceManager mapServerService,
            UrlHelperService urlHelperService,
            LoginManager loginManagerService,
            EncryptionCertificateService encryptionCertificate,
            PerformanceLoggerService performanceLogger)
            : base(mapServerService, loginManagerService, encryptionCertificate)
        {
            _mapServerService = mapServerService;
            _urlHelperService = urlHelperService;
            _loginManagerService = loginManagerService;
            _encryptionCertificate = encryptionCertificate;
            _performanceLogger = performanceLogger;
        }

        public const double Version = 10.61;
        public const string FullVersion = "10.6.1";
        //private const string DefaultFolder = "default";

        public int JsonExportResponse { get; private set; }

        public IActionResult Index()
        {
            return RedirectToAction("Services");
        }

        public IActionResult RestInfo()
        {
            return Result(new RestInfoResponse()
            {
                CurrentVersion = Version,
                FullVersion = FullVersion,
                AuthInfoInstance = new RestInfoResponse.AuthInfo()
                {
                    IsTokenBasedSecurity = true,
                    ShortLivedTokenValidity = 60,
                    TokenServicesUrl = $"{_urlHelperService.AppRootUrl(this.Request)}/geoservices/tokens"
                }
            });
        }

        public Task<IActionResult> Services()
        {
            return Folder(String.Empty);
        }

        async public Task<IActionResult> Folder(string id)
        {
            return await SecureMethodHandler(async (identity) =>
            {
                _mapServerService.ReloadServices(id, true);

                if (!String.IsNullOrWhiteSpace(id))
                {
                    var folderService = _mapServerService.MapServices
                        .Where(f => f.Type == MapServiceType.Folder && String.IsNullOrWhiteSpace(f.Folder) && id.Equals(f.Name, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                    if (folderService == null || !await folderService.HasAnyAccess(identity))
                    {
                        throw new Exception("Unknown folder or forbidden");
                    }
                }

                List<string> folders = new List<string>();
                foreach (var f in _mapServerService.MapServices.Where(s => s.Type == MapServiceType.Folder && s.Folder == id))
                {
                    if (await f.HasAnyAccess(identity))
                    {
                        folders.Add(f.Name);
                    }
                }

                List<AgsService> services = new List<AgsService>();
                foreach (var s in _mapServerService.MapServices)
                {
                    if (s.Type != MapServiceType.Folder &&
                       s.Folder == id &&
                       (await s.GetSettingsAsync()).IsRunningOrIdle() &&
                        await s.HasAnyAccess(identity))
                    {
                        services.AddRange(await AgsServices(identity, s));
                    }
                }

                var jsonService = String.IsNullOrEmpty(id) ?
                    new JsonServicesRoot() : new JsonServices();

                jsonService.CurrentVersion = Version;
                jsonService.Folders = folders.ToArray();
                jsonService.Services = services.ToArray();

                return Result(jsonService);
            });
        }

        async public Task<IActionResult> Service(string id, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var mapService = _mapServerService.Instance.GetMapService(id, folder);
                if (mapService == null)
                {
                    throw new Exception($"Unknown service: {id}");
                }

                await mapService.CheckAccess(identity, _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter)));

                using (var map = await _mapServerService.Instance.GetServiceMapAsync(id, folder))
                {
                    if (map == null)
                    {
                        throw new MapServerException($"unable to create map: {id}. Check log file for details");
                    }

                    gView.Framework.Geometry.IEnvelope fullExtent = map.FullExtent();
                    var spatialReference = map.Display.SpatialReference;
                    int epsgCode = spatialReference != null ? spatialReference.EpsgCode : 0;

                    return Result(new JsonMapService()
                    {
                        CurrentVersion = 10.61,
                        MapName = String.IsNullOrWhiteSpace(map.Title) ?
                            (map.Name.Contains("/") ? map.Name.Substring(map.Name.LastIndexOf("/") + 1) : map.Name) :
                            map.Title,
                        CopyrightText = map.GetLayerCopyrightText(Map.MapCopyrightTextId),
                        ServiceDescription = map.GetLayerDescription(Map.MapDescriptionId),
                        Layers = map.MapElements
                            .Where(e =>
                            {
                                var tocElement = map.TOC.GetTOCElement(e as ILayer);

                                return tocElement == null ? false : tocElement.IsHidden() == false;
                            })
                            .Select(e =>
                            {
                                var tocElement = map.TOC.GetTOCElement(e as ILayer);

                                int parentLayerId =
                                    (e is IFeatureLayer && ((IFeatureLayer)e).GroupLayer != null) ?
                                    ((IFeatureLayer)e).GroupLayer.ID :
                                    -1;

                                return new JsonMapService.Layer()
                                {
                                    Id = e.ID,
                                    ParentLayerId = parentLayerId,
                                    Name = tocElement != null ? tocElement.Name : e.Title,
                                    DefaultVisibility = tocElement != null ? tocElement.LayerVisible : true,
                                    MaxScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MinimumScale > 1 ? tocElement.Layers[0].MinimumScale : 0, 0) : 0,
                                    MinScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MaximumScale > 1 ? tocElement.Layers[0].MaximumScale : 0, 0) : 0,
                                };
                            })
                            .ToArray(),
                        SpatialReferenceInstance = epsgCode > 0 ? new JsonMapService.SpatialReference(epsgCode) : null,
                        InitialExtend = map.Display.Envelope == null ? null : new JsonMapService.Extent()
                        {
                            XMin = fullExtent != null ? fullExtent.minx : 0D,
                            YMin = fullExtent != null ? fullExtent.miny : 0D,
                            XMax = fullExtent != null ? fullExtent.maxx : 0D,
                            YMax = fullExtent != null ? fullExtent.maxy : 0D,
                            SpatialReference = new JsonMapService.SpatialReference(epsgCode)
                        },
                        FullExtent = new JsonMapService.Extent()
                        {
                            XMin = fullExtent != null ? fullExtent.minx : 0D,
                            YMin = fullExtent != null ? fullExtent.miny : 0D,
                            XMax = fullExtent != null ? fullExtent.maxx : 0D,
                            YMax = fullExtent != null ? fullExtent.maxy : 0D,
                            SpatialReference = new JsonMapService.SpatialReference(epsgCode)
                        },
                    });
                }
            });
        }

        async public Task<IActionResult> ServiceLayers(string id, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var mapService = _mapServerService.Instance.GetMapService(id, folder);
                if (mapService == null)
                {
                    throw new Exception("Unknown service: " + id);
                }

                await mapService.CheckAccess(identity, _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter)));

                using (var map = await _mapServerService.Instance.GetServiceMapAsync(id, folder))
                {
                    if (map == null)
                    {
                        throw new MapServerException($"unable to create map: {id}. Check log file for details");
                    }

                    var jsonLayers = new JsonLayers();
                    jsonLayers.Layers = map.MapElements
                        .Where(e =>
                        {   // Just show layer in Toc (and not hidden)
                            var tocElement = map.TOC.GetTOCElement(e as ILayer);

                            return tocElement == null ? false : tocElement.IsHidden() == false;
                        })
                        .Select(e => JsonLayer(map, e.ID))
                        .ToArray();

                    return Result(jsonLayers);
                }
            });
        }

        async public Task<IActionResult> ServiceLayer(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var mapService = _mapServerService.Instance.GetMapService(id, folder);
                if (mapService == null)
                {
                    throw new Exception("Unknown service: " + id);
                }

                await mapService.CheckAccess(identity, _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter)));

                var map = await _mapServerService.Instance.GetServiceMapAsync(id, folder);
                if (map == null)
                {
                    throw new MapServerException($"unable to create map: {id}. Check log file for details");
                }

                var jsonLayers = new JsonLayers();
                return Result(JsonLayer(map, layerId));
            });
        }

        #region MapServer

        public Task<IActionResult> ExportMap(string id, string folder = "")
            => SecureMethodHandler(async (identity) =>
        {
            var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

            #region Request

            JsonExportMap exportMap = Deserialize<JsonExportMap>(
                Request.HasFormContentType ?
                Request.Form :
                (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);

            ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(exportMap))
            {
                OnlineResource = _mapServerService.Options.OnlineResource,
                OutputUrl = _mapServerService.Options.OutputUrl,
                Method = "export",
                Identity = identity
            };

            #endregion

            #region Queue & Wait

            IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                _mapServerService.Instance,
                interpreter,
                serviceRequest);

            string format = ResultFormat();
            if (String.IsNullOrWhiteSpace(format))
            {
                using (var serviceMap = await context.CreateServiceMapInstance())
                {
                    exportMap.InitForm(serviceMap);
                    return FormResult(exportMap);
                }
            }

            await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

            #endregion

            if (serviceRequest.Succeeded)
            {
                if (ResultFormat() == "image" && serviceRequest.Response is byte[])
                {
                    return Result((byte[])serviceRequest.Response, serviceRequest.ResponseContentType);
                }
                else
                {
                    return Result(serviceRequest.Response, folder, id, "ExportMap");
                    //return Result(JsonConvert.DeserializeObject<JsonExportResponse>(serviceRequest.ResponseAsString), folder, id, "ExportMap");
                }
            }
            else
            {
                return Result(serviceRequest.Response);
            }
        });

        async public Task<IActionResult> Query(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonQueryLayer queryLayer = Deserialize<JsonQueryLayer>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);
                queryLayer.LayerId = layerId;

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(queryLayer))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "query",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return FormResult(queryLayer);
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                if (serviceRequest.Succeeded)
                {
                    return Result(serviceRequest.Response, folder, id, "Query", contentType: serviceRequest.ResponseContentType);
                }
                else
                {
                    return Result(serviceRequest.Response);
                }
            });
        }

        async public Task<IActionResult> Legend(string id, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, String.Empty)
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "legend",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                return Result(serviceRequest.Response, folder, id, "Legend");
            });
        }

        async public Task<IActionResult> Identify(string id, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonIdentify identify = Deserialize<JsonIdentify>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(identify))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "identify",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    using (var serviceMap = await context.CreateServiceMapInstance())
                    {
                        identify.InitForm(serviceMap);
                        return FormResult(identify);
                    }
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                if (serviceRequest.Succeeded)
                {
                    return Result(serviceRequest.Response, folder, id, "Identify");
                }
                else
                {
                    return Result(serviceRequest.Response);
                }

            });
        }

        #endregion

        #region WmsServer 

        public Task<IActionResult> WmsServer(string id, string folder = "") 
            => SecureMethodHandler(async (identity) =>
        {
            try
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(WMSRequest));

                #region Request

                string requestString = Request.QueryString.ToString();
                if (Request.Method.ToLower() == "post" && Request.Body.CanRead)
                {
                    string body = await Request.GetBody();

                    if (!String.IsNullOrWhiteSpace(body))
                    {
                        requestString = body;
                    }
                }

                while (requestString.StartsWith("?"))
                {
                    requestString = requestString.Substring(1);
                }

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, requestString)
                {
                    OnlineResource = _mapServerService.Options.OnlineResource.AppendWmsServerPath(this.Request, id, folder),
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                       _mapServerService.Instance,
                       interpreter,
                       serviceRequest);


                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                var response = serviceRequest.Response;

                if (response is byte[])
                {
                    return WmsResult((byte[])response, serviceRequest.ResponseContentType);
                }

                return WmsResult(response?.ToString() ?? String.Empty, serviceRequest.ResponseContentType);
            }
            catch (Exception ex)
            {
                // ToDo: OgcXmlExcpetion
                return WmsResult(ex.Message, "text/plain");
            }
        });

        #endregion

        #region FeatureServer

        async public Task<IActionResult> FeatureServerService(string id, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var mapService = _mapServerService.Instance.GetMapService(id, folder);
                if (mapService == null)
                {
                    throw new Exception("Unknown service: " + id);
                }

                await mapService.CheckAccess(identity, _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter)));

                var map = await _mapServerService.Instance.GetServiceMapAsync(id, folder);
                if (map == null)
                {
                    throw new MapServerException($"unable to create map: {id}. Check log file for details");
                }

                gView.Framework.Geometry.Envelope fullExtent = null;
                var spatialReference = map.Display.SpatialReference;
                int epsg = spatialReference != null ? spatialReference.EpsgCode : 0;

                return Result(new JsonFeatureService()
                {
                    CurrentVersion = 10.61,
                    Layers = map.MapElements
                    .Where(e =>
                    {
                        var tocElement = map.TOC.GetTOCElement(e as ILayer);

                        return tocElement == null ? false : tocElement.IsHidden() == false;
                    })
                    .Select(e =>
                    {
                        var tocElement = map.TOC.GetTOCElement(e as ILayer);

                        int parentLayerId =
                                    (e is IFeatureLayer && ((IFeatureLayer)e).GroupLayer != null) ?
                                    ((IFeatureLayer)e).GroupLayer.ID :
                                    -1;

                        IFeatureClass fc = (IFeatureClass)e.Class;

                        if (fc?.Envelope != null)
                        {
                            if (fullExtent == null)
                            {
                                fullExtent = new Framework.Geometry.Envelope(fc.Envelope);
                            }
                            else
                            {
                                fullExtent.Union(fc.Envelope);
                            }
                        }

                        if (epsg == 0 && fc?.SpatialReference != null && fc.SpatialReference.Name.ToLower().StartsWith("epsg:"))
                        {
                            int.TryParse(fc.SpatialReference.Name.Substring(5), out epsg);
                        }

                        var geometryType = e.Class is IFeatureClass ?
                            ((IFeatureClass)e.Class).GeometryType :
                            Framework.Geometry.GeometryType.Unknown;

                        if (geometryType == Framework.Geometry.GeometryType.Unknown && e is IFeatureLayer)   // if layer is SQL Spatial with undefined geometrytype...
                        {
                            geometryType = ((IFeatureLayer)e).LayerGeometryType;                             // take the settings from layer-properties
                        }

                        if (fc != null || tocElement.Layers?.FirstOrDefault() is IGroupLayer)
                        {
                            return new JsonMapService.Layer()
                            {
                                Id = e.ID,
                                ParentLayerId = parentLayerId,
                                Name = tocElement != null ? tocElement.Name : e.Title,
                                DefaultVisibility = tocElement != null ? tocElement.LayerVisible : true,
                                MaxScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MinimumScale > 1 ? tocElement.Layers[0].MinimumScale : 0, 0) : 0,
                                MinScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MaximumScale > 1 ? tocElement.Layers[0].MaximumScale : 0, 0) : 0,
                                GeometryType = e.Class is IFeatureClass ?
                                    Interoperability.GeoServices.Rest.Json.JsonLayer.ToGeometryType(geometryType).ToString() :
                                    null,
                                LayerType = fc != null ? "Feature Layer" : "Group Layer"
                            };
                        }

                        return null;
                    })
                    .Where(e => e != null)
                    .ToArray(),
                    SpatialReferenceInstance = epsg > 0 ? new JsonMapService.SpatialReference(epsg) : null,
                    InitialExtend = new JsonMapService.Extent()
                    {
                        XMin = fullExtent != null ? fullExtent.minx : 0D,
                        YMin = fullExtent != null ? fullExtent.miny : 0D,
                        XMax = fullExtent != null ? fullExtent.maxx : 0D,
                        YMax = fullExtent != null ? fullExtent.maxy : 0D,
                        SpatialReference = new JsonMapService.SpatialReference(epsg)
                    },
                    FullExtent = new JsonMapService.Extent()
                    {
                        XMin = fullExtent != null ? fullExtent.minx : 0D,
                        YMin = fullExtent != null ? fullExtent.miny : 0D,
                        XMax = fullExtent != null ? fullExtent.maxx : 0D,
                        YMax = fullExtent != null ? fullExtent.maxy : 0D,
                        SpatialReference = new JsonMapService.SpatialReference(epsg)
                    }
                });
            });
        }

        async public Task<IActionResult> FeatureServerQuery(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonQueryLayer queryLayer = Deserialize<JsonQueryLayer>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);
                queryLayer.LayerId = layerId;

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(queryLayer))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "featureserver_query",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return FormResult(queryLayer);
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                if (serviceRequest.Succeeded)
                {
                    return Result(serviceRequest.Response, folder, id, "Query", contentType: serviceRequest.ResponseContentType);
                }
                else
                {
                    return Result(serviceRequest.Response);
                }
            });
        }

        async public Task<IActionResult> FeatureServerAddFeatures(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonFeatureServerUpdateRequest editRequest = Deserialize<JsonFeatureServerUpdateRequest>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);
                editRequest.LayerId = layerId;

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(editRequest))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "featureserver_addfeatures",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return FormResult(editRequest);
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                return Result(JsonConvert.DeserializeObject<JsonFeatureServerResponse>(serviceRequest.ResponseAsString));
            },
            onException: (ex) =>
            {
                return Result(new JsonFeatureServerResponse()
                {
                    AddResults = new JsonFeatureServerResponse.JsonResponse[]
                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code=999,
                                Description=ex.Message
                            }
                        }
                    }
                });
            });
        }

        async public Task<IActionResult> FeatureServerUpdateFeatures(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonFeatureServerUpdateRequest editRequest = Deserialize<JsonFeatureServerUpdateRequest>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);
                editRequest.LayerId = layerId;

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(editRequest))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "featureserver_updatefeatures",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return FormResult(editRequest);
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                return Result(JsonConvert.DeserializeObject<JsonFeatureServerResponse>(serviceRequest.ResponseAsString));
            },
            onException: (ex) =>
            {
                return Result(new JsonFeatureServerResponse()
                {
                    UpdateResults = new JsonFeatureServerResponse.JsonResponse[]
                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code=999,
                                Description=ex.Message
                            }
                        }
                    }
                });
            });
        }

        async public Task<IActionResult> FeatureServerDeleteFeatures(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var interpreter = _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter));

                #region Request

                JsonFeatureServerDeleteRequest editRequest = Deserialize<JsonFeatureServerDeleteRequest>(
                    Request.HasFormContentType ?
                    Request.Form :
                    (IEnumerable<KeyValuePair<string, StringValues>>)Request.Query);
                editRequest.LayerId = layerId;

                ServiceRequest serviceRequest = new ServiceRequest(id, folder, JsonConvert.SerializeObject(editRequest))
                {
                    OnlineResource = _mapServerService.Options.OnlineResource,
                    OutputUrl = _mapServerService.Options.OutputUrl,
                    Method = "featureserver_deletefeatures",
                    Identity = identity
                };

                #endregion

                #region Queue & Wait

                IServiceRequestContext context = await ServiceRequestContext.TryCreate(
                    _mapServerService.Instance,
                    interpreter,
                    serviceRequest);

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return FormResult(editRequest);
                }

                await _mapServerService.TaskQueue.AwaitRequest(interpreter.Request, context);

                #endregion

                return Result(JsonConvert.DeserializeObject<JsonFeatureServerResponse>(serviceRequest.ResponseAsString));
            },
            onException: (ex) =>
            {
                return Result(new JsonFeatureServerResponse()
                {
                    DeleteResults = new JsonFeatureServerResponse.JsonResponse[]
                                    {
                        new JsonFeatureServerResponse.JsonResponse()
                        {
                            Success=false,
                            Error=new JsonFeatureServerResponse.JsonError()
                            {
                                Code=999,
                                Description=ex.Message
                            }
                        }
                    }
                });
            });
        }

        async public Task<IActionResult> FeatureServerLayer(string id, int layerId, string folder = "")
        {
            return await SecureMethodHandler(async (identity) =>
            {
                var mapService = _mapServerService.Instance.GetMapService(id, folder);
                if (mapService == null)
                {
                    throw new Exception("Unknown service: " + id);
                }

                await mapService.CheckAccess(identity, _mapServerService.GetInterpreter(typeof(GeoServicesRestInterperter)));

                var map = await _mapServerService.Instance.GetServiceMapAsync(id, folder);
                if (map == null)
                {
                    throw new MapServerException($"unable to create map: {id}. Check log file for details");
                }

                var jsonLayers = new JsonLayers();
                return Result(JsonFeatureServerLayer(map, layerId));
            });
        }

        #endregion

        #region Security

        public Task<IActionResult> GenerateToken(string request, string username, string password, int expiration = 60)
        {
            try
            {
                // https://developers.arcgis.com/rest/users-groups-and-items/generate-token.htm

                string format = ResultFormat();
                if (String.IsNullOrWhiteSpace(format))
                {
                    return Task.FromResult(FormResult(new JsonGenerateToken()));
                }

                if (request?.ToLower() == "gettoken")
                {
                    if (String.IsNullOrWhiteSpace(username))
                    {
                        throw new Exception("username required");
                    }

                    if (String.IsNullOrWhiteSpace(password))
                    {
                        throw new Exception("password required");
                    }

                    var authToken = _loginManagerService.GetAuthToken(username, password, expireMinutes: expiration);
                    if (authToken == null)
                    {
                        throw new Exception("unknown username or password");
                    }

                    return Task.FromResult(Result(new JsonSecurityToken()
                    {
                        token = _encryptionCertificate.ToToken(authToken),
                        expires = Convert.ToInt64((new DateTime(authToken.Expire, DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds)
                    }));
                }
                else
                {
                    throw new Exception("unkown request: " + request);
                }
            }
            catch (Exception ex)
            {
                return Task.FromResult(Result(new JsonError()
                {
                    Error = new JsonError.ErrorDef() { Code = 400, Message = ex.Message }
                }));
            }
        }

        #endregion

        #region Helper

        #region Json

        private JsonLayer JsonLayer(IServiceMap map, int layerId, JsonLayer result = null)
        {
            var datasetElement = map.MapElements.Where(e => e.ID == layerId).FirstOrDefault();
            if (datasetElement == null)
            {
                throw new Exception("Unknown layer: " + layerId);
            }

            var tocElement = map.TOC.GetTOCElement(datasetElement as ILayer);
            bool isJsonFeatureServiceLayer = result is JsonFeatureServerLayer;

            JsonLayerLink parentLayer = null;
            if (datasetElement is ILayer && ((ILayer)datasetElement).GroupLayer != null)
            {
                parentLayer = new JsonLayerLink()
                {
                    Id = ((ILayer)datasetElement).GroupLayer.ID,
                    Name = ((ILayer)datasetElement).GroupLayer.Title
                };
            }

            if (datasetElement is GroupLayer && datasetElement.Class == null)  // GroupLayer
            {
                var groupLayer = (GroupLayer)datasetElement;
                string type = "Group Layer";
                var childLayers = groupLayer.ChildLayer != null ?
                        groupLayer.ChildLayer.Where(l => map.TOC.GetTOCElement(l as ILayer) != null).Select(l =>
                        {
                            var childTocElement = map.TOC.GetTOCElement(l as ILayer);

                            return new JsonLayerLink()
                            {
                                Id = l.ID,
                                Name = childTocElement.Name
                            };
                        }).ToArray() :
                        new JsonLayerLink[0];

                if (groupLayer.MapServerStyle == MapServerGrouplayerStyle.Checkbox)
                {
                    type = "Feature Layer";
                    childLayers = null;
                }


                var jsonGroupLayer = new JsonLayer()
                {
                    CurrentVersion = Version,
                    Id = groupLayer.ID,
                    Name = groupLayer.Title,
                    DefaultVisibility = groupLayer.Visible,
                    MaxScale = Math.Max(groupLayer.MinimumScale > 1 ? groupLayer.MinimumScale : 0, 0),
                    MinScale = Math.Max(groupLayer.MaximumScale > 1 ? groupLayer.MaximumScale : 0, 0),
                    Type = type,
                    ParentLayer = parentLayer,
                    SubLayers = childLayers
                };

                if (jsonGroupLayer.SubLayers != null)
                {
                    JsonSpatialReference spatialReference = new JsonSpatialReference(map.Display.SpatialReference != null ? map.Display.SpatialReference.EpsgCode : 0);
                    JsonExtent extent = null;
                    foreach (var subLayer in jsonGroupLayer.SubLayers)
                    {
                        var featureClass = map.MapElements.Where(e => e.ID == subLayer.Id && e.Class is IFeatureClass).Select(l => (IFeatureClass)l.Class).FirstOrDefault();

                        if (featureClass != null)
                        {
                            int epsgCode = featureClass.SpatialReference != null ? featureClass.SpatialReference.EpsgCode : 0;
                            if (epsgCode == spatialReference.Wkid || epsgCode == 0)
                            {
                                var envelope = featureClass.Envelope;
                                if (envelope != null)
                                {
                                    if (extent == null)
                                    {
                                        extent = new JsonExtent()
                                        {
                                            Xmin = featureClass.Envelope.minx,
                                            Ymin = featureClass.Envelope.miny,
                                            Xmax = featureClass.Envelope.maxx,
                                            Ymax = featureClass.Envelope.maxy,
                                            SpatialReference = spatialReference
                                        };
                                    }
                                    else
                                    {
                                        extent.Xmin = Math.Min(extent.Xmin, featureClass.Envelope.minx);
                                        extent.Ymin = Math.Min(extent.Ymin, featureClass.Envelope.miny);
                                        extent.Xmax = Math.Min(extent.Xmax, featureClass.Envelope.maxx);
                                        extent.Ymax = Math.Min(extent.Ymax, featureClass.Envelope.maxy);
                                    }
                                }
                            }
                        }
                    }
                    jsonGroupLayer.Extent = extent;

                    jsonGroupLayer.Description = map.GetLayerDescription(layerId);
                    jsonGroupLayer.CopyrightText = map.GetLayerCopyrightText(layerId);
                }
                return jsonGroupLayer;
            }
            else // Featurelayer, Rasterlayer
            {
                JsonField[] fields = new JsonField[0];
                if (datasetElement.Class is ITableClass)
                {
                    fields = ((ITableClass)datasetElement.Class).Fields.ToEnumerable()
                        .Where(f=> isJsonFeatureServiceLayer && f.name.EndsWith("()") ? false : true)  // FeatureServer => don't show functions line STArea, STLength, ... only supported with MapServer
                        .Select(f =>
                        {
                            if (isJsonFeatureServiceLayer)
                            {
                                return new JsonFeatureLayerField()
                                {
                                    Name = f.name,
                                    Alias = f.aliasname,
                                    Type = JsonField.ToType(f.type).ToString(),
                                    Editable = f.type != FieldType.ID,
                                    Nullable = f.type != FieldType.ID,
                                    Length = f.size
                                };
                            }
                            else
                            {
                                return new JsonField()
                                {
                                    Name = f.name,
                                    Alias = f.aliasname,
                                    Type = JsonField.ToType(f.type).ToString()
                                };
                            }
                        })
                        .ToArray();
                }

                JsonExtent extent = null;
                var spatialReference = (datasetElement.Class is IFeatureClass ? ((IFeatureClass)datasetElement.Class).SpatialReference : null) ?? (map.LayerDefaultSpatialReference ?? map.Display.SpatialReference);
                int epsgCode = spatialReference != null ? spatialReference.EpsgCode : 0;

                if (datasetElement.Class is IFeatureClass && ((IFeatureClass)datasetElement.Class).Envelope != null)
                {
                    extent = new JsonExtent()
                    {
                        // DoTo: SpatialReference
                        Xmin = ((IFeatureClass)datasetElement.Class).Envelope.minx,
                        Ymin = ((IFeatureClass)datasetElement.Class).Envelope.miny,
                        Xmax = ((IFeatureClass)datasetElement.Class).Envelope.maxx,
                        Ymax = ((IFeatureClass)datasetElement.Class).Envelope.maxy,
                        SpatialReference = new JsonSpatialReference(epsgCode)
                    };
                }

                string type = "Feature Layer";
                if (datasetElement.Class is IRasterClass &&
                    !(datasetElement.Class is IRasterCatalogClass)) // RasterCatalogClass is like a Featureclass (Features a rendert as Image, but you can query/filter them as Polygons with attributes...)
                {
                    type = "Raster Layer";
                }

                JsonDrawingInfo drawingInfo = null;
                if (datasetElement is IFeatureLayer)
                {
                    var featureLayer = (IFeatureLayer)datasetElement;

                    drawingInfo = new JsonDrawingInfo()
                    {
                        Renderer = JsonRenderer.FromFeatureRenderer(featureLayer.FeatureRenderer)
                    };
                }

                result = result ?? new JsonLayer();

                var geometryType = datasetElement.Class is IFeatureClass ?
                    ((IFeatureClass)datasetElement.Class).GeometryType :
                    Framework.Geometry.GeometryType.Unknown;

                if (geometryType == Framework.Geometry.GeometryType.Unknown && datasetElement is IFeatureLayer)   // if layer is SQL Spatial with undefined geometrytype...
                {
                    geometryType = ((IFeatureLayer)datasetElement).LayerGeometryType;                             // take the settings from layer-properties
                }

                result.CurrentVersion = Version;
                result.Id = datasetElement.ID;
                result.Name = tocElement != null ? tocElement.Name : datasetElement.Title;
                result.DefaultVisibility = tocElement != null ? tocElement.LayerVisible : true;
                result.MaxScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MinimumScale > 1 ? tocElement.Layers[0].MinimumScale : 0, 0) : 0;
                result.MinScale = tocElement != null && tocElement.Layers.Count() > 0 ? Math.Max(tocElement.Layers[0].MaximumScale > 1 ? tocElement.Layers[0].MaximumScale : 0, 0) : 0;
                result.Fields = fields;
                result.Extent = extent;
                result.Type = type;
                result.ParentLayer = parentLayer;
                result.DrawingInfo = drawingInfo;
                result.GeometryType = datasetElement.Class is IFeatureClass ?
                    Interoperability.GeoServices.Rest.Json.JsonLayer.ToGeometryType(geometryType).ToString() :
                    EsriGeometryType.esriGeometryNull.ToString();

                result.Description = map.GetLayerDescription(layerId);
                result.CopyrightText = map.GetLayerCopyrightText(layerId);

                if (result is JsonFeatureServerLayer)
                {
                    var editorModule = map.GetModule<gView.Plugins.Modules.EditorModule>();
                    if (editorModule != null)
                    {
                        var editLayer = editorModule.GetEditLayer(result.Id);
                        if (editLayer != null)
                        {
                            List<string> editOperations = new List<string>();
                            foreach (Framework.Editor.Core.EditStatements statement in Enum.GetValues(typeof(Framework.Editor.Core.EditStatements)))
                            {
                                if (statement != Framework.Editor.Core.EditStatements.NONE && editLayer.Statements.HasFlag(statement))
                                {
                                    editOperations.Add(statement.ToString());
                                }
                            }

                            if (editOperations.Count > 0)
                            {
                                ((JsonFeatureServerLayer)result).IsEditable = true;
                                ((JsonFeatureServerLayer)result).EditOperations = editOperations.ToArray();
                            }
                        }
                    }
                }

                return result;
            }
        }

        private JsonLayer JsonFeatureServerLayer(IServiceMap map, int layerId)
        {
            return JsonLayer(map, layerId, new JsonFeatureServerLayer());
        }

        #endregion

        private IActionResult Result(object obj, string folder = null, string id = null, string method = null, string contentType = "text/plain")
        {
            if (base.ActionStartTime.HasValue && obj is JsonStopWatch)
            {
                ((JsonStopWatch)obj).DurationMilliseconds = (DateTime.UtcNow - base.ActionStartTime.Value).TotalMilliseconds;

                ((JsonStopWatch)obj).AddPerformanceLoggerItem(_performanceLogger,
                                                              folder, id, method);
            }

            string format = ResultFormat();

            if (format == "json")
            {
                return Json(obj);
            }
            else if (format == "pjson")
            {
                return Json(obj, new Newtonsoft.Json.JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                });
            }
            else if (IsRawResultFormat(format))
            {
                if (obj is byte[])
                {
                    return File((byte[])obj, contentType);
                }
                else if (obj is string)
                {
                    return File(Encoding.UTF8.GetBytes(obj.ToString()), contentType);
                }
                else
                {
                    return Json(obj);
                }
            }

            #region ToHtml

            AddNavigation();
            ViewData["htmlbody"] = ToHtml(obj);
            return View("_htmlbody");

            #endregion
        }

        private IActionResult Result(byte[] data, string contentType)
        {
            return File(data, contentType);
        }

        public IActionResult FormResult(object obj)
        {
            AddNavigation();
            ViewData["htmlBody"] = ToHtmlForm(obj);
            return View("_htmlbody");
        }

        private IActionResult WmsResult(string response, string contentType)
        {
            byte[] data = null;
            if (response.StartsWith("base64:"))
            {
                response = response.Substring("base64:".Length);
                data = Convert.FromBase64String(response);
            }
            else
            {
                data = Encoding.UTF8.GetBytes(response);
            }
            return WmsResult(data, contentType);
        }

        private IActionResult WmsResult(byte[] data, string contentType)
        {
            if (String.IsNullOrEmpty(contentType))
            {
                contentType = "text/plain";
            }

            return File(data, contentType);
        }

        private void AddNavigation()
        {
            var requestPath = this.Request.Path.ToString();
            while (requestPath.StartsWith("/"))
            {
                requestPath = requestPath.Substring(1);
            }

            string path = _urlHelperService.AppRootUrl(this.Request);
            string[] requestPathItems = requestPath.Split('/');
            string[] serverTypes = new string[] { "mapserver", "featureserver" };

            Dictionary<string, string> menuItems = new Dictionary<string, string>();
            bool usePath = false;
            for (int i = 0, to = requestPathItems.Length; i < to; i++)
            {
                var item = requestPathItems[i];
                path += "/" + item;

                if (i < to - 1 && serverTypes.Contains(requestPathItems[i + 1].ToLower()))
                {
                    continue;
                }
                else if (i > 0 && serverTypes.Contains(item.ToLower()))
                {
                    item = requestPathItems[i - 1] + " (" + item + ")";
                }

                if (usePath)
                {
                    menuItems.Add(item, path);
                }

                if (item.ToLower() == "rest")
                {
                    usePath = true;
                }
            }

            ViewData["mainMenuItems"] = "_mainMenuGeoServicesPartial";
            ViewData["menuItems"] = menuItems;
        }

        private string ResultFormat()
        {
            if (!String.IsNullOrWhiteSpace(Request.Query["f"]))
            {
                return Request.Query["f"].ToString().ToLower();
            }
            if (Request.HasFormContentType && !String.IsNullOrWhiteSpace(Request.Form["f"].ToString().ToLower()))
            {
                return Request.Form["f"];
            }

            return String.Empty;
        }

        private bool IsRawResultFormat(string resultFormat = null)
        {
            if (String.IsNullOrEmpty(resultFormat))
            {
                resultFormat = ResultFormat();
            }

            switch (resultFormat?.ToLower())
            {
                case "geojson":
                    return true;
            }

            return false;
        }

        async private Task<IEnumerable<string>> ServiceTypes(IIdentity identity, IMapService mapService)
        {
            var accessTypes = await mapService.GetAccessTypes(identity);

            List<string> serviceTypes = new List<string>(2);
            if (accessTypes.HasFlag(AccessTypes.Map))
            {
                serviceTypes.Add("MapServer");
            }

            if (accessTypes.HasFlag(AccessTypes.Edit) /* || accessTypes.HasFlag(AccessTypes.Query)*/)
            {
                serviceTypes.Add("FeatureServer");
            }

            return serviceTypes;
        }

        async private Task<IEnumerable<AgsService>> AgsServices(Identity identity, IMapService mapService)
        {
            return (await ServiceTypes(identity, mapService))
                        .Select(s => new AgsService()
                        {
                            Name = (String.IsNullOrWhiteSpace(mapService.Folder) ? "" : mapService.Folder + "/") + mapService.Name,
                            Type = s
                        });
        }

        #region Html

        private string ToHtml(object obj)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<div class='html-body'>");

            var typeString = obj.GetType().ToString();
            if (typeString.Contains("."))
            {
                typeString = typeString.Substring(typeString.LastIndexOf(".") + 1);
            }

            if (typeString.StartsWith("Json"))
            {
                typeString = typeString.Substring(4);
            }

            sb.Append("<h3>" + typeString + " (YAML):</h3>");

            foreach (var serviceMethodAttribute in obj.GetType().GetCustomAttributes<ServiceMethodAttribute>(false))
            {
                var url = serviceMethodAttribute.Method;
                var target = String.Empty;

                if (serviceMethodAttribute.Method.StartsWith("http://") || serviceMethodAttribute.Method.StartsWith("https://"))
                {
                    url = url.Replace("{onlineresource-url}", _mapServerService.Options.OnlineResource);
                    target = "_blank";
                }
                else
                {
                    url = $"{_mapServerService.Options.OnlineResource}{this.Request.Path}/{url}";
                }

                sb.Append($"<a href='{url}' target='{target}' >{serviceMethodAttribute.Name}</a>");
            }

            sb.Append("<div class='code-block'>");
            sb.Append(ToYamlHtml(obj));
            sb.Append("</div>");

            sb.Append("</div>");

            return sb.ToString();
        }

        private string ToYamlHtml(object obj, int spaces = 0, bool isArray = false)
        {
            if (obj == null)
            {
                return String.Empty;
            }

            var type = obj.GetType();

            StringBuilder sb = new StringBuilder();
            sb.Append("<div class='yaml-code'>");

            bool isFirst = true;

            foreach (var propertyInfo in SortYamlProperties(type.GetProperties()))
            {
                if (propertyInfo.GetValue(obj) == null)
                {
                    continue;
                }

                var jsonPropertyAttribute = propertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAttribute == null)
                {
                    continue;
                }

                var linkAttribute = propertyInfo.GetCustomAttribute<HtmlLinkAttribute>();

                bool newLine = !(propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string));
                if (newLine == true)
                {
                    if (propertyInfo.PropertyType.IsArray && (propertyInfo.GetValue(obj) == null || ((Array)propertyInfo.GetValue(obj)).Length == 0))
                    {
                        newLine = false;
                    }
                }

                string spacesValue = HtmlYamlSpaces(spaces);
                if (isArray && isFirst)
                {
                    spacesValue += "-&nbsp;";
                    spaces += 2;
                }
                sb.Append("<div class='property-name" + (newLine ? " array" : "") + "'>" + spacesValue + propertyInfo.Name + ":&nbsp;</div>");
                sb.Append("<div class='property-value'>");

                if (propertyInfo.PropertyType.IsArray)
                {
                    var array = (Array)propertyInfo.GetValue(obj);
                    if (array == null)
                    {
                        sb.Append("null");
                    }
                    else if (array.Length == 0)
                    {
                        sb.Append("[]");
                    }
                    else
                    {
                        List<object> arrayValues = new List<object>();
                        List<string> groupByValues = new List<string>();

                        for (int i = 0; i < array.Length; i++)
                        {
                            arrayValues.Add(array.GetValue(i));
                        }

                        var firstElement = arrayValues.Where(v => v != null).FirstOrDefault();
                        YamlGroupByAttribute groupByAttribute = null;
                        if (firstElement != null && !firstElement.GetType().IsValueType && arrayValues.Where(v => firstElement.GetType() == v?.GetType()).Count() == arrayValues.Count())
                        {
                            groupByAttribute = firstElement.GetType().GetCustomAttribute<YamlGroupByAttribute>();
                            if (!String.IsNullOrEmpty(groupByAttribute?.GroupByField))
                            {
                                groupByValues.AddRange(arrayValues.Select(v => v.GetType().GetProperty(groupByAttribute.GroupByField).GetValue(v).ToString())
                                                                  .Distinct());
                            }
                        }

                        foreach (var groupBy in groupByAttribute != null ? groupByValues.ToArray() : new string[] { String.Empty })
                        {
                            int arrayIndex = 0;
                            foreach (var val in arrayValues)
                            {
                                if (val == null)
                                {
                                    sb.Append("null");
                                }
                                else if (val.GetType().IsValueType || val.GetType() == typeof(string))
                                {
                                    if (arrayIndex == 0)
                                    {
                                        sb.Append("[");
                                    }
                                    else
                                    {
                                        sb.Append(", ");
                                    }
                                    sb.Append(HtmlYamlValue(linkAttribute, val, obj));
                                    if (arrayIndex == array.Length - 1)
                                    {
                                        sb.Append("]");
                                    }
                                }
                                else
                                {
                                    if (!String.IsNullOrEmpty(groupByAttribute?.GroupByField))
                                    {
                                        if (groupBy != val.GetType().GetProperty(groupByAttribute.GroupByField).GetValue(val)?.ToString())
                                        {
                                            continue;
                                        }

                                        if (arrayIndex == 0)
                                        {
                                            sb.Append("<div class='yaml-comment'>");
                                            sb.Append($"<div>{HtmlYamlSpaces(spaces + 2)}#</div>");
                                            sb.Append($"<div>{HtmlYamlSpaces(spaces + 2)}# {groupByAttribute.GroupByField}: {groupBy}</div>");
                                            sb.Append($"<div>{HtmlYamlSpaces(spaces + 2)}#</div>");
                                            sb.Append("</div>");
                                        }
                                    }

                                    sb.Append(ToYamlHtml(val, spaces + 2, true));
                                }
                                arrayIndex++;
                            }
                        }
                    }
                }
                else if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string))
                {
                    sb.Append(HtmlYamlValue(linkAttribute, propertyInfo.GetValue(obj), obj));
                }
                else
                {
                    sb.Append(ToYamlHtml(propertyInfo.GetValue(obj), spaces + 2));
                }
                sb.Append("</div>");
                sb.Append("<br/>");

                isFirst = false;
            }
            sb.Append("</div>");

            return sb.ToString();
        }

        private string HtmlYamlSpaces(int spaces)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < spaces; i++)
            {
                sb.Append("&nbsp;");
            }

            return sb.ToString();
        }

        private string HtmlYamlValue(HtmlLinkAttribute htmlLink, object val, object instance)
        {
            string valString = val?.ToString() ?? String.Empty;

            if (val is double || val is float)
            {
                valString = valString.Replace(",", ".");
            }

            if (htmlLink == null)
            {
                return valString;
            }

            string link = htmlLink.LinkTemplate
                .Replace("{url}", _urlHelperService.AppRootUrl(this.Request).CombineUri(Request.Path))
                .Replace("{0}", valString);

            if (instance != null && link.Contains("{") && link.Contains("}"))  // Replace instance properties
            {
                foreach (var propertyInfo in instance.GetType().GetProperties())
                {
                    var placeHolder = "{" + propertyInfo.Name + "}";
                    if (link.Contains(placeHolder))
                    {
                        object propertyValue = propertyInfo.GetValue(instance);
                        link = link.Replace(placeHolder, propertyValue != null ? propertyValue.ToString() : "");
                    }
                }
            }

            return "<a href='" + link.ToValidUri() + "'>" + valString + "</a>";
        }

        private IEnumerable<PropertyInfo> SortYamlProperties(IEnumerable<PropertyInfo> propertyInfos)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();

            List<string> orderItems = new List<string>(new string[] { "name", "id" });

            foreach (var orderItem in orderItems)
            {
                var propertyInfo = propertyInfos.Where(p => p.Name.ToLower() == orderItem).FirstOrDefault();
                if (propertyInfo != null)
                {
                    result.Add(propertyInfo);
                }
            }

            foreach (var propertyInfo in propertyInfos)
            {
                if (orderItems.Contains(propertyInfo.Name.ToLower()))
                {
                    continue;
                }

                result.Add(propertyInfo);
            }

            return result;
        }

        private string ToHtmlForm(object obj)
        {
            if (obj == null)
            {
                return String.Empty;
            }

            StringBuilder sb = new StringBuilder();

            sb.Append("<div class='html-body'>");

            sb.Append("<form>");

            sb.Append("<table>");

            bool hasFormatAttribute = false;

            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                var jsonPropertyAttribute = propertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAttribute == null)
                {
                    continue;
                }

                var inputAttribute = propertyInfo.GetCustomAttribute<FormInputAttribute>();
                if (inputAttribute != null && inputAttribute.InputType == FormInputAttribute.InputTypes.Ignore)
                {
                    continue;
                }

                var inputType = inputAttribute?.InputType ?? FormInputAttribute.InputTypes.Text;


                if (propertyInfo.GetMethod.IsPublic && propertyInfo.SetMethod.IsPublic)
                {
                    sb.Append("<tr>");
                    sb.Append("<td>");

                    if (jsonPropertyAttribute.PropertyName == "f")
                    {
                        hasFormatAttribute = true;
                    }

                    if (inputType != FormInputAttribute.InputTypes.Hidden)
                    {
                        sb.Append("<span>" + propertyInfo.Name + ":</span>");
                    }
                    sb.Append("</td><td class='input'>");

                    if (propertyInfo.PropertyType.Equals(typeof(bool)))
                    {
                        sb.Append("<select name='" + jsonPropertyAttribute.PropertyName + "' style='min-width:auto;'><option value='false'>False</option><option value='true'>True</option></select>");
                    }
                    else
                    {
                        switch (inputType)
                        {
                            case FormInputAttribute.InputTypes.TextBox:
                                sb.Append("<textarea rows='3' name='" + jsonPropertyAttribute.PropertyName + "'>" + (propertyInfo.GetValue(obj)?.ToString() ?? String.Empty) + "</textarea>");
                                break;
                            case FormInputAttribute.InputTypes.TextBox10:
                                sb.Append("<textarea rows='10' name='" + jsonPropertyAttribute.PropertyName + "'>" + (propertyInfo.GetValue(obj)?.ToString() ?? String.Empty) + "</textarea>");
                                break;
                            case FormInputAttribute.InputTypes.Hidden:
                                sb.Append("<input type='hidden' name='" + jsonPropertyAttribute.PropertyName + "' value='" + (propertyInfo.GetValue(obj)?.ToString() ?? String.Empty) + "'>");
                                break;
                            case FormInputAttribute.InputTypes.Password:
                                sb.Append("<input name='" + jsonPropertyAttribute.PropertyName + "' type='password' value='" + (propertyInfo.GetValue(obj)?.ToString() ?? String.Empty) + "'>");
                                break;
                            default:
                                if (inputAttribute?.Values != null && inputAttribute.Values.Count() > 0)
                                {
                                    sb.Append("<select name='" + jsonPropertyAttribute.PropertyName + "' style='min-width:auto;'>");
                                    foreach (var val in inputAttribute.Values)
                                    {
                                        sb.Append("<option value='" + val + "'>" + val + "</option>");
                                    }
                                    sb.Append("</select>");
                                }
                                else
                                {
                                    sb.Append("<input name='" + jsonPropertyAttribute.PropertyName + "' value='" + (propertyInfo.GetValue(obj)?.ToString() ?? String.Empty) + "'>");
                                }
                                break;
                        }
                    }
                    sb.Append("</td>");
                    sb.Append("</tr>");
                }
            }

            if (!hasFormatAttribute)
            {
                sb.Append("<tr>");
                sb.Append("<td>");
                sb.Append("<span>Format:</span>");
                sb.Append("</td><td>");
                sb.Append("<select name='f'><option value='pjson'>JSON</option></select>");
                sb.Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("<tr>");
            sb.Append("<td>");
            sb.Append("</td><td>");
            sb.Append("<button>Submit</button>");
            sb.Append("</td>");
            sb.Append("</tr>");

            sb.Append("</table>");



            sb.Append("</form>");

            sb.Append("</div>");

            return sb.ToString();
        }

        #endregion

        private T Deserialize<T>(IEnumerable<KeyValuePair<string, StringValues>> nv)
        {
            var instance = (T)Activator.CreateInstance(typeof(T));

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                if (propertyInfo.SetMethod == null)
                {
                    continue;
                }

                var jsonPropertyAttribute = propertyInfo.GetCustomAttribute<JsonPropertyAttribute>();
                if (jsonPropertyAttribute == null)
                {
                    continue;
                }

                string key = jsonPropertyAttribute.PropertyName ?? propertyInfo.Name;
                var keyValuePair = nv.Where(k => key.Equals(k.Key, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                if (keyValuePair.Key == null)
                {
                    key = "&" + key;
                    keyValuePair = nv.Where(k => k.Key == key).FirstOrDefault();   // Sometimes the keyvalue-key starts with an & ??
                }

                if (key.Equals(keyValuePair.Key, StringComparison.InvariantCultureIgnoreCase))
                {
                    var val = keyValuePair.Value.ToString();

                    if (propertyInfo.PropertyType == typeof(double))
                    {
                        if (!String.IsNullOrWhiteSpace(val))
                        {
                            propertyInfo.SetValue(instance, NumberConverter.ToDouble(keyValuePair.Value.ToString()));
                        }
                    }
                    else if (propertyInfo.PropertyType == typeof(float))
                    {
                        if (!String.IsNullOrWhiteSpace(val))
                        {
                            propertyInfo.SetValue(instance, NumberConverter.ToFloat(keyValuePair.Value.ToString()));
                        }
                    }
                    else if (propertyInfo.PropertyType == typeof(System.Int16))
                    {
                        if (!String.IsNullOrWhiteSpace(val))
                        {
                            propertyInfo.SetValue(instance, Convert.ToInt16(val));
                        }
                    }
                    else if (propertyInfo.PropertyType == typeof(System.Int32))
                    {
                        if (!String.IsNullOrWhiteSpace(val))
                        {
                            propertyInfo.SetValue(instance, Convert.ToInt32(val));
                        }
                    }
                    else if (propertyInfo.PropertyType == typeof(System.Int64))
                    {
                        if (!String.IsNullOrWhiteSpace(val))
                        {
                            propertyInfo.SetValue(instance, Convert.ToInt64(val));
                        }
                    }
                    else if(propertyInfo.PropertyType == typeof(bool))
                    {
                        if(!String.IsNullOrWhiteSpace(val)) 
                        {
                            propertyInfo.SetValue(instance, Convert.ToBoolean(val));
                        }
                    }
                    else if (propertyInfo.PropertyType == typeof(System.String))
                    {
                        propertyInfo.SetValue(instance, val);
                    }
                    else
                    {
                        if ((val.Trim().StartsWith("{") && val.Trim().EndsWith("}")) ||
                            (val.Trim().StartsWith("[") && val.Trim().EndsWith("]")))
                        {
                            propertyInfo.SetValue(instance, JsonConvert.DeserializeObject(val, propertyInfo.PropertyType));
                        }
                        else
                        {
                            propertyInfo.SetValue(instance, Convert.ChangeType(keyValuePair.Value.ToString(), propertyInfo.PropertyType));
                        }
                    }
                }
            }

            return instance;
        }

        async override protected Task<IActionResult> SecureMethodHandler(Func<Identity, Task<IActionResult>> func, Func<Exception, IActionResult> onException = null)
        {
            if (onException == null)
            {
                onException = (e) =>
                {
                    try
                    {
                        throw e;
                    }
                    catch (NotAuthorizedException nae)
                    {
                        return Result(new JsonError()
                        {
                            Error = new JsonError.ErrorDef() { Code = 403, Message = nae.Message }
                        });
                    }
                    catch (TokenRequiredException tre)
                    {
                        return Result(new JsonError()
                        {
                            Error = new JsonError.ErrorDef() { Code = 499, Message = tre.Message }
                        });
                    }
                    catch (InvalidTokenException ite)
                    {
                        return Result(new JsonError()
                        {
                            Error = new JsonError.ErrorDef() { Code = 498, Message = ite.Message }
                        });
                    }
                    catch (MapServerException mse)
                    {
                        return Result(new JsonError()
                        {
                            Error = new JsonError.ErrorDef() { Code = 999, Message = mse.Message }
                        });
                    }
                    catch (Exception)
                    {
                        return Result(new JsonError()
                        {
                            Error = new JsonError.ErrorDef() { Code = 999, Message = "unknown error" }
                        });

                    }
                };
            }

            return await base.SecureMethodHandler(func, onException: onException);
        }

        #endregion
    }
}