using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Framework.Blazor.Models;

public enum ModalDialogWidth
{
    ExtraExtraLarge,
    ExtraLarge,
    Large,
    Medium,
    Small,
    ExtraSmall,
}

public class ModalDialogOptions
{
    public bool ShowCloseButton { get; set; } = true;
    public bool CloseOnEscapeKey { get; set; } = false;

    public ModalDialogWidth Width { get; set; } = ModalDialogWidth.ExtraLarge;
    public bool FullWidth { get; set; } = false;
}
