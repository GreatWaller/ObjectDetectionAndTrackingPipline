using OpenCvSharp.Dnn;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace ObjectDetectionAndTrackingPipeline.Detection
{
    internal class OnnxDetecter : IDetectionModule
    {
        private readonly InferenceSession _session;
        private readonly int _inputWidth;
        private readonly int _inputHeight;
        private readonly float _confidenceThreshold;
        private readonly float _nmsThreshold;
        private readonly string[] _classLabels;
        private readonly List<int> _filterClassIds;

        public OnnxDetecter(string modelPath, string labelsPath, List<string> classes,
            float confidenceThreshold = 0.6f, float nmsThreshold = 0.4f, int inputWidth = 640, int inputHeight = 640)
        {
            // Create session options and enable CUDA provider
            var sessionOptions = new SessionOptions();
            sessionOptions.AppendExecutionProvider_CUDA(deviceId: 0); // 使用第一个 GPU
            _session = new InferenceSession(modelPath, sessionOptions);
            // 加载类别名称
            _classLabels = new List<string>(File.ReadAllLines(labelsPath)).ToArray();
            if (classes.Count>0)
            {
                _filterClassIds = classes
                    .Select(className => Array.IndexOf(_classLabels, className))
                    .Where(id => id >= 0) // 排除未找到的类别
                    .ToList();
            }
            else
            {
                _filterClassIds = new List<int>(); // 不过滤
            }

            
            _confidenceThreshold = confidenceThreshold;
            _inputWidth = inputWidth;
            _inputHeight = inputHeight;
            _confidenceThreshold = confidenceThreshold;
            _nmsThreshold = nmsThreshold;
        }

        public List<DetectionResult> Detect(Mat frame)
        {
            // Step 1: Preprocess image - Resize, convert to RGB, and normalize
            //Cv2.CvtColor(frame, frame, ColorConversionCodes.BGR2RGB);
            Mat resizedFrame = new Mat();
            Cv2.Resize(frame, resizedFrame, new Size(_inputWidth, _inputHeight));
            // Create input tensor
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });
            var indexer = resizedFrame.GetGenericIndexer<Vec3b>();
            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    var pixel = indexer[y, x];
                    inputTensor[0, 0, y, x] = pixel.Item2 / 255.0f; // R
                    inputTensor[0, 1, y, x] = pixel.Item1 / 255.0f; // G
                    inputTensor[0, 2, y, x] = pixel.Item0 / 255.0f; // B
                }
            }

            // Run inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // Parse detections
            return ParseDetections(output, frame.Width, frame.Height); ;
        }
        public List<DetectionResult> Detect(SKBitmap frame)
        {
            // Step 1: Preprocess image - Resize and normalize
            SKBitmap resizedFrame = ResizeImage(frame, _inputWidth, _inputHeight);

            // Step 2: Create input tensor (1, 3, height, width) for RGB
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, _inputHeight, _inputWidth });
            for (int y = 0; y < _inputHeight; y++)
            {
                for (int x = 0; x < _inputWidth; x++)
                {
                    SKColor pixel = resizedFrame.GetPixel(x, y);
                    inputTensor[0, 0, y, x] = pixel.Red / 255.0f;   // R
                    inputTensor[0, 1, y, x] = pixel.Green / 255.0f; // G
                    inputTensor[0, 2, y, x] = pixel.Blue / 255.0f;  // B
                }
            }

            // Step 3: Run inference
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            // Step 4: Parse detections
            return ParseDetections(output, frame.Width, frame.Height);
        }

        private SKBitmap ResizeImage(SKBitmap source, int targetWidth, int targetHeight)
        {
            // 创建目标尺寸的 SKBitmap
            var resized = new SKBitmap(targetWidth, targetHeight);
            using var canvas = new SKCanvas(resized);
            canvas.Clear(SKColors.Black); // 设置背景色

            // 计算缩放比例，保持宽高比
            float scale = Math.Min((float)targetWidth / source.Width, (float)targetHeight / source.Height);
            int scaledWidth = (int)(source.Width * scale);
            int scaledHeight = (int)(source.Height * scale);

            // 居中绘制
            var destRect = SKRect.Create((targetWidth - scaledWidth) / 2, (targetHeight - scaledHeight) / 2, scaledWidth, scaledHeight);
            canvas.DrawBitmap(source, destRect);
            canvas.Flush();

            return resized;
        }

        private List<DetectionResult> ParseDetections(Tensor<float> output, int originalWidth, int originalHeight)
        {
            var results = new List<DetectionResult>();
            var boxes = new List<Rect>();
            var confidences = new List<float>();

            int numDetections = output.Dimensions[1]; // 每个检测含 85 个元素
            for (int i = 0; i < numDetections; i++)
            {
                float confidence = output[0, i, 4];
                if (confidence > _confidenceThreshold)
                {
                    // 获取检测的类别概率
                    // Find the class with the maximum probability using Cv2.MinMaxLoc
                    float[] classScores = new float[output.Dimensions[2] - 5];
                    for (int j = 5; j < output.Dimensions[2]; j++)
                    {
                        classScores[j - 5] = output[0, i, j];
                    }

                    // Use Cv2.MinMaxLoc to get the max probability and the class ID
                    Cv2.MinMaxLoc(InputArray.Create(classScores), out _, out double maxProb, out _, out Point maxLoc);
                    int classId = maxLoc.Y; // class ID is the index of the maximum probability
                    if (_filterClassIds.Count > 0 && !_filterClassIds.Contains(classId))
                        continue; // 跳过不需要的类别

                    float centerX = output[0, i, 0] * originalWidth / _inputWidth;
                    float centerY = output[0, i, 1] * originalHeight / _inputHeight;
                    float width = output[0, i, 2] * originalWidth / _inputWidth;
                    float height = output[0, i, 3] * originalHeight / _inputHeight;

                    int x = (int)(centerX - width / 2);
                    int y = (int)(centerY - height / 2);
                    var box = new Rect(x, y, (int)width, (int)height);
                    boxes.Add(box);
                    confidences.Add(confidence);

                    // Get class name
                    string className = classId < _classLabels.Length ? _classLabels[classId] : $"Class {classId}";

                    results.Add(new DetectionResult(box, confidence, classId, className));
                }
            }

            // Apply non-maximum suppression to filter overlapping boxes
            CvDnn.NMSBoxes(boxes, confidences, _confidenceThreshold, _nmsThreshold, out int[] indices);

            return indices.Select(index => results[index]).ToList();
        }



        ~OnnxDetecter()
        {
            _session.Dispose();
        }
    }
}
