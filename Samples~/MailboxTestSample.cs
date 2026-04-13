using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Data;
using Truesoft.Supabase.Unity;
using UnityEngine;

namespace Truesoft.Supabase.Samples
{
    /// <summary>
    /// 우편함(Mailbox) 기능 테스트용 샘플 클래스.
    /// 게임 시작 시 핸들러 등록 후 각종 우편 테스트를 수행합니다.
    /// </summary>
    public class MailboxTestSample : MonoBehaviour
    {
        [Header("테스트 설정")]
        [Tooltip("테스트할 메일 ID (수동 입력 또는 자동 할당)")]
        public string testMailId;

        [Tooltip("자동으로 목록을 가져와서 테스트할지 여부")]
        public bool autoFetchOnStart = true;

        private async void Start()
        {
            // 1. 핸들러 등록 (게임 시작 시 한 번만)
            RegisterMailHandlers();

            if (autoFetchOnStart)
            {
                await TestMailboxFlow();
            }
        }

        /// <summary>
        /// 메일 아이템 핸들러 등록
        /// 게임 시작 시 한 번만 호출하세요.
        /// </summary>
        private void RegisterMailHandlers()
        {
            // 골드 핸들러
            MailItemHandlerRegistry.Register(new GoldMailHandler());
            
            // 보석(프리미엄 재화) 핸들러
            MailItemHandlerRegistry.Register(new GemMailHandler());
            
            // 무기 아이템 핸들러
            MailItemHandlerRegistry.Register(new WeaponMailHandler());
            
            // 포션/소모품 핸들러
            MailItemHandlerRegistry.Register(new ConsumableMailHandler());
            
            // 테스트용 아이템 핸들러
            MailItemHandlerRegistry.Register(new TestItemHandler());

            Debug.Log("[Mailbox] 핸들러 등록 완료: Gold, Gem, Weapon, Consumable, TestItem");
        }

        /// <summary>
        /// 전체 우편 테스트 플로우
        /// </summary>
        private async Task TestMailboxFlow()
        {
            Debug.Log("===== 우편함 테스트 시작 =====");

            // 1. 미수령 카운트 확인
            await TestInboxCounts();

            // 2. 우편함 목록 조회
            var mails = await TestGetMailList();
            if (mails == null || mails.Count == 0)
            {
                Debug.LogWarning("[Mailbox] 테스트할 우편이 없습니다. SQL Editor에서 먼저 테스트 우편을 발송해주세요.");
                return;
            }

            // 3. 첫 번째 메일 상세 조회
            var firstMail = mails[0];
            testMailId = firstMail.Id;
            await TestGetMailDetail(testMailId);

            // 4. 단일 메일 수령 테스트 (미수령 보상이 있는 경우)
            if (firstMail.HasUnclaimedItems)
            {
                await TestClaimSingleMail(testMailId);
            }
            else
            {
                Debug.Log($"[Mailbox] 메일 '{firstMail.Title}'은 보상이 없거나 이미 수령됨");
            }

            // 5. 전체 일괄 수령 테스트
            await TestClaimAllMails();

            // 6. 읽음 처리 테스트
            await TestMarkAsRead(testMailId);

            // 7. 삭제 테스트 (보상 없는 메일만 가능)
            if (firstMail.CanDeleteFromInbox)
            {
                await TestDeleteMail(testMailId);
            }

            Debug.Log("===== 우편함 테스트 완료 =====");
        }

        #region 테스트 메서드

        /// <summary>
        /// 우편함 카운트 테스트
        /// </summary>
        private async Task TestInboxCounts()
        {
            Debug.Log("[Test] 미읽음/미수령 카운트 조회...");
            
            var unreadCount = await Supabase.TryGetUnreadMailCountAsync();
            var unclaimedCount = await Supabase.TryGetUnclaimedItemMailCountAsync();
            
            Debug.Log($"[Mailbox] 미읽음: {unreadCount ?? 0}개, 미수령 보상: {unclaimedCount ?? 0}개");
        }

