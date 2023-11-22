using gView.Framework.DataExplorer.Events;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerObjectRenamable
{
    event ExplorerObjectRenamedEvent ExplorerObjectRenamed;
    Task<bool> RenameExplorerObject(string newName);
}
