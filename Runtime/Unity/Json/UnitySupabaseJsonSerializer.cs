using System;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    public sealed class UnitySupabaseJsonSerializer : ISupabaseJsonSerializer
    {
        [Serializable]
        private sealed class ArrayWrapper<T>
        {
            public T[] items;
        }

        public string ToJson<T>(T value)
        {
            return JsonUtility.ToJson(value);
        }

        public T FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonUtility.FromJson<T>(json);
        }

        public T[] FromJsonArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Array.Empty<T>();

            var wrapped = "{ \"items\": " + json + "}";
            var result = JsonUtility.FromJson<ArrayWrapper<T>>(wrapped);
            return result?.items ?? Array.Empty<T>();
        }
    }
}