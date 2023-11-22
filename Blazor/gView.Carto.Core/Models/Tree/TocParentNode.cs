using gView.Framework.UI;
using System.Linq;

namespace gView.Carto.Core.Models.Tree;

public class TocParentNode : TocTreeNode
{
    public TocParentNode(ITocElement tocElement) : base(tocElement)
    {
    }

    public override bool IsExpanded
    {
        get => base.TocElement?.ElementType switch
        {
            TocElementType.ClosedGroup => false,
            TocElementType.OpenedGroup => true,
            _ => false
        };
        set
        {
            base.TocElement?.OpenCloseGroup(value);
        }
    }

    public override bool IsChecked
    {
        get
        {
            return base.TocElement.LayerVisible;
        }
        set
        {
            base.TocElement.LayerVisible = value;
        }
    }
}
