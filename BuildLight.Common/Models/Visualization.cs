using BuildLight.Common.Extensions;
using BuildLight.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildLight.Common.Models
{
    public class Visualization
    {
        public string Name { get; set; }
        public string[] AssociatedProjects { get; set; }

        public RgbPinSet RgbPinSet { get; set; }
        public AnimatedVisualizationStates AnimatedVisualizationState { get; set; } = AnimatedVisualizationStates.StartingUp;
        public DateTime StateChangeTime { get; set; } = DateTime.Now;
        public TimeSpan TimeInState => DateTime.Now.Subtract(StateChangeTime);
        public Dictionary<string, VisualizationStates> States { get; set; }
        public VisualizationStates OverallState =>
            States.Any(s => s.Value == VisualizationStates.Running)
                ? VisualizationStates.Running
                : States.Any(s => s.Value == VisualizationStates.Queued)
                    ? VisualizationStates.Queued
                    : States.Any(s => s.Value == VisualizationStates.Failed)
                        ? VisualizationStates.Failed
                        : States.Any(s => s.Value == VisualizationStates.Succeeded)
                            ? VisualizationStates.Succeeded
                            : VisualizationStates.None;

        public static Visualization FromConfig(VisualizationConfig config, IPwmController pwmController)
        {
            return new Visualization
            {
                AssociatedProjects = config.AssociatedProjects,
                Name = config.Name,
                States = config.AssociatedProjects.ToDictionary(p => p, p => VisualizationStates.None),
                RgbPinSet = config.HardwareOutput.FromConfig(pwmController).Start().Stop().Start()
            };
        }
    }
}