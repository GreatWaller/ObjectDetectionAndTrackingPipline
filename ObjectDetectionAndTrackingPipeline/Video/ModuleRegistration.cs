using OpenCvSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal static class ModuleRegistration
    {
        public static void RegisterAll()
        {
            VideoCaptureFactory<Mat>.Register("Vlc", (config, token) =>
                new VlcFrameCapture(config.VideoCaptureModule.Source, token,
                    config.VideoCaptureModule.Resolution?.Width ?? 1920,
                    config.VideoCaptureModule.Resolution?.Height ?? 1080));

            VideoCaptureFactory<Mat>.Register("FFmpeg", (config, token) =>
                new FFmpegVideoCapture(config.VideoCaptureModule.Source, token,
                    config.VideoCaptureModule.Resolution?.Width ?? 1920,
                    config.VideoCaptureModule.Resolution?.Height ?? 1080));

            VideoCaptureFactory<Mat>.Register("Gst", (config, token) =>
                new GStreamerVideoCapture(
                       config.VideoCaptureModule.Source,
                       token,
                       config.VideoCaptureModule.Resolution?.Width ?? 1920,
                       config.VideoCaptureModule.Resolution?.Height ?? 1080));

           VideoCaptureFactory<SKBitmap>.Register("FFmpegSkia", (config, token) =>
                new FFmpegVideoCaptureSkia(config.VideoCaptureModule.Source, token,
                    config.VideoCaptureModule.Resolution?.Width ?? 1920,
                    config.VideoCaptureModule.Resolution?.Height ?? 1080));
        }
    }
}
