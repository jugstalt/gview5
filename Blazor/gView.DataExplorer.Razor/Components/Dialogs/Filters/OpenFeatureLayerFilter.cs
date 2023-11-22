using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Geometry;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenFeatureLayerFilter : ExplorerOpenDialogFilter
{
    private GeometryType _geomType = GeometryType.Unknown;

    public OpenFeatureLayerFilter()
        : base("Map Feature Layer")
    {
        ObjectTypes.Add(typeof(IFeatureLayer));
    }
    public OpenFeatureLayerFilter(GeometryType geomType)
        : this()
    {
        _geomType = geomType;
    }

    async public override Task<bool> Match(IExplorerObject exObject)
    {
        bool match = await base.Match(exObject);

        var instatnce = await exObject.GetInstanceAsync();
        if (match && _geomType != GeometryType.Unknown && instatnce is IFeatureLayer && ((IFeatureLayer)instatnce).FeatureClass != null)
        {
            return ((IFeatureLayer)instatnce).FeatureClass.GeometryType == _geomType;
        }

        return match;
    }
}
