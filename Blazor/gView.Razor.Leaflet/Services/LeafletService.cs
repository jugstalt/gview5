namespace gView.Razor.Leaflet.Services;

public class LeafletService
{
    private readonly LeafletInteropService _leafletInterop;

    public LeafletService(LeafletInteropService leafletInterop)
    {
        _leafletInterop = leafletInterop;
    }

    public LMap CreateMap(string? id)
    {
        return new LMap(_leafletInterop, id);
    }
}
