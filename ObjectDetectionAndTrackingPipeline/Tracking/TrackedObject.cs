using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking
{
    internal class TrackedObject
    {
        public int Id { get; set; }      // 唯一编号
        public Rect BoundingBox { get; set; } // 目标的边界框位置

        public TrackedObject(int id, Rect boundingBox)
        {
            Id = id;
            BoundingBox = boundingBox;
        }
    }
}
