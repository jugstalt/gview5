namespace gView.Razor.Leaflet.Models.Layers;

public class ImageLayer : InteractiveLayer
{
    public ImageLayer(string url, LatLng southWest, LatLng northEast)
        : base()
        => (Url, SouthWest, NorthEast) = (url, southWest, northEast);

    public float Opacity { get; set; } = 1.0f;

    public string Alt { get; set; } = string.Empty;

    public string CrossOrigin { get; set; } = string.Empty;

    public string ErrorOverlayUrl { get; set; } = string.Empty;

    public int zIndex { get; set; } = 1;

    public string ClassName { get; set; } = string.Empty;

    public string Url { get; }

    public LatLng SouthWest { get; }

    public LatLng NorthEast { get; }
}
