using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking.DeepSort
{
    internal class OSNetFeatureExtractor : IFeatureExtractor
    {
        private readonly InferenceSession _session;

        public OSNetFeatureExtractor(string modelPath)
        {
            _session = new InferenceSession(modelPath);
        }

        /// <summary>
        /// 提取外观特征
        /// </summary>
        /// <param name="image">裁剪后的检测框图像 (Mat)</param>
        /// <returns>OSNet 提取的外观特征向量</returns>
        public float[] ExtractFeature(Mat frame, Rect boundingBox)
        {
            // 从帧中裁剪目标区域
            Rect validBoundingBox = AdjustBoundingBoxToImage(frame, boundingBox);

            // 从帧中裁剪目标区域
            Mat roi = new Mat(frame, validBoundingBox);
            // 创建 ONNX 输入张量
            var inputTensor = CreateInputTensor(roi);

            // 构建模型输入
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            // 推理
            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            return output; // 返回特征向量
        }

        /// <summary>
        /// 预处理 OpenCvSharp 的 Mat 图像为 OSNet 输入格式
        /// </summary>
        /// <param name="image">OpenCvSharp 的 Mat 类型输入图像</param>
        /// <returns>归一化后的图片张量 (1, 3, 256, 128)</returns>
        private static DenseTensor<float> CreateInputTensor(Mat image)
        {
            const int width = 128;  // OSNet 输入宽度
            const int height = 256; // OSNet 输入高度

            // 检查输入是否为空
            if (image == null || image.Empty())
            {
                throw new ArgumentException("输入图像为空");
            }

            // 调整大小到 256x128
            Mat resizedImage = new Mat();
            Cv2.Resize(image, resizedImage, new Size(width, height));

            // 转换为张量并归一化
            var inputTensor = new DenseTensor<float>(new[] { 1, 3, 256, 128 });

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 获取像素值（BGR 顺序）
                    Vec3b pixel = resizedImage.At<Vec3b>(y, x);

                    // 按 RGB 通道填充
                    inputTensor[0, 0, y, x] = pixel.Item2 / 255.0f; // R 通道
                    inputTensor[0, 1, y, x] = pixel.Item1 / 255.0f; // G 通道
                    inputTensor[0, 2, y, x] = pixel.Item0 / 255.0f; // B 通道
                }
            }

            return inputTensor;
        }

        /// <summary>
        /// 调整边界框使其在图像范围内
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="boundingBox">原始边界框</param>
        /// <returns>修正后的边界框</returns>
        private Rect AdjustBoundingBoxToImage(Mat image, Rect boundingBox)
        {
            int x = Math.Max(boundingBox.X, 0);
            int y = Math.Max(boundingBox.Y, 0);
            int width = Math.Min(boundingBox.Width, image.Width - x);
            int height = Math.Min(boundingBox.Height, image.Height - y);

            // 确保宽度和高度不为负
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);

            return new Rect(x, y, width, height);
        }
    }
}
