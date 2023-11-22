using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Framework.UI.Events;
using System.Threading.Tasks;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("E1C01E9D-8ADC-477b-BCD1-6B7BBA756D44")]
    public class ZoomOutStatic : gView.Framework.UI.ITool
    {
        #region ITool Members

        public string Name
        {
            get
            {
                return LocalizedResources.GetResString("Tools.ZoomOutStatic", "Zoom Out Static");
            }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return ""; }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get
            {
                Buttons dlg = new Buttons();
                return dlg.imageList1.Images[15];
            }
        }

        public void OnCreate(object hook)
        {

        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (!(MapEvent is MapEvent))
            {
                return Task.FromResult(true);
            }

            MapEvent ev = (MapEvent)MapEvent;
            if (ev.Map == null)
            {
                return Task.FromResult(true);
            }

            ev.Map.Display.MapScale *= 2.0;

            ev.refreshMap = true;

            return Task.FromResult(true);
        }

        #endregion
    }
}
