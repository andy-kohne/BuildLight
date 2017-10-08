using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Pwm;

namespace BuildLight.Common.Services
{
    public interface IPwmController
    {
        int PinCount { get; }
        double ActualFrequency { get; }
        double SetDesiredFrequency(double desiredFrequency);
        double MinFrequency { get; }
        double MaxFrequency { get; }
        IPwmPin OpenPin(int pinNumber);
    }

    public class PwmControllerProxy : IPwmController
    {
        private readonly PwmController _pwmController;

        public PwmControllerProxy(PwmController pwmController)
        {
            _pwmController = pwmController;
        }

        public int PinCount => _pwmController.PinCount;
        public double ActualFrequency => _pwmController.ActualFrequency;
        public double SetDesiredFrequency(double desiredFrequency)
        {
            return _pwmController.SetDesiredFrequency(desiredFrequency);
        }

        public double MinFrequency => _pwmController.MinFrequency;
        public double MaxFrequency => _pwmController.MaxFrequency;
        public IPwmPin OpenPin(int pinNumber)
        {
            return new PwmPinProxy(_pwmController.OpenPin(pinNumber));
        }

        public static async Task<IPwmController> GetGontroller()
        {
            if (!LightningProvider.IsLightningEnabled)
                return null;

            LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();

            var provider = LightningPwmProvider.GetPwmProvider();
            var pwmControllers = await PwmController.GetControllersAsync(provider);
            var controller = new PwmControllerProxy(pwmControllers[1]);
            controller.SetDesiredFrequency(100);
            return controller;
        }
    }
}