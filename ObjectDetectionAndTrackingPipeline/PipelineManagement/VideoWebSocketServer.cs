using ObjectDetectionAndTrackingPipeline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ObjectDetectionAndTrackingPipeline.PipelineManagement
{
    internal class VideoWebSocketServer
    {
        private readonly HttpListener _httpListener;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentBag<WebSocket> _activeWebSockets = new();

        public VideoWebSocketServer(int port)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            _httpListener.Start();
            Console.WriteLine("WebSocket server started.");
            Task.Run(() => AcceptWebSocketClientsAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _httpListener.Stop();
            Console.WriteLine("WebSocket server stopped.");
        }

        private async Task AcceptWebSocketClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        var webSocketContext = await context.AcceptWebSocketAsync(null);
                        _activeWebSockets.Add(webSocketContext.WebSocket);
                        _ = HandleWebSocketConnectionAsync(webSocketContext.WebSocket, token);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting WebSocket client: {ex.Message}");
                }
            }
        }

        private async Task HandleWebSocketConnectionAsync(WebSocket webSocket, CancellationToken token)
        {
            Console.WriteLine("WebSocket client connected.");

            while (webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                // Keep the connection alive
                await Task.Delay(1000);
            }

            Console.WriteLine("WebSocket client disconnected.");
            _activeWebSockets.TryTake(out webSocket);
        }

        public async Task SendFrameWithDetectionsAsync(Mat frame, List<DetectionResult> detections, CancellationToken token)
        {
            if (frame != null && detections != null && _httpListener.IsListening)
            {
                string frameBase64 = ConvertFrameToBase64(frame);

                var message = new
                {
                    Frame = frameBase64,
                    Detections = detections.ConvertAll(d => new
                    {
                        Id = d.Id,
                        BoundingBox = new
                        {
                            X = d.BoundingBox.X,
                            Y = d.BoundingBox.Y,
                            Width = d.BoundingBox.Width,
                            Height = d.BoundingBox.Height
                        },
                        Confidence = d.Confidence,
                        ClassId = d.ClassId,
                        ClassName = d.ClassName
                    })
                };

                string jsonMessage = JsonSerializer.Serialize(message);
                byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);

                foreach (var webSocket in GetActiveWebSockets())
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, token);
                        }
                        catch (WebSocketException ex)
                        {
                            Console.WriteLine($"WebSocket send error: {ex.Message}");
                        }
                    }
                }

                frame.Dispose();
            }
        }

        private IEnumerable<WebSocket> GetActiveWebSockets()
        {
            return _activeWebSockets;
        }

        private string ConvertFrameToBase64(Mat frame)
        {
            using (var buffer = new Mat())
            {
                Cv2.ImEncode(".jpg", frame, out var data);
                return Convert.ToBase64String(data);
            }
        }
    }
}
