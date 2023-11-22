using gView.Framework.IO;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;

static public class ArcImsConnectionModelExtensions
{
    static public string ToConnectionString(this ArcImsConnectionModel connectionModel)
    {
        return $"server={connectionModel.Server};user={connectionModel.Username};pwd={connectionModel.Password}";
    }

    static public ArcImsConnectionModel ToArcImsConnectionModel(this string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return new ArcImsConnectionModel();
        }

        return new ArcImsConnectionModel()
        {
            Server = ConfigTextStream.ExtractValue(connectionString, "server"),
            Username = ConfigTextStream.ExtractValue(connectionString, "user"),
            Password = ConfigTextStream.ExtractValue(connectionString, "pwd")
        };
    }
}
