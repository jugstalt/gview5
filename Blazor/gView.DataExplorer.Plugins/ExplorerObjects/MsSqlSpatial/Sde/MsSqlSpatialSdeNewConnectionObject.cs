using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.Framework.Blazor;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.DataExplorer.Events;
using gView.Framework.Db;
using gView.Framework.IO;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.MsSqlSpatial.Sde;

[gView.Framework.system.RegisterPlugIn("0B552B1D-49EF-4065-BB93-5F63517161A4")]
public class MsSqlSpatialSdeNewConnectionObject : 
                    ExplorerObjectCls<IExplorerObject>, 
                    IExplorerSimpleObject, 
                    IExplorerObjectDoubleClick, 
                    IExplorerObjectCreatable
{
    public MsSqlSpatialSdeNewConnectionObject()
        : base()
    {
    }

    public MsSqlSpatialSdeNewConnectionObject(IExplorerObject parent)
        : base(parent, 0)
    {
    }

    #region IExplorerSimpleObject Members

    public string Icon => "basic:database-plus";

    #endregion

    #region IExplorerObject Members

    public string Name => "New Connection...";

    public string FullName
    {
        get { return ""; }
    }

    public string Type => "New MsSql Spatial ArcSde Connection";

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);

    #endregion

    #region IExplorerObjectDoubleClick Members

    async public Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e)
    {
        var model = await appScope.ToExplorerScopeService().ShowKnownDialog(KnownDialogs.ConnectionString,
                                                                    model: new ConnectionStringModel("mssql", false));

        if (model != null)
        {
            DbConnectionString dbConnStr = model.DbConnectionString;
            ConfigConnections connStream = new ConfigConnections("mssql-sde", "546B0513-D71D-4490-9E27-94CD5D72C64A");

            string connectionString = dbConnStr.ConnectionString;
            string id = ConfigTextStream.ExtractValue(connectionString, "Database");
            id += "@" + ConfigTextStream.ExtractValue(connectionString, "Server");
            if (id == "@")
            {
                id = "MsSql Spatial ArcSde Connection";
            }
            id = connStream.GetName(String.IsNullOrWhiteSpace(id) ? "mssql-database" : id.Trim());
            connStream.Add(id, dbConnStr.ToString());

            e.NewExplorerObject = new MsSqlSpatialSdeExplorerObject(base.Parent, id, dbConnStr);
        }
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache != null && cache.Contains(FullName))
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return (parentExObject is MsSqlSpatialSdeExplorerGroupObject);
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        ExplorerObjectEventArgs e = new ExplorerObjectEventArgs();

        await ExplorerObjectDoubleClick(appScope, e);
        return e.NewExplorerObject;
    }

    #endregion
}
