using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.UI;
using gView.Interoperability.GeoServices.Rest.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace gView.Win.Plugin.Tools.Plugins.MapTools.Dialogs
{
    public partial class FormMatchGeoserviceLayerIds : Form
    {
        private IMap _map;

        public FormMatchGeoserviceLayerIds(IMap map)
        {
            _map = map;

            InitializeComponent();
        }

        private void FormMatchGeoserviceLayerIds_Load(object sender, EventArgs e)
        {
            try
            {
                var toc = _map?.TOC;
                if (toc == null || toc.Elements == null)
                {
                    return;
                }

                foreach (var tocElement in toc.Elements)
                {
                    var layer = tocElement.Layers?.FirstOrDefault();
                    if (layer == null)
                    {
                        continue;
                    }

                    if (tocElement.Layers.Count > 1)
                    {
                        throw new Exception($"More than one layer assigned to TOC Element {tocElement.Name}. This is not allowed with this Method.");
                    }

                    lstLayers.Items.Add(new TocElementListViewitem(tocElement, new string[]
                    {
                        String.Empty,
                        TocElementFullname(tocElement),
                        layer.ID.ToString(),
                        String.Empty
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        async private void btnMatch_Click(object sender, EventArgs e)
        {
            try
            {
                var url = txtMapServerUrl.Text + "/layers?f=pjson";

                List<ListViewItem> remove = new List<ListViewItem>();
                foreach (ListViewItem item in lstLayers.Items)
                {
                    if (item.SubItems[0].Text?.ToLower() == "missing")
                    {
                        remove.Add(item);
                    }
                    else
                    {
                        item.SubItems[0].Text = item is TocElementListViewitem && ((TocElementListViewitem)item).IsHidden == true ? "hidden" : "Not matched!!";
                        item.SubItems[3].Text = "";
                    }
                }
                foreach (var item in remove)
                {
                    lstLayers.Items.Remove(item);
                }

                var maxId = 0;
                using (var client = new WebClient())
                {
                    var jsonBytes = await client.DownloadDataTaskAsync(url);
                    var json = Encoding.UTF8.GetString(jsonBytes);

                    var serviceLayers = JsonConvert.DeserializeObject<JsonLayers>(json);

                    if (serviceLayers != null)
                    {
                        maxId = serviceLayers.Layers.Max(l => l.Id);

                        foreach (var serviceLayer in serviceLayers.Layers)
                        {
                            var layerFullname = LayerFullname(serviceLayers, serviceLayer);

                            var listViewItem = GetItem(layerFullname);
                            if (listViewItem != null)
                            {
                                listViewItem.SubItems[0].Text = "OK";
                                listViewItem.SubItems[3].Text = serviceLayer.Id.ToString();
                            }
                            else
                            {
                                //string errMessage = $"Layer not found {layerFullname}";
                                lstLayers.Items.Add(new ListViewItem(new string[] { "missing", "", "", serviceLayer.Id.ToString(), layerFullname }));
                            }
                        }
                    }
                }

                foreach (ListViewItem item in lstLayers.Items)
                {
                    if (item.SubItems[0].Text?.ToLower() == "hidden")
                    {
                        item.SubItems[3].Text = (++maxId).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateUI();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (btnApply.Enabled == false)
            {
                return;
            }

            foreach (ListViewItem item in lstLayers.Items)
            {

                if (item is TocElementListViewitem)
                {
                    if (int.TryParse(item.SubItems[3].Text, out int geoServiceLayerId))
                    {
                        ((TocElementListViewitem)item).TOCElement.Layers.First().ID = geoServiceLayerId;
                    }
                }
            }

            if (_map is IRefreshSequences)
            {
                ((IRefreshSequences)_map).RefreshSequences();
            }

            this.Close();
        }

        #region UI Helper

        private void UpdateUI()
        {
            bool enableApplyButton =
                lstLayers.Items.Count > 0;

            List<int> ids = new List<int>();
            foreach (ListViewItem item in lstLayers.Items)
            {
                if (int.TryParse(item.SubItems[3].Text, out int layerId) == true)
                {
                    if (ids.Contains(layerId))  // Must be unique
                    {
                        enableApplyButton = false;
                        break;
                    }
                    ids.Add(layerId);
                }
                else
                {
                    enableApplyButton = false;
                    break;
                }
            }

            btnApply.Enabled = enableApplyButton;
        }

        #endregion

        #region Helper

        private string TocElementFullname(ITocElement tocElement)
        {
            string name = tocElement.Name;

            while (tocElement.ParentGroup != null)
            {
                name = tocElement.ParentGroup.Name + "/" + name;
                tocElement = tocElement.ParentGroup;
            }

            return name;
        }

        private string LayerFullname(JsonLayers layers, JsonLayer layer)
        {
            string name = layer.Name;

            while (layer != null && layer.ParentLayer != null)
            {
                layer = layers.LayerById(layer.ParentLayer.Id);
                if (layer != null)
                {
                    if (layer.Type == "Annotation Layer" || layer.Type == "Annotation SubLayer")
                    {
                        name = $"{layer.Name}/{layer.Name} ({name})";
                    }
                    else
                    {
                        name = $"{layer.Name}/{name}";
                    }
                }
            }

            return name;
        }

        private ListViewItem GetItem(string layerFullname)
        {
            foreach (ListViewItem item in lstLayers.Items)
            {
                if (item.SubItems[1].Text == layerFullname)
                {
                    return item;
                }
            }

            return null;
        }

        #endregion

        #region Item Classes

        private class TocElementListViewitem : ListViewItem
        {
            public TocElementListViewitem(ITocElement tocElement, string[] subItems)
                : base(subItems)
            {
                this.TOCElement = tocElement;
            }

            public ITocElement TOCElement { get; private set; }

            public bool IsHidden
            {
                get
                {
                    var parent = this.TOCElement?.ParentGroup;
                    while (parent != null)
                    {
                        IGroupLayer groupLayer = parent.Layers.FirstOrDefault() as IGroupLayer;
                        if (groupLayer != null && groupLayer.MapServerStyle == MapServerGrouplayerStyle.Checkbox)
                        {
                            return true;
                        }
                        parent = parent.ParentGroup;
                    }

                    return false;
                }
            }
        }

        #endregion
    }
}
