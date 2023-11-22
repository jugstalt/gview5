using gView.Razor.Leaflet.Models;
using gView.Razor.Leaflet.Models.Events;

namespace BlazorLeaflet.Models.Events;

public class MouseEvent : Event
{
    public LatLng? LatLng { get; set; }

    public Point2d LayerPoint { get; set; }

    public Point2d ContainerPoint { get; set; }

}
