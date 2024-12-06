using ObjectDetectionAndTrackingPipline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Tracking
{
    internal interface ITrackingModule
    {
        List<DetectionResult> Track(Mat frame, List<DetectionResult> detectedObjects);
    }
}
