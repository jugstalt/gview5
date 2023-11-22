using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.Extensions;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataSources.Fdb.MSAccess;
using gView.DataSources.Fdb.MSSql;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.MsSql;

[RegisterPlugIn("42610C64-F3A9-4241-A7A8-1FEE4A4FE9FE")]
public class SqlFdbDatasetExplorerObject : ExplorerParentObject<SqlFdbExplorerObject, SqlFDBDataset>,
                                           IExplorerSimpleObject,
                                           IExplorerObjectDeletable,
                                           ISerializableExplorerObject,
                                           IExplorerObjectCreatable,
                                           IExplorerObjectContextTools,
                                           IExplorerObjectRenamable
{
    public string _icon = "";
    private bool _isImageDataset = false;
    private AccessFDB? _fdb = null;
    private string _dsname = "";
    private IFeatureDataset? _dataset;
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

    public SqlFdbDatasetExplorerObject() : base() { }
    public SqlFdbDatasetExplorerObject(SqlFdbExplorerObject parent, string dsname)
        : base(parent, 0)
    {
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

    #region IExplorerObjectContextTools Member

    public IEnumerable<IExplorerObjectContextTool> ContextTools
        => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();


    #endregion

    internal string ConnectionString
    {
        get
        {
            if (TypedParent == null)
            {
                return "";
            }

            return TypedParent.ConnectionString;
        }
    }

    internal bool IsImageDataset => _isImageDataset;

    #region IExplorerObject Members

    public string Name => _dsname;

    public string FullName
    {
        get
        {
            if (Parent == null)
            {
                return "";
            }

            return @$"{Parent.FullName}\{this.Name}";
        }
    }
    public string Type => "Sql Feature Database Dataset";
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
            _dataset = new SqlFDBDataset();
            await _dataset.SetConnectionString($"{this.ConnectionString};dsname={_dsname}");
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

        _dataset = new SqlFDBDataset();
        await _dataset.SetConnectionString(this.ConnectionString + ";dsname=" + _dsname);

        if (await _dataset.Open())
        {
            foreach (IDatasetElement element in await _dataset.Elements())
            {
                base.AddChildObject(new SqlFdbFeatureClassExplorerObject(this, _dsname, element));
            }
        }
        _fdb = (SqlFDB)_dataset.Database;

        return true;
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
            throw new GeneralException(_dataset.Database.LastErrorMessage);
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
            throw new GeneralException(_dataset.Database.LastErrorMessage);
        }
        return true;
    }

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return cache[FullName];
        }

        FullName = FullName.Replace("\\", @"/");
        int lastIndex = FullName.LastIndexOf(@"/");
        if (lastIndex == -1)
        {
            return null;
        }

        string fdbName = FullName.Substring(0, lastIndex);
        string dsName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

        SqlFdbExplorerObject? fdbObject = new SqlFdbExplorerObject();
        fdbObject = (SqlFdbExplorerObject?)await fdbObject.CreateInstanceByFullName(fdbName, cache);
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

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        if (parentExObject is SqlFdbExplorerObject)
        {
            return true;
        }

        return false;
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope scope, IExplorerObject parentExObject)
    {
        if (parentExObject == null || !CanCreate(parentExObject))
        {
            return null;
        }

        var scopeService = scope.ToExplorerScopeService();

        var element = await scopeService.CreateDataset(parentExObject);

        if (parentExObject is SqlFdbExplorerObject && element != null)
        {
            return new SqlFdbDatasetExplorerObject(
                (SqlFdbExplorerObject)parentExObject,
                element.DatasetName);
        }
        else
        {
            await scopeService.ForceContentRefresh();

            return null;
        }
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

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

        if (_dataset == null || !(_dataset.Database is AccessFDB))
        {
            throw new GeneralException("Can't rename dataset...\nUncorrect dataset !!!");
        }
        if (!await ((AccessFDB)_dataset.Database).RenameDataset(this.Name, newName))
        {
            throw new GeneralException("Can't rename dataset...\n" + ((AccessFDB)_dataset.Database).LastErrorMessage);
        }

        _dsname = newName;

        if (ExplorerObjectRenamed != null)
        {
            ExplorerObjectRenamed(this);
        }

        return true;
    }

    #endregion
}

