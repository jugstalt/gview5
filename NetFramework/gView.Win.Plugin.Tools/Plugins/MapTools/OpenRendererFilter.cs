using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using System.Threading.Tasks;

namespace gView.Plugins.MapTools
{
    internal class OpenRendererFilter : ExplorerOpenDialogFilter
    {
        private GeometryType _geomType = GeometryType.Unknown;

        public OpenRendererFilter()
            : base("Map Feature Layer")
        {
            ObjectTypes.Add(typeof(ITocElement));
        }
        public OpenRendererFilter(GeometryType geomType)
            : this()
        {
            _geomType = geomType;
        }

        async public override Task<bool> Match(IExplorerObject exObject)
        {
            bool match = await base.Match(exObject);

            // Grouplayer nicht...
            if (exObject is IExplorerParentObject)
            {
                return false;
            }

            var instance = await exObject?.GetInstanceAsync();

            if (match && instance is ITocElement && _geomType != GeometryType.Unknown)
            {
                ITocElement tocElement = instance as ITocElement;
                if (tocElement.Layers != null)
                {
                    foreach (ILayer layer in tocElement.Layers)
                    {
                        if (layer is IFeatureLayer && ((IFeatureLayer)layer).FeatureClass != null)
                        {
                            return ((IFeatureLayer)layer).FeatureClass.GeometryType == _geomType;
                        }
                    }
                }
            }
            //if (match && _geomType != geometryType.Unknown && exObject.Object is IFeatureLayer && ((IFeatureLayer)exObject.Object).FeatureClass != null)
            //{
            //    return ((IFeatureLayer)exObject.Object).FeatureClass.GeometryType == _geomType;
            //}

            return match;
        }
    }
}
