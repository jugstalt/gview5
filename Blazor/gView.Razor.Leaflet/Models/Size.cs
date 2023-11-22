namespace gView.Razor.Leaflet.Models;

public class Size
{
    public Size(int width, int height)
        => (Width, Height) = (width, height);

    public int Width { get; set; }
    public int Height { get; set; } 

    public int X { set { Width = value; } }
    public int Y { set { Height = value; } }
}
