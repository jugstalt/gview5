using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.Extensions;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.SqLite;

[RegisterPlugIn("E76179C7-FD21-4C0F-ADA0-1DC260E8C50E")]
public class SqLiteFdbDatasetExplorerObject : ExplorerObjectFeatureClassImport<IExplorerObject, SQLiteFDBDataset>,
                                              IExplorerSimpleObject,
                                              IExplorerObjectCommandParameters,
                                              ISerializableExplorerObject,
                                              IExplorerObjectDeletable,
                                              IExplorerObjectCreatable,
                                              IExplorerObjectRenamable,
                                              IExplorerObjectContextTools
{
    private string _icon = "";
    private string _filename = "";
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;
    private bool _isImageDataset = false;

    public SqLiteFdbDatasetExplorerObject() : base() { }

    public SqLiteFdbDatasetExplorerObject(IExplorerObject parent, string filename, string dsname)
        : base(parent)
    {
        _filename = filename;

        if (dsname.IndexOf("#") == 0)
        {
            _isImageDataset = true;
            dsname = dsname.Substring(1, dsname.Length - 1);
            _icon = "webgis:tiles";
        }
        else
        {
            _isImageDataset = false;
            _icon = "webgis:layer-middle";
        }
        _dsname = dsname;

        _contextTools = new IExplorerObjectContextTool[]
        {
            new SetSpatialReference(),
            new ShrinkSpatialIndices(),
            new RepairSpatialIndex()
        };
    }

    //async void SpatialReference_Click(object sender, EventArgs e)
    //{
    //    if (_dataset == null || _fdb == null)
    //    {
    //        await Refresh();
    //        if (_dataset == null || _fdb == null)
    //        {
    //            MessageBox.Show("Can't open dataset...");
    //            return;
    //        }
    //    }

    //    FormSpatialReference dlg = new FormSpatialReference(await _dataset.GetSpatialReference());

    //    if (dlg.ShowDialog() == DialogResult.OK)
    //    {
    //        int id = await _fdb.CreateSpatialReference(dlg.SpatialReference);
    //        if (id == -1)
    //        {
    //            MessageBox.Show("Can't create Spatial Reference!\n", _fdb.LastErrorMessage);
    //            return;
    //        }
    //        if (!await _fdb.SetSpatialReferenceID(_dataset.DatasetName, id))
    //        {
    //            MessageBox.Show("Can't set Spatial Reference!\n", _fdb.LastErrorMessage);
    //            return;
    //        }
    //        _dataset.SetSpatialReference(dlg.SpatialReference);
    //    }
    //}

    //async void ShrinkSpatialIndices_Click(object sender, EventArgs e)
    //{
    //    if (_dataset == null)
    //    {
    //        return;
    //    }

    //    List<IClass> classes = new List<IClass>();
    //    foreach (IDatasetElement element in await _dataset.Elements())
    //    {
    //        if (element == null || element.Class == null)
    //        {
    //            continue;
    //        }

    //        classes.Add(element.Class);
    //    }

    //    SpatialIndexShrinker rebuilder = new SpatialIndexShrinker();
    //    rebuilder.RebuildIndices(classes);
    //}

    internal bool IsImageDataset
    {
        get { return _isImageDataset; }
    }

    #region IExplorerObject Members

    public string Name
    {
        get { return _dsname; }
    }

    public string FullName
    {
        get
        {
            return _filename + ((_filename != "") ? @"\" : "") + _dsname;
        }
    }
    public string Type
    {
        get { return "SQLite Feature Database Dataset"; }
    }
    public string Icon => _icon;

    public override void Dispose()
    {
        base.Dispose();

        _fdb = null;
        if (_dataset != null)
        {
            _dataset.Dispose();
            _dataset = null;
        }
    }
    async public Task<object?> GetInstanceAsync()
    {
        if (_dataset == null)
        {
            _dataset = new SQLiteFDBDataset();
            await _dataset.SetConnectionString("Data Source=" + _filename + ";dsname=" + _dsname);
            await _dataset.Open();
        }

        return _dataset;
    }

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        this.Dispose();

        _dataset = new SQLiteFDBDataset();
        await _dataset.SetConnectionString("Data Source=" + _filename + ";dsname=" + _dsname);

        if (await _dataset.Open())
        {
            foreach (IDatasetElement element in await _dataset.Elements())
            {
                base.AddChildObject(new SqLiteFdbFeatureClassExplorerObject(this, _filename, _dsname, element));
            }
        }
        _fdb = (SQLiteFDB)_dataset.Database;

        return true;
    }

    #endregion

    #region IExplorerObjectCommandParameters Members

    public Dictionary<string, string> Parameters
    {
        get
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("SQLiteFDB", _filename);
            parameters.Add("Dataset", _dsname);
            return parameters;
        }
    }

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        FullName = FullName.Replace("/", @"\");
        int lastIndex = FullName.LastIndexOf(@"\");
        if (lastIndex == -1)
        {
            return null;
        }

        string mdbName = FullName.Substring(0, lastIndex);
        string dsName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

        SqLiteFdbExplorerObject? fdbObject = new SqLiteFdbExplorerObject();
        fdbObject = (SqLiteFdbExplorerObject?)await fdbObject.CreateInstanceByFullName(mdbName, cache);
        if (fdbObject == null || await fdbObject.ChildObjects() == null)
        {
            return null;
        }

        foreach (IExplorerObject exObject in await fdbObject.ChildObjects())
        {
            if (exObject.Name == dsName)
            {
                cache?.Append(exObject);
                return exObject;
            }
        }
        return null;
    }

    #endregion

    async internal Task<bool> DeleteFeatureClass(string name)
    {
        if (_dataset == null || !(_dataset.Database is IFeatureDatabase))
        {
            return false;
        }

        if (!await ((IFeatureDatabase)_dataset.Database).DeleteFeatureClass(name))
        {
            throw new Exception(_dataset.Database.LastErrorMessage);
        }
        return true;
    }

    async internal Task<bool> DeleteDataset(string dsname)
    {
        if (_dataset == null)
        {
            await Refresh();
        }
        if (!(_dataset?.Database is IFeatureDatabase))
        {
            return false;
        }

        if (!await ((IFeatureDatabase)_dataset.Database).DeleteDataset(dsname))
        {
            throw new Exception(_dataset.Database.LastErrorMessage);
        }
        return true;
    }

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject? parentExObject)
        => parentExObject is SqLiteFdbExplorerObject;

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope scope, IExplorerObject? parentExObject)
    {
        if (parentExObject == null || !CanCreate(parentExObject))
        {
            return null;
        }

        var scopeService = scope.ToExplorerScopeService();

        var element = await scopeService.CreateDataset(parentExObject);

        if (parentExObject is SqLiteFdbExplorerObject && element != null)
        {
            return new SqLiteFdbDatasetExplorerObject(
                (SqLiteFdbExplorerObject)parentExObject,
                element.ConnectionString,
                element.DatasetName);
        }
        else
        {
            await scopeService.ForceContentRefresh();

            return null;
        }
    }

    #endregion

    #region IExplorerObjectContextTools Member

    public IEnumerable<IExplorerObjectContextTool> ContextTools
        => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();


    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted = null;

    async public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        if (await DeleteDataset(_dsname))
        {
            if (ExplorerObjectDeleted != null)
            {
                ExplorerObjectDeleted(this);
            }

            return true;
        }
        return false;
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

    async public Task<bool> RenameExplorerObject(string newName)
    {
        if (newName == this.Name)
        {
            return false;
        }

        if (_dataset == null || !(_dataset.Database is SQLiteFDB))
        {
            throw new Exception("Can't rename dataset...\nUncorrect dataset !!!");
        }
        if (!await ((SQLiteFDB)_dataset.Database).RenameDataset(this.Name, newName))
        {
            throw new Exception("Can't rename dataset...\n" + ((SQLiteFDB)_dataset.Database).LastErrorMessage);
        }

        _dsname = newName;

        if (ExplorerObjectRenamed != null)
        {
            ExplorerObjectRenamed(this);
        }

        return true;
    }

    #endregion

    public string FileName { get { return _filename; } }
}
