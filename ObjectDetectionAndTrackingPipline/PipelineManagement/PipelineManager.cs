using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipline.PipelineManagement
{
    internal class PipelineManager
    {
        private readonly Dictionary<string, Pipeline> _pipelines = new();  // 管道集合
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new(); // 每个管道的取消令牌

        /// <summary>
        /// 添加一个新的 Pipeline。
        /// </summary>
        /// <param name="id">Pipeline 的唯一标识。</param>
        /// <param name="pipeline">Pipeline 实例。</param>
        public void AddPipeline(string id, Pipeline pipeline)
        {
            if (_pipelines.ContainsKey(id))
                throw new ArgumentException($"Pipeline with ID {id} already exists.");

            _pipelines[id] = pipeline;
            _cancellationTokens[id] = new CancellationTokenSource();
        }

        /// <summary>
        /// 启动指定的 Pipeline。
        /// </summary>
        /// <param name="id">Pipeline 的唯一标识。</param>
        public void StartPipeline(string id)
        {
            if (!_pipelines.ContainsKey(id))
                throw new ArgumentException($"Pipeline with ID {id} does not exist.");

            var cts = _cancellationTokens[id];
            _pipelines[id].Start(); // 启动管道
            Console.WriteLine($"Pipeline {id} started.");
        }

        /// <summary>
        /// 停止指定的 Pipeline。
        /// </summary>
        /// <param name="id">Pipeline 的唯一标识。</param>
        public void StopPipeline(string id)
        {
            if (!_pipelines.ContainsKey(id))
                throw new ArgumentException($"Pipeline with ID {id} does not exist.");

            _cancellationTokens[id].Cancel(); // 取消令牌
            Console.WriteLine($"Pipeline {id} stopped.");
        }

        /// <summary>
        /// 启动所有 Pipeline。
        /// </summary>
        public void StartAll()
        {
            foreach (var id in _pipelines.Keys)
            {
                StartPipeline(id);
            }
        }

        /// <summary>
        /// 停止所有 Pipeline。
        /// </summary>
        public void StopAll()
        {
            foreach (var id in _pipelines.Keys)
            {
                StopPipeline(id);
            }
        }

        /// <summary>
        /// 检查某个 Pipeline 是否已启动。
        /// </summary>
        /// <param name="id">Pipeline 的唯一标识。</param>
        /// <returns>是否正在运行。</returns>
        public bool IsPipelineRunning(string id)
        {
            return _pipelines.ContainsKey(id) && !_cancellationTokens[id].IsCancellationRequested;
        }
    }
}
