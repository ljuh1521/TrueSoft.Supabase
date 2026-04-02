using System;
using System.Threading.Tasks;
using UnityEngine;
using Truesoft.Supabase.Core.Data;
using SupabaseSdk = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// OpenAPI 유저 데이터 생성기가 출력하는 <c>public static class</c> + 내부 Row 패턴의 수동 예시입니다.
    /// 컬럼 구성은 <see cref="ExampleSupabaseScenarios"/> 의 세이브 데모와 동일하게 맞춰 두었습니다.
    /// </summary>
    public static class SampleStaticUserSave
    {
        private const string SyncKey = "Truesoft.SupabaseUnity.Samples.SampleStaticUserSave";
        private static readonly SampleStaticUserSaveRow Current = new SampleStaticUserSaveRow();
        private static SampleStaticUserSaveRow LastSynced = new SampleStaticUserSaveRow();
        private static bool IsDirty;
        private static bool IsRegistered;

        static SampleStaticUserSave()
        {
            EnsureRegistered();
        }

        private static void EnsureRegistered()
        {
            if (IsRegistered)
                return;

            SupabaseSdk.RegisterUserSaveStaticSync(SyncKey, HasDirty, FlushDirtyAsync, ResetLocalState);
            IsRegistered = true;
        }

        public static void ConfigureCooldown(float seconds)
        {
            SupabaseSdk.ConfigureUserSaveAutoSyncCooldown(seconds);
        }

        public static bool TryRequestImmediateSave()
        {
            EnsureRegistered();
            return SupabaseSdk.RequestImmediateUserSaveStaticFlush(SyncKey);
        }

        public static Task<bool> TryFlushNowAsync(int timeoutMs = 5000)
        {
            EnsureRegistered();
            return SupabaseSdk.TryFlushUserSaveImmediateAsync(SyncKey, timeoutMs);
        }

        public static async Task<bool> TryLoadAsync(bool includeUpdatedAt = true)
        {
            EnsureRegistered();
            var loaded = await SupabaseSdk.TryLoadUserSaveAttributedAsync<SampleStaticUserSaveRow>(
                defaultValue: null,
                includeUpdatedAt: includeUpdatedAt);
            if (loaded == null)
                return false;

            CopyInto(Current, loaded);
            LastSynced = CloneRow(loaded);
            IsDirty = false;
            return true;
        }

        public static int Level
        {
            get => Current.level;
            set
            {
                if (Equals(Current.level, value))
                    return;
                Current.level = value;
                MarkDirty();
            }
        }

        public static int Coins
        {
            get => Current.coins;
            set
            {
                if (Equals(Current.coins, value))
                    return;
                Current.coins = value;
                MarkDirty();
            }
        }

        public static string UpdatedAt
        {
            get => Current.updated_at;
            set
            {
                if (Equals(Current.updated_at, value))
                    return;
                Current.updated_at = value;
                MarkDirty();
            }
        }

        private static void MarkDirty()
        {
            EnsureRegistered();
            IsDirty = true;
            SupabaseSdk.MarkUserSaveStaticDirty(SyncKey);
        }

        private static bool HasDirty() => IsDirty;

        private static async Task<bool> FlushDirtyAsync()
        {
            if (!IsDirty)
                return true;

            var ok = await SupabaseSdk.TryPatchUserSaveDiffAsync(
                LastSynced,
                Current,
                ensureRowFirst: true,
                setUpdatedAtIsoUtc: true);
            if (!ok)
                return false;

            LastSynced = CloneRow(Current);
            IsDirty = false;
            return true;
        }

        private static void ResetLocalState()
        {
            CopyInto(Current, new SampleStaticUserSaveRow());
            LastSynced = new SampleStaticUserSaveRow();
            IsDirty = false;
        }

        private static SampleStaticUserSaveRow CloneRow(SampleStaticUserSaveRow src)
        {
            if (src == null)
                return new SampleStaticUserSaveRow();

            var copy = new SampleStaticUserSaveRow();
            copy.level = src.level;
            copy.coins = src.coins;
            copy.updated_at = src.updated_at;
            return copy;
        }

        private static void CopyInto(SampleStaticUserSaveRow dst, SampleStaticUserSaveRow src)
        {
            if (dst == null || src == null)
                return;
            dst.level = src.level;
            dst.coins = src.coins;
            dst.updated_at = src.updated_at;
        }

        [Serializable]
        private sealed class SampleStaticUserSaveRow
        {
            [UserSaveColumn("level")] public int level;
            [UserSaveColumn("coins")] public int coins;
            [UserSaveColumn("updated_at")] public string updated_at;
        }
    }
}
