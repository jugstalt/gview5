using gView.Framework.Blazor.Services.Abstraction;
using gView.Framework.system;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gView.Framework.Blazor.Services;
public abstract class ApplictionBusyHandler : IApplicationBusyHandling
{
    private int _runningBusyTasks = 0;
    private ConcurrentDictionary<Guid, string> _tasks = new ConcurrentDictionary<Guid, string>();

    public async Task<IAsyncDisposable> RegisterBusyTaskAsync(string task)
    {
        using (var mutex = await FuzzyMutexAsync.LockAsync("busy"))
        {
            _runningBusyTasks++;

            var id=Guid.NewGuid();
            _tasks.TryAdd(id, task);

            await HandleBusyStatusChanged(true, ComposeMessage());
            await SetBusyCursor();

            return new BusyTask(this, id);
        }
    }

    async private Task ReleaseBusyTaskAsync(BusyTask busyTask)
    {
        using (var mutex = await FuzzyMutexAsync.LockAsync("busy"))
        {
            --_runningBusyTasks;

            _tasks.TryRemove(busyTask.Id, out string _);

            if (_runningBusyTasks == 0)
            {
                await SetDefaultCursor();
            }

            await HandleBusyStatusChanged(_runningBusyTasks > 0, ComposeMessage());
        }
    }

    private string ComposeMessage()
        => String.Join(", ", _tasks.Keys.ToArray()
                                        .Select(k => _tasks[k]));
                 
    #region Abstract Members

    abstract protected Task HandleBusyStatusChanged(bool isBussy, string message);
    abstract protected ValueTask SetBusyCursor();
    abstract protected ValueTask SetDefaultCursor();

    #endregion

    #region Classes

    private class BusyTask : IAsyncDisposable
    {
        private readonly ApplictionBusyHandler _busyHandler;
        private readonly Guid _id;

        public BusyTask(ApplictionBusyHandler busyHandler, Guid id)
        {
            _busyHandler = busyHandler;
            _id = id;
        }

        public Guid Id => _id;

        #region IDisposable

        async public ValueTask DisposeAsync()
        {
            await _busyHandler.ReleaseBusyTaskAsync(this);
        }

        #endregion
    }

    #endregion

}
