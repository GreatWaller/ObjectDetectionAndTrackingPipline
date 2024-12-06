using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.MessageBus
{
    internal class InMemoryMessageBus : IMessageBus
    {
        private readonly ConcurrentDictionary<string, List<Delegate>> _subscriptions = new();

        public void Publish<T>(string topic, T message)
        {
            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    if (handler is Action<T> action)
                    {
                        action(message);
                    }
                }
            }
        }

        public void Subscribe<T>(string topic, Action<T> handler)
        {
            _subscriptions.AddOrUpdate(topic,
                _ => new List<Delegate> { handler },
                (_, existingHandlers) =>
                {
                    existingHandlers.Add(handler);
                    return existingHandlers;
                });
        }

        public void Unsubscribe<T>(string topic, Action<T> handler)
        {
            if (_subscriptions.TryGetValue(topic, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }
}
