using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Unity.Config;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 유니티 전역에서 Supabase SDK에 쉽게 접근하기 위한 정적 진입점.
    /// 사용 예: Supabase.Auth.GetSessionAsync(), Supabase.UserSaves.LoadAsync&lt;T&gt;(session)
    /// </summary>
    public static class Supabase
    {
        /// <summary>SDK가 초기화되었는지 여부.</summary>
        public static bool IsInitialized => SupabaseSDK.IsInitialized;

        /// <summary>인증 서비스 (로그인, 세션, 로그아웃 등).</summary>
        public static SupabaseAuthService Auth => SupabaseSDK.Auth;

        /// <summary>유저 데이터 저장/불러오기 퍼사드.</summary>
        public static UserSavesFacade UserSaves => SupabaseSDK.UserSaves;

        /// <summary>이벤트 전송 퍼사드 (서버 권한 패턴용).</summary>
        public static UserEventsFacade Events => SupabaseSDK.Events;
    }
}
