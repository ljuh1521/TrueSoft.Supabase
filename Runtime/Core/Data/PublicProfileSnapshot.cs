namespace Truesoft.Supabase.Core.Data
{
    /// <summary>
    /// 공개 프로필 조회 결과. <c>profiles</c> 테이블의 <c>id</c>, <c>nickname</c>, <c>withdrawn_at</c>를 가정합니다.
    /// </summary>
    public sealed class PublicProfileSnapshot
    {
        public PublicProfileSnapshot(string userId, string nickname, string withdrawnAtIso)
        {
            UserId = userId ?? string.Empty;
            Nickname = nickname ?? string.Empty;
            WithdrawnAtIso = withdrawnAtIso;
        }

        public string UserId { get; }
        public string Nickname { get; }

        /// <summary>ISO 8601 문자열. null이거나 빈 문자열이면 탈퇴(비활성) 처리 전제가 아님.</summary>
        public string WithdrawnAtIso { get; }

        public bool IsWithdrawn => string.IsNullOrWhiteSpace(WithdrawnAtIso) == false;
    }
}
