using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.VectorData;
using gView.DataSources.EventTable;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.EventTable;

[gView.Framework.system.RegisterPlugIn("653DA8F8-D321-4701-861F-23317E9FEC4D")]
public class EventTableGroupObject : 
        ExplorerParentObject, 
        IVectorDataExplorerGroupObject
{
    public EventTableGroupObject()
        : base()
    {
    }

    #region IExplorerObject Member

    public string Name => "Eventtable Connections";

    public string FullName => @"VectorData\EventTableConnections";

    public string Type => "Eventtable Connections";

    public string Icon => "webgis:coordinates2d";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        if (this.FullName == FullName)
        {
            EventTableGroupObject exObject = new EventTableGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();

        base.AddChildObject(new EventTableNewConnectionObject(this));

        ConfigConnections configStream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        Dictionary<string, string> DbConnectionStrings = configStream.Connections;
        foreach (string DbConnName in DbConnectionStrings.Keys)
        {
            EventTableConnection dbConn = new EventTableConnection();
            dbConn.FromXmlString(DbConnectionStrings[DbConnName]);
            
            base.AddChildObject(new EventTableObject(this, DbConnName, dbConn));
        }

        return true;
    }
    
    #endregion

    #region IOgcGroupExplorerObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
