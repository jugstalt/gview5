using gView.Blazor.Models.Tree;
using gView.Framework.DataExplorer.Abstraction;
using System.Linq;

namespace gView.DataExplorer.Core.Models.Tree;

public class ExplorerTreeNode : TreeItem<ExplorerTreeNode>
{
    private readonly IExplorerObject _exObject;

    public ExplorerTreeNode(IExplorerObject exObject)
    {
        _exObject = exObject;

        base.Text = exObject.Name;
        base.Icon = exObject.Icon;
        base.IsExpanded = false;
        base.HasChildren = exObject is IExplorerParentObject;

        if (HasChildren)
        {
            base.Children = new System.Collections.Generic.HashSet<ExplorerTreeNode>()
            {
                new ExplorerTreeNode(new DummyExplorerObject())
            };
        }
    }

    public IExplorerObject ExObject => _exObject;

    public bool IsServerLoaded
        => (this.Children == null || this.Children.FirstOrDefault()?.ExObject is DummyExplorerObject) == false;

    public override void Dispose()
    {
        
    }
}