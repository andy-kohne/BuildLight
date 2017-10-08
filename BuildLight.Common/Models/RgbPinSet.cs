using BuildLight.Common.Services;

namespace BuildLight.Common.Models
{
    public class RgbPinSet
    {
        public IPwmPin Red { get; set; }
        public IPwmPin Green { get; set; }
        public IPwmPin Blue { get; set; }
    }
}