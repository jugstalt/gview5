using gView.DataExplorer.Plugins.ExplorerObjects.Base;
using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Extensions;

static internal class IExplorerObjectExtensions
{
    static public bool IsNull(this IExplorerObject exObject)
        => exObject == null || exObject is NullParentExplorerObject;
}
