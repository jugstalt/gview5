using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.Carto.Core.Extensions;
internal static class EventExtensions
{
    async static public Task FireAsync(this Func<Task> eventFunction)
    {
        if (eventFunction != null)
        {
            foreach (var handler in eventFunction.GetInvocationList()
                                                 .OfType<Func<Task>>())
            {
                await handler.Invoke();
            }
        }
    }

    async static public Task FireAsync<T>(this Func<T, Task> eventFunction, T eventArg)
    {
        if (eventFunction != null)
        {
            foreach (var handler in eventFunction.GetInvocationList()
                                                 .OfType<Func<T, Task>>())
            {
                await handler.Invoke(eventArg);
            }
        }
    }

    async static public Task FireAsync<T1, T2>(this Func<T1, T2, Task> eventFunction, T1 eventArg1, T2 eventArg2)
    {
        if (eventFunction != null)
        {
            foreach (var handler in eventFunction.GetInvocationList()
                                                 .OfType<Func<T1, T2, Task>>())
            {
                await handler.Invoke(eventArg1, eventArg2);
            }
        }
    }
}
