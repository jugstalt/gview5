using gView.Core.Framework.Exceptions;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using gView.Framework.UI;
using gView.MapServer;
using gView.Server.AppCode.Extensions;
using gView.Server.Services.Logging;
using gView.Server.Services.MapServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace gView.Server.AppCode;

public class MapServerInstance : IMapServer
{
    public enum ServerLicType { Unknown = 0, Private = 1, Professional = 2, Express = 3 };
    private bool _log_requests, _log_request_details, _log_errors;
    private object _lockThis = new object();
    private Dictionary<string, object> _lockers = new Dictionary<string, object>();
    //private int _maxGDIServers = int.MaxValue;
    private int _maxServices = int.MaxValue;
    private readonly MapServiceManager _mapServiceMananger;
    private readonly MapServiceDeploymentManager _mapServiceDeploymentMananger;
    private readonly MapServicesEventLogger _logger;
    private readonly string _etcPath;

    public MapServerInstance(
        MapServiceManager mapServiceMananger,
        MapServiceDeploymentManager mapServiceDeploymentMananger,
        MapServicesEventLogger logger,
        int port)
    {
        _mapServiceMananger = mapServiceMananger;
        _mapServiceDeploymentMananger = mapServiceDeploymentMananger;
        _logger = logger;

        _log_requests = _mapServiceMananger.Options.LogServiceRequests;
        _log_request_details = _mapServiceMananger.Options.LogServiceRequestDetails;
        _log_errors = _mapServiceMananger.Options.LogServiceErrors;

        _etcPath = $"{new DirectoryInfo(_mapServiceMananger.Options.ServicesPath).Parent.FullName}/etc";
    }

    async private Task<IServiceMap> Map(string name, string folder, IServiceRequestContext context)
    {
        try
        {
            if (_mapServiceDeploymentMananger.MapDocument == null)
            {
                throw new MapServerException("Fatal error: map(server) document missing");
            }

            folder = folder ?? String.Empty;

            object locker = null;
            lock (_lockThis)
            {
                if (!_lockers.ContainsKey(name))
                {
                    _lockers.Add(name, new object());
                }

                locker = _lockers[name];
            }

            IMapService mapService = GetMapService(name, folder);
            if (mapService == null || !(await mapService.GetSettingsAsync()).IsRunningOrIdle())
            {
                throw new MapServerException("unknown service: " + name);
            }

            if (await mapService.RefreshRequired())
            {
                _mapServiceDeploymentMananger.MapDocument.RemoveMap(mapService.Fullname);
            }

            //lock (locker)
            {
                string alias = name;

                if (mapService is MapServiceAlias)
                {
                    name = ((MapServiceAlias)mapService).ServiceName;
                }

                if (!String.IsNullOrWhiteSpace(folder))
                {
                    name = folder + "/" + name;
                }

                return await FindServiceMap(name, alias, context);
            }
        }
        catch (MapServerException/* mse*/)
        {
            throw;
        }
        catch (Exception ex)
        {
            await LogAsync(ToMapName(name, folder), "MapServer.Map", loggingMethod.error, ex.Message + "\n" + ex.StackTrace);
            throw new MapServerException("unknown error");
        }
    }

    #region IMapServer Member

    async public Task<IEnumerable<IMapService>> Maps(IIdentity identity)
    {
        try
        {
            if (identity == null)
            {
                return _mapServiceMananger.MapServices.ToArray();
            }

            List<IMapService> services = new List<IMapService>();
            foreach (var service in _mapServiceMananger.MapServices)
            {
                if (await service.HasAnyAccess(identity))
                {
                    services.Add(service);
                }
            }

            return services;
        }
        catch (MapServerException/* mse*/)
        {
            throw;
        }
        catch (Exception ex)
        {
            await LogAsync(String.Empty, "MapServer.Map", loggingMethod.error, ex.Message + "\n" + ex.StackTrace);
            //return new List<IMapService>();
            throw new MapServerException("unknown error");
        }
    }

