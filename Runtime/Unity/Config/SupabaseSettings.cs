using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// Supabase 프로젝트의 "공통 설정값"을 담는 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 이 에셋은 프로젝트 전역에서 재사용되는 정적 값(서버 주소, 키, 기본 옵션)만 정의합니다.
    /// 씬 실행 정책(자동 복원, 폴링 주기 등)은 <see cref="Config.SupabaseRuntime"/>에서 제어합니다.
    /// 런타임에서는 <c>Resources/SupabaseSettings</c> 이름으로 로드되므로 경로·파일명을 맞춰야 합니다.
    /// <see cref="Supabase.TrySignInWithGoogleAsync(bool)"/> 호출 시 <see cref="googleWebClientId"/>를 읽습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "TrueSoft/Supabase Settings")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        [Header("Project Values (공통 설정값)")]
        [Tooltip("Supabase 프로젝트 URL (https://xxx.supabase.co 형태).")]
        public string projectUrl;

        [Tooltip("Supabase Publishable(anon) API 키.")]
        public string publishableKey;

        [Tooltip("Google Cloud OAuth 2.0 Web Client ID. Android 네이티브 Google 로그인(SignInWithGoogleAsync 무인자)에 사용합니다.")]
        public string googleWebClientId;

        [Header("Default SDK Options (기본 동작값)")]
        [Tooltip("Try API 결과 로그 출력 여부. 켜면 API별 고정 태그(예: Supabase.UserData.Save)로 성공/실패가 Console에 출력됩니다.")]
        public bool enableApiResultLogs = true;

        [Tooltip("HTTP 요청 타임아웃(초).")]
        public int timeoutSeconds = 30;

        public SupabaseOptions ToOptions()
        {
            return new SupabaseOptions
            {
                ProjectURL = projectUrl,
                PublishableKey = publishableKey,
                TimeoutSeconds = timeoutSeconds
            };
        }
    }
}