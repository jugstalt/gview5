using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class SelectMapServerServiceModel : IDialogResultItem
{
    public string Server { get; set; } = "";
    public string Service { get; set; } = "";

    public bool UseAuthentication { get; set; }

    public string? Client { get; set; }
    public string? Secret { get; set; }
}
