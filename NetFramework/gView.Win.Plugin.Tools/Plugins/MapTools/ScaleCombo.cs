using gView.Framework.Carto;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("03058244-16EE-44dd-B185-5522281498F5")]
    public class ScaleCombo : gView.Framework.UI.ITool, gView.Framework.UI.IToolItem
    {
        private IMapDocument _doc = null;
        private ToolStripComboBox _dropDown;
        public ScaleCombo()
        {
            _dropDown = new ToolStripComboBox();
            _dropDown.Items.Add("1:1000");
            _dropDown.Items.Add("1:5000");
            _dropDown.Items.Add("1:10000");
            _dropDown.Items.Add("1:50000");
            _dropDown.Items.Add("1:100000");
            _dropDown.Items.Add("1:500000");
            _dropDown.Items.Add("1:1000000");
            _dropDown.Visible = true;
            _dropDown.Width = 100;

            _dropDown.KeyDown += new KeyEventHandler(dropDown_KeyDown);
            _dropDown.SelectedIndexChanged += new EventHandler(dropDown_SelectedIndexChanged);
        }

        private void dropDown_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                int scale = GetScaleFromText(_dropDown.Text);
                if (scale <= 0)
                {
                    return;
                }

                int index = 0;
                foreach (string txt in _dropDown.Items)
                {
                    int s = GetScaleFromText(txt);
                    if (s > scale)
                    {
                        _dropDown.Items.Insert(index, "1:" + scale);
                        break;
                    }
                    index++;
                }
                SetMapScale(scale);
            }
        }

        private void dropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetMapScale(GetScaleFromText(_dropDown.Text));
        }

        private delegate void MapScaleChangedCallback(IDisplay display);
        private void doc_MapScaleChanged(IDisplay display)
        {
            if (_dropDown == null || _dropDown.Owner == null)
            {
                return;
            }

            if (_dropDown.Owner.InvokeRequired)
            {
                MapScaleChangedCallback d = new MapScaleChangedCallback(doc_MapScaleChanged);
                _dropDown.Owner.Invoke(d, new object[] { display });
            }
            else
            {
                if (display == null)
                {
                    _dropDown.Text = "";
                    _dropDown.Enabled = false;
                    return;
                }
                else if (display.MapUnits == GeoUnits.Unknown)
                {
                    _dropDown.Text = "";
                    _dropDown.Enabled = false;
                    return;
                }
                try
                {
                    _dropDown.Enabled = true;
                    _dropDown.Text = "1:" + (int)(display.MapScale + 0.5);
                }
                catch { }
            }
        }

        private void SetMapScale(int scale)
        {
            if (_doc == null)
            {
                return;
            }

            if (_doc.FocusMap == null || _doc.Application == null)
            {
                return;
            }

            if (scale <= 0 || scale == (int)((IDisplay)_doc.FocusMap).MapScale)
            {
                return;
            }

            _doc.FocusMap.Display.MapScale = scale;
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }
        }
        private int GetScaleFromText(string txt)
        {
            if (_doc == null)
            {
                return 0;
            }

            if (_doc.FocusMap == null)
            {
                return 0;
            }

            int pos = txt.IndexOf(":");
            if (pos != 0)
            {
                txt = txt.Substring(pos + 1, txt.Length - pos - 1);
            }
            try
            {
                double scale = Convert.ToDouble(txt);
                return (int)Math.Abs(scale);
            }
            catch
            {
                return 0;
                //return (int)((IDisplay)_doc.FocusMap).mapScale;
            }
        }
        #region ITool Members

        public string Name
        {
            get { return "ScaleCombo"; }
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
            get { return null; }
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;

                if (_doc is IMapDocumentEvents)
                {
                    ((IMapDocumentEvents)_doc).MapScaleChanged += new MapScaleChangedEvent(doc_MapScaleChanged);
                }
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Members


        public ToolStripItem ToolItem
        {
            get
            {
                return _dropDown;
            }
        }

        #endregion
    }
}
