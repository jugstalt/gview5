using gView.Blazor.Models.Extensions;
using gView.Blazor.Models.Table;
using gView.DataSources.Fdb.MSSql;
using gView.DataSources.Fdb.PostgreSql;
using gView.DataSources.Fdb.SQLite;
using gView.Framework.Data;
using gView.Framework.Data.Cursors;
using gView.Framework.Data.Filters;
using gView.Framework.DataExplorer.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace gView.DataExplorer.Plugins.ExplorerTabPages;

[RegisterPlugIn("C71F3914-968A-486C-ADE4-B7D517A31560")]
internal class ImageDatasetTapPage : IExplorerTabPage
{
    private IExplorerObject? _exObject;
    private TableItem? _currentTable;

    #region IExplorerTabPage

    public Type RazorComponent => typeof(Razor.Components.Contents.TabPageDataTable);

    public string Title => "Images";

    public IExplorerObject? GetExplorerObject() => _exObject;

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
        if (instance is ITableClass)
        {
            var tableClass = (ITableClass)instance;

            var fieldNames = new[] { "Exists", "Path", "Provider" };
            var table = new TableItem(fieldNames);

            var filter = new QueryFilter()
            {
                Limit = 1000,
                SubFields = "PATH,RF_PROVIDER"
            };

            using var cursor = await tableClass.Search(filter);
            if (cursor == null)
            {
                return table.ToResult();
            }

            Dictionary<string, Type?> providerTypes = new();
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

                string? path = row["PATH"]?.ToString();

                rowItem.AddData("Exists", String.IsNullOrEmpty(path) ? false : File.Exists(path));
                rowItem.AddData("Path", path);

                var providerGuid = row["RF_PROVIDER"]?.ToString();
                if (!String.IsNullOrEmpty(providerGuid))
                {
                    Type? providerType = null;
                    if (providerTypes.ContainsKey(providerGuid))
                    {
                        providerType = providerTypes[providerGuid];
                    }
                    else
                    {
                        providerType = PlugInManager.Create(new Guid(providerGuid))?.GetType();
                        providerTypes[providerGuid] = providerType;
                    }

                    rowItem.AddData("Provider", providerType?.Name ?? "Unknown");
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

    async public Task<bool> ShowWith(IExplorerObject? exObject)
    {
        if (exObject == null)
        {
            return false;
        }

        if (exObject.ObjectType != null)
        {
            if (!exObject.ObjectType.IsAssignableTo(typeof(IFeatureClass)))
            {
                return false;
            }


            if (TypeHelper.Match(exObject.ObjectType, typeof(SqlFDBImageCatalogClass)))
            {
                return true;
            }

            if (TypeHelper.Match(exObject.ObjectType, typeof(pgImageCatalogClass)))
            {
                return true;
            }

            if (TypeHelper.Match(exObject.ObjectType, typeof(SQLiteFDBImageCatalogClass)))
            {
                return true;
            }
        }

        var instance = await exObject.GetInstanceAsync();

        if (instance is SqlFDBImageCatalogClass)
        {
            return true;
        }

        if (instance is pgImageCatalogClass)
        {
            return true;
        }

        if (instance is SQLiteFDBImageCatalogClass)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region IOrder

    public int SortOrder => 0;

    #endregion
}
