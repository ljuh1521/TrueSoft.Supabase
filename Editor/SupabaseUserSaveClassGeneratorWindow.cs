using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Truesoft.Supabase.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Truesoft.Supabase.Editor
{
    /// <summary>
    /// PostgREST OpenAPI로 세이브 테이블 스키마를 가져와 <c>[Serializable]</c> + <c>[UserSaveColumn]</c> C# 클래스 소스를 생성합니다.
    /// </summary>
    public sealed class SupabaseUserSaveClassGeneratorWindow : EditorWindow
    {
        private const string DefaultClassName = "UserSaveRow";
        private const string DialogTitle = "유저 데이터 클래스";

        [SerializeField] private SupabaseSettings settings;
        [SerializeField] private string projectUrl = "";
        [FormerlySerializedAs("serviceRoleKey")]
        [SerializeField] private string secretKeyInput = "";
        [SerializeField] private string tableName = "user_saves";
        [SerializeField] private string skipColumnsCsv = "";
        [SerializeField] private string className = DefaultClassName;
        [SerializeField] private string namespaceName = "";
        [SerializeField] private string previewText = "";
        [SerializeField] private Vector2 scroll;

        private List<string> _lastWarnings = new List<string>();

        [MenuItem("TrueSoft/Supabase/유저 데이터 클래스 생성")]
        private static void Open()
        {
            var w = GetWindow<SupabaseUserSaveClassGeneratorWindow>(true, "유저 데이터 클래스 생성", true);
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
                "OpenAPI로 세이브 테이블 C# 초안을 만듭니다. API 가져오기는 Secret 키·에디터 전용, JSON은 키 없음.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            settings = (SupabaseSettings)EditorGUILayout.ObjectField("설정", settings, typeof(SupabaseSettings), false);
            if (EditorGUI.EndChangeCheck() && settings != null)
                PullFromSettings();

            projectUrl = EditorGUILayout.TextField("프로젝트 URL", projectUrl);
            secretKeyInput = EditorGUILayout.PasswordField("Secret 키", secretKeyInput);

            tableName = EditorGUILayout.TextField("테이블 이름", tableName);
            skipColumnsCsv = EditorGUILayout.TextField("제외 컬럼", skipColumnsCsv);
            className = EditorGUILayout.TextField("클래스 이름", className);
            namespaceName = EditorGUILayout.TextField("네임스페이스", namespaceName);

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(secretKeyInput)))
                {
                    if (GUILayout.Button("API에서 가져와 미리보기", GUILayout.Height(28)))
                        FetchFromApi();
                }

                if (GUILayout.Button("OpenAPI JSON 가져오기…", GUILayout.Height(28)))
                    ImportJsonFile();
            }

            foreach (var w in _lastWarnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);

            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            var ro = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.TextArea(string.IsNullOrEmpty(previewText) ? "(비어 있음)" : previewText, GUILayout.MinHeight(220));
            GUI.enabled = ro;
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(previewText)))
            {
                if (GUILayout.Button("프로젝트에 .cs 저장…", GUILayout.Height(26)))
                    SaveToProject();
            }
        }

        private void PullFromSettings()
        {
            projectUrl = settings.projectUrl ?? "";
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
                var key = secretKeyInput;
                if (string.IsNullOrWhiteSpace(key))
                {
                    EditorUtility.DisplayDialog(DialogTitle, "Secret 키를 입력하세요.", "확인");
                    return;
                }

                var url = PostgrestOpenApiUserSaveClass.BuildRestRootUrl(projectUrl);
                var timeout = settings != null ? settings.timeoutSeconds : 30;
                var json = PostgrestOpenApiUserSaveClass.FetchOpenApiJson(url, key, timeout);
                BuildPreviewFromOpenApi(json);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(DialogTitle, "가져오기에 실패했습니다.\n" + e.Message, "확인");
            }
        }

        private void ImportJsonFile()
        {
            var path = EditorUtility.OpenFilePanel("OpenAPI JSON 열기", "", "json");
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
                EditorUtility.DisplayDialog(DialogTitle, "가져오기에 실패했습니다.\n" + e.Message, "확인");
            }
        }

        private void BuildPreviewFromOpenApi(string openApiJson)
        {
            var cn = string.IsNullOrWhiteSpace(className) ? DefaultClassName : className.Trim();
            if (IsValidTypeName(cn) == false)
            {
                EditorUtility.DisplayDialog(DialogTitle, "클래스 이름이 C# 식별자 규칙에 맞지 않습니다.", "확인");
                return;
            }

            var skip = ParseSkipCsv(skipColumnsCsv);
            var parsed = PostgrestOpenApiUserSaveClass.ParseTableColumns(openApiJson, tableName, skip);
            if (!parsed.IsSuccess)
            {
                EditorUtility.DisplayDialog(DialogTitle, parsed.ErrorMessage, "확인");
                return;
            }

            _lastWarnings = new List<string>(parsed.Warnings);

            if (parsed.Columns == null || parsed.Columns.Count == 0)
            {
                EditorUtility.DisplayDialog(DialogTitle, "생성할 컬럼이 없습니다. 제외 목록·스키마를 확인하세요.", "확인");
                return;
            }

            previewText = PostgrestOpenApiUserSaveClass.GenerateSource(
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

            var path = EditorUtility.SaveFilePanelInProject("유저 데이터 클래스 저장", name, "cs", "");
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
                EditorUtility.DisplayDialog(DialogTitle, e.Message, "확인");
            }
        }
    }
}
