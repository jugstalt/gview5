using gView.Blazor.Models.Dialogs;

namespace gView.Blazor.Models.Extensions;

static public class DialogResultExtensions
{
    static public DialogResult ToResult(this IDialogResultItem item)
        => new DialogResult() { Result = item };
}
