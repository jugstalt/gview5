using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Databases;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Db;
using gView.Framework.IO;
using gView.Framework.system;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.MsSqlSpatial.Geometry;
[RegisterPlugIn("25F9A2DE-D54F-4DCD-832A-C2EFB544CF8F")]
public class MsSqlSpatialExplorerGroupObject :
                    ExplorerParentObject,
                    IDatabasesExplorerGroupObject
{
    public MsSqlSpatialExplorerGroupObject()
        : base()
    { }

    #region IExplorerGroupObject Members

    public string Icon => "basic:edit-database";

    #endregion

    #region IExplorerObject Members

    public string Name => "MsSql Spatial Geometry";

    public string FullName => @"Databases\MsSqlSpatialGeometry";

    public string Type => "MsSql Spatial Geometry Connection";

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerParentObject Members

    async public override Task<bool> Refresh()
    {
        await base.Refresh();
        base.AddChildObject(new MsSqlSpatialNewConnectionObject(this));

        ConfigConnections conStream = new ConfigConnections("mssql-geometry", "546B0513-D71D-4490-9E27-94CD5D72C64A");
        Dictionary<string, string> DbConnectionStrings = conStream.Connections;

        foreach (string DbConnName in DbConnectionStrings.Keys)
        {
            DbConnectionString dbConn = new DbConnectionString();
            dbConn.FromString(DbConnectionStrings[DbConnName]);
            base.AddChildObject(new MsSqlSpatialExplorerObject(this, DbConnName, dbConn));
        }

        return true;
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        if (this.FullName == FullName)
        {
            MsSqlSpatialExplorerGroupObject exObject = new MsSqlSpatialExplorerGroupObject();
            cache?.Append(exObject);
            return Task.FromResult<IExplorerObject?>(exObject);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IOgcGroupExplorerObject

    public void SetParentExplorerObject(IExplorerObject parentExplorerObject)
    {
        base.Parent = parentExplorerObject;
    }

    #endregion
}
