using gView.Blazor.Models.Dialogs;
using gView.Framework.Data;

namespace gView.Carto.Razor.Components.Dialogs.Models;

public class QueryBuilderModel : IDialogResultItem
{
    public ITableClass? TableClass { get; set; }

    public string QueryString { get; set; } = string.Empty;
}
