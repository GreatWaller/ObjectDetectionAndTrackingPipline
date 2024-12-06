using System.Collections.Generic;

namespace ObjectDetectionAndTrackingPipline.PipelineManagement
{
    public class VideoCaptureConfig
    {
        public string Type { get; set; } // 视频捕获模块类型，例如 "VLC" 或 "OpenCV"
        public string Source { get; set; } // 视频源地址
        public ResolutionConfig Resolution { get; set; } // 可选，分辨率配置
    }

    public class DetectionModuleConfig
    {
        public string Type { get; set; } // 检测模块类型，例如 "YOLO" 或 "Dummy"
        public string ModelFilePath { get; set; } // 模型文件路径（可选）
        public string LabelFilePath { get; set; } // 标签文件路径（可选）
        public List<string>? FilterClasses { get; set; } // 需要筛选的类别名称列表
    }

    public class FeatureExtractorConfig
    {
        public string Type { get; set; }
        public string ModelFilePath { get; set; }
    }
    public class TrackingModuleConfig
    {
        public string Type { get; set; }
        
        public float Lambda { get; set; }
        public float CostThreshold { get; set; }
        public float AppearanceWeight { get; set; }
    }
        public class PipelineConfig
    {
        public string Id { get; set; }
        public VideoCaptureConfig VideoCaptureModule { get; set; }
        public DetectionModuleConfig DetectionModule { get; set; }
        public FeatureExtractorConfig FeatureExtractor { get; set; }
        public TrackingModuleConfig TrackingModule { get; set; }
        public List<EventHandlingModuleConfig> EventHandlingModules { get; set; } // 支持多个事件处理模块
    }
    public class ResolutionConfig
    {
        public int Width { get; set; }                      // 视频宽度
        public int Height { get; set; }                     // 视频高度
    }
    public class EventHandlingModuleConfig
    {
        public string Type { get; set; } // 模块类型，例如 "Logging" 或 "Alerting"
        public Dictionary<string, object> Parameters { get; set; } // 自定义参数
    }
    public class PipelineSettings
    {
        public List<PipelineConfig> Pipelines { get; set; } // 多个 Pipeline 配置
    }
}
