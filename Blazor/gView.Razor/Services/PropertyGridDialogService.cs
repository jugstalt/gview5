using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;

namespace gView.Razor.Services;

public class PropertyGridDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.PropertyGridDialog;

    public Type RazorType => typeof(gView.Razor.Dialogs.PropertyGridDialog);

    public string Title => "Property Grid";

    public ModalDialogOptions? DialogOptions => null;
}
