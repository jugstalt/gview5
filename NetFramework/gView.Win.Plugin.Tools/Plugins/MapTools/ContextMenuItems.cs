using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Geometry;
using gView.Framework.Globalisation;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using gView.Framework.UI.Dialogs;
using gView.Plugins.MapTools.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.MapTools
{
    [gView.Framework.system.RegisterPlugIn("363E60E3-C223-4db2-8555-A6072E1E91D3")]
    public class ImportRendererContextMenuItem : IDatasetElementContextMenuItem
    {
        private IMapDocument _doc = null;
        #region IDatasetElementContextMenuItem Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.ImportRenderer", "Import Renderer & Properties..."); ; }
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

            if (element is IFeatureLayer)
            {
                return true;
            }

            return false;
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

        async public Task<bool> OnEvent(object element, object dataset)
        {
            if (!(element is IFeatureLayer))
            {
                MessageBox.Show("Item is not a featurelayer");
                return true;
            }
            if (((IFeatureLayer)element).FeatureClass == null)
            {
                MessageBox.Show("Item has not a valid featureclass!");
                return true;
            }

            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
            filters.Add(new OpenRendererFilter(((IFeatureLayer)element).FeatureClass.GeometryType));

            ExplorerDialog dlg = new ExplorerDialog("Open Featurelayer...", filters, true);
            dlg.MulitSelection = false;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                IExplorerObject exObject = dlg.ExplorerObjects[0];

                var instatnce = await exObject?.GetInstanceAsync();

                if (!(instatnce is ITocElement))
                {
                    return true;
                }

                IFeatureLayer source = ((ITocElement)instatnce).Layers[0] as IFeatureLayer;
                if (source == null)
                {
                    return true;
                }

                IFeatureLayer dest = (IFeatureLayer)element;

                Dialogs.FormImportRenderers dlg2 = new Dialogs.FormImportRenderers();
                if (dlg2.ShowDialog() == DialogResult.OK)
                {
                    if (dlg2.FeatureRenderer)
                    {
                        dest.FeatureRenderer = source.FeatureRenderer;
                    }

                    if (dlg2.LabelRenderer)
                    {
                        dest.LabelRenderer = source.LabelRenderer;
                    }

                    if (dlg2.SelectionRenderer)
                    {
                        dest.SelectionRenderer = source.SelectionRenderer;
                    }

                    if (dlg2.RenderScales)
                    {
                        dest.MinimumScale = source.MinimumScale;
                        dest.MaximumScale = source.MaximumScale;
                        if (dlg2.FeatureRenderer)
                        {
                            dest.MaxRefScaleFactor = source.MaxRefScaleFactor;
                        }
                    }

                    if (dlg2.LabelScales)
                    {
                        dest.MinimumLabelScale = source.MinimumLabelScale;
                        dest.MaximumLabelScale = source.MaximumLabelScale;
                        if (dlg2.LabelRenderer)
                        {
                            dest.MaxLabelRefScaleFactor = source.MaxLabelRefScaleFactor;
                        }
                    }

                    if (dlg2.FilterQuery)
                    {
                        dest.FilterQuery = source.FilterQuery;
                    }
                }

            }
            return true;
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.import; }
        }

        public int SortOrder
        {
            get { return 62; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("85B5404B-1B48-4252-AA61-84698826B841")]
    public class ZoomToLayer : IDatasetElementContextMenuItem
    {
        IMapDocument _doc = null;

        #region IDatasetElementContextMenuItem Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.ZoomToLayer", "Zoom To Layer"); }
        }

        public bool Enable(object element)
        {
            if (_doc == null)
            {
                return false;
            }

            if ((element is IFeatureLayer && ((IFeatureLayer)element).FeatureClass != null) ||
                    (element is IRasterLayer && ((IRasterLayer)element).RasterClass != null) ||
                    (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null))
            {
                return true;
            }

            return false;
        }

        public bool Visible(object element)
        {
            return true;
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object element, object dataset)
        {
            if (_doc == null || _doc.FocusMap == null || _doc.FocusMap.Display == null)
            {
                return Task.FromResult(true);
            }

            if (element is IFeatureLayer && ((IFeatureLayer)element).FeatureClass != null && ((IFeatureLayer)element).FeatureClass.Envelope != null)
            {
                IEnvelope envelope = ((IFeatureLayer)element).FeatureClass.Envelope;
                if (((IFeatureLayer)element).FeatureClass.SpatialReference != null && !((IFeatureLayer)element).FeatureClass.SpatialReference.Equals(_doc.FocusMap.Display.SpatialReference))
                {
                    IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, ((IFeatureLayer)element).FeatureClass.SpatialReference, _doc.FocusMap.Display.SpatialReference);
                    if (geom == null)
                    {
                        return Task.FromResult(true);
                    }

                    envelope = geom.Envelope;
                }
                _doc.FocusMap.Display.ZoomTo(envelope);
            }
            else if (element is IRasterLayer && ((IRasterLayer)element).RasterClass != null && ((IRasterLayer)element).RasterClass.Polygon != null)
            {
                IEnvelope envelope = ((IRasterLayer)element).RasterClass.Polygon.Envelope;
                if (((IRasterLayer)element).RasterClass.SpatialReference != null && !((IRasterLayer)element).RasterClass.SpatialReference.Equals(_doc.FocusMap.Display.SpatialReference))
                {
                    IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, ((IRasterLayer)element).RasterClass.SpatialReference, _doc.FocusMap.Display.SpatialReference);
                    if (geom == null)
                    {
                        return Task.FromResult(true);
                    }

                    envelope = geom.Envelope;
                }
                _doc.FocusMap.Display.ZoomTo(envelope);
            }
            else if (element is IWebServiceLayer && ((IWebServiceLayer)element).WebServiceClass != null && ((IWebServiceLayer)element).WebServiceClass.Envelope != null)
            {
                IEnvelope envelope = ((IWebServiceLayer)element).WebServiceClass.Envelope;
                if (((IWebServiceLayer)element).WebServiceClass.SpatialReference != null && !((IWebServiceLayer)element).WebServiceClass.SpatialReference.Equals(_doc.FocusMap.Display.SpatialReference))
                {
                    IGeometry geom = GeometricTransformerFactory.Transform2D(envelope, ((IWebServiceLayer)element).WebServiceClass.SpatialReference, _doc.FocusMap.Display.SpatialReference);
                    if (geom == null)
                    {
                        return Task.FromResult(true);
                    }

                    envelope = geom.Envelope;
                }
                _doc.FocusMap.Display.ZoomTo(envelope);
            }
            else
            {
                return Task.FromResult(true);
            }
            if (_doc.Application is IMapApplication)
            {
                ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return (new gView.Plugins.ExTools.Icons()).imageList2.Images[0]; }
        }

        public int SortOrder
        {
            get { return 12; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("f5233497-9e4c-43a9-bf8e-29b74048aefa")]
    public class MapProperties : IMapContextMenuItem
    {
        private IMapDocument _doc = null;

        #region IContextMenuTool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.MapProperties", "Map Properties"); }
        }

        public bool Enable(object element)
        {
            return element is IMap;
        }

        public bool Visible(object element)
        {
            return Enable(element);
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object element, object parent)
        {
            if (_doc == null || !(_doc.Application is IMapApplication) || !(element is IMap))
            {
                return Task.FromResult(true);
            }

            FormMapProperties dlg = new FormMapProperties(
                (IMapApplication)_doc.Application,
                (IMap)element,
                ((IMap)element).Display);

            dlg.ShowDialog();

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.document_properties; }
        }

        #endregion

        #region IOrder Member

        public int SortOrder
        {
            get { return 99; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("bbc9c72f-fca2-4e24-8807-5bf4a5df6098")]
    public class AddMap : IMapContextMenuItem
    {
        private IMapDocument _doc = null;

        #region IContextMenuTool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.AddMap", "Add Map"); }
        }

        public bool Enable(object element)
        {
            return element is IMap;
        }

        public bool Visible(object element)
        {
            return Enable(element);
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object element, object parent)
        {
            if (_doc != null)
            {
                int counter = _doc.Maps.Count() + 1;
                string name = LocalizedResources.GetResString("Tools.Map", "Map") + " " + (counter);

                while (_doc[name] != null)
                {
                    name = LocalizedResources.GetResString("Tools.Map", "Map") + " " + (++counter);
                }

                Map map = new Map();
                map.Name = name;

                _doc.AddMap(map);
            }

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.add_map; }
        }

        #endregion

        #region IOrder Member

        public int SortOrder
        {
            get { return 1; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("08c1943c-64e8-4ae4-99df-f6c4651b0171")]
    public class RemoveMap : IMapContextMenuItem
    {
        private IMapDocument _doc = null;

        #region IContextMenuTool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.RemoveMap", "Remove Map"); }
        }

        public bool Enable(object element)
        {
            return element is IMap;
        }

        public bool Visible(object element)
        {
            return Enable(element);
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object element, object parent)
        {
            if (_doc != null && element is IMap)
            {
                if (MessageBox.Show("Delete map " + ((IMap)element).Name, String.Empty, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _doc.RemoveMap((IMap)element);
                }
            }

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.remove_map; }
        }

        #endregion

        #region IOrder Member

        public int SortOrder
        {
            get { return 5; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("6ebaa3fa-d543-4d17-afa8-e7a3a1afe7ad")]
    public class MapDatasets : IMapContextMenuItem
    {
        private IMapDocument _doc = null;

        #region IContextMenuTool Member

        public string Name
        {
            get { return LocalizedResources.GetResString("Tools.MapDatasets", "Map Datasets"); }
        }

        public bool Enable(object element)
        {
            return element is IMap;
        }

        public bool Visible(object element)
        {
            return Enable(element);
        }

        public void OnCreate(object hook)
        {
            if (hook is IMapDocument)
            {
                _doc = (IMapDocument)hook;
            }
        }

        public Task<bool> OnEvent(object element, object parent)
        {
            if (_doc != null && element is IMap)
            {
                var map = (IMap)element;

                FormMapDatasets dlg = new FormMapDatasets(map);
                dlg.ShowDialog();

                if (_doc.Application is IMapApplication)
                {
                    ((IMapApplication)_doc.Application).RefreshTOC();
                }
            }

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.data_info; }
        }

        #endregion

        #region IOrder Member

        public int SortOrder
        {
            get { return 58; }
        }

        #endregion
    }

    [gView.Framework.system.RegisterPlugIn("a92d4950-4ffe-4d53-9789-aac2ee2512a1")]
    public class ChartContextMenuItem : IDatasetElementContextMenuItem
    {
        private IMapDocument _doc = null;

        #region IContextMenuTool Member

        public string Name
        {
            get { return "Chart"; }
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

            if (element is IFeatureLayer)
            {
                return true;
            }

            return false;
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

        public Task<bool> OnEvent(object element, object parent)
        {
            if (!(element is IFeatureLayer) || _doc == null || _doc.FocusMap == null)
            {
                return Task.FromResult(true);
            }

            if (_doc.Application is IGUIApplication)
            {
                FormChartWizard dlg = new FormChartWizard(_doc.FocusMap.Display, (IFeatureLayer)element);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    FormChart chart = new FormChart(dlg.ChartTitle, dlg.Series);
                    chart.DisplayMode = dlg.DisplayMode;
                    chart.ChartType = dlg.ChartType;

                    ((IGUIApplication)_doc.Application).AddDockableWindow(chart, String.Empty);
                }
            }

            return Task.FromResult(true);
        }

        public object Image
        {
            get { return gView.Win.Plugin.Tools.Properties.Resources.pie_diagram; }
        }

        #endregion

        #region IOrder Member

        public int SortOrder
        {
            get { return 28; }
        }

        #endregion
    }
}
