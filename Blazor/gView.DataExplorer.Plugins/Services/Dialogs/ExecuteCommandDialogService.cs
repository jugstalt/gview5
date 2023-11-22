using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs;
internal class ExecuteCommandDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.ExecuteCommand;

    public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.ExecuteCommandDialog);

    public string Title => "Execute Command";

    public ModalDialogOptions? DialogOptions => new ModalDialogOptions()
    {
        ShowCloseButton = false
    };
}
