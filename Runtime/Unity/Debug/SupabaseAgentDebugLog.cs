using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Truesoft.Supabase.Unity.Diagnostics
{
    /// <summary>Debug-mode NDJSON sink (session a19a0d). Do not log secrets. Namespace avoids shadowing UnityEngine.Debug.</summary>
    internal static class SupabaseAgentDebugLog
    {
        internal static void Write(
            string hypothesisId,
            string location,
            string message,
            IReadOnlyDictionary<string, string> data,
            string runId = "pre-fix")
        {
            try
            {
                var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var sb = new StringBuilder(400);
                sb.Append("{\"sessionId\":\"a19a0d\",\"timestamp\":").Append(ts)
                    .Append(",\"runId\":\"").Append(Escape(runId))
                    .Append("\",\"hypothesisId\":\"").Append(Escape(hypothesisId))
                    .Append("\",\"location\":\"").Append(Escape(location))
                    .Append("\",\"message\":\"").Append(Escape(message))
                    .Append("\",\"data\":{");
                if (data != null)
                {
                    var first = true;
                    foreach (var kv in data)
                    {
                        if (!first)
                            sb.Append(',');
                        first = false;
                        sb.Append('"').Append(Escape(kv.Key)).Append("\":\"").Append(Escape(kv.Value ?? ""))
                            .Append('"');
                    }
                }

                sb.Append("}}\n");
                TryAppend(sb.ToString());
            }
            catch
            {
                // intentionally empty
            }
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", " ");
        }

        static void TryAppend(string line)
        {
            var candidates = new[]
            {
                Path.Combine(Application.persistentDataPath, "debug-a19a0d.log"),
                Path.Combine(Application.dataPath, "..", "debug-a19a0d.log"),
            };
            foreach (var c in candidates)
            {
                try
                {
                    var full = Path.GetFullPath(c);
                    File.AppendAllText(full, line);
                    return;
                }
                catch
                {
                    // try next
                }
            }
        }
    }
}
