using ObjectDetectionAndTrackingPipeline.Detection;
using ObjectDetectionAndTrackingPipeline.Event;
using ObjectDetectionAndTrackingPipeline.Tracking;
using ObjectDetectionAndTrackingPipeline.Video;
using OpenCvSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.PipelineManagement
{
    internal class Pipeline<TFrame>: IPipeline
    {
        private string _id;
        private readonly IVideoCaptureModule<TFrame> _videoCapture;
        private readonly IDetectionModule _objectDetection;
        private readonly ITrackingModule _tracking;
        private readonly EventProcessor _eventProcessor;
        private readonly CancellationToken _cancellationToken;
        private readonly Stopwatch _stopwatch;
        private VideoWebSocketServer _videoWebSocketServer;
        private long _frameCount = 0;
        public Pipeline(
            string id,
            IVideoCaptureModule<TFrame> videoCapture,
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
            // 指定 WebSocket 服务器端口
            // int port = 58080;
            // _videoWebSocketServer = new VideoWebSocketServer(port);

            // 启动 WebSocket 服务器
            //_videoWebSocketServer.Start();
            Task.Run(ProcessFrames, _cancellationToken);

        }

        private async Task ProcessFrames()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                TFrame tFrame = _videoCapture.GetFrame();
                // 编译期类型安全
                if (tFrame is Mat frame)
                {
                    // OpenCV 处理逻辑
                    Console.WriteLine("处理 Mat");
                    if (frame == null || frame.Empty())
                    {
                        await Task.Delay(10); // 等待下一帧
                        continue;
                    }
                    _frameCount++;
                    //_stopwatch.Restart();
                    // 检测目标
                    var trackedObjects = _objectDetection.Detect(frame);
                    //Console.WriteLine($"[Timer] Detecting: {_stopwatch.ElapsedMilliseconds} ms");
                    //_stopwatch.Restart();

                    // 跟踪检测到的目标
                    //var trackedObjects = _tracking.Track(frame, detections);

                    //Console.WriteLine($"[Timer] Tracking: {_stopwatch.ElapsedMilliseconds} ms");

                    // 输出结果或进一步处理
                    Console.WriteLine($"[{_id}:{_frameCount}]Detected and tracked {trackedObjects.Count} objects.");

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

                }
                else if (tFrame is SKBitmap bitmap)
                {
                    // SkiaSharp 处理逻辑
                    Console.WriteLine("处理 SKBitmap");
                    if (bitmap == null || bitmap.IsEmpty)
                    {
                        await Task.Delay(10); // 等待下一帧
                        continue;
                    }
                    _frameCount++;
                    var trackedObjects = _objectDetection.Detect(bitmap);
                    Console.WriteLine($"[{_id}:{_frameCount}]Detected and tracked {trackedObjects.Count} objects.");

                }
                //await Task.Delay(33); // 控制帧处理频率

                //await _videoWebSocketServer.SendFrameWithDetectionsAsync(frame, trackedObjects, _cancellationToken);
            }

            _videoCapture.Release(); // 停止视频流捕获
            Console.WriteLine("Pipeline stopped.");
        }
    }
}
