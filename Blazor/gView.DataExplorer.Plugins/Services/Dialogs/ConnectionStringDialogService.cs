using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs;

internal class ConnectionStringDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.ConnectionString;

    public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.ConnectionStringDialog);

    public string Title => "Data Connection";

    public ModalDialogOptions? DialogOptions => null;
}
