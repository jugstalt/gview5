namespace gView.Razor.Leaflet.Exceptions;

public class MapNotIntializedException : Exception
{
    static public void ThrowIfFalse(bool intialized)
    {
        if (!intialized) throw new MapNotIntializedException();
    }
}
