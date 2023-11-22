using System;
using System.Collections.Generic;

namespace gView.Blazor.Models.Tree;

abstract public class TreeItem<T> : IDisposable
    where T : TreeItem<T>
{
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    virtual public bool IsExpanded { get; set; }
    virtual public bool HasChildren { get; set; }
    
    virtual public bool IsSelected { get; set; }
    
    public HashSet<T>? Children { get; set; }

    public object? RefObject { get; set; }

    public abstract void Dispose();
}
