using BuildLight.Common.Extensions;
using BuildLight.Common.Models;

namespace BuildLight.Common
{
    public class SettingsService
    {
        public static Settings ReadSettings(string text)
        {
            return text.ConvertJsonTo<Settings>();

        }
    }
}
