using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataSources.Fdb.MSSql;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.Db;
using gView.Framework.IO;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.MsSql;

public class SqlFdbExplorerObject : ExplorerParentObject<SqlFdbExplorerGroupObject>, 
                                    IExplorerSimpleObject, 
                                    IExplorerObjectDeletable, 
                                    IExplorerObjectRenamable, 
                                    ISerializableExplorerObject
                                    //, IExplorerObjectContextMenu
{
    private string _server = "", _connectionString = "", _errMsg = "";
    private DbConnectionString? _dbConnectionString = null;

    public SqlFdbExplorerObject()
        : base() { }
    public SqlFdbExplorerObject(SqlFdbExplorerGroupObject parent, string server, string connectionString)
        : base(parent, 1)
    {
        _server = server;
        _connectionString = connectionString;

        //_contextItems = new ToolStripItem[1];
        //_contextItems[0] = new ToolStripMenuItem("Tasks");

        //ToolStripMenuItem item = new ToolStripMenuItem("Shrink database");
        //item.Click += new EventHandler(ShrinkDatabase_Click);
        //((ToolStripMenuItem)_contextItems[0]).DropDownItems.Add(item);
        //((ToolStripMenuItem)_contextItems[0]).DropDownItems.Add(new ToolStripSeparator());

        //item = new ToolStripMenuItem("Check SpatialEngine Version");
        //item.Click += new EventHandler(CheckSpatialEngineVersion_Click);
        //((ToolStripMenuItem)_contextItems[0]).DropDownItems.Add(item);
        //item = new ToolStripMenuItem("Create Spatial Engine");
        //item.Click += new EventHandler(CreateSpatialEngine_Click);
        //((ToolStripMenuItem)_contextItems[0]).DropDownItems.Add(item);
        //item = new ToolStripMenuItem("Drop Spatial Engine");
        //item.Click += new EventHandler(DropSpatialEngine_Click);
        //((ToolStripMenuItem)_contextItems[0]).DropDownItems.Add(item);
    }
    public SqlFdbExplorerObject(SqlFdbExplorerGroupObject parent, string server, DbConnectionString dbConnectionString)
        : this(parent, server, (dbConnectionString != null) ? dbConnectionString.ConnectionString : String.Empty)
    {
        _dbConnectionString = dbConnectionString;

        //List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();
        //if (_contextItems != null)
        //{
        //    foreach (ToolStripMenuItem i in _contextItems)
        //    {
        //        items.Add(i);
        //    }
        //}

        //ToolStripMenuItem item = new ToolStripMenuItem(LocalizedResources.GetResString("Menu.ConnectionProperties", "Connection Properties..."));
        //item.Click += new EventHandler(ConnectionProperties_Click);
        //items.Add(item);

        //_contextItems = items.ToArray();
    }

    //void CheckSpatialEngineVersion_Click(object sender, EventArgs e)
    //{
    //    try
    //    {
    //        using (SqlConnection connection = new SqlConnection(_connectionString))
    //        {
    //            connection.Open();

    //            DataTable tab = new DataTable("ASSEMBLIES");
    //            SqlDataAdapter adapter = new SqlDataAdapter("select * from sys.assemblies where name='MSSqlSpatialEngine'", connection);
    //            adapter.Fill(tab);

    //            if (tab.Rows.Count == 0)
    //            {
    //                MessageBox.Show("SpatialEngine is not installed for this database...");
    //                return;
    //            }
    //            adapter.Dispose();

    //            string clr_name = tab.Rows[0]["clr_name"].ToString().Replace(",", ";");
    //            string version = ConfigTextStream.ExtractValue(clr_name, "version");
    //            MessageBox.Show("SpatialEngine is installed on this database.\nVersion: " + version);

    //            connection.Close();
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        MessageBox.Show("ERROR: " + ex.Message);
    //    }
    //}

    //private bool ExecuteQueries(SqlConnection connection, StreamReader reader)
    //{
    //    string line = "";
    //    StringBuilder sql = new StringBuilder();
    //    while ((line = reader.ReadLine()) != null)
    //    {
    //        if (line.Trim().ToLower() == "go")
    //        {
    //            SqlCommand command = new SqlCommand(sql.ToString(), connection);
    //            try
    //            {
    //                command.ExecuteNonQuery();
    //                sql = new StringBuilder();
    //            }
    //            catch (Exception ex)
    //            {
    //                MessageBox.Show(ex.Message);
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            sql.Append(line + " ");
    //        }
    //    }
    //    return true;
    //}

    //void DropSpatialEngine_Click(object sender, EventArgs e)
    //{
    //    SqlCommandInterpreter interpreter = new SqlCommandInterpreter(_connectionString, SystemVariables.ApplicationDirectory + @"/sql/sqlFDB/dropSpatialEngine.sql");
    //    SqlCommandProgressReporter reporter = new SqlCommandProgressReporter(interpreter);

    //    FormTaskProgress progress = new FormTaskProgress(reporter, interpreter.Exectue(null));
    //    progress.Text = "Execute Commands:";
    //    progress.Mode = ProgressMode.ProgressDisk;
    //    progress.ShowDialog();
    //}

    //void CreateSpatialEngine_Click(object sender, EventArgs e)
    //{
    //    DropSpatialEngine_Click(sender, e);

    //    SqlCommandInterpreter interpreter = new SqlCommandInterpreter(_connectionString, SystemVariables.ApplicationDirectory + @"/sql/sqlFDB/createSpatialEngine.sql");
    //    SqlCommandProgressReporter reporter = new SqlCommandProgressReporter(interpreter);

    //    FormTaskProgress progress = new FormTaskProgress(reporter, interpreter.Exectue(null));
    //    progress.Text = "Execute Commands:";
    //    progress.Mode = ProgressMode.ProgressDisk;
    //    progress.ShowDialog();
    //}

    //void ShrinkDatabase_Click(object sender, EventArgs e)
    //{
    //    SqlCommandInterpreter interpreter = new SqlCommandInterpreter(_connectionString, SystemVariables.ApplicationDirectory + @"/sql/sqlFDB/shrinkDatabase.sql");
    //    interpreter.DatabaseName = ConfigTextStream.ExtractValue(_connectionString, "database"); ;
    //    SqlCommandProgressReporter reporter = new SqlCommandProgressReporter(interpreter);

    //    FormTaskProgress progress = new FormTaskProgress(reporter, interpreter.Exectue(null));
    //    progress.Text = "Execute Commands:";
    //    progress.Mode = ProgressMode.ProgressDisk;
    //    progress.ShowDialog();
    //}

    //void ConnectionProperties_Click(object sender, EventArgs e)
    //{
    //    if (_dbConnectionString == null)
    //    {
    //        return;
    //    }

    //    FormConnectionString dlg = new FormConnectionString(_dbConnectionString);
    //    dlg.ProviderID = "mssql";
    //    dlg.UseProviderInConnectionString = false;

    //    if (dlg.ShowDialog() == DialogResult.OK)
    //    {
    //        DbConnectionString dbConnStr = dlg.DbConnectionString;

    //        ConfigConnections connStream = new ConfigConnections("sqlfdb", "546B0513-D71D-4490-9E27-94CD5D72C64A");
    //        connStream.Add(_server, dbConnStr.ToString());

    //        _dbConnectionString = dbConnStr;
    //        _connectionString = dbConnStr.ConnectionString;
    //    }
    //}

    internal string ConnectionString=>_connectionString;

    #region IExplorerObject Members

    public string Name=> _server;

    public string FullName=> @$"Databases\SqlFDBConnections\{_server}";

    public string Type=> "Sql Feature Database";

    public string Icon => "basic:database";


    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    async private Task<string[]?> DatasetNames()
    {
        try
        {
            SqlFDB fdb = new SqlFDB();
            if (!await fdb.Open(_connectionString))
            {
                _errMsg = fdb.LastErrorMessage;
                return null;
            }

            if (fdb.LastException != null)
            {
                throw new GeneralException(fdb.LastException.Message);
            }
            string[] ds = await fdb.DatasetNames();

            if (ds == null)
            {
                if (String.IsNullOrEmpty(fdb.LastErrorMessage))
                {
                    return Array.Empty<string>();
                }
                _errMsg = fdb.LastErrorMessage;
                throw new GeneralException(_errMsg);
            }

            string[] dsMod = new string[ds.Length];

            int i = 0;
            foreach (string dsname in ds)
            {
                var isImageDatasetResult = await fdb.IsImageDataset(dsname);
                string imageSpace = isImageDatasetResult.imageSpace;
                if (isImageDatasetResult.isImageDataset)
                {
                    dsMod[i++] = "#" + dsname;
                }
                else
                {
                    dsMod[i++] = dsname;
                }
            }

            fdb.Dispose();

            return dsMod;
        }
        catch (Exception ex)
        {
            _errMsg = ex.Message;
            return null;
        }
    }

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        string[]? ds = await DatasetNames();
        if (ds == null)
        {
            throw new GeneralException(_errMsg);
        }
        else
        {
            foreach (string dsname in ds)
            {
                if (dsname == "")
                {
                    continue;
                }

                base.AddChildObject(new SqlFdbDatasetExplorerObject(this, dsname));
            }
        }

        return true;
    }
    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return cache[FullName];
        }

        SqlFdbExplorerGroupObject? group = new SqlFdbExplorerGroupObject();
        if (FullName.IndexOf(group.FullName) != 0 || FullName.Length < group.FullName.Length + 2)
        {
            return null;
        }

        group = (SqlFdbExplorerGroupObject?)((cache?.Contains(group.FullName) == true) ? cache[group.FullName] : group);

        if (group != null)
        {
            foreach (IExplorerObject exObject in await group.ChildObjects())
            {
                if (exObject.FullName == FullName)
                {
                    cache?.Append(exObject);
                    return exObject;
                }
            }
        }

        return null;
    }

    #endregion

    #region IExplorerObjectDeletable Member

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        if (_dbConnectionString != null)
        {
            ConfigConnections stream = new ConfigConnections("sqlfdb", "546B0513-D71D-4490-9E27-94CD5D72C64A");
            stream.Remove(_server);
        }
        else
        {
            ConfigTextStream stream = new ConfigTextStream("sqlfdb_connections", true, true);
            stream.Remove(this.Name, _connectionString);
            stream.Close();
        }
        if (ExplorerObjectDeleted != null)
        {
            ExplorerObjectDeleted(this);
        }

        return Task.FromResult(true);
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed;

    public Task<bool> RenameExplorerObject(string newName)
    {
        bool ret = false;
        if (_dbConnectionString != null)
        {
            ConfigConnections stream = new ConfigConnections("sqlfdb", "546B0513-D71D-4490-9E27-94CD5D72C64A");
            ret = stream.Rename(_server, newName);
        }
        else
        {
            ConfigTextStream stream = new ConfigTextStream("sqlfdb_connections", true, true);
            ret = stream.ReplaceHoleLine(ConfigTextStream.BuildLine(_server, _connectionString), ConfigTextStream.BuildLine(newName, _connectionString));
            stream.Close();
        }
        if (ret == true)
        {
            _server = newName;
            if (ExplorerObjectRenamed != null)
            {
                ExplorerObjectRenamed(this);
            }
        }
        return Task.FromResult(ret);
    }

    #endregion
}

