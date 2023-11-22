using gView.Framework.Blazor;
using gView.Framework.Blazor.Models;
using System;

namespace gView.Blazor.Core.Services.Abstraction;

public interface IKnownDialogService
{
    KnownDialogs Dialog { get; }
    Type RazorType { get; }
    string Title { get; }
    ModalDialogOptions? DialogOptions { get; }
}
