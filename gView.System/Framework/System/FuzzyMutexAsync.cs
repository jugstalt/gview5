using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace gView.Framework.system
{
    public class FuzzyMutexAsync
    {
        private static readonly ConcurrentDictionary<string, DateTime> _mutexDirectory = new ConcurrentDictionary<string, DateTime>();

        async static public Task<IMutex> LockAsync(string key, int timeoutMilliSeconds = 20000)
        {
            var random = new Random(Environment.TickCount);
            var start = DateTime.Now;
            bool wasBlocked = false;

            while (true)
            {
                if (_mutexDirectory.TryAdd(key, DateTime.Now))
                {
                    break;
                }
                else
                {
                    wasBlocked = true;
                    if ((DateTime.Now - start).TotalMilliseconds > timeoutMilliSeconds)
                    {
                        throw new FuzzyMutexAsyncExcepiton($"FuzzyMutex - timeout milliseconds reached: {(DateTime.Now - start).TotalMilliseconds} > {timeoutMilliSeconds}");
                    }

                    await Task.Delay(random.Next(50));
                }
            }

            return new Mutex(key, wasBlocked);
        }

        #region Classes

        public interface IMutex : IDisposable
        {
            bool WasBlocked { get; }  
        }

        private class Mutex : IMutex
        {
            private readonly string _key;
            private readonly bool _wasBlocked;
            public Mutex(string key, bool hadLocks)
            {
                _key = key;
                _wasBlocked = hadLocks;
            }

            public bool WasBlocked => _wasBlocked;

            #region IDisposable

            public void Dispose()
            {
                if (!_mutexDirectory.TryRemove(_key, out DateTime removed))
                {

                }
            }

            #endregion
        }

        #endregion
    }

    public class FuzzyMutexAsyncExcepiton : Exception
    {
        public FuzzyMutexAsyncExcepiton(string message)
                : base(message)
        { }
    }
}
