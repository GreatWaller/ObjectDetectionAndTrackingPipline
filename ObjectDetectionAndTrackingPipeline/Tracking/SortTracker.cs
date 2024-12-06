using ObjectDetectionAndTrackingPipeline.Detection;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.Tracking
{
    internal class SortTracker : ITrackingModule
    {
        private List<Track> tracks = new List<Track>();
        private int nextId = 0;
        private const int maxMissedFrames = 24;

        public List<DetectionResult> Track(Mat frame, List<DetectionResult> detectedObjects)
        {
            // 更新和分配 ID
            Update(detectedObjects);
            return detectedObjects; // 返回赋了 ID 的原列表
        }

        private void Update(List<DetectionResult> detections)
        {
            var unmatchedTracks = new List<Track>(tracks);
            var matchedPairs = AssociateDetectionsToTracks(detections, unmatchedTracks);

            // 更新匹配的检测结果
            foreach (var (detection, track) in matchedPairs)
            {
                track.Update(detection);
                detection.Id = track.Id; // 给传入的 DetectionResult 分配 ID
                unmatchedTracks.Remove(track);
            }

            // 为未匹配的检测结果创建新 Track，并分配新的 ID
            foreach (var detection in detections.Except(matchedPairs.Select(p => p.Item1)))
            {
                detection.Id = nextId; // 给新的检测分配 ID
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
        }

        private List<(DetectionResult, Track)> AssociateDetectionsToTracks(List<DetectionResult> detections, List<Track> unmatchedTracks)
        {
            var matches = new List<(DetectionResult, Track)>();
            var costMatrix = new float[detections.Count, unmatchedTracks.Count];

            // 计算检测结果和未匹配 Track 之间的距离矩阵
            for (int i = 0; i < detections.Count; i++)
            {
                var detectionCenter = new Point(
                    detections[i].BoundingBox.X + detections[i].BoundingBox.Width / 2,
                    detections[i].BoundingBox.Y + detections[i].BoundingBox.Height / 2);

                for (int j = 0; j < unmatchedTracks.Count; j++)
                {
                    var trackCenter = unmatchedTracks[j].Predict();

                    // 欧几里得距离
                    costMatrix[i, j] = MathF.Sqrt(
                        MathF.Pow(detectionCenter.X - trackCenter.X, 2) +
                        MathF.Pow(detectionCenter.Y - trackCenter.Y, 2));
                }
            }

            // 匈牙利算法解决匹配问题
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
    }
}
