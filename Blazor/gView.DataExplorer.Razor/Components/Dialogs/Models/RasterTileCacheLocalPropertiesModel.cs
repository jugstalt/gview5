using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class RasterTileCacheLocalPropertiesModel : IDialogResultItem
{
    public bool UseLocalCache { get; set; }
    public string LocalCacheFolder { get; set; } = string.Empty;
}
