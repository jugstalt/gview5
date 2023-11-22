using gView.Blazor.Core.Exceptions;
using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Base.ContextTools;
using gView.DataSources.MSSqlSpatial;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.Db;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.MsSqlSpatial.Geography;

public class MsSqlSpatialExplorerObject : ExplorerParentObject<IExplorerObject, IFeatureDataset>,
                                          IExplorerSimpleObject,
                                          IExplorerObjectDeletable,
                                          IExplorerObjectRenamable,
                                          ISerializableExplorerObject,
                                          IExplorerObjectContextTools,
                                          IUpdateConnectionString
{
    private string _server = "";
    private DbConnectionString? _connectionString;
    private IFeatureDataset? _dataset;
    private IEnumerable<IExplorerObjectContextTool>? _contextTools = null;

    public MsSqlSpatialExplorerObject() : base() { }
    public MsSqlSpatialExplorerObject(IExplorerObject parent, string server, DbConnectionString connectionString)
        : base(parent, 0)
    {
        _server = server;
        _connectionString = connectionString;

        _contextTools = new IExplorerObjectContextTool[]
        {
            new UpdateConnectionString()
        };
    }

    internal string ConnectionString => _connectionString?.ConnectionString ?? String.Empty;

    #region IUpdateConnectionString

    public DbConnectionString GetDbConnectionString()
    {
        if (_connectionString == null)
        {
            throw new GeneralException("Error: connection string not set already for this item");
        }

        return _connectionString.Clone();
    }
    public Task<bool> UpdateDbConnectionString(DbConnectionString dbConnnectionString)
    {
        ConfigConnections connStream = new ConfigConnections("mssql-geography", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        connStream.Add(_server, dbConnnectionString.ToString());

        _connectionString = dbConnnectionString;

        return Task.FromResult(true);
    }

    #endregion

    #region IExplorerObjectContextTools

    public IEnumerable<IExplorerObjectContextTool> ContextTools => _contextTools ?? Array.Empty<IExplorerObjectContextTool>();

    #endregion

    #region IExplorerObject Members

    public string Name => _server;

    public string FullName => @$"Databases\MsSqlSpatialGeography\{_server}";

    public string Type => "MsSqlSpatial Database";
    public string Icon => "basic:database";

    async public Task<object?> GetInstanceAsync()
    {
        if (_connectionString == null)
        {
            return null;
        }

        if (_dataset == null)
        {
            _dataset = new GeographyDataset();
            await _dataset.SetConnectionString(_connectionString.ConnectionString);
            await _dataset.Open();
        }

        return _dataset;
    }

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        if (_connectionString == null)
        {
            return false;
        }

        GeographyDataset dataset = new GeographyDataset();
        await dataset.SetConnectionString(_connectionString.ConnectionString);
        await dataset.Open();

        List<IDatasetElement> elements = await dataset.Elements();

        if (!String.IsNullOrEmpty(dataset.LastErrorMessage))
        {
            throw new Exception($"Can't open dataset: {dataset.LastErrorMessage}");
        }

        foreach (IDatasetElement element in elements.OrEmpty())
        {
            if (element.Class is IFeatureClass)
            {
                base.AddChildObject(new MsSqlSpatialFeatureClassExplorerObject(this, element));
            }
        }

        return true;
    }

    #endregion

    #region ISerializableExplorerObject Member

    async public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return cache[FullName];
        }

        MsSqlSpatialExplorerGroupObject? group = new MsSqlSpatialExplorerGroupObject();
        if (FullName.IndexOf(group.FullName) != 0 || FullName.Length < group.FullName.Length + 2)
        {
            return null;
        }

        group = (MsSqlSpatialExplorerGroupObject?)((cache != null && cache.Contains(group.FullName)) ? cache[group.FullName] : group);

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

    public event ExplorerObjectDeletedEvent? ExplorerObjectDeleted = null;

    public Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e)
    {
        bool ret = false;
        if (_connectionString != null)
        {
            ConfigConnections stream = new ConfigConnections("mssql-geography", "546B0513-D71D-4490-9E27-94CD5D72C64A");
            ret = stream.Remove(_server);
        }

        if (ret && ExplorerObjectDeleted != null)
        {
            ExplorerObjectDeleted(this);
        }

        return Task.FromResult(ret);
    }

    #endregion

    #region IExplorerObjectRenamable Member

    public event ExplorerObjectRenamedEvent? ExplorerObjectRenamed = null;

    public Task<bool> RenameExplorerObject(string newName)
    {
        bool ret = false;
        if (_connectionString != null)
        {
            ConfigConnections stream = new ConfigConnections("mssql-geography", "546B0513-D71D-4490-9E27-94CD5D72C64A");
            ret = stream.Rename(_server, newName);
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
