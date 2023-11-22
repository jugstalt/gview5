using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerObjects.Tiles;

public interface ITilesExplorerGroupObject : IExplorerObject
{
    void SetParentExplorerObject(IExplorerObject parentExplorerObject);
}
