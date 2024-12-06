using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal interface IVideoCaptureModule
    {
        bool IsOpened { get; }
        void Start();
        Mat GetFrame();
        void Release();
    }
}
