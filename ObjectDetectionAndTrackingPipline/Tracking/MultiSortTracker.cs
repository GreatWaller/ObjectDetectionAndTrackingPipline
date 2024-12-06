using ObjectDetectionAndTrackingPipline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Tracking
{
    internal class MultiSortTracker: ITrackingModule
    {
        // 每个类别独立管理 Tracks 和 ID
        private Dictionary<int, List<Track>> categoryTracks = new Dictionary<int, List<Track>>();
        private Dictionary<int, int> categoryNextIds = new Dictionary<int, int>();
        private const int maxMissedFrames = 24;

        public List<DetectionResult> Track(Mat frame, List<DetectionResult> detectedObjects)
        {
            // 根据 ClassId 对检测结果分组
            var groupedDetections = detectedObjects.GroupBy(d => d.ClassId);

            foreach (var group in groupedDetections)
            {
                int classId = group.Key;

                // 初始化类别相关数据
                if (!categoryTracks.ContainsKey(classId))
                {
                    categoryTracks[classId] = new List<Track>();
                    categoryNextIds[classId] = 0;
                }

                // 更新每个类别的追踪状态
                Update(classId, group.ToList());
            }

            return detectedObjects; // 返回带有 ID 的检测结果
        }

        private void Update(int classId, List<DetectionResult> detections)
        {
            var tracks = categoryTracks[classId];
            var nextId = categoryNextIds[classId];

            var unmatchedTracks = new List<Track>(tracks);
            var matchedPairs = AssociateDetectionsToTracks(detections, unmatchedTracks);

            // 更新匹配的检测结果
            foreach (var (detection, track) in matchedPairs)
            {
                track.Update(detection);
                detection.Id = track.Id; // 给检测结果分配 ID
                unmatchedTracks.Remove(track);
            }

            // 为未匹配的检测结果创建新 Track，并分配新的 ID
            foreach (var detection in detections.Except(matchedPairs.Select(p => p.Item1)))
            {
                detection.Id = nextId; // 分配新的 ID
                tracks.Add(new Track(nextId++, detection));
            }

            // 增加未匹配 Track 的丢失帧计数
            foreach (var track in unmatchedTracks)
            {
                track.Predict();
                track.MissedFrames++;
            }

            // 删除丢失帧超过阈值的 Track
            tracks.RemoveAll(t => t.MissedFrames > maxMissedFrames);

            // 更新类别的 ID 计数器
            categoryNextIds[classId] = nextId;
        }

        private List<(DetectionResult, Track)> AssociateDetectionsToTracks(
            List<DetectionResult> detections,
            List<Track> unmatchedTracks)
        {
            var matches = new List<(DetectionResult, Track)>();
            var costMatrix = new float[detections.Count, unmatchedTracks.Count];

            // 计算检测结果与未匹配 Track 的距离矩阵
            for (int i = 0; i < detections.Count; i++)
            {
                var detectionCenter = GetBoundingBoxCenter(detections[i].BoundingBox);

                for (int j = 0; j < unmatchedTracks.Count; j++)
                {
                    var trackCenter = unmatchedTracks[j].Predict();

                    // 计算欧几里得距离
                    costMatrix[i, j] = MathF.Sqrt(
                        MathF.Pow(detectionCenter.X - trackCenter.X, 2) +
                        MathF.Pow(detectionCenter.Y - trackCenter.Y, 2));
                }
            }

            // 使用匈牙利算法进行匹配
            var matchedIndices = HungarianAlgorithm.Solve(costMatrix);

            // 筛选有效匹配
            for (int i = 0; i < matchedIndices.Length; i++)
            {
                if (matchedIndices[i] >= 0 && costMatrix[i, matchedIndices[i]] < 100)
                {
                    matches.Add((detections[i], unmatchedTracks[matchedIndices[i]]));
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
