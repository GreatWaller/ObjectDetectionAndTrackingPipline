using OpenCvSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal interface IVideoCaptureModule<out TFrame>
    {
        bool IsOpened { get; }
        void Start();
        TFrame GetFrame();
        void Release();
    }
}
