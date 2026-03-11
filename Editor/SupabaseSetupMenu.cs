using System.IO;
using Truesoft.Supabase.Unity;
using UnityEditor;
using UnityEngine;

namespace Truesoft.Supabase.Editor
{
    public static class SupabaseSetupMenu
    {
        private const string DefaultFolder = "Assets/Resources";
        private const string AssetName = "SupabaseSettings.asset";

        [MenuItem("TrueSoft/Supabase/Create Settings Asset")]
        public static void CreateSettingsAsset()
        {
            if (Directory.Exists(DefaultFolder) == false)
                Directory.CreateDirectory(DefaultFolder);

            var assetPath = Path.Combine(DefaultFolder, AssetName).Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<SupabaseSettings>(assetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[Supabase] Settings asset already exists: " + assetPath);
                return;
            }

            var settings = ScriptableObject.CreateInstance<SupabaseSettings>();

            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            Debug.Log("[Supabase] Settings asset created: " + assetPath);
        }
    }
}