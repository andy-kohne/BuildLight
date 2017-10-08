using BuildLight.Common.Models;
using BuildLight.Common.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;

namespace BuildLight.Common.Extensions
{
    public static class RgbPinSetFluentExtensions
    {
        public static RgbPinSet FromConfig(this RgbOutputPinSet pinSet, IPwmController pwmController)
        {
            return new RgbPinSet
            {
                Red = pwmController == null || pinSet?.RedPin == null ? null : pwmController.OpenPin(pinSet.RedPin.Value),
                Green = pwmController == null || pinSet?.GreenPin == null ? null : pwmController.OpenPin(pinSet.GreenPin.Value),
                Blue = pwmController == null || pinSet?.BluePin == null ? null : pwmController.OpenPin(pinSet.BluePin.Value)
            };
        }

        public static RgbPinSet Start(this RgbPinSet pinSet)
        {
            if (!pinSet.Red?.IsStarted ?? false) pinSet.Red?.Start();
            if (!pinSet.Green?.IsStarted ?? false) pinSet.Green?.Start();
            if (!pinSet.Blue?.IsStarted ?? false) pinSet.Blue?.Start();
            return pinSet;
        }

        public static RgbPinSet Stop(this RgbPinSet pinSet)
        {
            if (pinSet.Red?.IsStarted ?? false) pinSet.Red.Stop();
            if (pinSet.Green?.IsStarted ?? false) pinSet.Green.Stop();
            if (pinSet.Blue?.IsStarted ?? false) pinSet.Blue.Stop();
            return pinSet;
        }

        public static Task<RgbPinSet> SetColorAsync(this RgbPinSet pinSet, Color c)
        {
            var r = (double)c.R / 255;
            var g = (double)c.G / 255;
            var b = (double)c.B / 255;

            pinSet.Red?.SetActiveDutyCyclePercentage(r);
            pinSet.Green?.SetActiveDutyCyclePercentage(g);
            pinSet.Blue?.SetActiveDutyCyclePercentage(b);

            return Task.FromResult(pinSet);
        }

        public static async Task<RgbPinSet> SetColorAsync(this Task<RgbPinSet> pinSet, Color c)
        {
            return await SetColorAsync(await pinSet, c);
        }

        public static async Task<RgbPinSet> FadeToColorAsync(this RgbPinSet pinSet, Color c, TimeSpan period, int steps, CancellationToken cancellationToken)
        {
            var sR = (pinSet.Red?.GetActiveDutyCyclePercentage() * 255) ?? 0;
            var sG = (pinSet.Green?.GetActiveDutyCyclePercentage() * 255) ?? 0;
            var sB = (pinSet.Blue?.GetActiveDutyCyclePercentage() * 255) ?? 0;

            var dR = (c.R - sR) / steps;
            var dG = (c.G - sG) / steps;
            var dB = (c.B - sB) / steps;

            var p = period.TotalMilliseconds / steps;

            for (var i = 0; i < steps; i++)
            {
                var puase = Task.Delay(TimeSpan.FromMilliseconds(p), cancellationToken);
                var stepColor = Color.FromArgb(0, (byte)(sR + dR * i), (byte)(sG + dG * i), (byte)(sB + dB * i));
                await pinSet.SetColorAsync(stepColor);
                await puase;
            }
            await pinSet.SetColorAsync(c);
            return pinSet;
        }

        public static async Task<RgbPinSet> FadeToColorAsync(this Task<RgbPinSet> pinSet, Color c, TimeSpan period, int steps, CancellationToken cancellationToken)
        {
            return await FadeToColorAsync(await pinSet, c, period, steps, cancellationToken);
        }

        public static async Task<RgbPinSet> HoldAsync(this RgbPinSet pinSet, TimeSpan duration, CancellationToken cancellationToken)
        {
            await Task.Delay(duration, cancellationToken);
            return pinSet;
        }

        public static async Task<RgbPinSet> HoldAsync(this Task<RgbPinSet> pinSet, TimeSpan duration, CancellationToken cancellationToken)
        {
            return await HoldAsync(await pinSet, duration, cancellationToken);
        }

    }
}