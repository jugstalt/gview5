using gView.DataExplorer.Plugins.ExplorerObjects.FileSystem;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.IO;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[gView.Framework.system.RegisterPlugIn("7cbbed5e-c071-46de-b30a-9c6140dafd75")]
internal class AddNetworkDirectory : IExplorerTool
{
    #region IExplorerTool

    public string Name => "Map (network) folder...";

    public bool IsEnabled(IApplicationScope scope) => true;

    public string ToolTip => "";

    public string Icon => "basic:open-in-window";

    public ExplorerToolTarget Target => ExplorerToolTarget.General;

    public async Task<bool> OnEvent(IApplicationScope scope)
    {
        MapNetworkFolderModel? model = null;
        var scopeService = scope.ToExplorerScopeService();

        if (scopeService.CurrentExplorerObject is DirectoryObject)
        {
            model = new MapNetworkFolderModel()
            {
                FolderPath = ((DirectoryObject)scopeService.CurrentExplorerObject).FullName
            };
        }

        model = await scopeService.ShowModalDialog(
            typeof(Razor.Components.Dialogs.MapNetworkFolderDialog),
            this.Name,
            model);

        if (!string.IsNullOrWhiteSpace(model?.FolderPath) &&
            Directory.Exists(model.FolderPath))
        {
            ConfigConnections connStream = new ConfigConnections("directories");
            connStream.Add(model.FolderPath.Trim(), model.FolderPath.Trim());

            await scopeService.ForceContentRefresh();
        }

        return true;
    }

    #endregion

    #region IOrder

    public int SortOrder => 25;

    #endregion

    #region IDisposable

    public void Dispose()
    {

    }

    #endregion
}
