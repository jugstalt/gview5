using gView.Framework.UI;

namespace gView.Carto.Core.Models.Tree;

public class TocLegendNode : TocTreeNode
{
    public TocLegendNode(ITocElement tocElement) : base(tocElement) 
    {
        this.Text = "Legende";
    }

    public override bool IsChecked { get; set; }
}
