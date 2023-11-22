using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Core.Services;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.system;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.FileSystem;

[RegisterPlugIn("458E62A0-4A93-45cf-B14D-2F958D67E522")]
public class DirectoryObject : ExplorerParentObject<IExplorerObject>,
                               IExplorerObject,
                               IExplorerObjectCommandParameters,
                               ISerializableExplorerObject,
                               IExplorerObjectDeletable,
                               IExplorerObjectRenamable,
                               IExplorerObjectCreatable
{
    string _path = "";

    public DirectoryObject() : base() { }
    public DirectoryObject(IExplorerObject parent, string path)
        : base(parent, 1)
    {
        _path = path;
    }

    #region IExplorerObject Members

    public string Filter
    {
        get { return ""; }
    }

    public string Name
    {
        get
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(_path);
                return di.Name;
            }
            catch { return ""; }
        }
    }

    public string FullName
    {
        get { return _path; }

    }

    public string? Type
    {
        get { return "Directory"; }
    }

    public string Icon => "basic:folder-white";

    public Task<object?> GetInstanceAsync()
    {
        return Task.FromResult<object?>(null);
    }

    public IExplorerObject? CreateInstanceByFullName(string FullName)
    {
        try
        {
            if (!new DirectoryInfo(FullName).Exists)
            {
                return null;
            }
        }
        catch { return null; }
        return new DirectoryObject(this, FullName);
    }

    #endregion

    #region IExplorerObjectCommandParameters Members

    public Dictionary<string, string> Parameters
    {
        get
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("Path", _path + @"\");

            return parameters;
        }
    }

    #endregion

    #region IExplorerParentObject Member

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        List<IExplorerObject> childs = await Refresh(this, FullName);
        if (childs == null)
        {
            return false;
        }

        foreach (IExplorerObject child in childs)
        {
            AddChildObject(child);
        }

        return true;
    }

    #endregion

    async internal static Task<List<IExplorerObject>> Refresh(IExplorerObject parent, string FullName)
    {
        List<IExplorerObject> childs = new List<IExplorerObject>();

        try
        {
            foreach (string subdir in Directory.GetDirectories(FullName))
            {
                DirectoryInfo di = new DirectoryInfo(subdir);
                childs.Add(new DirectoryObject(parent, di.FullName));
            }
        }
        catch /*(Exception ex)*/
        {
            //System.Windows.Forms.MessageBox.Show(ex.Message);
            //return null;
            throw;
        }

        PlugInManager manager = new PlugInManager();

        foreach (var exObjectType in manager.GetPlugins(Framework.system.Plugins.Type.IExplorerFileObject))
        {
            var exObj = manager.CreateInstance<IExplorerFileObject>(exObjectType);
            if (exObj == null)
            {
                continue;
            }

            foreach (string filter in ((IExplorerFileObject)exObj).Filter.Split('|'))
            {
                foreach (string file in Directory.GetFiles(FullName, filter))
                {
                    FileInfo fi = new FileInfo(file);
                    IExplorerFileObject? obj = await ((IExplorerFileObject)exObj).CreateInstance(parent, fi.FullName);
                    if (obj == null)
                    {
                        continue;
                    }

                    childs.Add(obj);
                }
            }
        }

        return childs;
    }

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult(cache[FullName]);
        }

        try
        {
            DirectoryInfo di = new DirectoryInfo(FullName);
            if (!di.Exists)
            {
                return Task.FromResult<IExplorerObject?>(null);
            }

            DirectoryObject dObject = new DirectoryObject(this, FullName);
            cache?.Append(dObject);
            return Task.FromResult<IExplorerObject?>(dObject);
        }
        catch
        {
            return Task.FromResult<IExplorerObject?>(null);
        }
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        try
        {
            DirectoryInfo di = new DirectoryInfo(_path);
            di.Delete();
            if (ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }

            return Task.FromResult(true);
        }
        catch /*(Exception ex)*/
        {
            //System.Windows.Forms.MessageBox.Show("ERROR: " + ex.Message);
            //return Task.FromResult(false);
            throw;
        }
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

    public Task<bool> RenameExplorerObject(string newName)
    {
        try
        {
            DirectoryInfo di = new DirectoryInfo(_path);
            string newPath = di.Parent?.FullName + @"\" + newName;

            Directory.Move(_path, newPath);

            _path = newPath;

            if (ExplorerObjectRenamed != null)
            {
                ExplorerObjectRenamed(this);
            }

            return Task.FromResult(true);
        }
        catch/* (Exception ex)*/
        {
            //System.Windows.Forms.MessageBox.Show("ERROR: " + ex.Message);
            //return Task.FromResult(false);
            throw;
        }
    }

    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        if (parentExObject is DirectoryObject ||
            parentExObject is DriveObject)
        {
            return true;
        }

        return false;
    }

    public async Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope scope,
                                                            IExplorerObject parentExObject)
    {
        var model = await scope.ToExplorerScopeService().ShowModalDialog(
            typeof(Razor.Components.Dialogs.InputBoxDialog),
            "Create",
            new InputBoxModel()
            {
                Value = "",
                Icon = Icon,
                Name = Type ?? string.Empty,
                Label = "Name",
                Prompt = "Enter new directory name"
            });

        if (model != null)
        {
            string newName = model.Value.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                return null;
            }

            DirectoryInfo di = new DirectoryInfo(Path.Combine(parentExObject.FullName, newName));
            if (di.Exists)
            {
                throw new GeneralException($"Directory {newName} alread exists!");
            }
            di.Create();

            return new DirectoryObject(parentExObject, di.FullName);
        }

        return null;
    }

    #endregion

    #region Import

    private FeatureImportService? _import = null;
    private IFeatureDataset? _dataset = null;

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
                //MessageBox.Show("ERROR: Import already runnung");
                return;
            }
            _import.SchemaOnly = schemaOnly;

            FeatureClassImportProgressReporter reporter = await FeatureClassImportProgressReporter.Create(_import, (IFeatureClass)datasetObject);

            // ToDo:
            //FormTaskProgress progress = new FormTaskProgress(reporter, ImportAsync(datasetObject));
            //progress.Text = "Import Featureclass: " + ((IFeatureClass)datasetObject).Name;
            //progress.ShowDialog();
            //_import = null;
        }

        // ToDo:
        //if (datasetObject is FeatureClassListViewItem)
        //{
        //    if (_import == null)
        //    {
        //        _import = new FeatureImportService();
        //    }
        //    else
        //    {
        //        //MessageBox.Show("ERROR: Import already runnung");
        //        return;
        //    }
        //    _import.SchemaOnly = schemaOnly;

        //    FeatureClassImportProgressReporter reporter = await FeatureClassImportProgressReporter.Create(_import, ((FeatureClassListViewItem)datasetObject).FeatureClass);


        //    FormTaskProgress progress = new FormTaskProgress(reporter, ImportAsync(datasetObject));
        //    progress.Text = "Import Featureclass: " + ((FeatureClassListViewItem)datasetObject).Text;
        //    progress.ShowDialog();
        //    _import = null;
        //}
    }

    async private Task ImportAsync(object element)
    {
        if (_import == null)
        {
            return;
        }

        if (element is IFeatureClass && _dataset != null)
        {
            if (!await _import.ImportToNewFeatureclass(
                _dataset,
                ((IFeatureClass)element).Name,
                (IFeatureClass)element,
                null,
                true))
            {
                //MessageBox.Show(_import.lastErrorMsg);
                throw new Exception(_import.lastErrorMsg);
            }
        }
        // ToDo
        //else if (element is FeatureClassListViewItem)
        //{
        //    FeatureClassListViewItem item = element as FeatureClassListViewItem;
        //    if (item.FeatureClass == null)
        //    {
        //        return;
        //    }

        //    if (!await _import.ImportToNewFeatureclass(
        //        _dataset,
        //        item.TargetName,
        //        item.FeatureClass,
        //        item.ImportFieldTranslation,
        //        true))
        //    {
        //        //MessageBox.Show(_import.lastErrorMsg);
        //        throw new Exception(_import.lastErrorMsg);
        //    }
        //}
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

            reporter._cancelTracker = import.CancelTracker;

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
            // ToDo:
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

    #endregion
}
