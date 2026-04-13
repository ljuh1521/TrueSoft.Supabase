using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Data;
using UnityEngine;

// ExampleSupabaseScenarios와 동일: Truesoft.Supabase 루트 네임스페이스와의 혼동·해석 오류(CS0234) 방지
using SupabaseClient = global::Truesoft.Supabase.Unity.Supabase;

namespace Truesoft.SupabaseUnity.Samples
{
    /// <summary>
    /// 우편함(Mailbox) 기능 테스트용 샘플.
    /// Package Manager에서 <b>Examples</b> 샘플을 Import한 뒤 씬에 붙여 사용합니다.
    /// </summary>
    /// <remarks>
    /// 테스트 데이터: 패키지 루트 <c>Sql/samples/MailboxTestData.sql</c> — Supabase SQL Editor에서 실행 후 <c>YOUR_ACCOUNT_UUID</c> / <c>YOUR_USER_ID</c> 교체.<br/>
    /// 가이드: 동일 폴더 <c>README-MailboxTest.md</c><br/>
    /// 우편 API가 포함된 패키지 버전을 쓰는지 확인하세요. 구버전 DLL만 있으면 Try* 메일 API가 없어 컴파일이 실패합니다.
    /// </remarks>
    public class MailboxTestSample : MonoBehaviour
    {
        [Header("테스트 설정")]
        [Tooltip("테스트할 메일 ID (수동 입력 또는 자동 할당)")]
        public string testMailId;

        [Tooltip(
            "Play 시 전체 우편함 테스트를 자동 실행합니다. 우편 API는 로그인(유효한 액세스 토큰)이 있어야 하므로, 세션이 없으면 경고만 남기고 건너뜁니다. " +
            "로그인/세션 복원은 다른 컴포넌트에서 먼저 수행하거나, 로그인 후 컨텍스트 메뉴 `Test: 전체 흐름`을 사용하세요.")]
        public bool autoFetchOnStart;

        private async void Start()
        {
            RegisterMailHandlers();

            if (!autoFetchOnStart)
                return;

            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] Auto Fetch On Start를 건너뜁니다. " + reason);
                return;
            }

