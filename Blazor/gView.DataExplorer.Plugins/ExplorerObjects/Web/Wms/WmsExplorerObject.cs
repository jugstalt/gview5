using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.WebServices;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.Wms;

[gView.Framework.system.RegisterPlugIn("41C75FD2-9AD0-4457-8248-E55EDA0C114E")]
public class WmsExplorerObject : ExplorerParentObject,
                                 IWebServicesExplorerGroupObject
{
    public WmsExplorerObject() : base() { }
    public WmsExplorerObject(IExplorerObject? parent)
        : base()
    {
    }

    #region IExplorerObject Member

    public string Name => "OGC Web Services (WMS/WFS)";

    public string FullName => @"WebServices\Web";

    public string Type => "OGC.WMS Connections";

    public string Icon => "basic:globe";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        base.AddChildObject(new WmsNewConnectionExplorerObject(this));

        ConfigConnections configStream = new ConfigConnections("ogc_web_connection", "546B0513-D71D-4490-9E27-94CD5D72C64A");

        Dictionary<string, string> connections = configStream.Connections;
        foreach (string name in connections.Keys)
        {
            base.AddChildObject(new WmsServiceExplorerObject(this, name, connections[name]));
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
            WmsExplorerObject exObject = new WmsExplorerObject(this.ParentExplorerObject);
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }
        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IOgcGroupExplorerObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
