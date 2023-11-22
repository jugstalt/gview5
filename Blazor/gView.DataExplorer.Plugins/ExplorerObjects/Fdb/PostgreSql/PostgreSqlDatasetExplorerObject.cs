using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.Extensions;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataSources.Fdb.MSAccess;
using gView.DataSources.Fdb.PostgreSql;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.FDB;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.PostgreSql;

[RegisterPlugIn("E1A2BCA0-A6E1-47f2-8AA2-201D2CE21815")]
public class PostgreSqlDatasetExplorerObject : ExplorerParentObject<PostgreSqlExplorerObject, pgDataset>,
                                            IExplorerSimpleObject,
                                            IExplorerObjectDeletable,
                                            ISerializableExplorerObject,
                                            IExplorerObjectCreatable,
                                            IExplorerObjectContextTools,
                                            IExplorerObjectRenamable
{
    private AccessFDB? _fdb = null;
    private string _dsname = "", _icon = "";
    private IFeatureDataset? _dataset;
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;
    private bool _isImageDataset = false;

    public PostgreSqlDatasetExplorerObject() : base() { }
    public PostgreSqlDatasetExplorerObject(PostgreSqlExplorerObject parent, string dsname)
        : base(parent, 1)
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

    internal string ConnectionString
    {
        get
        {
            if (TypedParent == null)
            {
                return String.Empty;
            }

            return TypedParent.ConnectionString;
        }
    }

    internal bool IsImageDataset
    {
        get { return _isImageDataset; }
    }

    #region IExplorerObjectContextTools Member

    public IEnumerable<IExplorerObjectContextTool> ContextTools
        => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();


    #endregion

    #region IExplorerObject Members

    public string Name
    {
        get { return _dsname; }
    }

    public string FullName
    {
        get
        {
            if (Parent == null)
            {
                return String.Empty;
            }

            return @$"{Parent.FullName}\{this.Name}";
        }
    }
    public string Type => "Postgre Feature Database Dataset";

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
            _dataset = new pgDataset();
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

        _dataset = new pgDataset();
        await _dataset.SetConnectionString($"{this.ConnectionString};dsname={_dsname}");

        if (await _dataset.Open())
        {
            foreach (IDatasetElement element in await _dataset.Elements())
            {
                base.AddChildObject(new PostgreSqlFeatureClassExplorerObject(this, _dsname, element));
            }
        }
        _fdb = (pgFDB)_dataset.Database;

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

        FullName = FullName.Replace(@"\", @"/");
        int lastIndex = FullName.LastIndexOf(@"/");
        if (lastIndex == -1)
        {
            return null;
        }

        string fdbName = FullName.Substring(0, lastIndex);
        string dsName = FullName.Substring(lastIndex + 1, FullName.Length - lastIndex - 1);

        PostgreSqlExplorerObject? fdbObject = new PostgreSqlExplorerObject(1);
        fdbObject = (PostgreSqlExplorerObject?)await fdbObject.CreateInstanceByFullName(fdbName, cache);
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
        if (parentExObject is PostgreSqlExplorerObject)
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

        if (parentExObject is PostgreSqlExplorerObject && element != null)
        {
            return new PostgreSqlDatasetExplorerObject(
                (PostgreSqlExplorerObject)parentExObject,
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

        if (_dataset == null || !(_dataset.Database is pgFDB))
        {
            throw new GeneralException("Can't rename dataset...\nUncorrect dataset !!!");
        }
        if (!await ((pgFDB)_dataset.Database).RenameDataset(this.Name, newName))
        {
            throw new GeneralException("Can't rename dataset...\n" + ((pgFDB)_dataset.Database).LastErrorMessage);
        }

        _dsname = newName;

        if (ExplorerObjectRenamed != null)
        {
            ExplorerObjectRenamed(this);
        }

        return true;
    }

    #endregion

    /*
    public override void Content_DragEnter(DragEventArgs e)
    {
        if (_icon is gView.DataSources.Fdb.UI.MSAccess.AccessFDBImageDatasetIcon)
        {
            e.Effect = DragDropEffects.None;
        }
        else
        {
            base.Content_DragEnter(e);
        }
    }
    */
}

//[RegisterPlugIn("af5f4d50-745b-11e1-b0c4-0800200c9a66")]
//public class GeographicViewExplorerObject : IExplorerSimpleObject, IExplorerObjectCreatable
//{
//    #region IExplorerObjectCreatable Member

//    public bool CanCreate(IExplorerObject parentExObject)
//    {
//        return parentExObject is DatasetExplorerObject;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        DatasetExplorerObject parent = (DatasetExplorerObject)parentExObject;

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

//        FormRegisterGeographicView dlg = await FormRegisterGeographicView.CreateAsync(dataset);
//        if (dlg.ShowDialog() == DialogResult.OK)
//        {
//            int fc_id = await fdb.CreateSpatialView(dataset.DatasetName, dlg.SpatialViewAlias);

//            IDatasetElement element = await dataset.Element(dlg.SpatialViewAlias);
//            return new FeatureClassExplorerObject(
//                parentExObject as DatasetExplorerObject,
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

//[RegisterPlugIn("D320CA4D-E63F-4DD4-96F4-CA11DC95A39E")]
//public class LinkedFeatureclassExplorerObject : IExplorerSimpleObject, IExplorerObjectCreatable
//{
//    #region IExplorerObjectCreatable Member

//    public bool CanCreate(IExplorerObject parentExObject)
//    {
//        return parentExObject is DatasetExplorerObject;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        DatasetExplorerObject parent = (DatasetExplorerObject)parentExObject;

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
//                    if (ret == null)
//                    {
//                        IDatasetElement element = await dataset.Element(((IFeatureClass)exObjectInstance).Name);
//                        if (element != null)
//                        {
//                            ret = new FeatureClassExplorerObject(
//                                parentExObject as DatasetExplorerObject,
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

////[gView.Framework.system.RegisterPlugIn("9B5B718C-2ECA-47ee-851F-9D33E3D82C55")]
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
//        get
//        {
//            return new gView.DataSources.Fdb.UI.MSAccess.AccessFDBRasterIcon();
//        }
//    }

//    public Task<object> GetInstanceAsync()
//    {
//        return Task.FromResult<object>(null);
//    }

//    public new int Priority { get { return 1; } }

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
//        if (parentExObject is DatasetExplorerObject)
//        {
//            return true;
//        }

//        return false;
//    }

//    async public Task<IExplorerObject> CreateExplorerObject(IExplorerObject parentExObject)
//    {
//        if (!(parentExObject is DatasetExplorerObject))
//        {
//            return null;
//        }

//        IFeatureDataset dataset = await ((DatasetExplorerObject)parentExObject).GetInstanceAsync() as IFeatureDataset;
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

//        IDatasetElement element = await dataset.Element(dlg.GridName);
//        return new FeatureClassExplorerObject(
//            parentExObject as DatasetExplorerObject,
//            parentExObject.Name,
//            element);
//    }

//    #endregion
//}
