using System;
using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public abstract class DefaultConfig<T> where T : Enum
    {
        private static readonly Dictionary<T, string> Values = new();

        static DefaultConfig()
        {
            foreach (T key in Enum.GetValues(typeof(T)))
            {
                Values[key] = $"GGemCo_{key}";
            }
        }

        public static Dictionary<T, string> GetValues()
        {
            return Values;
        }
        public static string GetValue(T key)
        {
            return Values.TryGetValue(key, out string value) ? value : throw new ArgumentException($"Invalid key: {key}");
        }
    }
}