using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs;

internal class WarningsDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.WarningsDialog;

    public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.WarningsDialog);

    public string Title => "Warnings";

    public ModalDialogOptions? DialogOptions => null;
}
