using gView.Blazor.Models.Table;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Threading.Tasks;
using gView.Blazor.Models.Extensions;

namespace gView.DataExplorer.Plugins.ExplorerTabPages;

[RegisterPlugIn("30B86C28-FB47-4ee2-B15F-06D4BD8F47D2")]
public class FeatureClassTabPage : IExplorerTabPage
{
    private IExplorerObject? _exObject;

    #region IExplorerTabPage

    public Type RazorComponent => typeof(gView.DataExplorer.Razor.Components.Contents.TabPageFeatureClass);

    public string Title => "FeatureClass";

    public IExplorerObject? GetExplorerObject()
        => _exObject;

    public void OnHide()
    {

    }

    public Task<bool> OnShow()
    {
        return Task.FromResult(true);
    }

    async public Task<IContentItemResult> RefreshContents()
    {
        var table = new TableItem(new[] { "Field", "Aliasname", "Type" })
            .SetExplorerObject(_exObject);

        if (_exObject == null)
        {
            return table.ToResult();
        }

        var instance = await _exObject.GetInstanceAsync();

        if (!(instance is IFeatureClass))
        {
            return table.ToResult();
        }

        IFeatureClass fc = (IFeatureClass)instance;

        if (fc.Fields == null)
        {
            return table.ToResult();
        }

        foreach (IField field in fc.Fields.ToEnumerable())
        {
            table.AddRow()
                .AddData("Field", field.name)
                .AddData("Aliasname", field.aliasname)
                .AddData("Type", field.type)
                .SetIcon("basic:table");
        }

        return table.ToResult();
    }

    public Task SetExplorerObjectAsync(IExplorerObject? value)
    {
        _exObject = value;

        return Task.CompletedTask;
    }

    public Task<bool> ShowWith(IExplorerObject? exObject)
    {
        if (exObject == null)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(TypeHelper.Match(exObject.ObjectType, typeof(IFeatureClass)));
    }

    #endregion

    #region IOrder

    public int SortOrder => 0;

    #endregion
}
