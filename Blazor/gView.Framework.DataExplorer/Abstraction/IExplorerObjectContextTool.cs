using gView.Framework.Blazor.Services.Abstraction;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction;

public interface IExplorerObjectContextTool
{
    string Name { get; }
    string Icon { get; }

    bool IsEnabled(IApplicationScope scope, IExplorerObject exObject);
    Task<bool> OnEvent(IApplicationScope scope, IExplorerObject exObject);
}
