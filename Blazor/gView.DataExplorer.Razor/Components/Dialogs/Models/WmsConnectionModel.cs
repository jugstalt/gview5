using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public enum WmsConnectionType
{
    WMS = 0,
    WFS = 1,
    WMS_WFS = 2
}

public class WmsConnectionModel : IDialogResultItem
{
    public WmsConnectionType ConnectionType { get; set; }

    public string WmsUrl { get; set; } = string.Empty;
    public string WfsUrl { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }

    public string? ServiceName { get; set; }
}
