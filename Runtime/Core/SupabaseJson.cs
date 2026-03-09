using System;
using UnityEngine;

namespace Truesoft.Supabase
{
    public static class SupabaseJson
    {
        public static string ToJson<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonUtility.FromJson<T>(json);
        }

        [Serializable]
        private sealed class ArrayWrapper<T>
        {
            public T[] items;
        }

        public static T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<T>();

            var wrapped = "{ \"items\": " + json + "}";
            var result = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
            return result?.items ?? Array.Empty<T>();
        }
    }
}