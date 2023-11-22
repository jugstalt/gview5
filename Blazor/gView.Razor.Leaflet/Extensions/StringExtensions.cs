namespace gView.Razor.Leaflet.Extensions;

static public class StringExtensions
{
    static public string AddGuid(this string str, string separator = "_")
        => $"{str}{separator}{Guid.NewGuid().ToString("N").ToLower()}";
}
