using gView.Framework.Data;

namespace gView.Framework.Carto
{
    internal class ServiceFeatureLayer : FeatureLayer
    {
        private IFeatureLayer _layer;

        public ServiceFeatureLayer(IFeatureLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            _layer = layer;
            _renderer = layer.FeatureRenderer;
            _selectionrenderer = layer.SelectionRenderer;
            _labelRenderer = layer.LabelRenderer;
            _filterQuery = layer.FilterQuery;

            _title = layer.Title;
            _visible = layer.Visible;
            _minimumScale = layer.MinimumScale;
            _maximumScale = layer.MaximumScale;
        }

        public IFeatureLayer FeatureLayer
        {
            get { return _layer; }
        }

        public override IFeatureClass FeatureClass
        {
            get
            {
                if (_layer == null)
                {
                    return null;
                }

                return _layer.FeatureClass;
            }
        }
    }
}
