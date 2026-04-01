using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 씬/샘플에서 SDK가 초기화되지 않을 때 동일한 점검 안내를 출력하기 위한 헬퍼입니다.
    /// </summary>
    public static class SupabaseUnitySetupHelp
    {
        /// <summary>콘솔에 그대로 붙여 넣을 수 있는 최소 체크리스트(한국어).</summary>
        public const string InitializationChecklistKo =
            "[Supabase 초기화 점검]\n" +
            "1) 메뉴 TrueSoft > Supabase > 설정 에셋 만들기 로 SupabaseSettings 생성\n" +
            "2) projectUrl, publishableKey 입력\n" +
            "3) 에셋을 Assets/Resources/SupabaseSettings.asset 경로·파일명으로 저장 (Resources.Load 이름은 'SupabaseSettings')\n" +
            "4) 씬에 SupabaseRuntime이 없으면 샘플이 자동 생성합니다. 수동 배치: TrueSoft > Supabase > 씬에 런타임 오브젝트 만들기";

        /// <summary>
        /// <paramref name="context"/>는 로그 태그로 사용됩니다 (예: BasicSetup, FullSDKUsage).
        /// </summary>
        public static void LogInitializationTimeout(string context)
        {
            Debug.LogError(
                $"[{context}] {timeoutMessageKo}\n{InitializationChecklistKo}");
        }

        private const string timeoutMessageKo =
            "SDK 초기화가 제한 시간 내에 완료되지 않았습니다. 아래를 순서대로 확인하세요.";
    }
}
