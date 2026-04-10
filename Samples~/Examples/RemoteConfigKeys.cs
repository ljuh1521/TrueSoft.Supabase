namespace Truesoft.Supabase.Unity.RemoteConfig
{
    /// <summary>
    /// Remote Config 키 상수 모음 샘플.
    /// 프로젝트에 맞게 수정하여 사용하세요.
    /// </summary>
    public static class RemoteConfigKeys
    {
        // 게임 설정 예시
        public const string MaxPlayers = "game.max_players";
        public const string MatchTimeoutSeconds = "game.match_timeout_seconds";
        public const string MaintenanceMode = "game.maintenance_mode";

        // 밸런스 조정 예시
        public const string PlayerSpeedMultiplier = "balance.player_speed_multiplier";
        public const string EnemyHealthMultiplier = "balance.enemy_health_multiplier";

        // A/B 테스트 예시
        public const string ExperimentVariant = "experiment.new_ui_variant";
    }
}
