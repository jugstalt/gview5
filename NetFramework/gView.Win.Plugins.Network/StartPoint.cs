using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.Geometry;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Events;
using gView.Plugins.Network.Graphic;
using System.Threading.Tasks;

namespace gView.Plugins.Network
{
    [RegisterPlugInAttribute("457B8BC3-1F92-4512-BD09-9E6A870ADA93")]
    public class StartPoint : ITool
    {
        private IMapDocument _doc = null;
        private Module _module = null;

        #region ITool Member

        public string Name
        {
            get { return "Start Point"; }
        }

        public bool Enabled
        {
            get
            {
                if (_module == null || _module.SelectedNetworkFeatureClass == null)
                {
                    return false;
                }

                return true;
            }
        }

        public string ToolTip
        {
            get { return "Start Point"; }
        }

        public ToolType toolType
        {
            get { return ToolType.click; }
        }

        public object Image
        {
            get
            {
                return global::gView.Win.Plugins.Network.Properties.Resources.start_point;
            }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
                _module = Module.GetModule(_doc);
            }
        }

        async public Task<bool> OnEvent(object MapEvent)
        {
            if (_module == null ||
                _module.SelectedNetworkFeatureClass == null ||
                !(MapEvent is MapEventClick))
            {
                return false;
            }

            MapEventClick ev = (MapEventClick)MapEvent;
            Point p = new Point(ev.x, ev.y);
            double tol = 5 * ((IDisplay)ev.Map).MapScale / (96 / 0.0254);  // [m]
            Envelope env = new Envelope(ev.x - tol, ev.y - tol, ev.x + tol, ev.y + tol);

            SpatialFilter filter = new SpatialFilter();
            filter.Geometry = env;
            filter.FeatureSpatialReference = ((IDisplay)ev.Map).SpatialReference;
            filter.FilterSpatialReference = ((IDisplay)ev.Map).SpatialReference;
            filter.AddField("FDB_SHAPE");
            filter.AddField("FDB_OID");

            double dist = double.MaxValue;
            int n1 = -1;
            IPoint p1 = null;
            IFeatureCursor cursor = await _module.SelectedNetworkFeatureClass.GetNodeFeatures(filter);
            IFeature feature;
            while ((feature = await cursor.NextFeature()) != null)
            {
                double d = p.Distance(feature.Shape as IPoint);
                if (d < dist)
                {
                    dist = d;
                    n1 = feature.OID;
                    p1 = feature.Shape as IPoint;
                }
            }

            _module.RemoveNetworkStartGraphicElement((IDisplay)ev.Map);
            if (n1 != -1)
            {
                _module.StartNodeIndex = n1;
            }

            if (p1 != null)
            {
                ((IDisplay)ev.Map).GraphicsContainer.Elements.Add(new GraphicStartPoint(p1));
                _module.StartPoint = p1;
            }

            ((MapEvent)MapEvent).drawPhase = DrawPhase.Graphics;
            ((MapEvent)MapEvent).refreshMap = true;

            return true;
        }

        #endregion
    }
}
