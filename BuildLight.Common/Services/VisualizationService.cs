using BuildLight.Common.Extensions;
using BuildLight.Common.Models;
using BuildLight.Common.Services.BuildMonitor;
using BuildLight.Common.Services.TeamCity;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace BuildLight.Common.Services
{
    public interface IVisualizationService
    {
        void Run(CancellationToken cancellationToken);
        void HandleBuildEvent(object sender, BuildStatusEventArgs e);
    }

    public class VisualizationService : IVisualizationService
    {
        internal readonly Visualization[] _visualizations;
        private readonly IPwmController _pwmController;

        public VisualizationService(VisualizationConfig[] visualizationConfigs, IPwmController pwmController)
        {
            _pwmController = pwmController;
            _visualizations =
               visualizationConfigs.Select(config => Visualization.FromConfig(config, _pwmController)).ToArray();
        }

        public void Run(CancellationToken cancellationToken)
        {
            foreach (var visualization in _visualizations)
            {
                Task.Run(() => { AnimateAsync(visualization, cancellationToken); });
            }
        }

        internal static async void AnimateAsync(Visualization vis, CancellationToken cancellationToken)
        {
            await vis.RgbPinSet.SetColorAsync(Color.FromArgb(0, 1, 1, 1));

            while (!cancellationToken.IsCancellationRequested)
            {
                Debug.WriteLine($"  {vis.Name} Visualization state {vis.AnimatedVisualizationState}");

                if (vis.AnimatedVisualizationState == AnimatedVisualizationStates.Failed && vis.TimeInState > TimeSpan.FromHours(1))
                    vis.AnimatedVisualizationState = AnimatedVisualizationStates.FailedAlert;

                switch (vis.AnimatedVisualizationState)
                {
                    case AnimatedVisualizationStates.None:
                        await vis.RgbPinSet.SetColorAsync(Colors.Black)
                                           .HoldAsync(TimeSpan.FromSeconds(3), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.Failed:
                        await vis.RgbPinSet.SetColorAsync(Colors.Red)
                                           .HoldAsync(TimeSpan.FromSeconds(3), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.Succeeded:
                        await vis.RgbPinSet.SetColorAsync(Colors.Green)
                                           .HoldAsync(TimeSpan.FromSeconds(3), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.Building:
                        await vis.RgbPinSet.SetColorAsync(Colors.Blue)
                                           .HoldAsync(TimeSpan.FromSeconds(3), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.FailedBuilding:
                        await vis.RgbPinSet.SetColorAsync(Colors.Red)
                                           .HoldAsync(TimeSpan.FromSeconds(2), cancellationToken)
                                           .FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(60), 30, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.SuccessBuilding:
                        await vis.RgbPinSet.SetColorAsync(Colors.Green)
                                           .HoldAsync(TimeSpan.FromSeconds(2), cancellationToken)
                                           .FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(60), 30, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken);
                        break;

                    case AnimatedVisualizationStates.FailedAlert:
                        await vis.RgbPinSet.SetColorAsync(Colors.Red)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Red, TimeSpan.FromMilliseconds(20), 10, cancellationToken);
                        break;

                    case AnimatedVisualizationStates.StartingUp:
                        await vis.RgbPinSet.SetColorAsync(Colors.Black)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Red, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Green, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(20), 10, cancellationToken)
                                           .HoldAsync(TimeSpan.FromSeconds(1), cancellationToken)
                                           .FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, cancellationToken);

                        break;
                }
            }

            await vis.RgbPinSet.SetColorAsync(Colors.Black);
            vis.RgbPinSet.Stop();
        }

        private static VisualizationStates MapBuildStatus(Status buildStatus)
        {
            switch (buildStatus)
            {
                case Status.Failure: return VisualizationStates.Failed;
                case Status.Queued: return VisualizationStates.Queued;
                case Status.Running: return VisualizationStates.Running;
                case Status.Success: return VisualizationStates.Succeeded;
                case Status.Unknown: return VisualizationStates.None;
                default: return VisualizationStates.None;
            }
        }

        public void HandleBuildEvent(object sender, BuildStatusEventArgs e)
        {
            var visState = MapBuildStatus(e.BuildStatus);

            foreach (var visualization in _visualizations.Where(v => v.AssociatedProjects.Contains(e.Project.Name, StringComparer.OrdinalIgnoreCase)))
            {
                var existingOverall = visualization.OverallState;
                visualization.States[e.Project.Name] = visState;
                var newState = visualization.OverallState;

                if (existingOverall != newState)
                {
                    visualization.StateChangeTime = DateTime.Now;
                    if (newState == VisualizationStates.Failed)
                    {
                        visualization.AnimatedVisualizationState = AnimatedVisualizationStates.Failed;
                    }
                    if (newState == VisualizationStates.Running)
                    {
                        if (existingOverall == VisualizationStates.Failed)
                            visualization.AnimatedVisualizationState = AnimatedVisualizationStates.FailedBuilding;
                        else if (existingOverall == VisualizationStates.Succeeded)
                            visualization.AnimatedVisualizationState = AnimatedVisualizationStates.SuccessBuilding;
                        else
                            visualization.AnimatedVisualizationState = AnimatedVisualizationStates.Building;
                    }
                    if (newState == VisualizationStates.Succeeded)
                    {
                        visualization.AnimatedVisualizationState = AnimatedVisualizationStates.Succeeded;
                    }

                    Debug.Write($"{DateTime.Now:T}       ");
                    Debug.Write($"{e.Project.Name} changed to ");
                    Debug.WriteLine($"{visState}");

                    Debug.Write($"{DateTime.Now:T}       ");
                    Debug.WriteLine($"{visualization.Name} changed to {newState}");
                }

            }

        }
    }
}
