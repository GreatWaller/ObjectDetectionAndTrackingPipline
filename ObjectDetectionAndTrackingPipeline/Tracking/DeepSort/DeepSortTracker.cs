using ObjectDetectionAndTrackingPipeline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking.DeepSort
{
    internal class DeepSortTracker : ITrackingModule
    {
        private IFeatureExtractor featureExtractor;
        private Dictionary<int, List<Track>> categoryTracks = new Dictionary<int, List<Track>>();
        private Dictionary<int, int> categoryNextIds = new Dictionary<int, int>();
        private const int maxMissedFrames = 24;
        private float lambda = 0.1f;
        private float appearanceWeight = 1.0f;
        private float costThreshold = 0.1f;

        public DeepSortTracker(IFeatureExtractor featureExtractor, float lambda=0.5f, float costThreshold = 0.5f, float appearanceWeight = 10.0f)
        {
            this.featureExtractor = featureExtractor;
            this.lambda = lambda;
            this.costThreshold = costThreshold;
            this.appearanceWeight = appearanceWeight;
        }

        public List<DetectionResult> Track(Mat frame, List<DetectionResult> detectedObjects)
        {
            var groupedDetections = detectedObjects.GroupBy(d => d.ClassId);

            foreach (var group in groupedDetections)
            {
                int classId = group.Key;

                if (!categoryTracks.ContainsKey(classId))
                {
                    categoryTracks[classId] = new List<Track>();
                    categoryNextIds[classId] = 0;
                }

                Update(frame, classId, group.ToList());
            }

            return detectedObjects;
        }

        private void Update(Mat frame, int classId, List<DetectionResult> detections)
        {
            var tracks = categoryTracks[classId];
            var nextId = categoryNextIds[classId];

            // 提取检测结果的特征向量
            var features = detections
                .Select(d => featureExtractor.ExtractFeature(frame, d.BoundingBox))
                .ToList();

            var unmatchedTracks = new List<Track>(tracks);
            var matchedPairs = AssociateDetectionsToTracks(detections, features, unmatchedTracks);

            // 更新匹配的检测结果
            foreach (var (detection, feature, track) in matchedPairs)
            {
                track.Update(detection, feature);
                detection.Id = track.Id;
                unmatchedTracks.Remove(track);
            }
            Console.WriteLine($"Match Pairs: {matchedPairs.Count}");
            // 创建新 Track
            foreach (var (detection, feature) in detections
                .Zip(features, (d, f) => (d, f))
                .Except(matchedPairs.Select(p => (p.Item1, p.Item2))))
            {
                detection.Id = nextId;
                tracks.Add(new Track(nextId++, detection, feature));
            }

            // 增加未匹配 Track 的丢失帧计数
            foreach (var track in unmatchedTracks)
            {
                track.Predict();
                track.MissedFrames++;
            }

            // 删除丢失帧超过阈值的 Track
            tracks.RemoveAll(t => t.MissedFrames > maxMissedFrames);
            categoryNextIds[classId] = nextId;
        }

        private List<(DetectionResult, float[], Track)> AssociateDetectionsToTracks(
            List<DetectionResult> detections,
            List<float[]> features,
            List<Track> unmatchedTracks)
        {
            var matches = new List<(DetectionResult, float[], Track)>();
            var costMatrix = new float[detections.Count, unmatchedTracks.Count];

            // 计算检测结果与未匹配 Track 的成本矩阵
            for (int i = 0; i < detections.Count; i++)
            {
                //var detectionCenter = GetBoundingBoxCenter(detections[i].BoundingBox);

                for (int j = 0; j < unmatchedTracks.Count; j++)
                {
                    unmatchedTracks[j].Predict();
                    var motionCost = 1 - unmatchedTracks[j].CalculateIoU(detections[i].BoundingBox);

                    var appearanceCost = unmatchedTracks[j].CalculateAppearanceDistance(features[i]);

                    costMatrix[i, j] = lambda * motionCost   + (1-lambda )* appearanceCost*appearanceWeight; // 权重可调
                    Console.WriteLine($"CostMatrix[{i},{j}] = {costMatrix[i, j]} ={ motionCost}  + {appearanceCost}");
                }
            }

            // 使用匈牙利算法解决匹配问题
            var matchedIndices = HungarianAlgorithm.Solve(costMatrix);
            // 打印匹配索引
            Console.WriteLine("Matched Indices:");
            for (int i = 0; i < matchedIndices.Length; i++)
            {
                Console.WriteLine($"Detection {i} -> Track {matchedIndices[i]}");
            }

            // 筛选有效匹配
            for (int i = 0; i < matchedIndices.Length; i++)
            {
                if (matchedIndices[i] >= 0 && costMatrix[i, matchedIndices[i]] < costThreshold)
                {
                    matches.Add((detections[i], features[i], unmatchedTracks[matchedIndices[i]]));
                }
            }

            return matches;
        }

        private Point GetBoundingBoxCenter(Rect boundingBox)
        {
            return new Point(
                boundingBox.X + boundingBox.Width / 2,
                boundingBox.Y + boundingBox.Height / 2
            );
        }
    }

}
