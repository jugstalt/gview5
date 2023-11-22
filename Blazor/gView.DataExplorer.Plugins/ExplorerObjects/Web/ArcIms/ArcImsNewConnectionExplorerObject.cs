using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using System.Threading.Tasks;
using gView.Framework.system;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.ArcIms;

[gView.Framework.system.RegisterPlugIn("1B6F2AF5-9146-498C-9C71-8C69E153FD35")]
public class ArcImsNewConnectionExplorerObject : ExplorerObjectCls<IExplorerObject>,
                                                 IExplorerSimpleObject,
                                                 IExplorerObjectDoubleClick,
                                                 IExplorerObjectCreatable
{
    public ArcImsNewConnectionExplorerObject()
        : base()
    {
    }

    public ArcImsNewConnectionExplorerObject(IExplorerObject parent)
        : base(parent, 0)
    {
    }

    #region IExplorerObject Member

    public string Name => "New ArcIMS Connection...";

    public string FullName => "";

    public string Type => "New Connection";

    public string Icon => "basic:edit-plus";

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerObjectDoubleClick Member

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        var model = await appScope.ToExplorerScopeService().ShowModalDialog(
            typeof(gView.DataExplorer.Razor.Components.Dialogs.ArcImsConnectionDialog),
            "GeoServices Connection",
            new ArcImsConnectionModel());

        if (model != null)
        {
            var connectionString = model.ToConnectionString();
            ConfigConnections connStream = new ConfigConnections("arcims_connections", "546B0513-D71D-4490-9E27-94CD5D72C64A");

            string id = connStream.GetName(ConfigTextStream.ExtractValue(connectionString, "server").UrlToConfigId());
            connStream.Add(id, connectionString);

            e.NewExplorerObject = new ArcImsConnectionExplorerObject(Parent, id, connectionString);
        }
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult(cache[FullName]);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return (parentExObject is ArcImsExplorerObjects);
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();
        await ExplorerObjectDoubleClick(appScope, e);
        return e.NewExplorerObject;
    }

    #endregion
}
