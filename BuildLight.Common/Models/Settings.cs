using Newtonsoft.Json;
using System;

namespace BuildLight.Common.Models
{
    public class Settings
    {
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int PollingSeconds { get; set; }
        public ProjectSettings[] Projects { get; set; }
        public VisualizationConfig[] Visualizations { get; set; }

        [JsonIgnore]
        public TimeSpan PollingTimespan => TimeSpan.FromSeconds(PollingSeconds);
    }

    public class ProjectSettings
    {
        public string Name { get; set; }
        public string[] IgnoredBuildConfigs { get; set; }
    }

    public class VisualizationConfig
    {
        public string Name { get; set; }
        public RgbOutputPinSet HardwareOutput { get; set; }
        public string[] AssociatedProjects { get; set; }
    }

    public class RgbOutputPinSet
    {
        public int? RedPin { get; set; }
        public int? GreenPin { get; set; } 
        public int? BluePin { get; set; }
    }
}
