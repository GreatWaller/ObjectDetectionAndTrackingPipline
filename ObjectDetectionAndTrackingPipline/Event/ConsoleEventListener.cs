using ObjectDetectionAndTrackingPipline.Detection;
using ObjectDetectionAndTrackingPipline.Tracking;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Event
{
    internal class ConsoleEventListener : IEventHandlingModule
    {
        public void OnEvent(string eventType, DetectionResult target)
        {
            Console.WriteLine($"Event: {eventType} detected at location {target}");
        }
    }
}
