using gView.Framework.DataExplorer.Abstraction;

namespace gView.DataExplorer.Plugins.ExplorerObjects.VectorData;

public interface IVectorDataExplorerGroupObject : IExplorerObject
{
    void SetParentExplorerObject(IExplorerObject parentExplorerObject);
}
