using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI.Events;
using System;
using System.Threading.Tasks;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("51D04E6F-A13E-40b6-BF28-1B8E7C24493D")]
    public class ZoomOut : gView.Framework.UI.ITool
    {
        public ZoomOut()
        {
        }
        #region ITool Member

        public string Name
        {
            get
            {
                return LocalizedResources.GetResString("Tools.ZoomOut", "Zoom Out");
            }
        }

        public bool Enabled
        {
            get
            {
                return true;
            }
        }

        public string ToolTip
        {
            get
            {
                return "";
            }
        }

        public gView.Framework.UI.ToolType toolType
        {
            get
            {
                return gView.Framework.UI.ToolType.rubberband;
            }
        }

        public void OnCreate(object hook)
        {

        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (!(MapEvent is MapEventRubberband))
            {
                return Task.FromResult(true);
            }

            MapEventRubberband ev = (MapEventRubberband)MapEvent;
            if (ev.Map == null)
            {
                return Task.FromResult(true);
            }

            if (!(ev.Map.Display is Display))
            {
                return Task.FromResult(true);
            }

            Display nav = (Display)ev.Map.Display;

            if (Math.Abs(ev.maxX - ev.minX) < 1e-5 ||
                Math.Abs(ev.maxY - ev.minY) < 1e-5)
            {
                nav.setScale(nav.MapScale * 2.0, ev.maxX, ev.maxY);
            }
            else
            {
                // Extent vergrößern
                Envelope env = new Envelope(ev.minX, ev.minY, ev.maxX, ev.maxY);
                Envelope mEnv = new Envelope(ev.Map.Display.Envelope);
                double area = env.Width * env.Height;
                double mArea = mEnv.Width * mEnv.Height;

                mEnv.Raise((mArea / area) * 100.0);
                nav.ZoomTo(mEnv.minx, mEnv.miny, mEnv.maxx, mEnv.maxy);
            }
            ev.refreshMap = true;

            return Task.FromResult(true);
        }
        public object Image
        {
            get
            {
                Buttons b = new Buttons();
                return b.imageList1.Images[5];
            }
        }
        #endregion
    }
}
