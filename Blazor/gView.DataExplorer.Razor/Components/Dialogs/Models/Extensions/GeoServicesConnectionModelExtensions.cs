using gView.Framework.IO;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;

static public class GeoServicesConnectionModelExtensions
{
    static public string ToConnectionString(this GeoServicesConnectionModel connectionModel)
    {
        return $"server={connectionModel.ServicesUrl};user={connectionModel.Username};pwd={connectionModel.Password}";
    }

    static public GeoServicesConnectionModel ToGeoServicesConnectionModel(this string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return new GeoServicesConnectionModel();
        }

        return new GeoServicesConnectionModel()
        {
            ServicesUrl = ConfigTextStream.ExtractValue(connectionString, "server"),
            Username = ConfigTextStream.ExtractValue(connectionString, "user"),
            Password = ConfigTextStream.ExtractValue(connectionString, "pwd")
        };
    }
}
