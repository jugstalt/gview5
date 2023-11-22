using gView.Blazor.Models.Dialogs;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class InputBoxModel : IDialogResultItem
{
    public required string Value { get; set; }

    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}
