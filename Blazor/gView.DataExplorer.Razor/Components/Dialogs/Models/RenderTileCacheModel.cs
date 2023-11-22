using gView.Blazor.Models.Dialogs;
using gView.Framework.Geometry.Tiling;
using gView.Framework.Metadata;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class RenderTileCacheModel : IDialogResultItem
{
    public RenderTileCacheModel()
    {
        Server = "";
        Service = "";
        Metadata = new TileServiceMetadata();
    }

    public RenderTileCacheModel(string server, string service, TileServiceMetadata metadata)
    {
        Server = server;
        Service = service;
        Metadata = metadata;

        EpsgCode = metadata.EPSGCodes != null && metadata.EPSGCodes.Count > 0 ? metadata.EPSGCodes[0] : 0;

        GridOrientation = metadata.UpperLeft ? GridOrientation.UpperLeft :
                          metadata.LowerLeft ? GridOrientation.LowerLeft : GridOrientation.UpperLeft;

        ImageFormat = metadata.FormatPng ? TileImageFormat.png : 
                      metadata.FormatJpg ? TileImageFormat.jpg : TileImageFormat.png;

        ThreadCount = 1;
    }

    public string Server { get; }
    public string Service { get; }
    public TileServiceMetadata Metadata { get; set; }

    public int EpsgCode { get; set; }
    public GridOrientation GridOrientation { get; set; }
    public TileImageFormat ImageFormat { get; set; }
    public int ThreadCount { get; set; }
    public bool Compact { get; set; }
    public ICollection<double> Scales { get; } = new List<double>();
}
