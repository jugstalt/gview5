using gView.Blazor.Models.Dialogs;
using gView.DataExplorer.Razor.Components.Dialogs.Filters;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Razor.Components.Dialogs.Models;

public class ExplorerDialogModel : IDialogResultItem
{
    public ExplorerDialogModel()
    {
        Result = new ModelResult();
    }

    public required List<ExplorerDialogFilter> Filters { get; set; }
    public required ExploerDialogMode Mode { get; set; }

    public ModelResult Result { get; }

    public class ModelResult
    {
        internal ModelResult() { ExplorerObjects = new List<IExplorerObject>(); }
        public string Name { get; set; } = string.Empty;
        public List<IExplorerObject> ExplorerObjects { get; }
    }
}
