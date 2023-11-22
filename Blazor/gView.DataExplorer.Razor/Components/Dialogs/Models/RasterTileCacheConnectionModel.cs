using gView.Blazor.Models.Dialogs;
using gView.Framework.Geometry;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class RasterTileCacheConnectionModel : IDialogResultItem
{
    public RasterTileCacheConnectionModel()
    {

    }
    public string Name { get; set; } = string.Empty;
    public IEnvelope Extent { get; set; } = new Envelope();
    public ISpatialReference? SpatialReference { get; set; }
    public TileOriginModel TileOrigin { get; set; } = new TileOriginModel();

    public TileScalesModel TileScales { get; set; } = new TileScalesModel();

    public int TileWidth { get; set; } = 256;
    public int TileHeight { get; set; } = 256;
    public string TileUrl { get; set; } = string.Empty;

    public string CopyrightInformation { get; set; } = string.Empty;
}
