using gView.Blazor.Models.Dialogs;
using gView.Cmd.Core.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class ExecuteCommandModel : IDialogResultItem
{
    public required IEnumerable<CommandItem> CommandItems { get; init; }

    public bool Result { get; set; }
}

public class CommandItem
{
    public required ICommand Command { get; init; }
    public required IDictionary<string, object> Parameters { get; init; }
}
