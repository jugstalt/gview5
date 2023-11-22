namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public enum ExploerDialogMode
{
    Open = 0,
    Save = 1
}

public enum TileOrigin
{
    LowerLeft = 0,
    UpperLeft = 1
}

public enum NewFeatureClassGeometryType
{
    Point,
    Polyline,
    Polygon
}

public enum NewFdbDatasetType
{
    FeatureDataset,
    ImageDataset
}