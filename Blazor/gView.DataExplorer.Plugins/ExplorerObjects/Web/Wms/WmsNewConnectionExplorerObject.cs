using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.Wms;

[gView.Framework.system.RegisterPlugIn("4C8FC31A-D988-4D6A-94C5-849237FB8E70")]
public class WmsNewConnectionExplorerObject : ExplorerObjectCls<WmsExplorerObject>, 
                                              IExplorerSimpleObject, 
                                              IExplorerObjectDoubleClick, 
                                              IExplorerObjectCreatable
{
    public WmsNewConnectionExplorerObject()
        : base()
    {
    }
    public WmsNewConnectionExplorerObject(WmsExplorerObject parent)
        : base(parent, 0)
    {
    }

    #region IExplorerObject Member

    public string Name => "New Connection...";


    public string FullName => "";

    public string Type => "New Connection";

    public string Icon => "basic:edit-plus";

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }

    #endregion

    #region IExplorerObjectDoubleClick Member

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        var model = await appScope.ToExplorerScopeService().ShowModalDialog(
            typeof(gView.DataExplorer.Razor.Components.Dialogs.WmsConnectionDialog),
            "WMS/WFS Connection",
            new WmsConnectionModel());

        if (model != null)
        {
            var connectionString = model.ToConnectionString();
            ConfigConnections connStream = new ConfigConnections("ogc_web_connection", "546B0513-D71D-4490-9E27-94CD5D72C64A");

            string id = String.Empty;
            switch (ConfigTextStream.ExtractValue(connectionString, "service").ToUpper())
            {
                case "WMS_WFS":
                case "WMS":
                    id = ConfigTextStream.ExtractValue(connectionString, "wms").UrlToConfigId();
                    break;
                case "WFS":
                    id = ConfigTextStream.ExtractValue(connectionString, "wfs").UrlToConfigId();
                    break;
                default:
                    return;
            }
            if (!String.IsNullOrWhiteSpace(model.ServiceName))
            {
                id = $"{model.ServiceName}@{id}";
            }

            connStream.Add(id = connStream.GetName(id), connectionString);

            e.NewExplorerObject = new WmsServiceExplorerObject(base.TypedParent, id, connectionString);
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
        return (parentExObject is WmsExplorerObject);
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();
        await ExplorerObjectDoubleClick(appScope, e);
        return e.NewExplorerObject;
    }

    #endregion
}
