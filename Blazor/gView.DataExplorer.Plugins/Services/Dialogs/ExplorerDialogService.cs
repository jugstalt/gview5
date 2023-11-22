using gView.Blazor.Core;
using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs;

internal class ExplorerDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.ExplorerDialog;

    public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.ExplorerDilaog);

    public string Title => "Explore";

    public ModalDialogOptions? DialogOptions => null;
}
