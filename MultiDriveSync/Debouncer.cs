using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiDriveSync
{
    class Debouncer<T>
    {
        private readonly ConcurrentDictionary<string, DebouncedEvent<T>> events;
        private readonly Action<T> debouncedEventHandler;

        public Debouncer(Action<T> debouncedEventHandler)
        {
            events = new ConcurrentDictionary<string, DebouncedEvent<T>>();
            this.debouncedEventHandler = debouncedEventHandler;
        }

        public void Debounce(string entryPath, T e)
        {
            events.AddOrUpdate(entryPath, addValueFactory, updateValueFactory);

            DebouncedEvent<T> addValueFactory(string key)
            {
                var debouncedEvent = new DebouncedEvent<T>(e, DateTime.Now);
                debouncedEvent.Execute(eventArgs => RemoveDebouncedEventThenExecuteEventHandler(key, eventArgs));
                return debouncedEvent;
            }

            DebouncedEvent<T> updateValueFactory(string key, DebouncedEvent<T> value)
            {
                value.UpdateLastBounceDate(DateTime.Now);
                return value;
            }
        }

        private void RemoveDebouncedEventThenExecuteEventHandler(string key, T eventArgs)
        {
            if (events.TryRemove(key, out var debouncedEvent))
            {
                debouncedEventHandler(eventArgs);
            }
        }

        private class DebouncedEvent<U>
        {
            private static readonly TimeSpan delay = TimeSpan.FromSeconds(1);

            private readonly U eventArgs;
            private long lastBounceDate;

            public DebouncedEvent(U eventArgs, DateTime lastBounceDate)
            {
                this.eventArgs = eventArgs;
                UpdateLastBounceDate(lastBounceDate);
            }

            public void UpdateLastBounceDate(DateTime dateTime)
            {
                Interlocked.Exchange(ref lastBounceDate, dateTime.Ticks);
            }

            public async void Execute(Action<U> eventHandler)
            {
                while (Interlocked.Read(ref lastBounceDate) + delay.Ticks > DateTime.Now.Ticks)
                {
                    await Task.Delay(delay);
                }

                eventHandler(eventArgs);
            }
        }
    }
}
