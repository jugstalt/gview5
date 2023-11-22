namespace gView.Razor.Leaflet.Models;

public class LatLng
{
    public LatLng() { }
    public LatLng(double lat, double lng) => (Lat, Lng) = (
        Math.Min(Math.Max(-90.0, lat), 90),
        Math.Min(Math.Max(-180.0, lng), 180));

    public double Lat { get; set; }

    public double Lng { get; set; }

    public double Alt { get; set; }
}