            await TestMailboxFlow();
        }

        /// <summary>우편함 API와 동일한 최소 조건: 세션 + 액세스 토큰.</summary>
        private static bool HasMailboxAuth(out string skipReason)
        {
            var s = SupabaseClient.Session;
            if (s == null || string.IsNullOrWhiteSpace(s.AccessToken))
            {
                skipReason =
                    "우편함은 로그인 세션이 필요합니다. `Supabase.TryStartAsync` 등으로 세션을 복원·로그인한 뒤 다시 실행하거나, " +
                    "Inspector의 Auto Fetch On Start를 끈 채 로그인 후 컨텍스트 메뉴 `Test: 전체 흐름`을 쓰세요.";
                return false;
            }

            skipReason = null;
            return true;
        }

        private void RegisterMailHandlers()
        {
            MailItemHandlerRegistry.Register(new GoldMailHandler());
            MailItemHandlerRegistry.Register(new GemMailHandler());
            MailItemHandlerRegistry.Register(new WeaponMailHandler());
            MailItemHandlerRegistry.Register(new ConsumableMailHandler());
            MailItemHandlerRegistry.Register(new TestItemHandler());

            Debug.Log("[Mailbox] 핸들러 등록 완료: Gold, Gem, Weapon, Consumable, TestItem");
        }

        private async Task TestMailboxFlow()
        {
            Debug.Log("===== 우편함 테스트 시작 =====");

            await TestInboxCounts();

            var mails = await TestGetMailList();
            if (mails == null || mails.Count == 0)
            {
                Debug.LogWarning("[Mailbox] 테스트할 우편이 없습니다. 패키지 Sql/samples/MailboxTestData.sql 을 SQL Editor에서 실행하세요.");
                return;
            }

            var firstMail = mails[0];
            testMailId = firstMail.Id;
            await TestGetMailDetail(testMailId);

            if (firstMail.HasUnclaimedItems)
            {
                await TestClaimSingleMail(testMailId);
            }
            else
            {
                Debug.Log($"[Mailbox] 메일 '{firstMail.Title}'은 보상이 없거나 이미 수령됨");
            }

            await TestClaimAllMails();

            if (firstMail.CanDeleteFromInbox)
            {
                await TestDeleteMail(testMailId);
            }

            Debug.Log("===== 우편함 테스트 완료 =====");
        }

        #region 테스트 메서드

        private async Task TestInboxCounts()
        {
            Debug.Log("[Test] 미읽음/미수령 카운트 조회...");

            var unreadCount = await SupabaseClient.TryGetUnreadMailCountAsync();
            var unclaimedCount = await SupabaseClient.TryGetUnclaimedItemMailCountAsync();

            Debug.Log($"[Mailbox] 미읽음: {unreadCount ?? 0}개, 미수령 보상: {unclaimedCount ?? 0}개");
        }

        private async Task<IReadOnlyList<Mail>> TestGetMailList()
        {
            Debug.Log("[Test] 우편함 목록 조회...");

            var mails = await SupabaseClient.TryGetMyMailsAsync(limit: 10);

            if (mails != null)
            {
                Debug.Log($"[Mailbox] 우편 {mails.Count}개 조회됨:");
                foreach (var mail in mails)
                {
                    var status = mail.IsExpired ? "[만료]" :
                        mail.HasUnclaimedItems ? "[미수령]" :
                        "[수령완료/없음]";
                    Debug.Log($"  - {status} {mail.Title} (발신: {mail.SenderName}, 만료: {mail.ExpiresAt:yyyy-MM-dd})");
                }
            }

            return mails;
        }

        private async Task TestGetMailDetail(string mailId)
        {
            Debug.Log($"[Test] 메일 상세 조회: {mailId}");

            var mail = await SupabaseClient.TryGetMailDetailAsync(mailId);

            if (mail != null)
            {
                Debug.Log("[Mailbox] 상세 정보:");
                Debug.Log($"  제목: {mail.Title}");
                Debug.Log($"  내용: {mail.Content}");
                Debug.Log($"  발신자: {mail.SenderName} ({mail.SenderType})");
                Debug.Log($"  읽음: {mail.IsRead}");
                Debug.Log($"  만료일: {mail.ExpiresAt}");
                Debug.Log($"  보상 수령 시각: {mail.ItemsClaimedAt?.ToString() ?? "미수령"}");
                Debug.Log($"  보상 개수: {mail.Items?.Count ?? 0}");

                if (mail.Items != null)
                {
                    foreach (var item in mail.Items)
                    {
                        Debug.Log($"    - {item.Key}: {item.Count}개");
                    }
                }
            }
        }

        private async Task TestClaimSingleMail(string mailId)
        {
            Debug.Log($"[Test] 단일 메일 수령: {mailId}");

            var results = await SupabaseClient.TryClaimMailItemsAsync(mailId);

            if (results != null)
            {
                Debug.Log($"[Mailbox] 수령 완료! {results.Count}개 아이템:");
                foreach (var result in results)
                {
                    Debug.Log($"  - [{result.ItemIndex}] {result.ItemKey}: {result.Count}개");
                }
            }
            else
            {
                Debug.LogError("[Mailbox] 수령 실패 (이미 수령했거나 만료됨)");
            }
        }

        private async Task TestClaimAllMails()
        {
            Debug.Log("[Test] 전체 일괄 수령...");

            var results = await SupabaseClient.TryClaimAllMailItemsAsync();

            if (results != null && results.Count > 0)
            {
                Debug.Log($"[Mailbox] 일괄 수령 완료! 총 {results.Count}개 아이템");

                var groupedByMail = new Dictionary<string, List<ClaimResult>>();
                foreach (var r in results)
                {
                    if (!groupedByMail.ContainsKey(r.MailId))
                        groupedByMail[r.MailId] = new List<ClaimResult>();
                    groupedByMail[r.MailId].Add(r);
                }

                foreach (var kvp in groupedByMail)
                {
                    Debug.Log($"  메일 {kvp.Key.Substring(0, 8)}...: {kvp.Value.Count}개 아이템");
                }
            }
            else
            {
                Debug.Log("[Mailbox] 수령할 보상이 없습니다.");
            }
        }

        private async Task TestDeleteMail(string mailId)
        {
            Debug.Log($"[Test] 메일 삭제: {mailId}");

            var success = await SupabaseClient.TryDeleteMailAsync(mailId);

            if (success)
            {
                Debug.Log("[Mailbox] 삭제 완료");
            }
            else
            {
                Debug.LogError("[Mailbox] 삭제 실패 (미수령 보상이 있거나 이미 삭제됨)");
            }
        }

        private async Task TestDeleteReadMailsBulk()
        {
            Debug.Log("[Test] 읽은 메일 일괄 삭제...");

            var n = await SupabaseClient.TryDeleteReadMailsAsync();
            if (n == null)
                Debug.LogError("[Mailbox] 읽은 메일 일괄 삭제 실패");
            else
                Debug.Log($"[Mailbox] 읽은 메일 일괄 삭제 완료: {n}건 숨김");
        }

        #endregion

        #region UI 버튼용 공개 메서드

        [ContextMenu("Test: 전체 흐름")]
        public void RunFullMailboxTestFromContextMenu()
        {
            RegisterMailHandlers();
            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] " + reason);
                return;
            }

            _ = TestMailboxFlow();
        }

        [ContextMenu("Test: 목록 조회")]
        public void TestListOnly()
        {
            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] " + reason);
                return;
            }

            _ = TestGetMailList();
        }

        [ContextMenu("Test: 수령만 실행")]
        public void TestClaimOnly()
        {
            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] " + reason);
                return;
            }

            if (!string.IsNullOrEmpty(testMailId))
                _ = TestClaimSingleMail(testMailId);
        }

        [ContextMenu("Test: 일괄 수령")]
        public void TestClaimAll()
        {
            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] " + reason);
                return;
            }

            _ = TestClaimAllMails();
        }

        [ContextMenu("Test: 읽은 메일 일괄 삭제")]
        public void TestDeleteReadMailsFromMenu()
        {
            if (!HasMailboxAuth(out var reason))
            {
                Debug.LogWarning("[Mailbox] " + reason);
                return;
            }

            _ = TestDeleteReadMailsBulk();
        }

        #endregion
    }

    #region 메일 아이템 핸들러 구현 예시

    public class GoldMailHandler : IMailItemHandler
    {
        public string ItemKey => "gold";

        public Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 골드 {count} 획득!");
            return Task.FromResult(Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult
                {
                    MailId = mailId,
                    ItemIndex = itemIndex,
                    ItemKey = itemKey,
                    Count = count
                }));
        }
    }

    public class GemMailHandler : IMailItemHandler
    {
        public string ItemKey => "gem";

        public Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 보석 {count}개 획득!");
            return Task.FromResult(Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }));
        }
    }

    public class WeaponMailHandler : IMailItemHandler
    {
        public string ItemKey => "weapon_001";

        public Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 무기 {itemKey} x{count} 획득!");
            return Task.FromResult(Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }));
        }
    }

    public class ConsumableMailHandler : IMailItemHandler
    {
        public string ItemKey => "potion_hp";

        public Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] HP 포션 {count}개 획득!");
            return Task.FromResult(Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }));
        }
    }

    public class TestItemHandler : IMailItemHandler
    {
        public string ItemKey => "test_item";

        public Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 테스트 아이템 x{count} 획득!");
            return Task.FromResult(Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }));
        }
    }

    #endregion
}
