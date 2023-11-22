using gView.Blazor.Models.Dialogs;

namespace gView.Razor.Base;

public class BaseDialogModel<T> : IDialogResultItem
{
    public T? Value { get; set; }
}
