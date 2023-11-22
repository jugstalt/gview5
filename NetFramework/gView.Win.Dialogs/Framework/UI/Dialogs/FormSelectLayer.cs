using gView.Framework.Carto;
using gView.Framework.Data;
using System;
using System.Windows.Forms;

namespace gView.Framework.UI.Dialogs
{
    public partial class FormSelectLayer : Form
    {
        IMapDocument _doc;

        public FormSelectLayer(IMapDocument doc)
        {
            _doc = doc;
            if (_doc == null)
            {
                return;
            }

            InitializeComponent();
        }

        private void FormSelectLayer_Load(object sender, EventArgs e)
        {
            if (_doc == null || _doc.Maps == null)
            {
                return;
            }

            foreach (IMap map in _doc.Maps)
            {
                if (map == null)
                {
                    continue;
                }

                cmbMaps.Items.Add(new MapItem(map));

                if (map == _doc.FocusMap)
                {
                    cmbMaps.SelectedIndex = cmbMaps.Items.Count - 1;
                }
            }
        }

        private void cmbMaps_SelectedIndexChanged(object sender, EventArgs e)
        {
            MapItem item = cmbMaps.SelectedItem as MapItem;
            cmbLayers.Items.Clear();

            if (item == null || item.Map == null || item.Map.MapElements == null)
            {
                return;
            }

            foreach (IDatasetElement element in item.Map.MapElements)
            {
                if (!(element is IFeatureLayer))
                {
                    continue;
                }

                cmbLayers.Items.Add(new LayerItem(item.Map, element as ILayer));
            }
            if (cmbLayers.Items.Count > 0)
            {
                cmbLayers.SelectedIndex = 0;
            }
        }

        public ILayer SelectedLayer
        {
            get
            {
                LayerItem item = cmbLayers.SelectedItem as LayerItem;
                if (item == null)
                {
                    return null;
                }

                return item.Layer;
            }
        }

        public string SelectedLayerAlias
        {
            get
            {
                LayerItem item = cmbLayers.SelectedItem as LayerItem;
                if (item == null)
                {
                    return "";
                }

                return item.ToString();
            }
        }
    }

    internal class MapItem
    {
        IMap _map;

        public MapItem(IMap map)
        {
            _map = map;
        }

        public IMap Map
        {
            get { return _map; }
        }

        public override string ToString()
        {
            if (_map == null)
            {
                return "???";
            }

            return _map.Name;
        }
    }

    internal class LayerItem
    {
        ILayer _layer;
        string _text = "";

        public LayerItem(IMap map, ILayer layer)
        {
            _layer = layer;
            if (_layer == null)
            {
                return;
            }

            _text = _layer.Title;

            if (map != null && map.TOC != null)
            {
                ITocElement tocelement = map.TOC.GetTOCElement(layer);
                if (tocelement != null)
                {
                    _text = tocelement.Name;
                }
            }
        }

        public ILayer Layer
        {
            get { return _layer; }
        }

        public override string ToString()
        {
            return _text;
        }
    }
}