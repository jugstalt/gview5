using BlazorLeaflet.Models.Events;
using Microsoft.JSInterop;

namespace gView.Razor.Leaflet.Models.Layers;

public abstract class InteractiveLayer : Layer
{
    public bool IsInteractive { get; set; } = true;

    public virtual bool IsBubblingMouseEvents { get; set; } = true;

    #region events

    public delegate void MouseEventHandler(InteractiveLayer sender, MouseEvent e);

    public event MouseEventHandler? OnClick;
    [JSInvokable]
    public void NotifyClick(MouseEvent eventArgs)
    {
        OnClick?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnDblClick;
    [JSInvokable]
    public void NotifyDblClick(MouseEvent eventArgs)
    {
        OnDblClick?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnMouseDown;
    [JSInvokable]
    public void NotifyMouseDown(MouseEvent eventArgs)
    {
        OnMouseDown?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnMouseUp;
    [JSInvokable]
    public void NotifyMouseUp(MouseEvent eventArgs)
    {
        OnMouseUp?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnMouseOver;
    [JSInvokable]
    public void NotifyMouseOver(MouseEvent eventArgs)
    {
        OnMouseOver?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnMouseOut;
    [JSInvokable]
    public void NotifyMouseOut(MouseEvent eventArgs)
    {
        OnMouseOut?.Invoke(this, eventArgs);
    }

    public event MouseEventHandler? OnContextMenu;
    [JSInvokable]
    public void NotifyContextMenu(MouseEvent eventArgs)
    {
        OnContextMenu?.Invoke(this, eventArgs);
    }

    #endregion

}
