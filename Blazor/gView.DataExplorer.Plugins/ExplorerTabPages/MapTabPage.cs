using gView.Blazor.Models.Content;
using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTabPages;

[RegisterPlugIn("3143FA90-587B-4cd0-B508-1BE88882E6B3")]
public class MapTabPage : IExplorerTabPage
{
    private IExplorerObject? _exObject;

    #region IExplorerTabPage

    public Type RazorComponent => typeof(gView.DataExplorer.Razor.Components.Contents.TabPageMap);

    public string Title => "Map";

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

    public Task<IContentItemResult> RefreshContents()
    {
        return Task.FromResult<IContentItemResult>(new ContentItemResult());
    }

    public Task SetExplorerObjectAsync(IExplorerObject? value)
    {
        _exObject = value;

        return Task.FromResult(true);
    }

    public Task<bool> ShowWith(IExplorerObject? exObject)
    {
        if (exObject == null)
        {
            return Task.FromResult(false);
        }

        if(TypeHelper.Match(exObject.ObjectType, typeof(IWebFeatureClass)))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(
            TypeHelper.Match(exObject.ObjectType, typeof(IFeatureClass)) ||
            TypeHelper.Match(exObject.ObjectType, typeof(IRasterClass)) ||
            TypeHelper.Match(exObject.ObjectType, typeof(IWebServiceClass)) ||
            TypeHelper.Match(exObject.ObjectType, typeof(IFeatureDataset)));
    }

    #endregion

    #region IOrder

    public int SortOrder => 10;

    #endregion
}
