using System;
using System.Collections.Generic;

namespace gView.Framework.Blazor.Models;
public class ClipboardItem
{
    public ClipboardItem(Type elementType)
    {
        this.ElementType = elementType;
    }

    public Type ElementType { get; }
    public IEnumerable<object>? Elements { get; set; }
}
