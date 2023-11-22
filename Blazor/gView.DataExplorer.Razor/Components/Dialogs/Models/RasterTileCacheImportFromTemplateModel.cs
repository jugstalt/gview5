using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class RasterTileCacheImportFromTemplateModel : IDialogResultItem
{
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public string? TemplatePath { get; set; }
}
