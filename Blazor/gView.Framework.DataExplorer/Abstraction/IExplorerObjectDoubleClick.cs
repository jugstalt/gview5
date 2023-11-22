using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.DataExplorer.Events;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerObjectDoubleClick
{
    Task ExplorerObjectDoubleClick(IApplicationScope appScope, ExplorerObjectEventArgs e);
}