    async public Task<IServiceMap> GetServiceMapAsync(string name, string folder)
    {
        return await this.Map(name, folder, null);
    }
    async public Task<IServiceMap> GetServiceMapAsync(IMapService service)
    {
        if (service == null)
        {
            return null;
        }

        return await GetServiceMapAsync(service.Name, service.Folder);
    }
    async public Task<IServiceMap> GetServiceMapAsync(IServiceRequestContext context)
    {
        try
        {
            if (context == null || context.ServiceRequest == null)
            {
                return null;
            }

            IServiceMap map = await this.Map(context.ServiceRequest.Service, context.ServiceRequest.Folder, context);
            if (map is ServiceMap)
            {
                ((ServiceMap)map).SetRequestContext(context);
            }

            return map;
        }
        catch (MapServerException/* mse*/)
        {
            throw;
        }
        catch (Exception ex)
        {
            await LogAsync(ToMapName(context?.ServiceRequest?.Service, context?.ServiceRequest?.Folder), "MapServer.Map", loggingMethod.error, ex.Message + "\n" + ex.StackTrace);
            throw new MapServerException("unknown error");
        }
    }

    async public Task<IMetadataProvider> GetMetadtaProviderAsync(IServiceRequestContext context, Guid metadataProviderId)
    {
        var map = await this.FindMap(ToMapName(context?.ServiceRequest?.Service, context?.ServiceRequest?.Folder), context);
        return map?.MetadataProvider(metadataProviderId);
    }

    public bool LoggingEnabled(loggingMethod method)
    {
        switch (method)
        {
            case loggingMethod.error:
                return _log_errors;
            case loggingMethod.request:
                return _log_requests;
            case loggingMethod.request_detail:
            case loggingMethod.request_detail_pro:
                return _log_request_details;
        }

        return false;
    }
    async public Task LogAsync(string mapName, string header, loggingMethod method, string msg)
    {
        if (!LoggingEnabled(method))
        {
            return;
        }

        switch (method)
        {
            case loggingMethod.error:
                if (!String.IsNullOrEmpty(header))
                {
                    msg = header + "\n" + msg;
                }

                await _logger.LogAsync(mapName, method, msg);
                break;
            case loggingMethod.request:
                if (!String.IsNullOrEmpty(header))
                {
                    msg = header + " - " + msg;
                }

                await _logger.LogAsync(mapName, method, msg);
                break;
            case loggingMethod.request_detail:
                if (!String.IsNullOrEmpty(header))
                {
                    msg = header + "\n" + msg;
                }

                await _logger.LogAsync(mapName, method, msg);
                break;
            case loggingMethod.request_detail_pro:
                if (!String.IsNullOrEmpty(header))
                {
                    header = header.Replace(".", "_");
                }

                await _logger.LogAsync(mapName, method, msg);
                break;
        }
    }

    async public Task LogAsync(IServiceRequestContext context, string header, loggingMethod method, string msg)
    {
        if (!LoggingEnabled(method))
        {
            return;
        }

        await LogAsync(ToMapName(context?.ServiceRequest?.Service, context?.ServiceRequest?.Folder), header, method, msg);
    }

    public string OutputUrl
    {
        get
        {
            return _mapServiceMananger.Options.OutputUrl; ;
        }
    }

    public string OutputPath
    {
        get
        {
            return _mapServiceMananger.Options.OutputPath;
        }
    }

    public string EtcPath
    {
        get
        {
            return _etcPath;
        }
    }

    public string TileCachePath
    {
        get
        {
            return _mapServiceMananger.Options.TileCachePath;
        }
    }

    public int FeatureQueryLimit => 1000;

    #endregion

