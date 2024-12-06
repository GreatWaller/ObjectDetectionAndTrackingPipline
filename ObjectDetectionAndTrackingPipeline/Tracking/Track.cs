using ObjectDetectionAndTrackingPipeline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking
{
    internal class Track
    {
        public int Id { get; private set; }
        public Rect BoundingBox { get; private set; }
        public int MissedFrames { get; set; }
        private KalmanFilter kalmanFilter;
        
        private Point2f lastPosition;

        public Track(int id, DetectionResult detection)
        {
            Id = id;
            BoundingBox = detection.BoundingBox;
            MissedFrames = 0;
            lastPosition = GetCenter(BoundingBox);

            // 初始化卡尔曼滤波器
            kalmanFilter = new KalmanFilter(4, 2);
            kalmanFilter.TransitionMatrix = new Mat(4, 4, MatType.CV_32F);
            kalmanFilter.TransitionMatrix.SetArray( new float[]
            {
                1, 0, 1, 0,
                0, 1, 0, 1,
                0, 0, 1, 0,
                0, 0, 0, 1
            });

            kalmanFilter.MeasurementMatrix = Mat.Eye(2, 4, MatType.CV_32F);
            kalmanFilter.ProcessNoiseCov = Mat.Eye(4, 4, MatType.CV_32F) * 0.03f;
            kalmanFilter.MeasurementNoiseCov = Mat.Eye(2, 2, MatType.CV_32F) * 0.5f;
            kalmanFilter.ErrorCovPost = Mat.Eye(4, 4, MatType.CV_32F) * 0.1f;

            var initialState = new Mat(4, 1, MatType.CV_32F); // 创建空矩阵
            initialState.SetArray(new float[] { lastPosition.X, lastPosition.Y, 0f, 0f }); // 填充数据
            kalmanFilter.StatePost = initialState;
        }

        /// <summary>
        /// 获取当前 BoundingBox 的中心点
        /// </summary>
        /// <param name="rect">BoundingBox</param>
        /// <returns>中心点坐标</returns>
        private Point2f GetCenter(Rect rect)
        {
            return new Point2f(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        }

        /// <summary>
        /// 使用卡尔曼滤波器预测下一个位置
        /// </summary>
        /// <returns>预测的中心点</returns>
        public Point2f Predict()
        {
            var prediction = kalmanFilter.Predict();
            return new Point2f(prediction.At<float>(0), prediction.At<float>(1));
        }

        /// <summary>
        /// 更新卡尔曼滤波器的测量值，并更新 BoundingBox 位置
        /// </summary>
        /// <param name="detection">检测结果</param>
        public void Update(DetectionResult detection)
        {
            lastPosition = GetCenter(detection.BoundingBox);
            var measurement = new Mat(2, 1, MatType.CV_32F);
            measurement.SetArray(new float[] { lastPosition.X, lastPosition.Y });
            kalmanFilter.Correct(measurement);

            BoundingBox = detection.BoundingBox;
            MissedFrames = 0;
        }
    }
}
