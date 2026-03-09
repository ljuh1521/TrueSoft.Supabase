using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Truesoft.Supabase.Editor
{
    public static class SupabaseSetupMenu
    {
        [MenuItem("TrueSoft/Supabase/Create Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var asset = ScriptableObject.CreateInstance<Truesoft.Supabase.SupabaseSettings>();
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/SupabaseSettings.asset");

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = asset;
            Debug.Log($"[Truesoft.Supabase] Created settings asset: {path}");
        }

        [MenuItem("TrueSoft/Supabase/Install In Current Scene")]
        public static void InstallInCurrentScene()
        {
            var existing = Object.FindFirstObjectByType<Truesoft.Supabase.SupabaseRunner>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.LogWarning("[Truesoft.Supabase] SupabaseRunner already exists.");
                return;
            }

            var go = new GameObject("SupabaseRunner");
            go.AddComponent<Truesoft.Supabase.SupabaseRunner>();

            Undo.RegisterCreatedObjectUndo(go, "Create SupabaseRunner");
            Selection.activeGameObject = go;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Truesoft.Supabase] Installed SupabaseRunner in current scene.");
        }
    }
}