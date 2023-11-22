using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.WebServices;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices;

[gView.Framework.system.RegisterPlugIn("517DEC80-F6A5-44BC-95EF-9A56543C373B")]
public class GeoServicesExplorerObjects : ExplorerParentObject,
                                          IWebServicesExplorerGroupObject
{
    public GeoServicesExplorerObjects() : base() { }

    #region IExplorerObject Member

    public string Name => "ESRI GeoServices";


    public string FullName => @"WebServices\gView.GeoServices";

    public string Type => "gView.GeoServices Connections";

    public string Icon => "basic:globe";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        base.AddChildObject(new GeoServicesNewConnectionExplorerObject(this));

        ConfigConnections configConnections = new ConfigConnections("geoservices_connection", "546B0513-D71D-4490-9E27-94CD5D72C64A");

        Dictionary<string, string> connections = configConnections.Connections;
        foreach (string name in connections.Keys)
        {
            base.AddChildObject(new GeoServicesConnectionExplorerObject(this, name, connections[name]));
        }

        return true;
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult(cache[FullName]);
        }

        if (FullName == this.FullName)
        {
            GeoServicesExplorerObjects exObject = new GeoServicesExplorerObjects();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }
        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IEsriGroupExplorerObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
