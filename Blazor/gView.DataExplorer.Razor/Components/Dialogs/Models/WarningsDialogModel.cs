using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class WarningsDialogModel : IDialogResultItem
{
    public IEnumerable<string> Warnings { get; set;} = Enumerable.Empty<string>();
}
