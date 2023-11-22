using gView.Blazor.Models.Extensions;
using gView.Blazor.Models.Table;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTabPages;

[RegisterPlugIn("62661EF5-2B98-4189-BFCD-7629476FA91C")]
public class DataTableTabPage : IExplorerTabPage
{
    private IExplorerObject? _exObject;
    private TableItem? _currentTable;

    public DataTableTabPage()
    {
    }

    #region IExplorerTabPage

    public Type RazorComponent => typeof(gView.DataExplorer.Razor.Components.Contents.TabPageDataTable);

    public string Title => "Data Table";

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
        if (_currentTable != null)
        {
            return _currentTable.ToResult();
        }

        if (_exObject == null)
        {
            return new TableItem(Array.Empty<string>()).ToResult();
        }

        var instance = await _exObject.GetInstanceAsync();
        if (instance is ITableClass tableClass)
        {
            var fieldNames = tableClass.Fields?
                                       .ToEnumerable()
                                       .Select(f => f.name)
                                       .ToArray() ?? Array.Empty<string>();
            
            var table = new TableItem(fieldNames);

            var filter = new QueryFilter()
            {
                Limit = 100,
                SubFields = "*"
            };

            using var cursor = await tableClass.Search(filter);
            if (cursor == null)
            {
                return table.ToResult();
            }

            var count = 0;
            while (true)
            {
                IRow? row = cursor switch
                {
                    IRowCursor rowCursor => await rowCursor.NextRow(),
                    IFeatureCursor featureCursor => await featureCursor.NextFeature(),
                    _ => null
                };

                if (row == null || ++count > filter.Limit)
                {
                    break;
                }

                var rowItem = table.AddRow();
                foreach (var fieldName in fieldNames)
                {
                    rowItem.AddData(fieldName, row[fieldName]);
                }
            }

            return (_currentTable = table).ToResult();
        }

        return new TableItem(Array.Empty<string>()).ToResult();
    }

    public Task SetExplorerObjectAsync(IExplorerObject? value)
    {
        if (_exObject != value)
        {
            _exObject = value;
            _currentTable = null;
        }

        return Task.CompletedTask;
    }

    public Task<bool> ShowWith(IExplorerObject? exObject)
    {
        if (exObject == null)
        {
            return Task.FromResult(false);
        }

        if (TypeHelper.Match(exObject.ObjectType, typeof(ITableClass)))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    #endregion

    #region IOrder

    public int SortOrder => 20;

    #endregion
}
