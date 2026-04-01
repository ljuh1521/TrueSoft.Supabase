using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Truesoft.Supabase.Unity;
using UnityEditor;
using UnityEngine;

namespace Truesoft.Supabase.Editor
{
    /// <summary>
    /// PostgREST OpenAPI로 세이브 테이블 스키마를 가져와 <c>[Serializable]</c> + <c>[UserSaveColumn]</c> POCO를 생성합니다.
    /// </summary>
    public sealed class SupabaseUserSavePocoGeneratorWindow : EditorWindow
    {
        private const string DefaultClassName = "UserSaveRow";

        [SerializeField] private SupabaseSettings settings;
        [SerializeField] private string projectUrl = "";
        [SerializeField] private string anonKey = "";
        [SerializeField] private bool useServiceRoleKey;
        [SerializeField] private string serviceRoleKey = "";
        [SerializeField] private string tableName = "user_saves";
        [SerializeField] private string skipColumnsCsv = "";
        [SerializeField] private string className = DefaultClassName;
        [SerializeField] private string namespaceName = "";
        [SerializeField] private string previewText = "";
        [SerializeField] private Vector2 scroll;

        private List<string> _lastWarnings = new List<string>();

        [MenuItem("TrueSoft/Supabase/Generate User Save POCO from OpenAPI…")]
        private static void Open()
        {
            var w = GetWindow<SupabaseUserSavePocoGeneratorWindow>(true, "User save POCO (OpenAPI)", true);
            w.minSize = new Vector2(520, 420);
        }

        private void OnEnable()
        {
            if (settings == null)
                settings = Resources.Load<SupabaseSettings>("SupabaseSettings");

            if (settings != null)
                PullFromSettings();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "PostgREST OpenAPI(GET …/rest/v1/)에서 테이블 정의를 읽어 POCO를 만듭니다. "
                + "Unity JsonUtility는 JSON 키와 필드 이름이 같아야 하므로 필드명은 DB 컬럼명과 동일합니다. "
                + "테이블이 anon OpenAPI에 없으면 Service Role 키(에디터 전용) 또는 저장된 openapi.json 임포트를 사용하세요.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            settings = (SupabaseSettings)EditorGUILayout.ObjectField("Settings (optional)", settings, typeof(SupabaseSettings), false);
            if (EditorGUI.EndChangeCheck() && settings != null)
                PullFromSettings();

            projectUrl = EditorGUILayout.TextField("Project URL", projectUrl);
            anonKey = EditorGUILayout.PasswordField("Publishable (anon) key", anonKey);

            useServiceRoleKey = EditorGUILayout.ToggleLeft(
                "Fetch with service role key (editor only — never ship in builds)",
                useServiceRoleKey);
            if (useServiceRoleKey)
                serviceRoleKey = EditorGUILayout.PasswordField("Service role key", serviceRoleKey);

            tableName = EditorGUILayout.TextField("Table name", tableName);
            skipColumnsCsv = EditorGUILayout.TextField("Skip columns (CSV)", skipColumnsCsv);
            className = EditorGUILayout.TextField("Class name", className);
            namespaceName = EditorGUILayout.TextField("Namespace (optional)", namespaceName);

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fetch from API & preview", GUILayout.Height(28)))
                    FetchFromApi();

                if (GUILayout.Button("Import OpenAPI JSON…", GUILayout.Height(28)))
                    ImportJsonFile();
            }

            foreach (var w in _lastWarnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            var ro = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.TextArea(string.IsNullOrEmpty(previewText) ? "(empty)" : previewText, GUILayout.MinHeight(220));
            GUI.enabled = ro;
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(previewText)))
            {
                if (GUILayout.Button("Save as .cs in project…", GUILayout.Height(26)))
                    SaveToProject();
            }
        }

        private void PullFromSettings()
        {
            projectUrl = settings.projectUrl ?? "";
            anonKey = settings.publishableKey ?? "";
            tableName = string.IsNullOrWhiteSpace(settings.userSavesTable) ? "user_saves" : settings.userSavesTable.Trim();
        }

        private static HashSet<string> ParseSkipCsv(string csv)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(csv))
                return set;
            foreach (var part in csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var t = part.Trim();
                if (t.Length > 0)
                    set.Add(t);
            }

            return set;
        }

        private void FetchFromApi()
        {
            _lastWarnings.Clear();
            previewText = "";

            try
            {
                var key = useServiceRoleKey ? serviceRoleKey : anonKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    EditorUtility.DisplayDialog("Supabase POCO", "API key is empty.", "OK");
                    return;
                }

                var url = PostgrestOpenApiUserSavePoco.BuildRestRootUrl(projectUrl);
                var timeout = settings != null ? settings.timeoutSeconds : 30;
                var json = PostgrestOpenApiUserSavePoco.FetchOpenApiJson(url, key, timeout);
                BuildPreviewFromOpenApi(json);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Supabase POCO", "Fetch failed:\n" + e.Message, "OK");
            }
        }

        private void ImportJsonFile()
        {
            var path = EditorUtility.OpenFilePanel("OpenAPI JSON", "", "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                _lastWarnings.Clear();
                var json = File.ReadAllText(path, Encoding.UTF8);
                BuildPreviewFromOpenApi(json);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Supabase POCO", "Import failed:\n" + e.Message, "OK");
            }
        }

        private void BuildPreviewFromOpenApi(string openApiJson)
        {
            var cn = string.IsNullOrWhiteSpace(className) ? DefaultClassName : className.Trim();
            if (IsValidTypeName(cn) == false)
            {
                EditorUtility.DisplayDialog("Supabase POCO", "Class name is not a valid C# identifier.", "OK");
                return;
            }

            var skip = ParseSkipCsv(skipColumnsCsv);
            var parsed = PostgrestOpenApiUserSavePoco.ParseTableColumns(openApiJson, tableName, skip);
            if (!parsed.IsSuccess)
            {
                EditorUtility.DisplayDialog("Supabase POCO", parsed.ErrorMessage, "OK");
                return;
            }

            _lastWarnings = new List<string>(parsed.Warnings);

            if (parsed.Columns == null || parsed.Columns.Count == 0)
            {
                EditorUtility.DisplayDialog("Supabase POCO", "No columns to emit (모두 스킵되었거나 스키마가 비었습니다).", "OK");
                return;
            }

            previewText = PostgrestOpenApiUserSavePoco.GenerateSource(
                parsed.Columns,
                cn,
                namespaceName,
                tableName);

            Repaint();
        }

        private static bool IsValidTypeName(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            if (char.IsLetter(s[0]) == false && s[0] != '_')
                return false;
            for (var i = 1; i < s.Length; i++)
            {
                var c = s[i];
                if (char.IsLetterOrDigit(c) == false && c != '_')
                    return false;
            }

            return true;
        }

        private void SaveToProject()
        {
            var name = string.IsNullOrWhiteSpace(className) ? DefaultClassName : className.Trim();
            if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) == false)
                name += ".cs";

            var path = EditorUtility.SaveFilePanelInProject("Save user save POCO", name, "cs", "");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, previewText, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(path);
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset != null)
                    EditorGUIUtility.PingObject(asset);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Supabase POCO", e.Message, "OK");
            }
        }
    }
}
