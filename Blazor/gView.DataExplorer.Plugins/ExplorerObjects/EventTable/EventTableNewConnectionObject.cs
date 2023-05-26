﻿using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.EventTable;

[gView.Framework.system.RegisterPlugIn("F45B7E98-B20A-47bf-A45D-E78D52F36314")]
public class EventTableNewConnectionObject : ExplorerObjectCls, IExplorerSimpleObject, IExplorerObjectDoubleClick, IExplorerObjectCreatable
{
    public EventTableNewConnectionObject()
        : base(null, null, 1)
    {
    }

    public EventTableNewConnectionObject(IExplorerObject parent)
        : base(parent, null, 1)
    {
    }

    #region IExplorerSimpleObject Members

    public string Icon => "basic:edit-table";

    #endregion

    #region IExplorerObject Members

    public string Name => "New Connection...";

    public string FullName => string.Empty;

    public string Type => "New Eventtable Connection"; 

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerObjectDoubleClick Members

    public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        return Task.CompletedTask;
        // ToDo:
        //FormEventTableConnection dlg = new FormEventTableConnection();

        //if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //{
        //    ConfigConnections connStream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");

        //    EventTableConnection etconn = new EventTableConnection(
        //        dlg.DbConnectionString,
        //        dlg.TableName,
        //        dlg.IdField, dlg.XField, dlg.YField,
        //        dlg.SpatialReference);

        //    string id = connStream.GetName(dlg.TableName);
        //    connStream.Add(id, etconn.ToXmlString());

        //    e.NewExplorerObject = new EventTableObject(this.ParentExplorerObject, id, etconn);
        //}
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
    {
        if (cache.Contains(FullName))
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return (parentExObject is EventTableGroupObject);
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();

        await ExplorerObjectDoubleClick(appScope, e);
        return e.NewExplorerObject;
    }

    #endregion
}
