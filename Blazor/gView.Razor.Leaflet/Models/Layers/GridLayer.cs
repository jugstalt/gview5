namespace gView.Razor.Leaflet.Models.Layers;

public abstract class GridLayer : Layer
{
    public GridLayer()
        : base()
    {
        this.Bounds = Bounds.World;
    }

    public Size TileSize { get; set; } = new Size(256, 256);

    public double Opacity { get; set; } = 1.0;

    public bool UpdateWhenZooming { get; set; } = true;

    public int UpdateInterval { get; set; } = 200;

    public int ZIndex { get; set; } = 1;

    public Bounds Bounds { get; set; }

}