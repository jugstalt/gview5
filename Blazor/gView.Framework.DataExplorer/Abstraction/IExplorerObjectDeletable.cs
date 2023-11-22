using gView.Framework.DataExplorer.Events;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerObjectDeletable
{
    event ExplorerObjectDeletedEvent ExplorerObjectDeleted;
    Task<bool> DeleteExplorerObject(ExplorerObjectEventArgs e);
}
