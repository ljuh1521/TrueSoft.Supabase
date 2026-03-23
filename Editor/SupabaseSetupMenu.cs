using System.IO;
using Truesoft.Supabase.Unity;
using Truesoft.Supabase.Unity.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            var settings = GetOrCreateSettingsAsset();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("TrueSoft/Supabase/Create Runtime Object In Scene")]
        public static void CreateRuntimeObjectInScene()
        {
            var existing = Object.FindObjectOfType<SupabaseRuntime>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                Debug.Log("[Supabase] Runtime object already exists in current scene.");
                return;
            }

            var settings = GetOrCreateSettingsAsset();

            var go = new GameObject("SupabaseSDK");
            Undo.RegisterCreatedObjectUndo(go, "Create Supabase Runtime Object");

            var runtime = go.AddComponent<SupabaseRuntime>();
            var so = new SerializedObject(runtime);
            var settingsProp = so.FindProperty("settings");
            settingsProp.objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeObject = go;
            EditorGUIUtility.PingObject(go);
            EditorSceneManager.MarkSceneDirty(go.scene);

            Debug.Log("[Supabase] Runtime object created in scene: " + go.name);
        }

        private static SupabaseSettings GetOrCreateSettingsAsset()
        {
            if (Directory.Exists(DefaultFolder) == false)
                Directory.CreateDirectory(DefaultFolder);

            var assetPath = Path.Combine(DefaultFolder, AssetName).Replace("\\", "/");

            var existing = AssetDatabase.LoadAssetAtPath<SupabaseSettings>(assetPath);
            if (existing != null)
            {
                Debug.Log("[Supabase] Settings asset already exists: " + assetPath);
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<SupabaseSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Supabase] Settings asset created: " + assetPath);
            return settings;
        }
    }
}