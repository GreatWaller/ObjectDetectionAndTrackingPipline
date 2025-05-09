using OpenCvSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal class GStreamerVideoCapture : IVideoCaptureModule<Mat>
    {
        private readonly string _rtspUrl;
        private readonly int _width;
        private readonly int _height;
        private readonly CancellationToken _cancellationToken;
        private Mat _latestFrame;
        private readonly object _frameLock = new();
        private Thread _captureThread;
        private bool _isOpened;
        private bool _isRunning;

        public bool IsOpened => _isOpened;

        // GStreamer constants
        private const int GST_STATE_NULL = 1;
        private const int GST_STATE_PLAYING = 4;
        private const int GST_MAP_READ = 1;

        // P/Invoke definitions
        [DllImport("gstreamer-1.0-0.dll")]
        private static extern void gst_init(ref int argc, ref IntPtr argv);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern IntPtr gst_parse_launch(string pipeline, ref IntPtr error);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern void gst_object_unref(IntPtr obj);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern void gst_element_set_state(IntPtr element, int state);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern IntPtr gst_bin_get_by_name(IntPtr bin, string name);

        [DllImport("gobject-2.0-0.dll")]
        private static extern void g_signal_emit_by_name(IntPtr instance, string signalName, out IntPtr returnValue);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern void gst_sample_unref(IntPtr sample);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern IntPtr gst_sample_get_buffer(IntPtr sample);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern bool gst_buffer_map(IntPtr buffer, out GstMapInfo mapInfo, int flags);

        [DllImport("gstreamer-1.0-0.dll")]
        private static extern void gst_buffer_unmap(IntPtr buffer, ref GstMapInfo mapInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct GstMapInfo
        {
            public IntPtr Memory;
            public ulong Size;
            public IntPtr Data;
            public ulong Maxsize;
            public ulong Flags;
        }

        public GStreamerVideoCapture(string rtspUrl, CancellationToken cancellationToken, int width = 1920, int height = 1080)
        {
            _rtspUrl = rtspUrl;
            _width = width;
            _height = height;
            _cancellationToken = cancellationToken;

            // Initialize GStreamer
            int argc = 0;
            IntPtr argv = IntPtr.Zero;
            gst_init(ref argc, ref argv);
        }

        public void Start()
        {
            if (_isOpened)
                throw new InvalidOperationException("Video capture is already started.");

            _isOpened = true;
            _isRunning = true;
            _captureThread = new Thread(CaptureFrames)
            {
                IsBackground = true
            };
            _captureThread.Start();
        }

        public Mat GetFrame()
        {
            lock (_frameLock)
            {
                //if (_latestFrame == null)
                //    throw new InvalidOperationException("No frame available yet.");

                return _latestFrame?.Clone();
            }
        }

        public void Release()
        {
            if (!_isOpened)
                return;

            _isRunning = false;
            _isOpened = false;

            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join();
                _captureThread = null;
            }
        }

        private void CaptureFrames()
        {
            string pipelineStr = $"rtspsrc location={_rtspUrl} latency=200 ! decodebin ! videoconvert ! video/x-raw,format=BGR,width={_width},height={_height} ! appsink name=sink";
            IntPtr error = IntPtr.Zero;
            IntPtr pipeline = gst_parse_launch(pipelineStr, ref error);

            if (pipeline == IntPtr.Zero)
            {
                Console.WriteLine("Failed to create pipeline.");
                return;
            }

            gst_element_set_state(pipeline, GST_STATE_PLAYING);
            IntPtr sink = gst_bin_get_by_name(pipeline, "sink");

            if (sink == IntPtr.Zero)
            {
                Console.WriteLine("Failed to get appsink from pipeline.");
                gst_object_unref(pipeline);
                return;
            }

            while (_isRunning && !_cancellationToken.IsCancellationRequested)
            {
                IntPtr sample = IntPtr.Zero;
                g_signal_emit_by_name(sink, "pull-sample", out sample);

                if (sample != IntPtr.Zero)
                {
                    HandleSample(sample);
                    gst_sample_unref(sample);
                }
            }

            gst_element_set_state(pipeline, GST_STATE_NULL);
            gst_object_unref(pipeline);
        }

        private void HandleSample(IntPtr sample)
        {
            IntPtr buffer = gst_sample_get_buffer(sample);
            if (buffer == IntPtr.Zero)
                return;

            if (gst_buffer_map(buffer, out GstMapInfo mapInfo, GST_MAP_READ))
            {
                lock (_frameLock)
                {
                    _latestFrame?.Dispose();
                    _latestFrame = Mat.FromPixelData(_height, _width, MatType.CV_8UC3, mapInfo.Data);
                }

                gst_buffer_unmap(buffer, ref mapInfo);
            }
        }
    }
}
