using gView.Blazor.Models.Content;
using gView.Blazor.Models.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace gView.Blazor.Models.Table;
public class RowItem : ContentItem
{
    private readonly Dictionary<string, object?> _data;

    public RowItem()
    {
        _data = new Dictionary<string, object?>();
    }

    public string? Icon { get; internal set; }

    public IDictionary<string, object?> Data => _data;

    public object? this[string column]
        => this.HasColumn(column) ? _data[column] : null;
}
