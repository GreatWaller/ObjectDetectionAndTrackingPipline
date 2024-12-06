using OpenCvSharp.Dnn;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Detection
{
    internal class YoloDetecter
    {
        private Net _net;
        private List<string> _classLabels;
        private float _confidenceThreshold;
        private float _nmsThreshold;

        public YoloDetecter(string configPath, string weightsPath, string namesPath, float confidenceThreshold = 0.5f, float nmsThreshold = 0.4f)
        {
            // 加载YOLO模型
            _net = CvDnn.ReadNetFromDarknet(configPath, weightsPath);
            _net.SetPreferableBackend(Backend.CUDA);
            _net.SetPreferableTarget(Target.CUDA);

            // 加载类别名称
            _classLabels = new List<string>(File.ReadAllLines(namesPath));

            _confidenceThreshold = confidenceThreshold;
            _nmsThreshold = nmsThreshold;
        }

        public List<Rect> Detect(Mat frame)
        {
            List<Rect> detectedObjects = new List<Rect>();
            List<int> classIds = new List<int>();
            List<float> confidences = new List<float>();

            // YOLO输入处理
            var blob = CvDnn.BlobFromImage(frame, 1 / 255.0, new Size(416, 416), new Scalar(0, 0, 0), true, false);
            _net.SetInput(blob);

            // YOLO前向传播
            var outputLayerNames = _net.GetUnconnectedOutLayersNames();
            Mat[] outputBlobs = outputLayerNames.Select(_ => new Mat()).ToArray();
            _net.Forward(outputBlobs, outputLayerNames);

            // 解析输出
            foreach (var output in outputBlobs)
            {
                for (int i = 0; i < output.Rows; i++)
                {
                    var confidence = output.At<float>(i, 4);
                    if (confidence > _confidenceThreshold)
                    {
                        // 获取检测的类别概率
                        Cv2.MinMaxLoc(output.Row(i).ColRange(5, output.Cols), out _, out var maxClassScore, out _, out var maxClassLoc);
                        var classId = maxClassLoc.X;
                        var classScore = maxClassScore;

                        if (classScore > _confidenceThreshold)
                        {
                            // 计算检测框
                            int centerX = (int)(output.At<float>(i, 0) * frame.Width);
                            int centerY = (int)(output.At<float>(i, 1) * frame.Height);
                            int width = (int)(output.At<float>(i, 2) * frame.Width);
                            int height = (int)(output.At<float>(i, 3) * frame.Height);
                            int x = centerX - width / 2;
                            int y = centerY - height / 2;

                            classIds.Add(classId);
                            confidences.Add((float)classScore);
                            detectedObjects.Add(new Rect(x, y, width, height));
                        }
                    }
                }
            }

            // 非极大值抑制，减少重叠的检测框
            CvDnn.NMSBoxes(detectedObjects, confidences, _confidenceThreshold, _nmsThreshold, out var indices);
            List<Rect> finalObjects = new List<Rect>();

            foreach (var idx in indices)
            {
                finalObjects.Add(detectedObjects[idx]);
                Cv2.Rectangle(frame, detectedObjects[idx], Scalar.Red, 2);
                Cv2.PutText(frame, _classLabels[classIds[idx]], new Point(detectedObjects[idx].X, detectedObjects[idx].Y - 10), HersheyFonts.HersheySimplex, 0.5, Scalar.Yellow, 2);
            }

            return finalObjects;
        }
    }
}
