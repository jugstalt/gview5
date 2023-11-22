namespace gView.Razor.Leaflet.Extensions;

static internal class CollectionsExtension
{
    static public IEnumerable<T> PickAll<T>(this System.Collections.IList? list)
    {
        if (list == null)
        {
            return Array.Empty<T>();
        }

        List<T> result = new List<T>();

        foreach (var item in list)
        {
            if (item != null && item is T)
            {
                result.Add((T)item);
            }
        }

        return result.ToArray();
    }
}
