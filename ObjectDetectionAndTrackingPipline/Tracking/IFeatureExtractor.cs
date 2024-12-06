using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Tracking
{
    internal interface IFeatureExtractor
    {
        float[] ExtractFeature(Mat frame, Rect boundingBox);
    }
}
