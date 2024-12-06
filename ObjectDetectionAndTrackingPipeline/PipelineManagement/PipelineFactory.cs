using ObjectDetectionAndTrackingPipeline.Detection;
using ObjectDetectionAndTrackingPipeline.Event;
using ObjectDetectionAndTrackingPipeline.Tracking;
using ObjectDetectionAndTrackingPipeline.Tracking.DeepSort;
using ObjectDetectionAndTrackingPipeline.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.PipelineManagement
{
    internal class PipelineFactory
    {
        public static Pipeline CreatePipeline(PipelineConfig config, CancellationToken token)
        {
            // 创建视频捕获模块
            IVideoCaptureModule videoCapture = config.VideoCaptureModule.Type switch
            {
                "Vlc" => new VlcFrameCapture(
                    config.VideoCaptureModule.Source,
                    token,
                    config.VideoCaptureModule.Resolution?.Width ?? 1920,
                    config.VideoCaptureModule.Resolution?.Height ?? 1080),
                //"OpenCV" => new OpenCVFrameProvider(config.VideoCaptureModule.Source),
                "FFmpeg" => new FFmpegVideoCapture(
                    config.VideoCaptureModule.Source,
                    token,
                    config.VideoCaptureModule.Resolution?.Width ?? 1920,
                    config.VideoCaptureModule.Resolution?.Height ?? 1080),
                _ => throw new NotSupportedException($"VideoCaptureModule type '{config.VideoCaptureModule.Type}' not supported.")
            };

            // 创建目标检测模块
            IDetectionModule detectionModule = config.DetectionModule.Type switch
            {
                "Onnx" => new OnnxDetecter(
                    config.DetectionModule.ModelFilePath,
                    config.DetectionModule.LabelFilePath,
                    config.DetectionModule.FilterClasses),
                //"Dummy" => new (),
                _ => throw new NotSupportedException($"DetectionModule type '{config.DetectionModule.Type}' not supported.")
            };

            IFeatureExtractor featureExtractor = config.FeatureExtractor.Type switch
            {
                "Resnet" => new FeatureExtractor(config.FeatureExtractor.ModelFilePath),
                "OSNet" => new OSNetFeatureExtractor(config.FeatureExtractor.ModelFilePath),
                _ => throw new NotSupportedException($"DetectionModule type '{config.FeatureExtractor.Type}' not supported.")
            };

            // 创建目标跟踪模块
            ITrackingModule trackingModule = config.TrackingModule.Type switch
            {
                "Sort" => new SortTracker(),
                "MultiSort" => new MultiSortTracker(),
                "DeepSort" => new DeepSortTracker(
                    featureExtractor,
                    config.TrackingModule.Lambda,
                    config.TrackingModule.CostThreshold,
                    config.TrackingModule.AppearanceWeight),
                _ => throw new NotSupportedException($"TrackingModule type '{config.TrackingModule}' not supported.")
            };

            // 创建多个事件处理模块
            var eventProcessor = new EventProcessor();
            //var eventHandlingModules = new List<IEventHandlingModule>();
            if (config.EventHandlingModules != null)
            {
                foreach (var ehConfig in config.EventHandlingModules)
                {
                    var eventModule = ehConfig.Type switch
                    {
                        "Console" => new ConsoleEventListener(),
                        //"Alerting" => new AlertingEventHandlingModule(
                        //    ehConfig.Parameters.TryGetValue("AlertThreshold", out var threshold) ? Convert.ToInt32(threshold) : 10),
                        _ => throw new NotSupportedException($"EventHandlingModule type '{ehConfig.Type}' not supported.")
                    };
                    //eventHandlingModules.Add(eventModule);
                    eventProcessor.RegisterListener(eventModule);
                }
            }

            // 返回组装的 Pipeline
            return new Pipeline(config.Id, videoCapture, detectionModule, trackingModule, eventProcessor, token);
        }
    }
}
