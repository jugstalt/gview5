using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.Carto.Abstraction;

public interface ICartoTool : IOrder, IDisposable
{
    string Name { get; }

    bool IsEnabled(IApplicationScope scope);

    string ToolTip { get; }

    ToolType ToolType { get; }

    string Icon { get; }

    CartoToolTarget Target { get; }

    Task<bool> OnEvent(IApplicationScope scope);
}