//[RegisterPlugIn("11D0739F-66FC-4ca7-BA58-887DBB6F088C")]
//public class SqlFDBGeographicViewExplorerObject : IExplorerSimpleObject, IExplorerObjectCreatable
//{
//    #region IExplorerObjectCreatable Member

//    public bool CanCreate(IExplorerObject parentExObject)
//    {
//        return parentExObject is SqlFDBDatasetExplorerObject;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        SqlFDBDatasetExplorerObject parent = (SqlFDBDatasetExplorerObject)parentExObject;

//        IFeatureDataset dataset = await parent.GetInstanceAsync() as IFeatureDataset;
//        if (dataset == null)
//        {
//            return null;
//        }

//        AccessFDB fdb = dataset.Database as AccessFDB;
//        if (fdb == null)
//        {
//            return null;
//        }

//        FormRegisterGeographicView dlg = await FormRegisterGeographicView.CreateAsync(dataset as IFeatureDataset);
//        if (dlg.ShowDialog() == DialogResult.OK)
//        {
//            int fc_id = await fdb.CreateSpatialView(dataset.DatasetName, dlg.SpatialViewAlias);

//            IDatasetElement element = await dataset.Element(dlg.SpatialViewAlias);
//            return new SqlFDBFeatureClassExplorerObject(
//                parentExObject as SqlFDBDatasetExplorerObject,
//                parentExObject.Name,
//                element);
//        }
//        return null;
//    }

//    #endregion

//    #region IExplorerObject Member

//    public string Name
//    {
//        get { return "Geographic View"; }
//    }

//    public string FullName
//    {
//        get { return "Geographic View"; }
//    }

//    public string Type
//    {
//        get { return String.Empty; }
//    }

//    public IExplorerIcon Icon
//    {
//        get { return new AccessFDBGeographicViewIcon(); }
//    }

//    public IExplorerObject ParentExplorerObject
//    {
//        get { return null; }
//    }

//    public Task<object> GetInstanceAsync()
//    {
//        return Task.FromResult<object>(null);
//    }

//    public Type ObjectType
//    {
//        get { return null; }
//    }

//    public int Priority { get { return 1; } }

//    #endregion

//    #region IDisposable Member

//    public void Dispose()
//    {

//    }

//    #endregion

//    #region ISerializableExplorerObject Member

//    public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
//    {
//        return Task.FromResult<IExplorerObject>(null);
//    }

//    #endregion
//}

//[RegisterPlugIn("19AF8E6C-8324-4290-AF7C-5B19E31A952E")]
//public class SqlFDBLinkedFeatureclassExplorerObject : IExplorerSimpleObject, IExplorerObjectCreatable
//{
//    #region IExplorerObjectCreatable Member

//    public bool CanCreate(IExplorerObject parentExObject)
//    {
//        return parentExObject is SqlFDBDatasetExplorerObject;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        SqlFDBDatasetExplorerObject parent = (SqlFDBDatasetExplorerObject)parentExObject;

//        IFeatureDataset dataset = await parent.GetInstanceAsync() as IFeatureDataset;
//        if (dataset == null)
//        {
//            return null;
//        }

//        AccessFDB fdb = dataset.Database as AccessFDB;
//        if (fdb == null)
//        {
//            return null;
//        }

//        List<ExplorerDialogFilter> filters = new List<ExplorerDialogFilter>();
//        filters.Add(new OpenFeatureclassFilter());
//        ExplorerDialog dlg = new ExplorerDialog("Select Featureclass", filters, true);

//        IExplorerObject ret = null;

//        if (dlg.ShowDialog() == DialogResult.OK &&
//            dlg.ExplorerObjects != null)
//        {
//            foreach (IExplorerObject exObj in dlg.ExplorerObjects)
//            {
//                var exObjectInstance = await exObj?.GetInstanceAsync();
//                if (exObjectInstance is IFeatureClass)
//                {
//                    int fcid = await fdb.CreateLinkedFeatureClass(dataset.DatasetName, (IFeatureClass)exObjectInstance);
//                    if (fcid < 0)
//                    {
//                        MessageBox.Show(fdb.LastErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                        continue;
//                    }
//                    if (ret == null)
//                    {
//                        IDatasetElement element = await dataset.Element(((IFeatureClass)exObjectInstance).Name);
//                        if (element != null)
//                        {
//                            ret = new SqlFDBFeatureClassExplorerObject(
//                                parentExObject as SqlFDBDatasetExplorerObject,
//                                parentExObject.Name,
//                                element);
//                        }
//                    }
//                }
//            }
//        }
//        return ret;
//    }

