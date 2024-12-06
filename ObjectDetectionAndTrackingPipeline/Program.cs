using ObjectDetectionAndTrackingPipeline.Detection;
using ObjectDetectionAndTrackingPipeline.Event;
using ObjectDetectionAndTrackingPipeline.PipelineManagement;
using ObjectDetectionAndTrackingPipeline.Tracking;
using ObjectDetectionAndTrackingPipeline.Video;

namespace ObjectDetectionAndTrackingPipeline
{
    internal class Program
    {
        static void Main1(string[] args)
        {
            Console.WriteLine("Hello, World!");
            string rtspUrl = "rtsp://192.168.1.140/Cam-15b3a2f4-Profile_1";
            CancellationTokenSource cts = new CancellationTokenSource();

            // 实例化视频捕获模块
            IVideoCaptureModule videoCapture = new VlcFrameCapture(rtspUrl, cts.Token, width: 1920, height: 1080);

            // 使用虚拟对象检测和跟踪模块进行测试
            //IDetectionModule detectionModule = new YoloDetection("Config/yolov4.cfg", "Config/yolov4.weights",
            //"Config/labels.txt");
            IDetectionModule detectionModule = new OnnxDetecter("Config/yolov5.onnx", "Config/labels.txt", new List<string>());
            ITrackingModule trackingModule = new SortTracker();

            var eventProcessor = new EventProcessor();
            //var consoleListener = new ConsoleEventListener();
            //eventProcessor.RegisterListener(consoleListener);

            // 创建并启动管道
            Pipeline pipeline = new Pipeline("pipeline",videoCapture, detectionModule, trackingModule, eventProcessor, cts.Token);
            pipeline.Start();

            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            // 停止处理
            cts.Cancel();
            Console.WriteLine("Processing stopped.");
        }

        static void Main(string[] args)
        {
            string configFilePath = "Config/pipelines_config.json";
            var settings = ConfigLoader.LoadFromJson(configFilePath);

            PipelineManager manager = new();

            foreach (var config in settings.Pipelines)
            {
                var cts = new CancellationTokenSource();
                var pipeline = PipelineFactory.CreatePipeline(config, cts.Token);
                manager.AddPipeline(config.Id, pipeline);
            }

            // 启动所有管道
            Console.WriteLine("Starting all pipelines...");
            manager.StartAll();

            Console.WriteLine("Press any key to stop all pipelines...");
            Console.ReadKey();

            // 停止所有管道
            Console.WriteLine("Stopping all pipelines...");
            manager.StopAll();

            Console.WriteLine("All pipelines stopped.");
        }
    }
}
