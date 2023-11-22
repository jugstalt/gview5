using gView.DataExplorer.Plugins.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using gView.Razor.Dialogs.Models;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTools;

[gView.Framework.system.RegisterPlugIn("1DB93AF4-F1E0-4CD7-A934-0E8BE3C35D98")]
public class About : IExplorerTool
{
    #region IExplorerTool

    public string Name => "About";

    public string ToolTip => "";

    public string Icon => "basic:help";

    public ExplorerToolTarget Target => ExplorerToolTarget.General;

    public bool IsEnabled(IApplicationScope scope) => true;

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToExplorerScopeService();

        await scopeService.ShowModalDialog(typeof(gView.Razor.Dialogs.AboutDialog),
                                     "About",
                                     new AboutDialogModel()
                                     {
                                         Title = "gView GIS DataExplorer",
                                         Version = SystemInfo.Version
                                     });

        return true;
    }

    #endregion

    #region IOrder

    public int SortOrder => 999;

    #endregion

    #region IDisposable

    public void Dispose()
    {
        
    }

    #endregion
}
