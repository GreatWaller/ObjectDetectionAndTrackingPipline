using LibVLCSharp.Shared;
using OpenCvSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Video
{
    internal class VlcFrameCapture : IVideoCaptureModule
    {
        private readonly LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private readonly CancellationToken _cancellationToken;
        private bool _isRunning;

        private readonly int _width;
        private readonly int _height;
        private readonly uint _pitch;
        private readonly nint _frameBuffer;
        private Mat _currentFrame;
        private readonly object _frameLock = new object();

        public bool IsOpened => _mediaPlayer?.IsPlaying == true;

        public VlcFrameCapture(string rtspUrl, CancellationToken cancellationToken, int width = 1920, int height = 1080)
        {
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC)
            {
                Media = new Media(_libVLC, rtspUrl, FromType.FromLocation)
            };
            _mediaPlayer.Media.AddOption(":network-caching=333");
            _mediaPlayer.Media.AddOption(":clock-jitter=0");
            _mediaPlayer.Media.AddOption(":clock-syncro=0");
            _mediaPlayer.Media.AddOption(":no-audio");

            _cancellationToken = cancellationToken;
            _width = width;
            _height = height;
            _pitch = (uint)(_width * 4); // 假设使用 32 位 RGBA
            _frameBuffer = Marshal.AllocHGlobal(_height * (int)_pitch);

            // 设置视频格式和回调函数
            _mediaPlayer.SetVideoFormat("RV32", (uint)_width, (uint)_height, _pitch);
            _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;

            Task.Run(() =>
            {
                _mediaPlayer.Play();
                while (!_cancellationToken.IsCancellationRequested && _isRunning)
                {
                    Thread.Sleep(10); // 控制帧读取频率
                }
                Stop();
            }, _cancellationToken);

            Console.WriteLine("VLC stream started.");
        }

        public Mat GetFrame()
        {
            if (!IsOpened || _currentFrame == null)
                return null;

            lock (_frameLock)
            {
                return _currentFrame.Clone(); // 返回当前帧的副本
            }
        }

        public void Release()
        {
            Stop(); // 停止流播放并释放资源
        }

        private void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVLC.Dispose();
            Marshal.FreeHGlobal(_frameBuffer);
            _currentFrame?.Dispose();

            Console.WriteLine("VLC stream stopped and resources released.");
        }

        // 锁定回调，用于返回帧缓冲区
        private nint LockCallback(nint opaque, nint planes)
        {
            Marshal.WriteIntPtr(planes, _frameBuffer);
            return _frameBuffer;
        }

        // 显示回调，每帧显示时调用
        private void DisplayCallback(nint opaque, nint picture)
        {
            if (!_isRunning || _cancellationToken.IsCancellationRequested)
            {
                Stop();
                return;
            }

            lock (_frameLock)
            {
                _currentFrame?.Dispose(); // 释放之前的帧
                _currentFrame = Mat.FromPixelData(_height, _width, MatType.CV_8UC4, _frameBuffer); // 更新当前帧
                Cv2.CvtColor(_currentFrame, _currentFrame, ColorConversionCodes.RGBA2RGB);
            }
        }
    }
}