//    #endregion

//    #region IExplorerObject Member

//    public string Name
//    {
//        get { return "Linked Featureclass"; }
//    }

//    public string FullName
//    {
//        get { return "Linked Featureclass"; }
//    }

//    public string Type
//    {
//        get { return String.Empty; }
//    }

//    public IExplorerIcon Icon
//    {
//        get { return new AccessFDBGeographicViewIcon(); }
//    }

//    public IExplorerObject ParentExplorerObject
//    {
//        get { return null; }
//    }

//    public Task<object> GetInstanceAsync()
//    {
//        return Task.FromResult<object>(null);
//    }

//    public Type ObjectType
//    {
//        get { return null; }
//    }

//    public int Priority { get { return 1; } }

//    #endregion

//    #region IDisposable Member

//    public void Dispose()
//    {

//    }

//    #endregion

//    #region ISerializableExplorerObject Member

//    public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
//    {
//        return Task.FromResult<IExplorerObject>(null);
//    }

//    #endregion
//}

////[RegisterPlugIn("C3D1F9CA-69B5-46e9-B4DB-05534512F8B9")]
//public class SqlFDBTileGridClassExplorerObject : ExplorerObjectCls, IExplorerSimpleObject, IExplorerObjectCreatable
//{
//    public SqlFDBTileGridClassExplorerObject() : base(null, null, 1) { }

//    #region IExplorerObject Member

//    public string Name
//    {
//        get { return String.Empty; }
//    }

//    public string FullName
//    {
//        get { return String.Empty; }
//    }

//    public string Type
//    {
//        get { return "Tile Grid Class"; }
//    }

//    public IExplorerIcon Icon
//    {
//        get { return new AccessFDBRasterIcon(); }
//    }

//    public Task<object> GetInstanceAsync()
//    {
//        return Task.FromResult<object>(null);
//    }

//    #endregion

//    #region IDisposable Member

//    public void Dispose()
//    {

//    }

//    #endregion

//    #region ISerializableExplorerObject Member

//    public Task<IExplorerObject> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache cache)
//    {
//        return Task.FromResult<IExplorerObject>(null);
//    }

//    #endregion

//    #region IExplorerObjectCreatable Member

//    public bool CanCreate(IExplorerObject parentExObject)
//    {
//        if (parentExObject is SqlFDBDatasetExplorerObject)
//        {
//            return true;
//        }

//        return false;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        if (!(parentExObject is SqlFDBDatasetExplorerObject))
//        {
//            return null;
//        }

//        var instance = await ((SqlFDBDatasetExplorerObject)parentExObject).GetInstanceAsync();
//        IFeatureDataset dataset = (SqlFDBDatasetExplorerObject)instance as IFeatureDataset;
//        if (dataset == null || !(dataset.Database is SqlFDB))
//        {
//            return null;
//        }

//        FormNewTileGridClass dlg = new FormNewTileGridClass();
//        if (dlg.ShowDialog() != DialogResult.OK)
//        {
//            return null;
//        }

//        CreateTileGridClass creator = new CreateTileGridClass(
//            dlg.GridName,
//            dataset, dlg.SpatialIndexDefinition,
//            dlg.RasterDataset,
//            dlg.TileSizeX, dlg.TileSizeY, dlg.ResolutionX, dlg.ResolutionY, dlg.Levels,
//            dlg.TileCacheDirectory,
//            dlg.TileGridType);
//        creator.CreateTiles = dlg.GenerateTileCache;
//        creator.TileLevelType = dlg.TileLevelType;
//        creator.CreateLevels = dlg.CreateLevels;

//        FormTaskProgress progress = new FormTaskProgress();
//        progress.ShowProgressDialog(creator, creator.RunTask());

//        IDatasetElement element = await ((IFeatureDataset)instance).Element(dlg.GridName);
//        return new SqlFDBFeatureClassExplorerObject(
//            parentExObject as SqlFDBDatasetExplorerObject,
//            parentExObject.Name,
//            element);
//    }

//    #endregion
//}
