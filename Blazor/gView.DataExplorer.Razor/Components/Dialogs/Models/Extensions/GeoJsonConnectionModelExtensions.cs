using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using gView.Framework.IO;
using gView.Framework.system;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
static public class GeoJsonConnectionModelExtensions
{
    static public string ToConnectionString(this GeoJsonConnectionModel connectionModel)
    {
        StringBuilder sb = new StringBuilder();

        sb.AddConnectionStringParameter("name", connectionModel.Name);
        sb.AddConnectionStringParameter("target", connectionModel.Uri);

        var waConnectionString = connectionModel.Credentials.ConnectionString;
        if (!String.IsNullOrEmpty(waConnectionString))
        {
            sb.Append($";{waConnectionString}");
        }

        return sb.ToString();
    }

    static public GeoJsonConnectionModel ToGeoJsonConnectionModel(this string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return new GeoJsonConnectionModel();
        }

        return new GeoJsonConnectionModel()
        {
            Name = ConfigTextStream.ExtractValue(connectionString, "name"),
            Uri = ConfigTextStream.ExtractValue(connectionString, "target"),
            Credentials = new Web.Framework.Web.Authorization.WebAuthorizationCredentials(connectionString)
        };
    }
}
