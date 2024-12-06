using ObjectDetectionAndTrackingPipeline.Detection;
using ObjectDetectionAndTrackingPipeline.Tracking;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Event
{
    internal class EventProcessor
    {
        private List<IEventHandlingModule> _listeners = new List<IEventHandlingModule>();

        public void RegisterListener(IEventHandlingModule listener)
        {
            _listeners.Add(listener);
        }

        public void TriggerEvent(string eventType, DetectionResult target)
        {
            foreach (var listener in _listeners)
            {
                listener.OnEvent(eventType, target);
            }
        }
    }
}
