using ObjectDetectionAndTrackingPipeline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking.DeepSort
{
    internal class Track
    {
        // Track的唯一ID
        public int Id { get; private set; }

        // 未匹配的连续帧计数
        public int MissedFrames { get; set; }

        // 当前的边界框
        public Rect BoundingBox { get; private set; }

        // 当前的外观特征向量
        public float[] AppearanceFeature { get; private set; }

        // 卡尔曼滤波器实例，用于轨迹预测
        private KalmanFilter kalmanFilter;

        // 初始化卡尔曼滤波器的状态矩阵
        private static readonly int StateSize = 8;  // x, y, w, h, vx, vy, vw, vh
        private static readonly int MeasSize = 4;  // x, y, w, h
        private static readonly int ContrSize = 0;

        /// <summary>
        /// 构造函数，初始化Track
        /// </summary>
        public Track(int id, DetectionResult detection, float[] feature)
        {
            Id = id;
            MissedFrames = 0;
            BoundingBox = detection.BoundingBox;
            AppearanceFeature = feature;

            // 初始化卡尔曼滤波器
            kalmanFilter = new KalmanFilter(StateSize, MeasSize, ContrSize, MatType.CV_32F);

            // 初始状态设置
            InitKalmanFilter(detection.BoundingBox);
        }

        /// <summary>
        /// 初始化卡尔曼滤波器
        /// </summary>
        private void InitKalmanFilter(Rect initialBoundingBox)
        {
            // 初始状态
            kalmanFilter.StatePost.SetArray(new float[]
            {
            initialBoundingBox.X,
            initialBoundingBox.Y,
            initialBoundingBox.Width,
            initialBoundingBox.Height,
            0, 0, 0, 0
            });

            // 状态转移矩阵
            kalmanFilter.TransitionMatrix.SetArray(new float[]
            {
            1, 0, 0, 0, 1, 0, 0, 0,
            0, 1, 0, 0, 0, 1, 0, 0,
            0, 0, 1, 0, 0, 0, 1, 0,
            0, 0, 0, 1, 0, 0, 0, 1,
            0, 0, 0, 0, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 1, 0,
            0, 0, 0, 0, 0, 0, 0, 1
            });

            // 过程噪声协方差矩阵
            kalmanFilter.ProcessNoiseCov = Mat.Eye(StateSize, StateSize, MatType.CV_32F) * 1e-2f;

            // 测量噪声协方差矩阵
            kalmanFilter.MeasurementNoiseCov = Mat.Eye(MeasSize, MeasSize, MatType.CV_32F) * 1e-1f;

            // 后验误差协方差矩阵
            kalmanFilter.ErrorCovPost = Mat.Eye(StateSize, StateSize, MatType.CV_32F);
        }

        /// <summary>
        /// 更新Track的状态
        /// </summary>
        public void Update(DetectionResult detection, float[] feature)
        {
            // 更新外观特征
            AppearanceFeature = feature;

            // 更新边界框
            BoundingBox = detection.BoundingBox;

            // 设置测量值
            Mat measurement = new Mat(MeasSize, 1, MatType.CV_32F);
            measurement.SetArray(new float[]
            {
            detection.BoundingBox.X,
            detection.BoundingBox.Y,
            detection.BoundingBox.Width,
            detection.BoundingBox.Height
            });

            // 更新卡尔曼滤波器
            kalmanFilter.Correct(measurement);

            // 重置未匹配帧计数
            MissedFrames = 0;
        }

        /// <summary>
        /// 预测Track的下一个状态
        /// </summary>
        //public Point Predict()
        //{
        //    Mat prediction = kalmanFilter.Predict();

        //    // 更新边界框的预测位置
        //    BoundingBox = new Rect(
        //        (int)prediction.At<float>(0),
        //        (int)prediction.At<float>(1),
        //        (int)prediction.At<float>(2),
        //        (int)prediction.At<float>(3)
        //    );

        //    return new Point(BoundingBox.X + BoundingBox.Width / 2, BoundingBox.Y + BoundingBox.Height / 2);
        //}

        public Rect Predict()
        {
            Mat prediction = kalmanFilter.Predict();

            // 更新边界框的预测位置
            BoundingBox = new Rect(
                (int)prediction.At<float>(0),
                (int)prediction.At<float>(1),
                (int)prediction.At<float>(2),
                (int)prediction.At<float>(3)
            );

            return BoundingBox;
        }

        /// <summary>
        /// 计算与其他特征向量的余弦距离
        /// </summary>
        public float CalculateAppearanceDistance(float[] otherFeature)
        {
            // 计算余弦距离 = 1 - (A·B) / (|A| * |B|)
            float dotProduct = AppearanceFeature.Zip(otherFeature, (a, b) => a * b).Sum();
            float magnitudeA = MathF.Sqrt(AppearanceFeature.Sum(f => f * f));
            float magnitudeB = MathF.Sqrt(otherFeature.Sum(f => f * f));
            var appearanceCost =  1 - (dotProduct / (magnitudeA * magnitudeB));
            //appearanceCost = -MathF.Log(1 - appearanceCost + 1e-6f);
            return appearanceCost;
        }
        public float CalculateIoU(Rect otherBox)
        {
            int x1 = Math.Max(BoundingBox.X, otherBox.X);
            int y1 = Math.Max(BoundingBox.Y, otherBox.Y);
            int x2 = Math.Min(BoundingBox.X + BoundingBox.Width, otherBox.X + otherBox.Width);
            int y2 = Math.Min(BoundingBox.Y + BoundingBox.Height, otherBox.Y + otherBox.Height);

            // 交集面积
            int intersectionWidth = Math.Max(0, x2 - x1);
            int intersectionHeight = Math.Max(0, y2 - y1);
            int intersectionArea = intersectionWidth * intersectionHeight;

            // 并集面积
            int areaA = BoundingBox.Width * BoundingBox.Height;
            int areaB = otherBox.Width * otherBox.Height;
            int unionArea = areaA + areaB - intersectionArea;

            if (unionArea == 0)
                return 0;

            // IoU
            return (float)intersectionArea / unionArea;
        }
    }
}
