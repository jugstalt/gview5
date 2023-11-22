using gView.Blazor.Models.Content;
using gView.Blazor.Models.Extensions;
using gView.Blazor.Models.Table;
using gView.Framework.DataExplorer.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTabPages;

[gView.Framework.system.RegisterPlugIn("47328671-87F7-4cf7-B59D-2BC111A29BA3")]
public class ContentTabPage : IExplorerTabPage
{
    private IExplorerObject? _exObject;

    #region IExplorerTabPage

    public Type RazorComponent => typeof(Razor.Components.Contents.TabPageConent);

    public string Title => "Content";

    public IExplorerObject? GetExplorerObject()
    {
        return _exObject;
    }

    public void OnHide()
    {

    }

    public Task<bool> OnShow()
    {
        return Task.FromResult(true);
    }

    async public Task<IContentItemResult> RefreshContents()
    {
        var table = new TableItem(new[] { "Name", "Type" })
            .SetExplorerObject(_exObject);

        if (_exObject is IExplorerParentObject)
        {
            var childs = await ((IExplorerParentObject)_exObject).ChildObjects();

            if (childs != null)
            {
                foreach (var child in childs.OrderBy(c => c.Priority))
                {
                    table.AddRow()
                        .SetExplorerObject(child)
                        .AddData("Name", child.Name)
                        .AddData("Type", child.Type)
                        .SetIcon(child.Icon);
                }
            }
        }

        return new ContentItemResult() { Item = table };
    }

    public Task SetExplorerObjectAsync(IExplorerObject? value)
    {
        _exObject = value;

        return Task.CompletedTask;
    }

    public Task<bool> ShowWith(IExplorerObject? exObject)
    {
        return Task.FromResult(exObject is IExplorerParentObject);
    }

    #endregion

    #region IOrder
    public int SortOrder => 0;

    #endregion
}
