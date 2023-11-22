using gView.Framework.Data;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.GeoProcessing;

namespace gView.DataExplorer.Razor.Components.Dialogs.Filters;

public class OpenActivityDataFilter : ExplorerOpenDialogFilter
{
    private IActivityData _aData;

    public OpenActivityDataFilter(IActivityData aData)
        : base(aData.DisplayName)
    {
        _aData = aData;
    }

    async public override Task<bool> Match(IExplorerObject exObject)
    {
        if (exObject == null)
        {
            return false;
        }

        var instatnce = await exObject.GetInstanceAsync();
        if (instatnce is IDatasetElement)
        {
            return _aData.ProcessAble((IDatasetElement)instatnce);
        }

        if (instatnce is IClass)
        {
            DatasetElement element = new DatasetElement((IClass)instatnce);
            return _aData.ProcessAble(element);
        }

        return false;
    }
}
