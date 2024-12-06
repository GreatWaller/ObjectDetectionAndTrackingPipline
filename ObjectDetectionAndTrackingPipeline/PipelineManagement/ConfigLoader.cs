using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ObjectDetectionAndTrackingPipeline.PipelineManagement
{
    internal static class ConfigLoader
    {
        public static PipelineSettings LoadFromJson(string configFilePath)
        {
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"Config file not found: {configFilePath}");

            string json = File.ReadAllText(configFilePath);
            return JsonSerializer.Deserialize<PipelineSettings>(json);
        }
    }
}
