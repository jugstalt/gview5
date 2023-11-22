using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.UI;
using System;
using System.Threading.Tasks;

namespace gView.Framework.DataExplorer.Abstraction
{
    public interface IExplorerTool : IOrder, IDisposable
    {
        string Name { get; }

        bool IsEnabled(IApplicationScope scope);

        string ToolTip { get; }

        string Icon { get; }

        ExplorerToolTarget Target { get; }

        Task<bool> OnEvent(IApplicationScope scope);
    }
}
