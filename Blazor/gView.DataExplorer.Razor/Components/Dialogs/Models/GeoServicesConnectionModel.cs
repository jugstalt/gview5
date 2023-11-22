using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class GeoServicesConnectionModel : IDialogResultItem
{
    public string ServicesUrl { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }
}