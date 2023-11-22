using gView.Blazor.Models.Table;
using System.Collections.Generic;

namespace gView.Blazor.Models.Extensions;

static public class RowItemExtensions
{
    static public RowItem AddData(this RowItem rowItem, string key, object? value)
    {
        ((Dictionary<string, object?>)rowItem.Data).Add(key, value);

        return rowItem;
    }

    static public RowItem SetIcon(this RowItem rowItem, string icon)
    {
        rowItem.Icon = icon;

        return rowItem;
    }



    static public bool HasColumn(this RowItem rowItem, string columnName)
        => rowItem.Data.ContainsKey(columnName);
}
