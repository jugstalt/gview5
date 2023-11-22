using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class MapNetworkFolderModel : IDialogResultItem
{
    public string FolderPath { get; set; } = string.Empty;
}
