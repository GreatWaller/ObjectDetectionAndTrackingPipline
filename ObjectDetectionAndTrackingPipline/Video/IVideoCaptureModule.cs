using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Video
{
    internal interface IVideoCaptureModule
    {
        bool IsOpened { get; }
        void Start();
        Mat GetFrame();
        void Release();
    }
}
