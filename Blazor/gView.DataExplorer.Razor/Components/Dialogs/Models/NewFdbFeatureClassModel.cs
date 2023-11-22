using gView.Blazor.Models.Dialogs;
using gView.Framework.Data;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class NewFdbFeatureClassModel : IDialogResultItem
{
    public NewFdbFeatureClassModel() 
    {
        this.SpatialIndex = new BinaryTreeDef(new Framework.Geometry.Envelope(), 5);
    }
    public NewFdbFeatureClassModel(BinaryTreeDef spatialIndex)
    {
        this.SpatialIndex = spatialIndex;
    }

    public string Name { get; set; } = string.Empty;

    public NewFeatureClassGeometryType GeometryType { get; set; }

    public BinaryTreeDef SpatialIndex { get; set; }

    public Dictionary<string, Field> Fields { get; set; } = new Dictionary<string, Field>();
}
