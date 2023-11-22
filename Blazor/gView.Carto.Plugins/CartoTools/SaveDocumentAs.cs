using gView.Carto.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Carto;
using gView.Framework.Carto.Abstraction;
using gView.Framework.IO;
using gView.Framework.system;

namespace gView.Carto.Plugins.CartoTools;

[RegisterPlugIn("1CA4CC29-FA96-4E0B-862F-C1D8CEDA7335")]
public class SaveDocumentAs : ICartoTool
{
    public string Name => "Save As ...";

    public string ToolTip => "Save the current map under a new filename";

    public ToolType ToolType => ToolType.Command;

    public string Icon => "basic:disks-white";

    public CartoToolTarget Target => CartoToolTarget.File;

    public int SortOrder => 4;

    public void Dispose()
    {

    }

    public bool IsEnabled(IApplicationScope scope)
    {
        return true;
    }

    async public Task<bool> OnEvent(IApplicationScope scope)
    {
        var scopeService = scope.ToCartoScopeService();

        var model = await scopeService.ShowKnownDialog(KnownDialogs.ExplorerDialog,
                                                       title: "Save current map",
                                                       model: new ExplorerDialogModel()
                                                       {
                                                           Filters = new List<ExplorerDialogFilter> {
                                                                new SaveFileFilter("Map", "*.mxl")
                                                           },
                                                           Mode = ExploerDialogMode.Save
                                                       });

        
        if (model?.Result.ExplorerObjects != null 
            && model.Result.ExplorerObjects.Count == 1 
            && !String.IsNullOrWhiteSpace(model.Result.Name))
        {
            string fileTitle = model.Result.Name.Trim();
            if (!fileTitle.EndsWith(".mxl", StringComparison.OrdinalIgnoreCase))
            {
                fileTitle = $"{fileTitle}.mxl";
            }

            string? mxlFilenPath = Path.Combine(model.Result.ExplorerObjects.First().FullName, fileTitle);
            bool performEncryption = true;

            XmlStream stream = new XmlStream("MapApplication", performEncryption);
            stream.Save("MapDocument", scopeService.Document);

            stream.WriteStream(mxlFilenPath);

            scopeService.Document.FilePath = mxlFilenPath;
        }

        return true;
    }
}
