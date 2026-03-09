using UnityEngine;

namespace Truesoft.Supabase
{
    public sealed class PlayerPrefsSessionStore
    {
        private readonly string _key;

        public PlayerPrefsSessionStore(string key)
        {
            _key = key;
        }

        public void Save(SupabaseSession session)
        {
            var json = SupabaseJson.ToJson(session);
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
        }

        public SupabaseSession Load()
        {
            if (!PlayerPrefs.HasKey(_key))
                return null;

            var json = PlayerPrefs.GetString(_key, string.Empty);
            return SupabaseJson.FromJson<SupabaseSession>(json);
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}