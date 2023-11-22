using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class VectorTileCacheConnectionModel : IDialogResultItem
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
