using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Web.GeoServices.ContextTools;

internal class UpdateConnectionString : IExplorerObjectContextTool
{
    #region IExplorerObjectContextTool

    public string Name => "Connection String";

    public string Icon => "basic:edit-database";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return exObject is GeoServicesConnectionExplorerObject;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var connectionString = ((GeoServicesConnectionExplorerObject)exObject).GetConnectionString();

        var model = await scope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.GeoServicesConnectionDialog),
                                                                 "GeoServices Connection",
                                                                  connectionString.ToGeoServicesConnectionModel());

        if (model != null)
        {
            return await ((GeoServicesConnectionExplorerObject)exObject).UpdateConnectionString(model.ToConnectionString());
        }

        return false;
    }

    #endregion
}

