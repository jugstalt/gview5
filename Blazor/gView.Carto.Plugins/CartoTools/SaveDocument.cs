using gView.Carto.Plugins.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;

namespace gView.Carto.Plugins.CartoTools;

[RegisterPlugIn("540E07A0-B783-4D6D-AF65-3BC63740A3E9")]
public class SaveDocument : ICartoTool
{
    public string Name => "Save Map";

    public string ToolTip => "Save the current map";

    public ToolType ToolType => ToolType.Command;

    public string Icon => "basic:disk-white";

    public CartoToolTarget Target => CartoToolTarget.File;

    public int SortOrder => 3;

    public void Dispose()
    {
        
    }

    public bool IsEnabled(IApplicationScope scope)
    {
        return !String.IsNullOrEmpty(scope.ToCartoScopeService().Document.FilePath);
    }

    public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();

        if (File.Exists(scopeService.Document.FilePath))
        {
            XmlStream stream = new XmlStream("MapApplication", true);
            stream.Save("MapDocument", scopeService.Document);

            stream.WriteStream(scopeService.Document.FilePath);

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
