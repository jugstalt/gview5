namespace gView.Razor.Leaflet.Models.Layers;

public class TileLayer : GridLayer
{
    public required string UrlTemplate { get; set; }

    public float MinimumZoom { get; set; }

    public float MaximumZoom { get; set; } = 18;

    public string[] Subdomains { get; set; } = new string[] { "abc" };


    public string ErrorTileUrl { get; set; } = string.Empty;


    public bool IsZoomReversed { get; set; }


    public double ZoomOffset { get; set; }

    public bool DetectRetina { get; set; }
}
