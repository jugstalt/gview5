using gView.Blazor.Models.Dialogs;

namespace gView.Razor.Dialogs.Models;

public class AboutDialogModel : IDialogResultItem
{
    public string Title { get; set; } = "gView GIS";

    public Version Version { get; set; } = new Version(1, 0);
}
