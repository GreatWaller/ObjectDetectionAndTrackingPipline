using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using ObjectDetectionAndTrackingPipeline.Detection;
using ObjectDetectionAndTrackingPipeline.Tracking.DeepSort;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking.DeepSort
{
    internal class FeatureExtractor: IFeatureExtractor
    {
        private readonly InferenceSession session;

        public FeatureExtractor(string modelPath)
        {
            session = new InferenceSession(modelPath, SessionOptions.MakeSessionOptionWithCudaProvider());
        }

        /// <summary>
        /// 从图像帧中的目标区域提取特征
        /// </summary>
        /// <param name="frame">输入帧</param>
        /// <param name="boundingBox">目标区域的边界框</param>
        /// <returns>特征向量</returns>
        public float[] ExtractFeature(Mat frame, Rect boundingBox)
        {
            // 从帧中裁剪目标区域
            // 修正边界框以适应图像范围
            Rect validBoundingBox = AdjustBoundingBoxToImage(frame, boundingBox);

            // 从帧中裁剪目标区域
            Mat roi = new Mat(frame, validBoundingBox);

            // 对 ROI 进行预处理
            var inputTensor = Preprocess(roi);

            // 推理得到特征向量
            var result = session.Run(new[] { NamedOnnxValue.CreateFromTensor("x", inputTensor) });
            var feature = result.First().AsEnumerable<float>().ToArray();

            return feature;
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
        /// <summary>
        /// 图像预处理，将图像转换为模型所需格式
        /// </summary>
        /// <param name="roi">裁剪的目标区域</param>
        /// <returns>输入深度学习模型的张量</returns>
        private DenseTensor<float> Preprocess(Mat roi)
        {
            // 将 ROI 缩放到模型所需尺寸
            var resized = roi.Resize(new Size(224, 224)); // 假设模型输入大小为 224x224

            // 转换为浮点张量
            Mat floatMat= new Mat();
            resized.ConvertTo(floatMat, MatType.CV_32F);

            // 标准化图像（例如减去均值和除以标准差）
            floatMat -= new Scalar(123.68, 116.779, 103.939); // 假设使用 ImageNet 的均值
            floatMat /= 255.0f;

            var inputTensor = CreateInputTensor(floatMat);

            return inputTensor;
        }

        /// <summary>
        /// 将 OpenCV Mat 转换为深度学习模型支持的张量格式
        /// </summary>
        /// <param name="mat">输入 Mat</param>
        /// <returns>转换后的张量</returns>
        private DenseTensor<float> CreateInputTensor(Mat roi)
        {

            // 预处理图像，转换为 Tensor 格式
            // 转换为张量
            int inputWidth = roi.Width;  // 根据模型的输入尺寸要求设置
            int inputHeight = roi.Height;
            var tensor = new DenseTensor<float>(new[] { 1, 3, inputHeight, inputWidth });
            for (int y = 0; y < inputHeight; y++)
            {
                for (int x = 0; x < inputWidth; x++)
                {
                    Vec3b pixel = roi.At<Vec3b>(y, x);
                    tensor[0, 0, y, x] = pixel.Item2 / 255.0f; // Red
                    tensor[0, 1, y, x] = pixel.Item1 / 255.0f; // Green
                    tensor[0, 2, y, x] = pixel.Item0 / 255.0f; // Blue
                }
            }
            return tensor;
        }

    }
}
