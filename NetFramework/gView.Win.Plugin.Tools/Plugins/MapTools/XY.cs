using gView.Framework.Carto;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Plugins.MapTools.Dialogs;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("2AC4447E-ACF3-453D-BB2E-72ECF0C8506E")]
    public class XY : ITool
    {
        IMapDocument _doc = null;

        #region ITool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.XY", "Zoom To Coordinate"); }
        }

        public bool Enabled
        {
            get { return true; }
        }

        public string ToolTip
        {
            get { return LocalizedResources.GetResString("Tools.XY", "Zoom To Coordinate"); }
        }

        public ToolType toolType
        {
            get { return ToolType.command; }
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.xy; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            if (_doc == null ||
                _doc.FocusMap == null ||
                _doc.FocusMap.Display == null)
            {
                return Task.FromResult(true);
            }

            FormXY dlg = new FormXY(_doc);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                IPoint p = dlg.GetPoint(_doc.FocusMap.Display.SpatialReference);

                _doc.FocusMap.Display.ZoomTo(
                    new Envelope(p.X - 1, p.Y - 1, p.X + 1, p.Y + 1));
                _doc.FocusMap.Display.MapScale = 1000;

                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
                }
            }

            return Task.FromResult(true);
        }

        #endregion
    }
}
