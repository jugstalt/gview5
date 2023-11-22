using gView.Framework.Db;
using gView.Framework.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Fdb.PostgreSql.Extensions;

static class DbConnectionStringExtensions
{
    static public PostgreSqlExplorerObject ToPostgreSqlExplorerObject(this DbConnectionString dbConnectionString,
                                                                   PostgreSqlExplorerGroupObject parent)
    {
        ConfigConnections connStream = new ConfigConnections("postgrefdb", "546B0513-D71D-4490-9E27-94CD5D72C64A");

        string connectionString = dbConnectionString.ConnectionString;
        string id = $"{ConfigTextStream.ExtractValue(connectionString, "database")}@{ConfigTextStream.ExtractValue(connectionString, "server")}";
        if (id == "@")
        {
            id = "PostgreFDB Connection";
        }

        id = connStream.GetName(id);

        connStream.Add(id, dbConnectionString.ToString());
        return new PostgreSqlExplorerObject(parent, id, dbConnectionString);
    }
}
