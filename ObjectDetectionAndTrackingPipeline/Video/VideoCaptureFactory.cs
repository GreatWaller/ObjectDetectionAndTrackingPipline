using ObjectDetectionAndTrackingPipeline.PipelineManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal static class VideoCaptureFactory<TFrame>
    {
        private static readonly Dictionary<string, Func<PipelineConfig, CancellationToken, IVideoCaptureModule<TFrame>>> _registry
            = new();

        public static void Register(string type, Func<PipelineConfig, CancellationToken, IVideoCaptureModule<TFrame>> creator)
        {
            _registry[type] = creator;
        }

        public static IVideoCaptureModule<TFrame> Create(string type, PipelineConfig config, CancellationToken token)
        {
            if (_registry.TryGetValue(type, out var creator))
            {
                return creator(config, token);
            }

            throw new NotSupportedException($"Video capture module '{type}' for frame type '{typeof(TFrame).Name}' not registered.");
        }
    }
}
