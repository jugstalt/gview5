using gView.Carto.Core.Extensions;
using gView.Framework.UI;
using System.Collections.Generic;

namespace gView.Carto.Core.Models.Tree;

public class TocLayerNode : TocTreeNode
{
    public TocLayerNode(ITocElement tocElement) : base(tocElement)
    {
        if (tocElement.HasLegendItems())
        {
            this.Children = new HashSet<TocTreeNode>();
            this.Children.Add(new TocLegendNode(tocElement));
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
