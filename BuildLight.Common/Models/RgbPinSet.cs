using Windows.Devices.Pwm;

namespace BuildLight.Common.Models
{
    public class RgbPinSet
    {
        public PwmPin Red { get; set; }
        public PwmPin Green { get; set; }
        public PwmPin Blue { get; set; }
    }
}