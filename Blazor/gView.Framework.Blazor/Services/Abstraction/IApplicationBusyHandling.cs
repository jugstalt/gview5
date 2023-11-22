using System.Threading.Tasks;
using System;

namespace gView.Framework.Blazor.Services.Abstraction;
public interface IApplicationBusyHandling
{
    Task<IAsyncDisposable> RegisterBusyTaskAsync(string task);
}
