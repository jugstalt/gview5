using gView.Carto.Plugins.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.system;
using gView.Razor.Dialogs.Models;

namespace gView.Carto.Plugins.CartoTools;


[RegisterPlugIn("C88FB907-852E-4799-8788-C34E04A73FFB")]

internal class About : ICartoTool
{
    public string Name => "About";

    public string ToolTip => "";

    public ToolType ToolType => ToolType.Click;

    public string Icon => "basic:help";

    public CartoToolTarget Target => CartoToolTarget.General;

    public int SortOrder => 999;

    public void Dispose()
    {
        
    }

    public bool IsEnabled(IApplicationScope scope) => true;

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();

        await scopeService.ShowModalDialog(typeof(gView.Razor.Dialogs.AboutDialog),
                                     "About",
                                     new AboutDialogModel()
                                     {
                                         Title = "gView GIS Carto",
                                         Version = SystemInfo.Version
                                     });

        return true;
    }
}
