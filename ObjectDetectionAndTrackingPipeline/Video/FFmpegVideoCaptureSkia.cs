using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal class FFmpegVideoCaptureSkia : IVideoCaptureModule<SKBitmap>
    {
        private readonly string _url;
        private readonly int _width;
        private readonly int _height;
        private Thread _captureThread;
        private Process _ffmpegProcess;
        private volatile bool _isRunning;

        private SKBitmap _latestSkiaFrame;
        private readonly object _frameLock = new(); // 用于保护 _latestSkiaFrame 的线程安全

        private readonly CancellationToken _cancellationToken;

        public bool IsOpened { get; private set; }

        public FFmpegVideoCaptureSkia(string url, CancellationToken cancellationToken, int width = 1920, int height = 1080)
        {
            _url = url;
            _width = width;
            _height = height;
            _cancellationToken = cancellationToken;
            IsOpened = false;
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            IsOpened = true;

            _captureThread = new Thread(CaptureFrames)
            {
                IsBackground = true
            };
            _captureThread.Start();
        }

        public SKBitmap GetFrame()
        {
            lock (_frameLock)
            {
                if (_latestSkiaFrame == null)
                    return null;

                // 返回 SKBitmap 的副本以避免外部修改
                var copy = new SKBitmap(_width, _height);
                using var canvas = new SKCanvas(copy);
                canvas.DrawBitmap(_latestSkiaFrame, 0, 0);
                return copy;
            }
        }

        public void Release()
        {
            if (!_isRunning) return;

            _isRunning = false;
            IsOpened = false;

            _ffmpegProcess?.Kill();
            _ffmpegProcess?.Dispose();
            _ffmpegProcess = null;

            _captureThread?.Join();

            lock (_frameLock)
            {
                _latestSkiaFrame?.Dispose();
                _latestSkiaFrame = null;
            }
        }

        private void CaptureFrames()
        {
            try
            {
                string arguments = $"-fflags +discardcorrupt -i \"{_url}\" -rtsp_transport tcp -buffer_size 1024000 -f image2pipe -pix_fmt bgr24 -vcodec rawvideo -preset veryfast -tune zerolatency -an -";

                _ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                _ffmpegProcess.Start();

                using (var stream = _ffmpegProcess.StandardOutput.BaseStream)
                {
                    int frameSize = _width * _height * 3; // 每帧的字节大小 (BGR24)
                    byte[] buffer = new byte[frameSize];

                    while (_isRunning && !_cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead = 0;

                        // 读取完整的帧数据
                        while (bytesRead < frameSize)
                        {
                            int read = stream.Read(buffer, bytesRead, frameSize - bytesRead);
                            if (read == 0)
                            {
                                // 流结束
                                Console.WriteLine("流已结束或读取中断");
                                StopCaptureLoop();
                                return;
                            }
                            bytesRead += read;
                        }

                        // 更新最新帧
                        lock (_frameLock)
                        {
                            _latestSkiaFrame?.Dispose(); // 释放旧 SKBitmap
                            _latestSkiaFrame = new SKBitmap(_width, _height);

                            // 从 BGR24 buffer 构造 SKBitmap
                            var pixels = new SKColor[_width * _height];
                            for (int y = 0; y < _height; y++)
                            {
                                for (int x = 0; x < _width; x++)
                                {
                                    int index = (y * _width + x) * 3;
                                    byte b = buffer[index];
                                    byte g = buffer[index + 1];
                                    byte r = buffer[index + 2];
                                    pixels[y * _width + x] = new SKColor(r, g, b); // BGR to RGB
                                }
                            }
                            _latestSkiaFrame.Pixels = pixels;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"捕获帧时发生错误: {ex.Message}");
            }
            finally
            {
                Release();
            }
        }

        private void StopCaptureLoop()
        {
            _isRunning = false;
            IsOpened = false;
        }
    }
}
