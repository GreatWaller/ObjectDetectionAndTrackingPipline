using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Detection
{
    internal interface IDetectionModule
    {
        List<DetectionResult> Detect(Mat frame);
    }
}
