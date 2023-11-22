using gView.Blazor.Models.Dialogs;
using gView.Framework.Db;
using Newtonsoft.Json.Converters;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class ConnectionStringModel : IDialogResultItem
{
    private readonly DbConnectionString _dbConnectionString;

    public ConnectionStringModel() 
        : this("", true)
    {
    }

    public ConnectionStringModel(string providerId,
                                 bool UseProviderInConnectionString)
    {
        _dbConnectionString = DbConnectionString.Build(providerId, UseProviderInConnectionString);
    }

    public ConnectionStringModel(DbConnectionString dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
    }

    public string ProviderId => _dbConnectionString.ProviderId;

    public DbConnectionString DbConnectionString => _dbConnectionString;
}
