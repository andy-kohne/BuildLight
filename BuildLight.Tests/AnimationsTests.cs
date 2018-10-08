using BuildLight.Common;
using BuildLight.Common.Extensions;
using BuildLight.Common.Models;
using BuildLight.Common.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Animation = BuildLight.Common.Models.Animation;

namespace BuildLight.Tests
{
    public class AnimationsTests
    {
        [Fact]
        public void GenerateJson()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        "none",
                        new Animation
                        {
                            Steps = new List<AnimationStep>
                            {
                                new AnimationStep
                                {
                                    Type = "SetColor",
                                    Parameters = new Dictionary<string, string>
                                    {
                                        {"Color", "Black"}
                                    }
                                },
                                new AnimationStep
                                {
                                    Type = "Hold",
                                    Parameters = new Dictionary<string, string>
                                    {
                                        { "Duration", TimeSpan.FromSeconds(3).ToString() }
                                    }
                                }
                            }

                        }
                    }
                }

            };

            var json = settings.ConvertToJson();
        }

        [Fact]
        public void ReadJson()
        {
            var text = @"{""Animations"":{""None"":{""Name"":null,""Steps"":[{""Type"":""SetColor"",""Parameters"":{""Color"":""Black""}},{""Type"":""Hold"",""Parameters"":{""Duration"":""00:00:03""}}]}}}"
                ;
            var s = SettingsService.ReadSettings(text);

            Assert.NotNull(s);
            Assert.NotNull(s.Animations);
        }

        [Fact]
        public void InvalidStateNameThrows()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        "Something", new Animation()
                    }
                }
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => Animations.GetEffects(settings));
        }

        [Fact]
        public void InvalidStepTypeThrows()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        nameof(AnimatedVisualizationStates.None), new Animation
                        {
                            Steps = new List<AnimationStep>
                            {
                               new AnimationStep
                               {
                                   Type = "Something",
                                   Parameters = new Dictionary<string, string>()
                               }
                            }
                        }
                    }
                }
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => Animations.GetEffects(settings));
        }

        [Fact]
        public void AnimationStepTypesHandled()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        nameof(AnimatedVisualizationStates.None), new Animation
                        {
                            Steps = new List<AnimationStep>
                            {
                                new AnimationStep
                                {
                                    Type = "SetColor",
                                    Parameters = new Dictionary<string, string> { { "Color", "Red"} }
                                },
                                new AnimationStep
                                {
                                    Type = "Hold",
                                    Parameters = new Dictionary<string, string> {{ "Duration", "00:00:05"}}
                                },
                                new AnimationStep
                                {
                                    Type = "FadeToColor",
                                    Parameters = new Dictionary<string, string> {{"Color", "Red"}, { "Period", "00:00:01" }, {  "Steps", "20"} }
                                },
                            }
                        }
                    }
                }
            };
            var effects = Animations.GetEffects(settings);

            Assert.Equal(3, effects[AnimatedVisualizationStates.None].Steps.Count);
        }

        [Fact]
        public async Task AnimationStepTypesCanRun()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        nameof(AnimatedVisualizationStates.None), new Animation
                        {
                            Steps = new List<AnimationStep>
                            {
                                new AnimationStep
                                {
                                    Type = "SetColor",
                                    Parameters = new Dictionary<string, string> { { "Color", "Red"} }
                                },
                                new AnimationStep
                                {
                                    Type = "Hold",
                                    Parameters = new Dictionary<string, string> {{ "Duration", "00:00:05"}}
                                },
                                new AnimationStep
                                {
                                    Type = "FadeToColor",
                                    Parameters = new Dictionary<string, string> {{"Color", "Red"}, { "Period", "00:00:01" }, {  "Steps", "20"} }
                                },
                            }
                        }
                    }
                }
            };
            var effects = Animations.GetEffects(settings);
            var rgbPinSet = new RgbPinSet();

            await effects[AnimatedVisualizationStates.None].Run(rgbPinSet, CancellationToken.None);
        }

        [Fact]
        public void AllAnimationStatesHaveDefaults()
        {
            var effects = Animations.DefaultEffects;

            Assert.NotNull(effects);
            Assert.Equal(Enum.GetValues(typeof(AnimatedVisualizationStates)).Length, effects.Count);

            foreach (var state in Enum.GetValues(typeof(AnimatedVisualizationStates)))
            {
                var s = (AnimatedVisualizationStates) state;
                Assert.True(effects.ContainsKey(s));
                Assert.NotNull(effects[s]);
            }
        }

        [Fact]
        public async Task AllAnimationStatesCanRun()
        {
            var effects = Animations.DefaultEffects;
            var rgbPinSet = new RgbPinSet();

            Assert.NotNull(effects);
            Assert.Equal(Enum.GetValues(typeof(AnimatedVisualizationStates)).Length, effects.Count);

            foreach (var state in effects)
            {
                await state.Value.Run(rgbPinSet, CancellationToken.None);
            }
        }

        [Fact]
        public void DefaultAnimationStateOverwritten()
        {
            var settings = new Settings
            {
                Animations = new Dictionary<string, Animation>
                {
                    {
                        "None", new Animation
                        {
                            Steps = new List<AnimationStep>
                            {
                                new AnimationStep
                                {
                                    Type = "SetColor",
                                    Parameters = new Dictionary<string, string> {{"Color", "Yellow"}}
                                }
                            }
                        }
                    }
                }
            };
            var effects = Animations.GetEffects(settings);

            Assert.NotNull(effects);
            Assert.Equal(Enum.GetValues(typeof(AnimatedVisualizationStates)).Length, effects.Count);

            foreach (var state in Enum.GetValues(typeof(AnimatedVisualizationStates)))
            {
                var s = (AnimatedVisualizationStates)state;
                Assert.True(effects.ContainsKey(s));
                Assert.NotNull(effects[s]);
            }

            Assert.Equal(1, effects[AnimatedVisualizationStates.None].Steps.Count);
            Assert.NotEqual(Animations.DefaultEffects[AnimatedVisualizationStates.None].Steps.Count, effects[AnimatedVisualizationStates.None].Steps.Count);
        }
    }
}