    async private Task<IServiceMap> FindServiceMap(string name, string alias, IServiceRequestContext context)
    {
        Map map = await FindMap(name, context);
        if (map != null)
        {
            IEnumerable<IMapApplicationModule> modules = null;
            if (_mapServiceDeploymentMananger.MapDocument is IMapDocumentModules)
            {
                modules = ((IMapDocumentModules)_mapServiceDeploymentMananger.MapDocument).GetMapModules(map);
            }
            return await ServiceMap.CreateAsync(map, this, modules);
        }

        if (name.Contains(",") /* && _serverLicType == ServerLicType.gdi*/)
        {
            Map newMap = null;

            string[] names = name.Split(',');

            #region Alias Service auflösen...
            StringBuilder sb = new StringBuilder();
            foreach (string n in names)
            {
                IMapService ms = GetMapService(n.ServiceName(), n.FolderName());
                if (ms == null)
                {
                    return null;
                }

                if (sb.Length > 0)
                {
                    sb.Append(",");
                }

                if (ms is MapServiceAlias)
                {
                    sb.Append(((MapServiceAlias)ms).ServiceName);
                }
                else
                {
                    sb.Append(ms.Name);
                }
            }
            names = sb.ToString().Split(',');
            #endregion
            Array.Reverse(names);

            //if (names.Length > _maxGDIServices)
            //{
            //    return null;
            //}

            foreach (string n in names)
            {
                Map m1 = await FindMap(n, context);
                if (m1.Name == n)
                {
                    if (newMap == null)
                    {
                        newMap = new Map(m1, true);
                    }
                    else
                    {
                        newMap.Append(m1, true);

                        // SpatialReference von am weitesten unten liegenden Karte übernehmen
                        // ist geschackssache...
                        if (m1.Display != null && m1.Display.SpatialReference != null)
                        {
                            newMap.Display.SpatialReference =
                                m1.Display.SpatialReference.Clone() as ISpatialReference;
                        }
                    }
                }
            }
            if (newMap != null)
            {
                // alle webServiceThemes im TOC vereinigen...
                if (newMap.TOC != null)
                {
                    foreach (ITocElement tocElement in newMap.TOC.Elements)
                    {
                        if (tocElement == null ||
                            tocElement.Layers == null)
                        {
                            continue;
                        }

                        foreach (ILayer layer in tocElement.Layers)
                        {
                            if (layer is IWebServiceLayer)
                            {
                                newMap.TOC.RenameElement(tocElement, newMap.Name + "_WebThemes");
                                break;
                            }
                        }
                    }
                }
                newMap.Name = alias;
                if (!_mapServiceDeploymentMananger.MapDocument.AddMap(newMap))
                {
                    return null;
                }

                bool found = false;
                foreach (IMapService ms in _mapServiceMananger.MapServices)
                {
                    if (ms != null &&
                        ms.Name == alias && ms.Type == MapServiceType.GDI)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    _mapServiceMananger.MapServices.Add(new MapService(_mapServiceMananger, name, String.Empty, MapServiceType.GDI));
                }

                return await ServiceMap.CreateAsync(newMap, this, null);
            }
        }

        return null;
    }

    public IMapService GetMapService(string name, string folder)
    {
        foreach (IMapService ms in _mapServiceMananger.MapServices)
        {
            if (ms == null)
            {
                continue;
            }

            if (ms.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) &&
                ms.Folder.Equals(folder ?? String.Empty, StringComparison.CurrentCultureIgnoreCase))
            {
                return ms;
            }
        }

        return _mapServiceMananger.TryAddService(name, folder);
    }

    public bool IsLoaded(string name, string folder)
    {
        if (!String.IsNullOrEmpty(folder))
        {
            name = $"{folder}/{name}";
        }

        foreach (IMap map in _mapServiceDeploymentMananger.MapDocument.Maps)
        {
            if (map.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && map is Map)
            {
                return true;
            }
        }

        return false;
    }

    async private Task<Map> FindMap(string name, IServiceRequestContext context)
    {
        var map = _mapServiceDeploymentMananger.GetMapByName(name);
        if (map != null)
        {
            return map;
        }

        if (name.Contains(",")) // not supported anymore
        {
            return null;
        }

        //return await _mapServiceDeploymentMananger.LoadMap(name) as Map;
        
        using (var mutex = await FuzzyMutexAsync.LockAsync(name))
        {
            // try again, already loaded?
            return _mapServiceDeploymentMananger.GetMapByName(name) ??
                   await _mapServiceDeploymentMananger.LoadMap(name) as Map;
        }
    }

    //internal int MaxGDIServers
    //{
    //    get { return _maxGDIServers; }
    //}

    internal int MaxServices
    {
        get { return _maxServices; }
    }

    private string ToMapName(string name, string folder)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            return String.Empty;
        }

        if (!String.IsNullOrWhiteSpace(folder))
        {
            name = folder + "/" + name;
        }

        return name;
    }
}
