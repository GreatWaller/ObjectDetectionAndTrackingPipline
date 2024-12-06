using ObjectDetectionAndTrackingPipline.Detection;
using ObjectDetectionAndTrackingPipline.Event;
using ObjectDetectionAndTrackingPipline.Tracking;
using ObjectDetectionAndTrackingPipline.Video;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.PipelineManagement
{
    internal class Pipeline
    {
        private string _id;
        private readonly IVideoCaptureModule _videoCapture;
        private readonly IDetectionModule _objectDetection;
        private readonly ITrackingModule _tracking;
        private readonly EventProcessor _eventProcessor;
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _stopwatch;

        public Pipeline(
            string id,
            IVideoCaptureModule videoCapture,
            IDetectionModule objectDetection,
            ITrackingModule tracking,
            EventProcessor eventProcessor,
            CancellationToken cancellationToken)
        {
            _id = id;
            _videoCapture = videoCapture;
            _objectDetection = objectDetection;
            _tracking = tracking;
            _eventProcessor = eventProcessor;
            _cancellationToken = cancellationToken;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Start()
        {
            _videoCapture.Start(); // 启动视频流
            Task.Run(ProcessFrames, _cancellationToken);
        }

        private async Task ProcessFrames()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                using Mat frame = _videoCapture.GetFrame();

                if (frame == null || frame.Empty())
                {
                    await Task.Delay(10); // 等待下一帧
                    continue;
                }
                //_stopwatch.Restart();
                // 检测目标
                var detections = _objectDetection.Detect(frame);
                //Console.WriteLine($"[Timer] Detecting: {_stopwatch.ElapsedMilliseconds} ms");
                //_stopwatch.Restart();

                // 跟踪检测到的目标
                var trackedObjects = _tracking.Track(frame, detections);
                //Console.WriteLine($"[Timer] Tracking: {_stopwatch.ElapsedMilliseconds} ms");

                // 输出结果或进一步处理
                Console.WriteLine($"[{_id}]Detected and tracked {trackedObjects.Count} objects.");

                // 事件处理
                foreach (var detectedObject in trackedObjects)
                {
                    _eventProcessor.TriggerEvent("ObjectDetected", detectedObject);
                }
                // 显示结果
                foreach (var obj in trackedObjects)
                {
                    Cv2.Rectangle(frame, obj.BoundingBox, Scalar.Red, 2);
                    Cv2.PutText(frame, $"{obj.ClassName}: {obj.Id}, {obj.Confidence}", new Point(obj.BoundingBox.X, obj.BoundingBox.Y - 10), HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 2);

                }

                Cv2.ImShow(_id, frame);
                if (Cv2.WaitKey(1) == 27) // 按下ESC退出
                    break;

                //await Task.Delay(33); // 控制帧处理频率
            }

            _videoCapture.Release(); // 停止视频流捕获
            Console.WriteLine("Pipeline stopped.");
        }
    }
}
