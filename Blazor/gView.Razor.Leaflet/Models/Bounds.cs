namespace gView.Razor.Leaflet.Models;

public class Bounds
{
    public Bounds(LatLng southWest, LatLng northEast)
        => (SouthWest, NorthEast) = (southWest, northEast);

    public LatLng SouthWest { get; set; }
    public LatLng NorthEast { get; set; }

    public double Width => NorthEast.Lng - SouthWest.Lng;
    public double Height => NorthEast.Lat - SouthWest.Lat;

    public LatLng Center
    {
        get => new LatLng(
            (SouthWest.Lat + NorthEast.Lat) * 0.5,
            (SouthWest.Lng + NorthEast.Lng) * 0.5);

        set
        {
            (double width, double height) = (Width, Height);
            var center = Center;

            SouthWest = new LatLng(center.Lat - height / 2.0, center.Lng - width / 2.0);
            NorthEast = new LatLng(center.Lat + height / 2.0, center.Lng + width / 2.0);
        }                   
    }

    public LatLng _southWest { set { this.SouthWest = value; } }
    public LatLng _northEast { set { this.NorthEast = value; } }

    static public Bounds World => new Bounds(new LatLng(-90, -180), new LatLng(90, 180));
}
