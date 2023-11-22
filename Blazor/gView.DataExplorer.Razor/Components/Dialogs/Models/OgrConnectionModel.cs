using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class OgrConnectionModel : IDialogResultItem
{
    public string ConnectionString { get; set; } = string.Empty;
}
