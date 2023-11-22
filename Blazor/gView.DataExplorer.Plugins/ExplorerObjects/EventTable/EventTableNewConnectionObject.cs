using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataSources.EventTable;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.EventTable;

[gView.Framework.system.RegisterPlugIn("F45B7E98-B20A-47bf-A45D-E78D52F36314")]
public class EventTableNewConnectionObject : 
                ExplorerObjectCls<IExplorerObject>, 
                IExplorerSimpleObject, 
                IExplorerObjectDoubleClick, 
                IExplorerObjectCreatable
{
    public EventTableNewConnectionObject()
        : base()
    {
    }

    public EventTableNewConnectionObject(IExplorerObject parent)
        : base(parent, 1)
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

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        var model = await appScope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.EventTableConnection),
                                                                    "EventTable Connection",
                                                                    new EventTableConnectionModel());

        if (!string.IsNullOrWhiteSpace(model?.TableName))
        {
            ConfigConnections connStream = new ConfigConnections("eventtable", "546B0513-D71D-4490-9E27-94CD5D72C64A");

            EventTableConnection etconn = new EventTableConnection(
                model.ConnectionString,
                model.TableName,
                model.IdFieldName, model.XFieldName, model.YFieldName,
                model.SpatialReference);

            string id = connStream.GetName(model.TableName);
            connStream.Add(id, etconn.ToXmlString());

            e.NewExplorerObject = new EventTableObject(Parent, id, etconn);
        }
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
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
