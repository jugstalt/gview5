using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.OSGeo.Ogr.ContextTools;

public class UpdateConnectionString : IExplorerObjectContextTool
{
    #region IExplorerObjectContextTool

    public string Name => "Connection String";

    public string Icon => "basic:edit-database";

    public bool IsEnabled(IApplicationScope scope, IExplorerObject exObject)
    {
        return exObject is OgrDatasetExplorerObject;
    }

    async public Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject)
    {
        var model = await scope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.OgrConnectionDialog),
                                                                 "EventTable Connection",
                                                                  new OgrConnectionModel()
                                                                  {
                                                                      ConnectionString = ((OgrDatasetExplorerObject)exObject).ConnectionString,
                                                                  });

        if (model != null)
        {
            return ((OgrDatasetExplorerObject)exObject).UpdateConnectionString(model.ConnectionString);
        }

        return false;
    }

    #endregion
}
