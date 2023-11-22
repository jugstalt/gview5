using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.WebServices;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.ArcIms;

[gView.Framework.system.RegisterPlugIn("FEEFD2E9-D0DD-4850-BCD6-86D88B543DB3")]
public class ArcImsExplorerObjects :
                    ExplorerParentObject,
                    IWebServicesExplorerGroupObject
{
    public ArcImsExplorerObjects() : base() { }

    #region IExplorerObject Member

    public string Name => "ESRI ArcIMS Services";

    public string FullName => @"WebServices\ArcIMS";

    public string Type => "ESRI ArcIMS Connections";

    public string Icon => "basic:globe";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        base.AddChildObject(new ArcImsNewConnectionExplorerObject(this));

        ConfigConnections configConnections = new ConfigConnections("arcims_connections", "546B0513-D71D-4490-9E27-94CD5D72C64A");

        Dictionary<string, string> connections = configConnections.Connections;
        foreach (string name in connections.Keys)
        {
            base.AddChildObject(new ArcImsConnectionExplorerObject(this, name, connections[name]));
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
            ArcImsExplorerObjects exObject = new ArcImsExplorerObjects();
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
