using System;
using Windows.Devices.Pwm;

namespace BuildLight.Common.Services
{
    public interface IPwmPin : IDisposable
    {
        double GetActiveDutyCyclePercentage();
        void SetActiveDutyCyclePercentage(double dutyCyclePercentage);
        void Start();
        void Stop();
        bool IsStarted { get; }
    }

    public class PwmPinProxy : IPwmPin
    {
        private readonly PwmPin _pwmPin;

        public PwmPinProxy(PwmPin pwmPin)
        {
            _pwmPin = pwmPin;
        }

        public void Dispose() => _pwmPin?.Dispose();
        public double GetActiveDutyCyclePercentage() => _pwmPin.GetActiveDutyCyclePercentage();
        public void SetActiveDutyCyclePercentage(double dutyCyclePercentage) => _pwmPin.SetActiveDutyCyclePercentage(dutyCyclePercentage);
        public void Start() => _pwmPin.Start();
        public void Stop() => _pwmPin.Stop();
        public bool IsStarted => _pwmPin.IsStarted;
    }
}