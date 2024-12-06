using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.Tracking
{
    internal static class HungarianAlgorithm
    {
        public static int[] Solve(float[,] costMatrix)
        {
            int rows = costMatrix.GetLength(0);
            int cols = costMatrix.GetLength(1);
            int[] result = new int[rows];
            Array.Fill(result, -1);

            // 模拟匈牙利算法步骤的伪实现，适用于中小规模的匹配任务
            for (int i = 0; i < rows; i++)
            {
                double minCost = double.MaxValue;
                int bestCol = -1;
                for (int j = 0; j < cols; j++)
                {
                    if (costMatrix[i, j] < minCost)
                    {
                        minCost = costMatrix[i, j];
                        bestCol = j;
                    }
                }
                if (bestCol != -1)
                {
                    result[i] = bestCol;
                }
            }
            return result;
        }
    }
}
