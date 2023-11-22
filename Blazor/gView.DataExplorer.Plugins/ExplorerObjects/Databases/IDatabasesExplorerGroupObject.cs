using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Databases;

public interface IDatabasesExplorerGroupObject : IExplorerObject
{
    void SetParentExplorerObject(IExplorerObject parentExplorerObject);
}
