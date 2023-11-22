using gView.Framework.Geometry;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class TileOriginModel
{
    public TileOrigin TileOrigin { get; set; } = TileOrigin.UpperLeft;

    public Point Origin { get; set; } = new Point();
}
