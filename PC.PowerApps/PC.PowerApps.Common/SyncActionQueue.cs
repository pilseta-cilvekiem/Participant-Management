using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PC.PowerApps.Common
{
    public class SyncActionQueue
    {
        private readonly Context context;
        private readonly ConcurrentQueue<Action> queue = new();

        public SyncActionQueue(Context context)
        {
            this.context = context;
        }

        public void Add(Action action)
        {
            queue.Enqueue(action);
        }

        public void AddForAll<T>(IEnumerable<T> sequence, Action<T> action)
        {
            foreach (T element in sequence)
            {
                Add(() => action(element));
            }
        }

        public void ExecuteAll()
        {
            List<Exception> exceptions = new();

            while (queue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    context.Logger.LogError(e, "An error occured while executing an action.");
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public static void ExecuteForAll<T>(Context context, IEnumerable<T> sequence, Action<T> action)
        {
            SyncActionQueue syncActionQueue = new(context);
            syncActionQueue.AddForAll(sequence, action);
            syncActionQueue.ExecuteAll();
        }
    }
}
