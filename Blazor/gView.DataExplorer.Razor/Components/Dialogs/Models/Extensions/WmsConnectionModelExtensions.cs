using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;

static public class WmsConnectionModelExtensions
{
    static public string ToConnectionString(this WmsConnectionModel connectionModel)
    {
        string connectionString = String.Empty;
        string credentials = String.IsNullOrEmpty(connectionModel.Username?.Trim()) ?
            string.Empty :
            $";usr={connectionModel.Username};pwd={connectionModel.Password ?? String.Empty}";

        switch (connectionModel.ConnectionType)
        {
            case WmsConnectionType.WMS:
                connectionString = $"wms={connectionModel.WmsUrl};service=WMS;servicename={connectionModel.ServiceName}{credentials}";
                break;
            case WmsConnectionType.WFS:
                connectionString = $"wfs={connectionModel.WfsUrl};service=WFS;servicename={connectionModel.ServiceName}{credentials}";
                break;
            case WmsConnectionType.WMS_WFS:
                connectionString = $"wms={connectionModel.WmsUrl};wfs={connectionModel.WfsUrl};service=WMS_WFS;servicename={connectionModel.ServiceName}{credentials}";
                break;
        }
        return connectionString;
    }

    static public WmsConnectionModel ToWmsConnectionModel(this string connectionString)
    {
        if(string.IsNullOrEmpty(connectionString))
        {
            return new WmsConnectionModel();
        }

        return new WmsConnectionModel()
        {
            ConnectionType = ConfigTextStream.ExtractValue(connectionString, "service")?.ToUpper() switch 
            { 
                "WFS" => WmsConnectionType.WFS,
                "WMS_WFS" => WmsConnectionType.WMS_WFS,
                _ => WmsConnectionType.WMS
            },
            WmsUrl = ConfigTextStream.ExtractValue(connectionString, "wms"),
            WfsUrl = ConfigTextStream.ExtractValue(connectionString, "wfs"),
            ServiceName = ConfigTextStream.ExtractValue(connectionString, "servicename"),
            Username = ConfigTextStream.ExtractValue(connectionString, "usr"),
            Password = ConfigTextStream.ExtractValue(connectionString, "pwd")
        };
    }
}
