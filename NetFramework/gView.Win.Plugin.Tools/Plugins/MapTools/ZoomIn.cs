using gView.Framework.Carto;
using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI.Events;
using System;
using System.Threading.Tasks;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("09007AFA-B255-4864-AC4F-965DF330BFC4")]
    public class ZoomIn : gView.Framework.UI.ITool
    {
        public ZoomIn()
        {

        }
        #region ITool Member

        public string Name
        {
            get
            {
                return LocalizedResources.GetResString("Tools.ZoomIn", "Zoom In");
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
                nav.setScale(nav.MapScale / 2.0, ev.maxX, ev.maxY);
            }
            else
            {
                nav.ZoomTo(ev.minX, ev.minY, ev.maxX, ev.maxY);
            }
            ev.refreshMap = true;

            return Task.FromResult(true);
        }
        public object Image
        {
            get
            {
                Buttons b = new Buttons();
                return b.imageList1.Images[4];
            }
        }
        #endregion
    }
}
