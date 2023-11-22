using gView.Razor.Leaflet.Models;
using gView.Razor.Leaflet.Models.Events;

namespace BlazorLeaflet.Models.Events;

public class ResizeEvent : Event
{
    public Point2d OldSize { get; set; }
    public Point2d NewSize { get; set; }
}
