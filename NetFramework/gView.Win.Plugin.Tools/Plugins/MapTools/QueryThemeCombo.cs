using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Globalisation;
using gView.Framework.system;
using gView.Framework.UI;
using gView.Plugins.MapTools.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools
{
    [RegisterPlugInAttribute("51A2CF81-E343-4c58-9A42-9207C8DFBC01")]
    public class QueryThemeCombo : gView.Framework.UI.ITool, gView.Framework.UI.IToolItem, IToolItemLabel
    {
        internal delegate void SelectedItemChangedEvent(string itemText);
        internal SelectedItemChangedEvent SelectedItemChanged = null;

        private IMapDocument _doc = null;
        private ToolStripComboBox _dropDown;
        private QueryThemes _queries = null;
        private QueryThemeMode _mode = QueryThemeMode.Default;

        public QueryThemeCombo()
        {
            _dropDown = new ToolStripComboBox();

            _dropDown.Items.Clear();
            _dropDown.Items.Add(new ModeItem(IdentifyMode.visible));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.selectable));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.all));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.topmost));
            _dropDown.SelectedIndex = 0;

            _dropDown.Width = 300;
            _dropDown.DropDownStyle = ComboBoxStyle.DropDownList;
            _dropDown.DropDown += new EventHandler(dropDown_DropDown);
            _dropDown.SelectedIndexChanged += new EventHandler(dropDown_SelectedIndexChanged);
        }

        #region DropDownEvents

        internal void RebuildCombo()
        {
            switch (_mode)
            {
                case QueryThemeMode.Default:
                    RebuildDefaultCombo();
                    break;
                case QueryThemeMode.Custom:
                    RebuildCustomCombo();
                    break;
            }
            if (_dropDown.SelectedIndex == -1 && _dropDown.Items.Count > 0)
            {
                _dropDown.SelectedIndex = 0;
            }
        }
        private void RebuildDefaultCombo()
        {
            ITocElement selectedElement = null;
            if (_dropDown.SelectedItem is TOCElementItem)
            {
                selectedElement = ((TOCElementItem)_dropDown.SelectedItem).Element;
            }

            _dropDown.Items.Clear();
            _dropDown.Items.Add(new ModeItem(IdentifyMode.visible));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.selectable));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.all));
            _dropDown.Items.Add(new ModeItem(IdentifyMode.topmost));

            if (_doc != null && _doc.FocusMap != null && _doc.FocusMap.TOC != null)
            {
                foreach (ITocElement element in _doc.FocusMap.TOC.Elements)
                {
                    foreach (ILayer layer in element.Layers)
                    {
                        if (layer == null)
                        {
                            continue;
                        }

                        if (layer.Class is IFeatureClass ||
                            layer.Class is IPointIdentify)
                        {
                            _dropDown.Items.Add(new TOCElementItem(element));
                            break;
                        }
                        //if (layer is IFeatureLayer)
                        //{
                        //    _dropDown.Items.Add(new TOCElementItem(element));
                        //    break;
                        //}
                    }
                }
            }

            if (_dropDown.SelectedItem == null)
            {
                for (int i = 0; i < _dropDown.Items.Count; i++)
                {
                    if (!(_dropDown.Items[i] is TOCElementItem))
                    {
                        continue;
                    }

                    if (((TOCElementItem)_dropDown.Items[i]).Element == selectedElement)
                    {
                        _dropDown.SelectedItem = _dropDown.Items[i];
                        break;
                    }
                }
            }

            using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1))
            {
                using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bm))
                {
                    _dropDown.DropDownWidth = _dropDown.Size.Width;
                    foreach (object obj in _dropDown.Items)
                    {
                        System.Drawing.SizeF size = gr.MeasureString(obj.ToString(), _dropDown.Font);

                        if (size.Width + 20 > _dropDown.DropDownWidth)
                        {
                            _dropDown.DropDownWidth = (int)size.Width + 20;
                        }
                    }
                }
            }
        }
        private void RebuildCustomCombo()
        {
            string selected = "";
            if (_dropDown.SelectedItem != null)
            {
                selected = _dropDown.SelectedItem.ToString();
            }

            _dropDown.Items.Clear();
            if (_queries == null)
            {
                return;
            }

            foreach (QueryTheme theme in _queries.Queries)
            {
                if (theme.Type == QueryTheme.NodeType.query)
                {
                    _dropDown.Items.Add(theme.Text);
                }
                else if (theme.Type == QueryTheme.NodeType.seperator)
                {
                    _dropDown.Items.Add("------------------------------");
                }
            }
            if (_dropDown.SelectedItem == null && _dropDown.Items.IndexOf(selected) != -1)
            {
                _dropDown.SelectedItem = selected;
            }

            using (System.Drawing.Bitmap bm = new System.Drawing.Bitmap(1, 1))
            {
                using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bm))
                {
                    _dropDown.DropDownWidth = _dropDown.Size.Width;
                    foreach (object obj in _dropDown.Items)
                    {
                        System.Drawing.SizeF size = gr.MeasureString(obj.ToString(), _dropDown.Font);

                        if (size.Width + 20 > _dropDown.DropDownWidth)
                        {
                            _dropDown.DropDownWidth = (int)size.Width + 20;
                        }
                    }
                }
            }
        }
        private void dropDown_DropDown(object sender, EventArgs e)
        {
            RebuildCombo();
        }
        void dropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_dropDown.SelectedItem == null)
            {
                return;
            }

            if (SelectedItemChanged != null)
            {
                SelectedItemChanged(_dropDown.SelectedItem.ToString());
            }
        }
        #endregion

        internal IdentifyMode Mode
        {
            get
            {
                if (_dropDown.SelectedItem is ModeItem)
                {
                    return ((ModeItem)_dropDown.SelectedItem).Mode;
                }

                return IdentifyMode.layer;
            }
        }
        internal List<ILayer> Layers
        {
            get
            {
                if (_dropDown.SelectedItem is TOCElementItem && ((TOCElementItem)_dropDown.SelectedItem).Element != null)
                {
                    return ((TOCElementItem)_dropDown.SelectedItem).Element.Layers;
                }
                return null;
            }
        }

        public List<IDatasetElement> QueryableDatasetElements
        {
            get
            {
                if (_doc == null || _doc.FocusMap == null)
                {
                    return null;
                }

                IMap map = _doc.FocusMap;

                List<IDatasetElement> layers;
                if (Mode == IdentifyMode.selectable)
                {
                    layers = map.SelectionEnvironment.SelectableElements;
                }
                else if (Mode == IdentifyMode.layer)
                {
                    if (this.Layers == null)
                    {
                        return null;
                    }

                    layers = new List<IDatasetElement>();
                    foreach (ILayer layer in this.Layers)
                    {
                        layers.Add(layer);
                    }
                }
                else
                {
                    if (map.TOC != null && map.TOC.Layers != null)
                    {
                        layers = new List<IDatasetElement>();
                        foreach (ILayer layer in map.TOC.Layers)
                        {
                            layers.Add(layer);
                        }
                    }
                    else
                    {
                        layers = map.MapElements;
                    }
                }

                // Service Themes hinzufügen...
                List<IDatasetElement> allQueryableElements = new List<IDatasetElement>();
                foreach (IDatasetElement element in layers)
                {
                    if (element == null)
                    {
                        continue;
                    }

                    if (element.Class is IFeatureClass ||
                        element.Class is IPointIdentify)
                    {
                        allQueryableElements.Add(element);
                    }
                    else if (element is IWebServiceLayer)
                    {
                        foreach (IWebServiceTheme theme in ((IWebServiceLayer)element).WebServiceClass.Themes)
                        {
                            if (theme.Class is IFeatureClass)
                            {
                                allQueryableElements.Add(theme);
                            }
                        }
                    }
                }

                if (Mode == IdentifyMode.visible)
                {
                    List<IDatasetElement> remove = new List<IDatasetElement>();
                    foreach (IDatasetElement element in allQueryableElements)
                    {
                        if (!(element is ILayer))
                        {
                            continue;
                        }

                        ILayer layer = element as ILayer;
                        if ((layer.Visible == false) ||
                            (layer.MinimumScale > 1 && layer.MinimumScale > map.Display.MapScale) ||
                            (layer.MaximumScale > 1 && layer.MaximumScale < map.Display.MapScale))
                        {
                            remove.Add(element);
                        }
                    }
                    foreach (IDatasetElement rem in remove)
                    {
                        allQueryableElements.Remove(rem);
                    }
                }

                return allQueryableElements;
            }
        }
        public string Text
        {
            get
            {
                if (_dropDown.SelectedItem == null)
                {
                    return "";
                }

                return _dropDown.SelectedItem.ToString();
            }
        }
        public QueryThemeMode ThemeMode
        {
            get { return _mode; }
            set { _mode = value; }
        }
        internal QueryThemes UserDefinedQueries
        {
            get { return _queries; }
            set { _queries = value; }
        }

        #region ITool Member

        public string Name
        {
            get { return "QueryThemeCombo"; }
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
            }
        }

        public Task<bool> OnEvent(object MapEvent)
        {
            return Task.FromResult(true);
        }

        #endregion

        #region IToolItem Member

        public ToolStripItem ToolItem
        {
            get { return _dropDown; }
        }

        #endregion

        #region IToolItemLabel Member

        public string Label
        {
            get { return LocalizedResources.GetResString("Text.Search", "Search:"); }
        }

        public ToolItemLabelPosition LabelPosition
        {
            get { return ToolItemLabelPosition.top; }
        }

        #endregion
    }
}
