using gView.Blazor.Models.Tree;
using gView.Framework.UI;
using System.Linq;

namespace gView.Carto.Core.Models.Tree;

abstract public class TocTreeNode : TreeItem<TocTreeNode>
{
    private readonly ITocElement _tocElement;

    public TocTreeNode(ITocElement tocElement)
    {
        _tocElement = tocElement;

        this.Text = _tocElement.Name ?? string.Empty;
    }

    public ITocElement TocElement => _tocElement;

    public override bool HasChildren
    {
        get => this.Children?.Any() ?? false;
        set { }
    }

    abstract public bool IsChecked { get; set; }

    public override void Dispose()
    {
        if (Children is not null)
        {
            foreach (var childNode in Children)
            {
                childNode.Dispose();
            }

            Children = null;
        }
    }
}
