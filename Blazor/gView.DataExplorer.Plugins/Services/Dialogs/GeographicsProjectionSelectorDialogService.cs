using gView.Blazor.Core;
using gView.Blazor.Core.Services.Abstraction;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.DataExplorer.Plugins.Services.Dialogs
{
    internal class GeographicsProjectionSelectorDialogService : IKnownDialogService
    {
        public KnownDialogs Dialog => KnownDialogs.GeographicProjectionSelectorDialog;

        public Type RazorType => typeof(gView.DataExplorer.Razor.Components.Dialogs.GeograpphicProjectionDialog);

        public string Title => "Geographic Projection";

        public ModalDialogOptions? DialogOptions => null;
    }
}
