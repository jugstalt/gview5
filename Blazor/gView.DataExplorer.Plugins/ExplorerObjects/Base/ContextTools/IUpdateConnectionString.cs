using gView.Framework.Db;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Base.ContextTools;

internal interface IUpdateConnectionString
{
    DbConnectionString GetDbConnectionString();
    Task<bool> UpdateDbConnectionString(DbConnectionString dbConnectionString);
}
