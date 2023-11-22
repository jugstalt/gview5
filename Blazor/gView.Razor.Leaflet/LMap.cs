using BlazorLeaflet.Models.Events;
using gView.Razor.Leaflet.Exceptions;
using gView.Razor.Leaflet.Extensions;
using gView.Razor.Leaflet.Models;
using gView.Razor.Leaflet.Models.Events;
using gView.Razor.Leaflet.Models.Layers;
using gView.Razor.Leaflet.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace gView.Razor.Leaflet;

public class LMap
{
    private readonly LeafletInteropService _leafletJs;
    private readonly List<Layer> _layers;

    private bool _isInitialized = false;

    internal LMap(LeafletInteropService leafletJs, string? id)
    {
        _leafletJs = leafletJs;
        _layers = new List<Layer>();

        this.Id = String.IsNullOrEmpty(id) ? "gview_leaflet_map".AddGuid() : id;
    }

    #region Properties

    public string Id { get; }

    internal bool IsIntialized => _isInitialized;

    public LatLng Center
    {
        get => Bounds.Center;
        set => Bounds.Center = value;
    }

    public Bounds Bounds { get; set; } = new Bounds(new LatLng(0, 0), new LatLng(0, 0));

    public Bounds MaxBounds { get; set; } = new Bounds(new LatLng(-180, -90), new LatLng(180, 90));

    public double Zoom { get; set; }
    public double MinZoom { get; set; }
    public double MaxZoom { get; set; }

    #endregion

    #region Events

    public event Func<Task>? OnIntialized;

    #endregion

    #region Methods

    internal Task FireOnIntialized()
    {
        _isInitialized = true;
        return OnIntialized?.Invoke() ?? Task.CompletedTask;
    }

    async public Task AddLayer(Layer layer)
    {
        MapNotIntializedException.ThrowIfFalse(_isInitialized);

        _layers.Add(layer);
        await _leafletJs.AddLayer(Id, layer);
    }

    async public Task RemoveLayer(Layer layer)
    {
        MapNotIntializedException.ThrowIfFalse(_isInitialized);

        _layers.Remove(layer);
        await _leafletJs.RemoveLayer(Id, layer.Id);
    }

    async public Task FitBounds(LatLng southWest, LatLng northEast, Point2d? padding = null, float? maxZoom = null)
    {
        await _leafletJs.FitBounds(Id, southWest, northEast, padding, maxZoom);
    }

    async public Task PanTo(LatLng latLng, bool animate = false, float duration = 0.25f, float easeLinearity = 0.25f, bool noMoveStart = false)
    {
        await _leafletJs.PanTo(Id, latLng, animate, duration, easeLinearity, noMoveStart);
    }

    public async Task<LatLng> GetCenter() => await _leafletJs.GetCenter(Id);

    public async Task<Bounds> GetBounds() => await _leafletJs.GetBounds(Id);

    public async Task<Size> GetImageSize() => await _leafletJs.GetImageSize(Id);

    public async Task<float> GetZoom() =>
        await _leafletJs.GetZoom(Id);

    public async Task ZoomIn(MouseEventArgs e) => await _leafletJs.ZoomIn(Id, e);

    public async Task ZoomOut(MouseEventArgs e) => await _leafletJs.ZoomOut(Id, e);

    public async Task Destroy() => await _leafletJs.Destroy(Id);

    public async Task Refresh() => await _leafletJs.Refresh(Id);

    public async Task UpdateImageLayer(ImageLayer imageLayer, string url, LatLng? southWest, LatLng? northEast) 
        => await _leafletJs.UpdateImageLayer(Id, imageLayer.Id, url, southWest, northEast);

    #endregion

    #region Interactive Map Events

    public delegate Task MapEventHandler(object sender, Event e);
    public delegate Task MapResizeEventHandler(object sender, ResizeEvent e);

    public event MapEventHandler? OnZoomLevelsChange;
    [JSInvokable]
    public void NotifyZoomLevelsChange(Event e) => OnZoomLevelsChange?.Invoke(this, e);

    public event MapResizeEventHandler? OnResize;
    [JSInvokable]
    public void NotifyResize(ResizeEvent e) => OnResize?.Invoke(this, e);

    public event MapEventHandler? OnUnload;
    [JSInvokable]
    public void NotifyUnload(Event e) => OnUnload?.Invoke(this, e);

    public event MapEventHandler? OnViewReset;
    [JSInvokable]
    public void NotifyViewReset(Event e) => OnViewReset?.Invoke(this, e);

    public event MapEventHandler? OnLoad;
    [JSInvokable]
    public void NotifyLoad(Event e) => OnLoad?.Invoke(this, e);

    public event MapEventHandler? OnZoomStart;
    [JSInvokable]
    public void NotifyZoomStart(Event e) => OnZoomStart?.Invoke(this, e);

    public event MapEventHandler? OnMoveStart;
    [JSInvokable]
    public void NotifyMoveStart(Event e) => OnMoveStart?.Invoke(this, e);

    public event MapEventHandler? OnZoom;
    [JSInvokable]
    public void NotifyZoom(Event e) => OnZoom?.Invoke(this, e);

    public event MapEventHandler? OnMove;
    [JSInvokable]
    public void NotifyMove(Event e) => OnMove?.Invoke(this, e);

    public event MapEventHandler? OnZoomEnd;
    [JSInvokable]
    public void NotifyZoomEnd(Event e) => OnZoomEnd?.Invoke(this, e);

    public event MapEventHandler? OnMoveEnd;
    [JSInvokable]
    public void NotifyMoveEnd(Event e) => OnMoveEnd?.Invoke(this, e);

    public event MouseEventHandler? OnMouseMove;
    [JSInvokable]
    public void NotifyMouseMove(MouseEvent eventArgs) => OnMouseMove?.Invoke(this, eventArgs);

    public event MapEventHandler? OnKeyPress;
    [JSInvokable]
    public void NotifyKeyPress(Event eventArgs) => OnKeyPress?.Invoke(this, eventArgs);

    public event MapEventHandler? OnKeyDown;
    [JSInvokable]
    public void NotifyKeyDown(Event eventArgs) => OnKeyDown?.Invoke(this, eventArgs);

    public event MapEventHandler? OnKeyUp;
    [JSInvokable]
    public void NotifyKeyUp(Event eventArgs) => OnKeyUp?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnPreClick;
    [JSInvokable]
    public void NotifyPreClick(MouseEvent eventArgs) => OnPreClick?.Invoke(this, eventArgs);

    #endregion

    #region Interactive Layer Events

    public delegate void MouseEventHandler(LMap sender, MouseEvent e);

    public event MouseEventHandler? OnClick;
    [JSInvokable]
    public void NotifyClick(MouseEvent eventArgs) => OnClick?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnDblClick;
    [JSInvokable]
    public void NotifyDblClick(MouseEvent eventArgs) => OnDblClick?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnMouseDown;
    [JSInvokable]
    public void NotifyMouseDown(MouseEvent eventArgs) => OnMouseDown?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnMouseUp;
    [JSInvokable]
    public void NotifyMouseUp(MouseEvent eventArgs) => OnMouseUp?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnMouseOver;
    [JSInvokable]
    public void NotifyMouseOver(MouseEvent eventArgs) => OnMouseOver?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnMouseOut;
    [JSInvokable]
    public void NotifyMouseOut(MouseEvent eventArgs) => OnMouseOut?.Invoke(this, eventArgs);

    public event MouseEventHandler? OnContextMenu;
    [JSInvokable]
    public void NotifyContextMenu(MouseEvent eventArgs) => OnContextMenu?.Invoke(this, eventArgs);

    #endregion
}
