using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.MessageBus
{
    internal interface IMessageBus
    {
        void Publish<T>(string topic, T message);
        void Subscribe<T>(string topic, Action<T> handler);
        void Unsubscribe<T>(string topic, Action<T> handler);
    }
}