//[RegisterPlugIn("97914E6A-3084-4fc0-8B31-4A6D2C990F72")]
//public class SqlFDBNetworkClassExplorerObject : ExplorerObjectCls, 
//                                                IExplorerSimpleObject, 
//                                                IExplorerObjectCreatable
//{
//    public SqlFDBNetworkClassExplorerObject() : base(null, typeof(SqlFDBNetworkFeatureclass), 0) { }

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
//        get { return "Network Class"; }
//    }

//    public IExplorerIcon Icon
//    {
//        get { return new AccessFDBNetworkIcon(); }
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

//        var instance = await parentExObject.GetInstanceAsync();
//        IFeatureDataset dataset = (SqlFDBDatasetExplorerObject)instance as IFeatureDataset;
//        if (dataset == null || !(dataset.Database is SqlFDB))
//        {
//            return null;
//        }

//        FormNewNetworkclass dlg = new FormNewNetworkclass(dataset, typeof(CreateFDBNetworkFeatureclass));
//        if (dlg.ShowDialog() != DialogResult.OK)
//        {
//            return null;
//        }

//        CreateFDBNetworkFeatureclass creator = new CreateFDBNetworkFeatureclass(
//            dataset, dlg.NetworkName,
//            dlg.EdgeFeatureclasses,
//            dlg.NodeFeatureclasses);
//        creator.SnapTolerance = dlg.SnapTolerance;
//        creator.ComplexEdgeFcIds = await dlg.ComplexEdgeFcIds();
//        creator.GraphWeights = dlg.GraphWeights;
//        creator.SwitchNodeFcIdAndFieldnames = dlg.SwitchNodeFcIds;
//        creator.NodeTypeFcIds = dlg.NetworkNodeTypeFcIds;

//        FormTaskProgress progress = new FormTaskProgress();
//        progress.ShowProgressDialog(creator, creator.Run());

//        IDatasetElement element = await ((IFeatureDataset)instance).Element(dlg.NetworkName);
//        return new SqlFDBFeatureClassExplorerObject(
//            parentExObject as SqlFDBDatasetExplorerObject,
//            parentExObject.Name,
//            element);
//    }

//    #endregion
//}

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
