using Microsoft.JSInterop;
using gView.Razor.Leaflet.Models.Layers;
using System.Collections.Concurrent;
using gView.Razor.Leaflet.Models;
using Microsoft.AspNetCore.Components.Web;

namespace gView.Razor.Leaflet.Services;

public class LeafletInteropService : IDisposable
{
    private const string _gViewLeaflet = "window.gViewLeafletInterops";

    private readonly IJSRuntime _jsRuntime;
    private readonly ConcurrentDictionary<string, (IDisposable, string, Layer)> _layerReferences;
            
    public LeafletInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _layerReferences = new ConcurrentDictionary<string, (IDisposable, string, Layer)>();
    }

    internal ValueTask Create(LMap map) =>
            _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.create", map, DotNetObjectReference.Create(map));

    internal ValueTask AddLayer(string mapId, Layer layer)
    {
        return layer switch
        {
            TileLayer tileLayer => _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.addTilelayer", mapId, tileLayer, CreateLayerReference(mapId, tileLayer)),
            ImageLayer image => _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.addImageLayer", mapId, image, CreateLayerReference(mapId, image)),
            _ => throw new NotImplementedException($"The layer {typeof(Layer).Name} has not been implemented."),
        };
    }

    internal async ValueTask RemoveLayer(string mapId, string layerId)
    {
        await _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.removeLayer", mapId, layerId);
        DisposeLayerReference(layerId);
    }

    internal ValueTask FitBounds(string mapId, LatLng southWest, LatLng northEast, Point2d? padding, float? maxZoom) =>
            _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.fitBounds", mapId, southWest, northEast, padding, maxZoom);

    internal ValueTask PanTo(string mapId, LatLng latLng, bool animate, float duration, float easeLinearity, bool noMoveStart) =>
        _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.panTo", mapId, latLng, animate, duration, easeLinearity, noMoveStart);

    internal ValueTask<LatLng> GetCenter(string mapId) =>
        _jsRuntime.InvokeAsync<LatLng>($"{_gViewLeaflet}.getCenter", mapId);

    internal ValueTask<Bounds> GetBounds(string mapId)
        => _jsRuntime.InvokeAsync<Bounds>($"{_gViewLeaflet}.getBounds", mapId);

    internal ValueTask<Size> GetImageSize(string mapId)
        => _jsRuntime.InvokeAsync<Size>($"{_gViewLeaflet}.getImageSize", mapId);

    internal ValueTask<float> GetZoom(string mapId) =>
        _jsRuntime.InvokeAsync<float>($"{_gViewLeaflet}.getZoom", mapId);

    internal ValueTask ZoomIn(string mapId, MouseEventArgs e) =>
        _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.zoomIn", mapId, e);

    internal ValueTask ZoomOut(string mapId, MouseEventArgs e) =>
        _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.zoomOut", mapId, e);

    internal ValueTask Destroy(string mapId) =>
        _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.destroy", mapId);

    internal ValueTask Refresh(string mapId) =>
        _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.refresh", mapId);

    internal ValueTask UpdateImageLayer(string mapId, string layerId, string url, LatLng? southWest, LatLng? northEast)
        => _jsRuntime.InvokeVoidAsync($"{_gViewLeaflet}.updateImageLayer", mapId, layerId, url, southWest, northEast);

    #region Helper

    private DotNetObjectReference<T> CreateLayerReference<T>(string mapId, T layer) where T : Layer
    {
        var result = DotNetObjectReference.Create(layer);
        _layerReferences.TryAdd(layer.Id, (result, mapId, layer));
        return result;
    }

    private void DisposeLayerReference(string layerId)
    {
        if (_layerReferences.TryRemove(layerId, out var value))
            value.Item1.Dispose();
    }

    public void Dispose()
    {
        foreach (var layerId in _layerReferences.Keys)
        {
            DisposeLayerReference(layerId);
        }

        _layerReferences.Clear();
    }

    #endregion
}
