using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.DataExplorer.Plugins.ExplorerObjects.Fdb.ContextTools;
using gView.DataExplorer.Plugins.Extensions;
using gView.DataExplorer.Razor.Components.Dialogs.Models;
using gView.DataExplorer.Razor.Components.Dialogs.Models.Extensions;
using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.Geometry;
using gView.Framework.IO;
using gView.Framework.system;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles.Raster;

[RegisterPlugIn("5FEA8712-9462-457F-B099-4B42CD946B77")]
public class TileCacheFromTemplate : ExplorerObjectCls<TileCacheGroupExplorerObject>,
                                     IExplorerObject,
                                     IExplorerObjectCreatable
{
    public TileCacheFromTemplate()
        : base()
    {
    }

    public TileCacheFromTemplate(TileCacheGroupExplorerObject parent)
        : base(parent, 0)
    {
    }

    #region IExplorerObject Member

    public string Name => "Import From Template";

    public string FullName => "";

    public string Type => "Import Tilecache from Template";

    public string Icon => "basic:screw-wrench-double";

    public void Dispose()
    {

    }

    public Task<object?> GetInstanceAsync() => Task.FromResult<object?>(null);


    #endregion

    #region IExplorerObjectCreatable Member

    public bool CanCreate(IExplorerObject parentExObject)
    {
        return (parentExObject is TileCacheGroupExplorerObject);
    }

    async public Task<IExplorerObject?> CreateExplorerObjectAsync(IApplicationScope appScope, IExplorerObject parentExObject)
    {
        var model = await appScope.ToExplorerScopeService().ShowModalDialog(typeof(gView.DataExplorer.Razor.Components.Dialogs.RasterTileCacheImportFromTemplateDialog),
                                                                    "Import from Template",
                                                                    new RasterTileCacheImportFromTemplateModel()
                                                                    {
                                                                        RootPath = System.IO.Path.Combine(SystemVariables.ApplicationDirectory, "misc", "tiling", "import")
                                                                    });

        if (!String.IsNullOrEmpty(model?.TemplatePath))
        {
            XmlStream stream = new XmlStream("TileService");
            stream.ReadStream(model.TemplatePath);

            var connectionModel = new RasterTileCacheConnectionModel();

            #region Load ConnectionModel from Stream

            connectionModel.Name = (string)stream.Load("name", String.Empty);
            connectionModel.Extent = new Envelope(
                (double)stream.Load("extent_minx0", 0D),
                (double)stream.Load("extent_miny0", 0D),
                (double)stream.Load("extent_maxx0", 0D),
                (double)stream.Load("extent_maxy0", 0D));

            connectionModel.TileOrigin.TileOrigin = (TileOrigin)(int)stream.Load("origin", (int)TileOrigin.UpperLeft);

            double? originX = (double?)stream.Load("origin_x", (double?)null);
            double? originY = (double?)stream.Load("origin_y", (double?)null);
            if (originX.HasValue && originY.HasValue) 
            {
                connectionModel.TileOrigin.Origin = new Point(originX.Value, originY.Value);
            }
            else
            {
                connectionModel.TileOrigin.Origin = new Point(connectionModel.Extent.minx,
                    connectionModel.TileOrigin.TileOrigin == TileOrigin.LowerLeft ? connectionModel.Extent.miny : connectionModel.Extent.maxy);
            }

            connectionModel.TileWidth = (int)stream.Load("tile_width", 100);
            connectionModel.TileHeight = (int)stream.Load("tile_height", 100);

            int scales_count = (int)stream.Load("scales_count", 0);
            for (int i = 0; i < scales_count; i++)
            {
                double s = (double)stream.Load("scale" + i, (double)0D);
                if (s == 0D)
                {
                    continue;
                }

                connectionModel.TileScales.Scales.Add(s);
            }

            connectionModel.SpatialReference = (ISpatialReference)stream.Load("SpatialReference", null, new SpatialReference());

            connectionModel.TileUrl = (string)stream.Load("tile_url", String.Empty);
            connectionModel.CopyrightInformation = (string)stream.Load("copyright", String.Empty);

            ConfigConnections connStream = new ConfigConnections("TileCache", "b9d6ae5b-9ca1-4a52-890f-caa4009784d4");

            #endregion

            string connectionString = connectionModel.ToConnectionString();
            string id = String.IsNullOrWhiteSpace(model.Name) ? 
                                    new FileInfo(model.TemplatePath).Name.Split('.').First() : 
                                    model.Name.Trim();
            id = connStream.GetName(id);

            connStream.Add(id, connectionString);

            return new TileCacheDatasetExplorerObject(new TileCacheGroupExplorerObject(), id, connectionString);
        }

        return null;
    }

    #endregion

    #region ISerializableExplorerObject Member

    public Task<IExplorerObject?> CreateInstanceByFullName(string FullName, ISerializableExplorerObjectCache? cache)
    {
        if (cache?.Contains(FullName) == true)
        {
            return Task.FromResult<IExplorerObject?>(cache[FullName]);
        }

        return Task.FromResult<IExplorerObject?>(null);
    }

    #endregion
}
