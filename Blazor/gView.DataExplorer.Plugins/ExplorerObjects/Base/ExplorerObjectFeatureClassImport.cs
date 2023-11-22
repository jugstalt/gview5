using gView.DataExplorer.Core.Services;
using gView.DataSources.Fdb.MSAccess;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.FDB;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base;

public abstract class ExplorerObjectFeatureClassImport<TParent, TObjectType> : 
                      ExplorerParentObject<TParent, TObjectType>
    where TParent : IExplorerObject
{
    protected AccessFDB? _fdb = null;
    protected string _dsname = "";
    protected IFeatureDataset? _dataset;
    private FeatureImportService? _import = null;

    public ExplorerObjectFeatureClassImport() : base() { }

    public ExplorerObjectFeatureClassImport(TParent parent)
       : base(parent, 0)
    {
    }

    async private Task ImportDatasetObject(object datasetObject, bool schemaOnly)
    {
        if (datasetObject is IFeatureDataset)
        {
            IFeatureDataset dataset = (IFeatureDataset)datasetObject;
            foreach (IDatasetElement element in await dataset.Elements())
            {
                if (element is IFeatureLayer)
                {
                    await ImportDatasetObject(((IFeatureLayer)element).FeatureClass, schemaOnly);
                }
            }
        }
        if (datasetObject is IFeatureClass)
        {
            if (_import == null)
            {
                _import = new FeatureImportService();
            }
            else
            {
                return;
            }
            _import.SchemaOnly = schemaOnly;
            FeatureClassImportProgressReporter reporter = await FeatureClassImportProgressReporter.Create(_import, (IFeatureClass)datasetObject);

            //FormTaskProgress progress = new FormTaskProgress(reporter, ImportAsync(datasetObject));
            //progress.Text = "Import Featureclass: " + ((IFeatureClass)datasetObject).Name;
            //progress.ShowDialog();
            _import = null;
        }
        //if (datasetObject is FeatureClassListViewItem)
        //{
        //    if (_import == null)
        //    {
        //        _import = new FDBImport();
        //    }
        //    else
        //    {
        //        MessageBox.Show("ERROR: Import already runnung");
        //        return;
        //    }
        //    _import.SchemaOnly = schemaOnly;
        //    FeatureClassImportProgressReporter reporter = await FeatureClassImportProgressReporter.Create(_import, ((FeatureClassListViewItem)datasetObject).FeatureClass);

        //    FormTaskProgress progress = new FormTaskProgress();
        //    progress.Text = "Import Featureclass: " + ((FeatureClassListViewItem)datasetObject).Text;
        //    progress.ShowProgressDialog(reporter, ImportAsync(datasetObject));
        //    _import = null;
        //}
    }

    async private Task ImportAsync(object element)
    {
        if (_fdb == null || _import == null)
        {
            return;
        }

        ISpatialIndexDef? sIndexDef = null;
        if (_fdb is AccessFDB)
        {
            sIndexDef = await _fdb.SpatialIndexDef(_dsname);
        }

        if (element is IFeatureClass)
        {
            // ToDo:
            //if (!await _import.ImportToNewFeatureclass(
            //    await _fdb.GetDataset(_dsname),
            //    ((IFeatureClass)element).Name,
            //    (IFeatureClass)element,
            //    null,
            //    true,
            //    sIndexDef))
            //{
            //    throw new Exception(_import.lastErrorMsg);
            //}
        }
        //else if (element is FeatureClassListViewItem)
        //{
        //    FeatureClassListViewItem item = element as FeatureClassListViewItem;
        //    if (item.FeatureClass == null)
        //    {
        //        return;
        //    }

        //    MSSpatialIndex msIndex = new MSSpatialIndex();
        //    msIndex.GeometryType = GeometryFieldType.MsGeometry;
        //    msIndex.SpatialIndexBounds = item.FeatureClass.Envelope;
        //    msIndex.Level1 = msIndex.Level2 = msIndex.Level3 = msIndex.Level4 = MSSpatialIndexLevelSize.LOW;

        //    if (!await _import.ImportToNewFeatureclass(
        //        _fdb,
        //        _dsname,
        //        item.TargetName,
        //        item.FeatureClass,
        //        item.ImportFieldTranslation,
        //        true,
        //        null,
        //        sIndexDef))
        //    {
        //        throw new Exception(_import.lastErrorMsg);
        //    }
        //}
    }
}

class FeatureClassImportProgressReporter : IProgressReporter
{
    private ProgressReport _report = new ProgressReport();
    private ICancelTracker _cancelTracker = new CancelTracker();

    private FeatureClassImportProgressReporter() { }

    async static public Task<FeatureClassImportProgressReporter> Create(FeatureImportService import, IFeatureClass source)
    {
        var reporter = new FeatureClassImportProgressReporter();

        if (import == null)
        {
            return reporter;
        }

        reporter._cancelTracker ??= import.CancelTracker;

        if (source != null)
        {
            reporter._report.featureMax = await source.CountFeatures();
        }

        import.ReportAction += new FeatureImportService.ReportActionEvent(reporter.import_ReportAction);
        import.ReportProgress += new FeatureImportService.ReportProgressEvent(reporter.import_ReportProgress);
        import.ReportRequest += new FeatureImportService.ReportRequestEvent(reporter.import_ReportRequest);

        return reporter;
    }

    void import_ReportRequest(FeatureImportService sender, RequestArgs args)
    {
        //args.Result = MessageBox.Show(
        //    args.Request,
        //    "Warning",
        //    args.Buttons,
        //    MessageBoxIcon.Warning);
    }

    void import_ReportProgress(FeatureImportService sender, int progress)
    {
        if (ReportProgress == null)
        {
            return;
        }

        _report.featureMax = Math.Max(_report.featureMax, progress);
        _report.featurePos = progress;

        ReportProgress(_report);
    }

    void import_ReportAction(FeatureImportService sender, string action)
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

    public event ProgressReporterEvent? ReportProgress;

    public ICancelTracker CancelTracker
    {
        get { return _cancelTracker; }
    }
    #endregion
}
