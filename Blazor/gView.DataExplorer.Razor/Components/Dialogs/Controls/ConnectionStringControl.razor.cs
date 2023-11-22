using gView.Framework.Db;
using gView.Framework.system;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace gView.DataExplorer.Razor.Components.Dialogs.Controls;

public partial class ConnectionStringControl
{
    private CommonDbConnectionsModelModel? _commonDbConnections;

    async private Task LoadConnectionStringsModel()
    {
        try
        {
            string configPath = Path.Combine(SystemVariables.ApplicationDirectory, "gView.DB.UI.json");

            _commonDbConnections = JsonSerializer.Deserialize<CommonDbConnectionsModelModel>(await File.ReadAllTextAsync(configPath),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch { }
    }

    private IEnumerable<string> ConnectionStringParameters(string connectionString)
    {
        try
        {
            string pattern = @"\[(.*?)\]";

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(connectionString);

            return matches.Select(x => x.Value.Substring(1, x.Value.Length - 2));
        }
        catch
        {
            return Array.Empty<string>();
        }

    }
}
