using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.UI;
using gView.Framework.XML;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace gView.Interoperability.ArcXML.UI.ImportAXL
{
    [gView.Framework.system.RegisterPlugIn("E5CDEE42-8E88-444f-A664-0A057DE680A0")]
    class LayerContextMenu : IDatasetElementContextMenuItem
    {
        private IMapDocument _doc = null;
        #region IDatasetElementContextMenuItem Member

        public string Name
        {
            get { return "Import Renderers from AXL"; }
        }

        public bool Enable(object element)
        {
            if (_doc == null || _doc.Application == null)
            {
                return false;
            }

            if (_doc.Application is IMapApplication &&
                    ((IMapApplication)_doc.Application).ReadOnly == true)
            {
                return false;
            }

            return
                (element is IFeatureLayer && ((IFeatureLayer)element).FeatureClass != null) ||
                (element is IGroupLayer);
        }

        public bool Visible(object element)
        {
            return Enable(element);
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = hook as IMapDocument;
            }
        }

        public Task<bool> OnEvent(object element, object dataset)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "AXL File(*.axl)|*.axl";

            if (dlg.ShowDialog() != DialogResult.OK)
            {
                return Task.FromResult(false);
            }

            try
            {
                XmlDocument axl = new XmlDocument();
                axl.Load(dlg.FileName);

                if (element is IFeatureLayer)
                {
                    FormSelectAXLLayer selectDlg = new FormSelectAXLLayer(axl, element as IFeatureLayer);
                    if (selectDlg.ShowDialog() != DialogResult.OK || selectDlg.SelectedNode == null)
                    {
                        return Task.FromResult(false);
                    }

                    XmlNode layerNode = selectDlg.SelectedNode;
                    SetLayernameAndScales(layerNode, element as IFeatureLayer);
                    SetRenderers(layerNode, element as IFeatureLayer);

                    if (_doc != null && _doc.Application is IMapApplication)
                    {
                        ((IMapApplication)_doc.Application).RefreshTOCElement((IDatasetElement)element);
                    }
                }
                else if (element is IGroupLayer)
                {
                    ParseGroupLayer(axl, element as IGroupLayer);
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");

                return Task.FromResult(false);
            }
        }

        public object Image
        {
            get { return global::gView.Win.Interoperability.ArcXML.UI.Properties.Resources.import; }
        }

        public int SortOrder
        {
            get { return 63; }
        }

        #endregion

        private void ParseGroupLayer(XmlDocument axl, IGroupLayer gLayer)
        {
            if (gLayer == null)
            {
                return;
            }

            foreach (ILayer layer in gLayer.ChildLayer)
            {
                if (layer is IGroupLayer)
                {
                    ParseGroupLayer(axl, layer as IGroupLayer);
                }
                else
                {
                    XmlNode layerNode = GetLayernode(axl, layer as IFeatureLayer);
                    if (layerNode == null)
                    {
                        continue;
                    }

                    SetLayernameAndScales(layerNode, layer as IFeatureLayer);
                    SetRenderers(layerNode, layer as IFeatureLayer);

                    if (_doc != null && _doc.Application is IMapApplication)
                    {
                        ((IMapApplication)_doc.Application).RefreshTOCElement(layer);
                    }
                }
            }
        }

        private XmlNode GetLayernode(XmlDocument axl, IFeatureLayer layer)
        {
            if (axl == null || layer == null)
            {
                return null;
            }

            foreach (XmlNode layerNode in axl.SelectNodes("ARCXML/CONFIG/MAP/LAYER[@type='featureclass']"))
            {
                if (layerNode.Attributes["name"] == null)
                {
                    continue;
                }

                XmlNode datasetNode = layerNode.SelectSingleNode("DATASET[@type]");
                if (datasetNode == null || datasetNode.Attributes["name"] == null)
                {
                    continue;
                }

                switch (datasetNode.Attributes["type"].Value)
                {
                    case "point":
                        if (layer.FeatureClass.GeometryType != GeometryType.Point)
                        {
                            continue;
                        }

                        break;
                    case "line":
                        if (layer.FeatureClass.GeometryType != GeometryType.Polyline)
                        {
                            continue;
                        }

                        break;
                    case "polygon":
                        if (layer.FeatureClass.GeometryType != GeometryType.Polygon)
                        {
                            continue;
                        }

                        break;
                }

                if (datasetNode.Attributes["name"].Value.ToLower() == layer.FeatureClass.Name.ToLower() ||
                    datasetNode.Attributes["name"].Value.Replace(".", "_").ToLower() == layer.FeatureClass.Name.ToLower())
                {
                    return layerNode;
                }
            }
            return null;
        }

        private void SetLayernameAndScales(XmlNode layerNode, IFeatureLayer fLayer)
        {
            if (_doc != null && _doc.FocusMap != null && _doc.FocusMap.TOC != null)
            {
                ITocElement tocElement = _doc.FocusMap.TOC.GetTOCElement(fLayer);
                tocElement.Name = layerNode.Attributes["name"].Value;
                if (layerNode.Attributes["visible"] != null)
                {
                    tocElement.LayerVisible = Convert.ToBoolean(layerNode.Attributes["visible"].Value);
                }
            }

            if (layerNode.Attributes["minscale"] != null)
            {
                string[] s = layerNode.Attributes["minscale"].Value.Split(':');
                double o;
                if (s.Length == 2)
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MinimumScale = o;
                    }
                }
                else
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MinimumScale = o * (96.0 / 0.254);
                    }
                }
            }
            if (layerNode.Attributes["maxscale"] != null)
            {
                string[] s = layerNode.Attributes["maxscale"].Value.Split(':');
                double o;
                if (s.Length == 2)
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MaximumScale = o;
                    }
                }
                else
                {
                    if (double.TryParse(s[1].Replace(".", ","), out o))
                    {
                        fLayer.MaximumScale = o * (96.0 / 0.254);
                    }
                }
            }
        }

        private void SetRenderers(XmlNode layerNode, IFeatureLayer fLayer)
        {
            fLayer.FeatureRenderer = null;
            fLayer.LabelRenderer = null;
            foreach (XmlNode child in layerNode.ChildNodes)
            {
                if (child.Name == "SIMPLERENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.SimpleRenderer(child);
                }
                else if (child.Name == "VALUEMAPRENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.ValueMapRenderer(child);
                }
                else if (child.Name == "SIMPLELABELRENDERER")
                {
                    fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child, fLayer.FeatureClass);
                }
                else if (child.Name == "GROUPRENDERER")
                {
                    fLayer.FeatureRenderer = ObjectFromAXLFactory.GroupRenderer(child);

                    foreach (XmlNode child2 in child.ChildNodes)
                    {
                        //if (child2.Name == "SIMPLERENDERER")
                        //{
                        //    fLayer.FeatureRenderer = ObjectFromAXLFactory.SimpleRenderer(child2);
                        //}
                        //else if (child2.Name == "VALUEMAPRENDERER")
                        //{
                        //    fLayer.FeatureRenderer = ObjectFromAXLFactory.ValueMapRenderer(child2);
                        //}
                        if (child2.Name == "SIMPLELABELRENDERER")
                        {
                            fLayer.LabelRenderer = ObjectFromAXLFactory.SimpleLabelRenderer(child2, fLayer.FeatureClass);
                        }
                    }
                }
            }
        }
    }
}
