using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// Supabase 프로젝트 URL·키 및(선택) Android Google Web Client ID를 담는 에셋입니다.
    /// </summary>
    /// <remarks>
    /// 런타임에서는 <c>Resources/SupabaseSettings</c> 이름으로 로드되므로 경로·파일명을 맞춰야 합니다.
    /// <see cref="Supabase.SignInWithGoogleAsync(bool)"/> 무인자 호출 시 <see cref="googleWebClientId"/>를 읽습니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "SupabaseSettings", menuName = "TrueSoft/Supabase Settings")]
    public sealed class SupabaseSettings : ScriptableObject
    {
        [Tooltip("Supabase 프로젝트 URL (https://xxx.supabase.co 형태).")]
        public string projectUrl;

        [Tooltip("Supabase Publishable(anon) API 키.")]
        public string publishableKey;

        [Tooltip("Google Cloud OAuth 2.0 Web Client ID. Android 네이티브 Google 로그인(SignInWithGoogleAsync 무인자)에 사용합니다.")]
        public string googleWebClientId;

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