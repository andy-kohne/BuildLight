using BuildLight.Common.Data;
using Newtonsoft.Json;
using System;

namespace BuildLight.Common.Extensions
{
    public static class StringExtensions
    {
        public static BuildStatus ConvertToBuildStatus(this string status)
        {
            return status.ConvertToEnum(BuildStatus.Unknown);
        }

        public static BuildState ConvertToBuildState(this string state)
        {
            return state.ConvertToEnum(BuildState.Unknown);
        }

        public static TEnum ConvertToEnum<TEnum>(this string s, TEnum defaultValue) where TEnum : struct
        {
            TEnum value;
            return Enum.TryParse(s, true, out value) ? value : defaultValue;
        }

        public static T ConvertJsonTo<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }

        public static string ConvertToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

        }

    }
}
