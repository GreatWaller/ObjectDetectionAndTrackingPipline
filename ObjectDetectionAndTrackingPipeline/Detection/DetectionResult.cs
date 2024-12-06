using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Detection
{
    internal class DetectionResult
    {
        public int Id { get; set; } = -1;      // 唯一编号
        public Rect BoundingBox { get; set; }
        public float Confidence { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }

        public DetectionResult(Rect box, float confidence, int classId, string className = null)
        {
            BoundingBox = box;
            Confidence = confidence;
            ClassId = classId;
            ClassName = className ?? $"Class {classId}";
        }
    }
}
