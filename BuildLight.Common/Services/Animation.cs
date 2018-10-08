using BuildLight.Common.Extensions;
using BuildLight.Common.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace BuildLight.Common.Services
{
    public class Animation
    {
        public List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>> Steps { get; set; }

        public async Task Run( RgbPinSet pinSet, CancellationToken cancellationToken)
        {
            foreach (var step in Steps)
            {
                await step(pinSet, cancellationToken);
            }
        }


    }

    public class Animations
    {
        public static Dictionary<AnimatedVisualizationStates, Animation> DefaultEffects => new Dictionary<AnimatedVisualizationStates, Animation>
        {
            {
                AnimatedVisualizationStates.None,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Black)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(3), c)}
                    }
                }
            },
            {
                AnimatedVisualizationStates.Failed,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Red)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(3), c)}
                    }
                }
            },
            {
                AnimatedVisualizationStates.Succeeded,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Green)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(3), c)}
                    }
                }
            },
            {
                AnimatedVisualizationStates.Building,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Blue)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(3), c)}
                    }
                }
            },
            {
                AnimatedVisualizationStates.FailedBuilding,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Red)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(2), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(60), 30, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                    }
                }
            },
            {
                AnimatedVisualizationStates.SuccessBuilding,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Green)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(2), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(60), 30, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                    }
                }
            },
            {
                AnimatedVisualizationStates.FailedAlert,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Red)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Red, TimeSpan.FromMilliseconds(20), 10, c) },
                    }
                }
            },
            {
                AnimatedVisualizationStates.StartingUp,
                new Animation
                {
                    Steps = new List<Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>>
                    {
                        {(p, c) => p.SetColorAsync(Colors.Black)},
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Red, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Green, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Blue, TimeSpan.FromMilliseconds(20), 10, c) },
                        {(p, c) => p.HoldAsync(TimeSpan.FromSeconds(1), c)},
                        {(p, c) => p.FadeToColorAsync(Colors.Black, TimeSpan.FromMilliseconds(20), 10, c) },
                    }
                }
            },
        };

        public static Dictionary<AnimatedVisualizationStates, Animation> GetEffects(Settings settings)
        {
            var ret = Animations.DefaultEffects;

            foreach (var a in settings.Animations)
            {
                if (!Enum.TryParse(a.Key, out AnimatedVisualizationStates state))
                    throw new ArgumentOutOfRangeException(nameof(a.Key), a.Key, "Invalid animation key");

                if (ret.ContainsKey(state))
                    ret.Remove(state);

                ret.Add(state, new Animation
                {
                    Steps = a.Value.Steps.Select(step =>
                    {
                        switch (step.Type)
                        {
                            case "SetColor":
                                var setColor = step.Parameters["Color"].ToColor();
                                return new Func<RgbPinSet, CancellationToken, Task<RgbPinSet>>((p, c) => p.SetColorAsync(setColor));
                            case "Hold":
                                var duration = TimeSpan.Parse(step.Parameters["Duration"]);
                                return (p, c) => p.HoldAsync(duration, c);
                            case "FadeToColor":
                                var fadeColor = step.Parameters["Color"].ToColor();
                                var period = TimeSpan.Parse(step.Parameters["Period"]);
                                var steps = int.Parse(step.Parameters["Steps"]);
                                return (p, c) => p.FadeToColorAsync(fadeColor, period, steps, c);
                        }

                        throw new ArgumentOutOfRangeException(nameof(step.Type), step.Type, "Not a valid type");
                    }).ToList()
                });
            }

            return ret;
        }


    }
}