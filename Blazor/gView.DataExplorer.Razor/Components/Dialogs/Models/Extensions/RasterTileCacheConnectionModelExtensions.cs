using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using System.Text;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
static public class RasterTileCacheConnectionModelExtensions
{
    static public string ToConnectionString(this RasterTileCacheConnectionModel connectionModel)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append($"name={connectionModel.Name}");
        sb.Append($";extent={connectionModel.Extent.minx.ToString(Numbers.Nhi)},{connectionModel.Extent.miny.ToString(Numbers.Nhi)},{connectionModel.Extent.maxx.ToString(Numbers.Nhi)},{connectionModel.Extent.maxy.ToString(Numbers.Nhi)}");
        sb.Append($";origin={(int)connectionModel.TileOrigin.TileOrigin}");
        sb.Append($";origin_x={connectionModel.TileOrigin.Origin.X.ToString(Numbers.Nhi)}");
        sb.Append($";origin_y={connectionModel.TileOrigin.Origin.Y.ToString(Numbers.Nhi)}");

        if (connectionModel.SpatialReference != null)
        {
            sb.Append(";sref64=" + connectionModel.SpatialReference.ToBase64String());
        }

        sb.Append($";scales={String.Join(",", connectionModel.TileScales.Scales.Select(s => s.ToString(Numbers.Nhi)))}");


        sb.Append($";tilewidth={connectionModel.TileWidth}");
        sb.Append($";tileheight={connectionModel.TileHeight}");
        sb.Append($";tileurl={connectionModel.TileUrl}");
        sb.Append($";copyright={connectionModel.CopyrightInformation}");

        return sb.ToString();
    }

    static public RasterTileCacheConnectionModel ToRasterTileCacheConnectionModel(this string connectionString)
    {
        if (String.IsNullOrEmpty(connectionString))
        {
            return new RasterTileCacheConnectionModel();
        }

        double[] extent = ConfigTextStream.ExtractValue(connectionString, "extent")
                .Split(',')
                .Select(s => double.Parse(s, Numbers.Nhi))
                .ToArray();
        double[] origin = ConfigTextStream.ExtractValue(connectionString, "extent")
                .Split(',')
                .Select(s => double.Parse(s, Numbers.Nhi))
                .ToArray();

        var model = new RasterTileCacheConnectionModel()
        {
            Name = ConfigTextStream.ExtractValue(connectionString, "name"),
            Extent = extent.Length == 4 ? new Envelope(extent[0], extent[1], extent[2], extent[3]) : Envelope.Null(),
            TileWidth = int.Parse(ConfigTextStream.ExtractValue(connectionString, "tilewidth")),
            TileHeight = int.Parse(ConfigTextStream.ExtractValue(connectionString, "tileheight")),
            TileUrl = ConfigTextStream.ExtractValue(connectionString, "tileurl"),
            CopyrightInformation = ConfigTextStream.ExtractValue(connectionString, "copyright")
        };

        if (!String.IsNullOrEmpty(ConfigTextStream.ExtractValue(connectionString, "sref64")))
        {
            model.SpatialReference = new SpatialReference();
            model.SpatialReference.FromBase64String(ConfigTextStream.ExtractValue(connectionString, "sref64"));
        }

        model.TileOrigin.TileOrigin = (TileOrigin)int.Parse(ConfigTextStream.ExtractValue(connectionString, "origin"));
        if (!String.IsNullOrEmpty(ConfigTextStream.ExtractValue(connectionString, "origin_x")) &&
            !String.IsNullOrEmpty(ConfigTextStream.ExtractValue(connectionString, "origin_y")))
        {
            model.TileOrigin.Origin = new Point(
                double.Parse(ConfigTextStream.ExtractValue(connectionString, "origin_x"), Numbers.Nhi),
                double.Parse(ConfigTextStream.ExtractValue(connectionString, "origin_y"), Numbers.Nhi));
        }

        if (!String.IsNullOrEmpty(ConfigTextStream.ExtractValue(connectionString, "scales")))
        {
            model.TileScales.Scales = ConfigTextStream.ExtractValue(connectionString, "scales")
                .Split(',')
                .Select(s => double.Parse(s, Numbers.Nhi))
                .ToList();
        }

        return model;
    }
}