        /// <summary>
        /// 우편함 목록 조회 테스트
        /// </summary>
        private async Task<IReadOnlyList<Mail>> TestGetMailList()
        {
            Debug.Log("[Test] 우편함 목록 조회...");
            
            var mails = await Supabase.TryGetMyMailsAsync(limit: 10);
            
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

        /// <summary>
        /// 메일 상세 조회 테스트
        /// </summary>
        private async Task TestGetMailDetail(string mailId)
        {
            Debug.Log($"[Test] 메일 상세 조회: {mailId}");
            
            var mail = await Supabase.TryGetMailDetailAsync(mailId);
            
            if (mail != null)
            {
                Debug.Log($"[Mailbox] 상세 정보:");
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

        /// <summary>
        /// 단일 메일 수령 테스트
        /// </summary>
        private async Task TestClaimSingleMail(string mailId)
        {
            Debug.Log($"[Test] 단일 메일 수령: {mailId}");
            
            var results = await Supabase.TryClaimMailItemsAsync(mailId);
            
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

        /// <summary>
        /// 전체 일괄 수령 테스트
        /// </summary>
        private async Task TestClaimAllMails()
        {
            Debug.Log("[Test] 전체 일괄 수령...");
            
            var results = await Supabase.TryClaimAllMailItemsAsync();
            
            if (results != null && results.Count > 0)
            {
                Debug.Log($"[Mailbox] 일괄 수령 완료! 총 {results.Count}개 아이템");
                
                // 메일별로 그룹화하여 출력
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

        /// <summary>
        /// 읽음 처리 테스트
        /// </summary>
        private async Task TestMarkAsRead(string mailId)
        {
            Debug.Log($"[Test] 읽음 처리: {mailId}");
            
            var success = await Supabase.TryMarkMailAsReadAsync(mailId);
            
            if (success)
            {
                Debug.Log("[Mailbox] 읽음 처리 완료");
            }
            else
            {
                Debug.LogError("[Mailbox] 읽음 처리 실패");
            }
        }

        /// <summary>
        /// 삭제 테스트
        /// </summary>
        private async Task TestDeleteMail(string mailId)
        {
            Debug.Log($"[Test] 메일 삭제: {mailId}");
            
            var success = await Supabase.TryDeleteMailAsync(mailId);
            
            if (success)
            {
                Debug.Log("[Mailbox] 삭제 완료");
            }
            else
            {
                Debug.LogError("[Mailbox] 삭제 실패 (미수령 보상이 있거나 이미 삭제됨)");
            }
        }

        #endregion

        #region UI 버튼용 공개 메서드

        [ContextMenu("Test: 목록 조회")]
        public void TestListOnly() => _ = TestGetMailList();

        [ContextMenu("Test: 수령만 실행")]
        public void TestClaimOnly() 
        {
            if (!string.IsNullOrEmpty(testMailId))
                _ = TestClaimSingleMail(testMailId);
        }

        [ContextMenu("Test: 일괄 수령")]
        public void TestClaimAll() => _ = TestClaimAllMails();

        #endregion
    }

    #region 메일 아이템 핸들러 구현 예시

    /// <summary>
    /// 골드 수령 핸들러
    /// </summary>
    public class GoldMailHandler : IMailItemHandler
    {
        public string ItemKey => "gold";

        public async Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 골드 {count} 획득!");
            
            // TODO: 실제 게임 재화 지급 로직
            // await GameManager.Instance.AddGoldAsync(count);
            
            return Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult 
                { 
                    MailId = mailId, 
                    ItemIndex = itemIndex, 
                    ItemKey = itemKey, 
                    Count = count 
                }
            );
        }
    }

    /// <summary>
    /// 보석(프리미엄 재화) 수령 핸들러
    /// </summary>
    public class GemMailHandler : IMailItemHandler
    {
        public string ItemKey => "gem";

        public async Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 보석 {count}개 획득!");
            
            // TODO: 실제 보석 지급 로직
            
            return Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }
            );
        }
    }

    /// <summary>
    /// 무기 아이템 수령 핸들러
    /// </summary>
    public class WeaponMailHandler : IMailItemHandler
    {
        public string ItemKey => "weapon_001";

        public async Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 무기 {itemKey} x{count} 획득!");
            
            // TODO: 인벤토리에 아이템 추가
            
            return Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }
            );
        }
    }

    /// <summary>
    /// 소모품(포션 등) 수령 핸들러
    /// </summary>
    public class ConsumableMailHandler : IMailItemHandler
    {
        public string ItemKey => "potion_hp";

        public async Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] HP 포션 {count}개 획득!");
            
            // TODO: 인벤토리에 포션 추가
            
            return Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }
            );
        }
    }

    /// <summary>
    /// 테스트용 아이템 핸들러
    /// </summary>
    public class TestItemHandler : IMailItemHandler
    {
        public string ItemKey => "test_item";

        public async Task<Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>> HandleAsync(
            string mailId, int itemIndex, string itemKey, int count)
        {
            Debug.Log($"[MailHandler] 테스트 아이템 x{count} 획득!");
            
            return Truesoft.Supabase.Core.Common.SupabaseResult<ClaimResult>.Success(
                new ClaimResult { MailId = mailId, ItemIndex = itemIndex, ItemKey = itemKey, Count = count }
            );
        }
    }

    #endregion
}
