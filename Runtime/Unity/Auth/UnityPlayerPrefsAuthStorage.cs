using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    public sealed class UnityPlayerPrefsAuthStorage : ISupabaseAuthStorage
    {
        private const string SessionKey = "Truesoft.Supabase.Session";

        public void SaveSession(string json)
        {
            if (json == null)
                json = string.Empty;

            PlayerPrefs.SetString(SessionKey, json);
            PlayerPrefs.Save();
        }

        public string LoadSession()
        {
            return PlayerPrefs.GetString(SessionKey, string.Empty);
        }

        public void ClearSession()
        {
            PlayerPrefs.DeleteKey(SessionKey);
            PlayerPrefs.Save();
        }
    }
}