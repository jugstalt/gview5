﻿using gView.Blazor.Models.Tree;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.Models.Tree;

public class ExplorerTreeNode : TreeItem
{
    private readonly IExplorerObject _exObject;

    public ExplorerTreeNode(IExplorerObject exObject)
    {
        _exObject = exObject;

        base.Text = exObject.Name;
        base.Icon = exObject.Icon;
        base.IsExpanded = false;
        base.HasChildren = exObject is IExplorerParentObject;
    }

    public IExplorerObject ExObject => _exObject;
}