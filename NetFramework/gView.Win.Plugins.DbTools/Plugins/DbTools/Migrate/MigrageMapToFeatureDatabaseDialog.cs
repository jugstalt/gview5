using gView.DataSources.Fdb.MSAccess;
using gView.DataSources.Fdb.UI;
using gView.Framework.Carto;
using gView.Framework.Data;
using gView.Framework.Data.Filters;
using gView.Framework.FDB;
using gView.Framework.system;
using gView.Framework.system.UI;
using gView.Framework.UI;
using gView.Framework.UI.Controls.Filter;
using gView.Framework.UI.Dialogs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gView.Plugins.DbTools.Migrate
{
    public partial class MigrateMapToFeatureDatabaseDialog : Form
    {
        private IMapDocument _doc;
        private IMap _map;

        public MigrateMapToFeatureDatabaseDialog(IMapDocument doc, IMap map)
        {
            InitializeComponent();

            _doc = doc;
            _map = map;

            #region Filter
            //All Features
            //Selected Features
            //Features in actual extent
            cmbExport.Items.Add(new ExportMethodItem("All Features", null));

            if (map.Display.Envelope != null)
            {
                SpatialFilter dispFilter = new SpatialFilter();
                dispFilter.SubFields = "*";

                dispFilter.FilterSpatialReference = map.Display.SpatialReference;
                dispFilter.Geometry = map.Display.Envelope;
                dispFilter.SpatialRelation = spatialRelation.SpatialRelationIntersects;

                cmbExport.Items.Add(new ExportMethodItem("Features in actual extent", dispFilter));
            }

            if (cmbExport.SelectedIndex == -1)
            {
                cmbExport.SelectedIndex = 0;
            }
            #endregion
        }

        async private void btnSelect_Click(object sender, EventArgs e)
        {
            List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
            filters.Add(new OpenFeatureDatasetFilter());

            ExplorerDialog exDlg = new ExplorerDialog("Target Feature Dataset",
                filters,
                true);

            if (exDlg.ShowDialog() == DialogResult.OK &&
                exDlg.ExplorerObjects.Count == 1)
            {
                IExplorerObject parentObject = exDlg.ExplorerObjects[0];
                var instance = await parentObject.GetInstanceAsync();
                if (instance is IFeatureDataset &&
                    ((IDataset)instance).Database is IFeatureUpdater)
                {
                    _dataset = (IFeatureDataset)instance;
                }
                else
                {
                    MessageBox.Show("Can't determine target featureclass!");
                    return;
                }

                txtDatasetName.Text = _dataset.Database.ToString();
                txtDatasetLocation.Text = parentObject.FullName;
            }
        }

        async private void btnStart_Click(object sender, EventArgs e)
        {
            txtMap.Text = txtMap.Text.ToUpper().Replace(" ", "_").Replace("Ä", "AE").Replace("Ö", "OE").Replace("Ü", "UE").Replace("ß", "SS");

            _filter = ((ExportMethodItem)cmbExport.SelectedItem).QueryFilter;

            Map migMap = new Map();
            migMap.Name = txtMap.Text;
            migMap.Display.ReferenceScale = _map.Display.ReferenceScale;
            migMap.Display.MapUnits = _map.Display.MapUnits;
            migMap.Display.DisplayUnits = _map.Display.DisplayUnits;
            migMap.Display.SpatialReference = _map.Display.SpatialReference;

            _doc.AddMap(migMap);
            _doc.FocusMap = migMap;
            migMap.Display.ZoomTo(_map.Display.Envelope);

            List<string> migratedClassNames = new List<string>();
            foreach (IDatasetElement element in _map.MapElements)
            {
                if (!(element is IFeatureLayer) || element.Class == null)
                {
                    continue;
                }

                try
                {
                    if (element.Class is IFeatureClass)
                    {
                        IFeatureClass fc = (IFeatureClass)element.Class;
                        if (!migratedClassNames.Contains(fc.Name))
                        {
                            await ExportDatasetObject(fc);
                            migratedClassNames.Add(fc.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show(ex.Message + "\n\nDo you want to continue?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) != DialogResult.OK)
                    {
                        break;
                    }
                }
                IDatasetElement destDatasetElement = await _dataset.Element((txtMap.Text + "_" + element.Class.Name).Replace(".", "_"));

                if (destDatasetElement != null)
                {
                    IFeatureLayer sourceLayer = (IFeatureLayer)element;

                    IFeatureLayer layer = LayerFactory.Create(destDatasetElement.Class) as IFeatureLayer;
                    if (layer != null)
                    {
                        var cloneOptions = new CloneOptions(migMap.Display, false);
                        if (sourceLayer.FeatureRenderer != null)
                        {
                            layer.FeatureRenderer = sourceLayer.FeatureRenderer.Clone(cloneOptions) as IFeatureRenderer;
                        }

                        if (sourceLayer.LabelRenderer != null)
                        {
                            layer.LabelRenderer = sourceLayer.LabelRenderer.Clone(cloneOptions) as ILabelRenderer;
                        }

                        if (sourceLayer.SelectionRenderer != null)
                        {
                            layer.SelectionRenderer = sourceLayer.SelectionRenderer.Clone(cloneOptions) as IFeatureRenderer;
                        }

                        layer.MinimumLabelScale = sourceLayer.MinimumLabelScale;
                        layer.MaximumLabelScale = sourceLayer.MaximumLabelScale;
                        layer.MinimumScale = sourceLayer.MinimumScale;
                        layer.MaximumScale = sourceLayer.MaximumScale;
                        layer.Visible = sourceLayer.Visible;
                    }
                    layer.SID = sourceLayer.SID;

                    migMap.AddLayer(layer);

                    ITocElement tocElement = migMap.TOC.GetTOCElement(layer);
                    ITocElement sourceTocElement = _map.TOC.GetTOCElement(sourceLayer);
                    if (tocElement != null && sourceTocElement != null)
                    {
                        tocElement.Name = sourceTocElement.Name;
                    }
                }
            }

            if (_doc.Application is IMapApplication)
            {
                await ((IMapApplication)_doc.Application).RefreshActiveMap(DrawPhase.All);
            }
        }

        #region Export

        //private FeatureImport _export = null;
        private FDBImport _fdbExport = null;
        private IFeatureDataset _dataset = null;
        private IQueryFilter _filter = null;

        async private Task ExportDatasetObject(object datasetObject)
        {
            if (_dataset.Database is AccessFDB)
            {
                await ExportDatasetObject_fdb(datasetObject);
            }
            else
            {
                MessageBox.Show("Can't migrate to selected dataset! Only gView Feature Database Dataset possible.");
            }
        }

        #region FDB
        async private Task ExportDatasetObject_fdb(object datasetObject)
        {
            if (datasetObject is IFeatureDataset)
            {
                IFeatureDataset dataset = (IFeatureDataset)datasetObject;
                foreach (IDatasetElement element in await dataset.Elements())
                {
                    if (element is IFeatureLayer)
                    {
                        await ExportDatasetObject(((IFeatureLayer)element).FeatureClass);
                    }
                }
            }
            if (datasetObject is IFeatureClass)
            {
                if (_fdbExport == null)
                {
                    _fdbExport = new FDBImport();
                }
                else
                {
                    MessageBox.Show("ERROR: Import already runnung");
                    return;
                }

                FeatureClassImportProgressReporter reporter = await FeatureClassImportProgressReporter.CreateAsync(_fdbExport, (IFeatureClass)datasetObject);

                FormTaskProgress progress = new FormTaskProgress(reporter, ExportAsync_fdb(datasetObject));
                progress.Text = "Export Features: " + ((IFeatureClass)datasetObject).Name;
                progress.ShowDialog();
                _fdbExport = null;
            }
        }
        async private Task ExportAsync_fdb(object element)
        {
            if (_fdbExport == null)
            {
                return;
            }

            List<IQueryFilter> filters = null;
            if (_filter != null)
            {
                filters = new List<IQueryFilter>();
                filters.Add(_filter);
            }

            if (element is IFeatureClass)
            {
                if (!await _fdbExport.ImportToNewFeatureclass(
                    _dataset.Database as IFeatureDatabase,
                    _dataset.DatasetName,
                    txtMap.Text + "_" + ((IFeatureClass)element).Name,
                    (IFeatureClass)element,
                    null,
                    true,
                    filters))
                {
                    MessageBox.Show("Featureclass: " + ((IFeatureClass)element).Name + "\n" + _fdbExport.lastErrorMsg);
                }
            }
        }
        #endregion

        class FeatureClassImportProgressReporter : IProgressReporter
        {
            private ProgressReport _report = new ProgressReport();
            private ICancelTracker _cancelTracker = null;

            private FeatureClassImportProgressReporter()
            {

            }

            async static public Task<FeatureClassImportProgressReporter> CreateAsync(object import, IFeatureClass source)
            {
                var reporter = new FeatureClassImportProgressReporter();

                if (import == null)
                {
                    return null;
                }

                if (import is FDBImport)
                {
                    reporter._cancelTracker = ((FDBImport)import).CancelTracker;

                    if (source != null)
                    {
                        reporter._report.featureMax = await source.CountFeatures();
                    }
                    ((FDBImport)import).ReportAction += new FDBImport.ReportActionEvent(reporter.FeatureClassImportProgressReporter_ReportAction);
                    ((FDBImport)import).ReportProgress += new FDBImport.ReportProgressEvent(reporter.FeatureClassImportProgressReporter_ReportProgress);
                    ((FDBImport)import).ReportRequest += new FDBImport.ReportRequestEvent(reporter.FeatureClassImportProgressReporter_ReportRequest);
                }
                if (import is FeatureImport)
                {
                    reporter._cancelTracker = ((FeatureImport)import).CancelTracker;

                    if (source != null)
                    {
                        reporter._report.featureMax = await source.CountFeatures();
                    }
                    ((FeatureImport)import).ReportAction += new FeatureImport.ReportActionEvent(reporter.import_ReportAction);
                    ((FeatureImport)import).ReportProgress += new FeatureImport.ReportProgressEvent(reporter.import_ReportProgress);
                    ((FeatureImport)import).ReportRequest += new FeatureImport.ReportRequestEvent(reporter.import_ReportRequest);
                }

                return reporter;
            }

            #region FDB
            void FeatureClassImportProgressReporter_ReportRequest(FDBImport sender, gView.DataSources.Fdb.UI.RequestArgs args)
            {
                args.Result = MessageBox.Show(
                    args.Request,
                    "Warning",
                    args.Buttons,
                    MessageBoxIcon.Warning);
            }

            void FeatureClassImportProgressReporter_ReportProgress(FDBImport sender, int progress)
            {
                if (ReportProgress == null)
                {
                    return;
                }

                _report.featureMax = Math.Max(_report.featureMax, progress);
                _report.featurePos = progress;

                ReportProgress(_report);
            }

            void FeatureClassImportProgressReporter_ReportAction(FDBImport sender, string action)
            {
                if (ReportProgress == null)
                {
                    return;
                }

                _report.featurePos = 0;
                _report.Message = action;

                ReportProgress(_report);
            }
            #endregion

            void import_ReportRequest(FeatureImport sender, gView.Framework.system.UI.RequestArgs args)
            {
                args.Result = MessageBox.Show(
                    args.Request,
                    "Warning",
                    args.Buttons,
                    MessageBoxIcon.Warning);
            }

            void import_ReportProgress(FeatureImport sender, int progress)
            {
                if (ReportProgress == null)
                {
                    return;
                }

                _report.featureMax = Math.Max(_report.featureMax, progress);
                _report.featurePos = progress;

                ReportProgress(_report);
            }

            void import_ReportAction(FeatureImport sender, string action)
            {
                if (ReportProgress == null)
                {
                    return;
                }

                _report.featurePos = 0;
                _report.Message = action;

                ReportProgress(_report);
            }

            #region IProgressReporter Member

            public event ProgressReporterEvent ReportProgress;

            public ICancelTracker CancelTracker
            {
                get { return _cancelTracker; }
            }
            #endregion
        }

        #endregion

        #region Item Classes
        private class ExportMethodItem
        {
            private string _text;
            private IQueryFilter _filter;
            public ExportMethodItem(string text, IQueryFilter filter)
            {
                _text = text;
                _filter = filter;
            }

            public IQueryFilter QueryFilter
            {
                get { return _filter; }
            }

            public override string ToString()
            {
                return _text;
            }
        }
        #endregion

        private void MigrateMapToFeatureDatabaseDialog_Shown(object sender, EventArgs e)
        {
            #region Featureklassen mit gleichen Namen suchen
            List<IDatasetElement> elements = _map.MapElements;
            for (int i = 0; i < elements.Count; i++)
            {
                IDatasetElement elementI = elements[i];
                if (!(elementI.Class is IFeatureClass))
                {
                    continue;
                }

                for (int j = i + 1; j < elements.Count; j++)
                {
                    IDatasetElement elementJ = elements[j];
                    if (!(elementJ.Class is IFeatureClass))
                    {
                        continue;
                    }

                    if (elementI.Class.Name == elementJ.Class.Name)
                    {
                        IFeatureClass fcI = (IFeatureClass)elementI.Class, fcJ = (IFeatureClass)elementJ.Class;
                        if (fcI.Dataset.ConnectionString.ToLower() == fcJ.Dataset.ConnectionString.ToLower())
                        {
                            continue;
                        }

                        MessageBox.Show("There is more than one featureclass with the name '" + elementI.Class.Name + "'!\nEvery featureclass must have different name to run this function...", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Close();
                        return;
                    }
                }
            }
            #endregion
        }


    }
}
