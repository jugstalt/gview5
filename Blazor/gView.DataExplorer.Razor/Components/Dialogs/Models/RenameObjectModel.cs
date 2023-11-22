using gView.Blazor.Models.Dialogs;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;
public class RenameObjectModel : IDialogResultItem
{
    private IExplorerObject? _exObject;

    public required IExplorerObject ExplorerObject
    {
        get => _exObject!;
        set
        {
            _exObject = value;
            NewName = value.Name;
        }
    }

    public string NewName { get; set; } = string.Empty;
}
