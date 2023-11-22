using gView.Razor.Leaflet.Extensions;
using gView.Razor.Leaflet.Models.Events;
using Microsoft.JSInterop;

namespace gView.Razor.Leaflet.Models.Layers;

public class Layer
{
    public Layer()
    {
        Id = "layer".AddGuid();
    }

    #region Properties

    public string Id { get; }

    public string Attribution { get; set; } = string.Empty;

    public virtual string Pane { get; set; } = LeafletPanes.OverlayPane;

    #endregion

    #region InterOp Events

    public delegate void LayerEventHandler(Layer layer, Event arg);

    public event LayerEventHandler? OnAdded;
    [JSInvokable]
    public void NotifyOnAdded(Event arg) => OnAdded?.Invoke(this, arg);

    public event LayerEventHandler? OnRemoved;
    [JSInvokable]
    public void NotifyOnRemoved(Event arg) => OnRemoved?.Invoke(this, arg);

    #endregion
}
