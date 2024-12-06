using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Video
{
    internal class FFmpegVideoCapture : IVideoCaptureModule
    {
        private readonly string _url;
        private readonly int _width;
        private readonly int _height;
        private Thread _captureThread;
        private Process _ffmpegProcess;
        private volatile bool _isRunning;

        private Mat _latestFrame;
        private readonly object _frameLock = new(); // 用于保护 _latestFrame 的线程安全

        private readonly CancellationToken _cancellationToken;

        public bool IsOpened { get; private set; }

        public FFmpegVideoCapture(string url, CancellationToken cancellationToken, int width = 1920, int height = 1080)
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

        public Mat GetFrame()
        {
            lock (_frameLock)
            {
                return _latestFrame?.Clone(); // 返回最新帧的副本
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
                _latestFrame?.Dispose();
                _latestFrame = null;
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
                            _latestFrame = Mat.FromPixelData(_height, _width, MatType.CV_8UC3, buffer);
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

