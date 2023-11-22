using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs;

internal class SpatialReferenceDialogService : IKnownDialogService
{
    public KnownDialogs Dialog => KnownDialogs.SpatialReferenceDialog;

    public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.SpatialReferenceDialog);

    public string Title => "Spatial Reference System";

    public ModalDialogOptions? DialogOptions => null;
}
