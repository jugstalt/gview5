using gView.Blazor.Models.Dialogs;
using gView.Blazor.Models.Extensions;
using Microsoft.AspNetCore.Components;

namespace gView.Razor.Base;

public class ModalDialogFormBase<T> : ModalDialogForm
    where T : IDialogResultItem
{
    [Parameter] public EventCallback<DialogResult> OnDialogClose { get; set; }
    [Parameter] public T Model { get; set; } = Activator.CreateInstance<T>();

    override protected Task Submit()
        => HandleAsync(async () =>
        {
            if (_form != null)
            {
                await _form.Validate();

                if (_form.IsValid != true)
                {
                    return;
                }
            }

            await OnDialogClose.InvokeAsync(Model.ToResult());
        });

    override protected Task Close()
        => HandleAsync(() => OnDialogClose.InvokeAsync(null));
}
