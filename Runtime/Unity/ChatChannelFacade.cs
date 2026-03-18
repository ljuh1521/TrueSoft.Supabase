using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Truesoft.Supabase.Core.Auth;
using Truesoft.Supabase.Core.Data;
using UnityEngine;

namespace Truesoft.Supabase.Unity
{
    /// <summary>
    /// 동일 channel_id 유저끼리 대화. 수신은 짧은 폴링(실시간에 가깝게).
    /// WebSocket Realtime은 추후 확장 가능.
    /// </summary>
    public sealed class ChatChannelFacade
    {
        private readonly SupabaseChatService _chat;
        private readonly Func<SupabaseSession> _sessionGetter;
        private readonly string _channelId;
        private readonly string _displayName;

        private readonly HashSet<string> _seenIds = new HashSet<string>();
        private string _lastCreatedAtIso;
        private Coroutine _pollCoroutine;
        private MonoBehaviour _pollHost;

        public string ChannelId => _channelId;

        public event Action<SupabaseChatService.ChatMessageRow> OnMessageReceived;

        public ChatChannelFacade(
            SupabaseChatService chat,
            Func<SupabaseSession> sessionGetter,
            string channelId,
            string displayName = null)
        {
            _chat = chat ?? throw new ArgumentNullException(nameof(chat));
            _sessionGetter = sessionGetter;
            _channelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
            _displayName = displayName ?? string.Empty;
        }

        /// <summary>최근 히스토리 로드 후 오래된 순으로 OnMessageReceived 호출.</summary>
        public async Task<bool> LoadHistoryAsync(int count = 50)
        {
            var session = _sessionGetter?.Invoke();
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                return false;

            var result = await _chat.FetchRecentAsync(session.AccessToken, _channelId, count);
            if (result.IsSuccess == false || result.Data == null)
                return false;

            var list = new List<SupabaseChatService.ChatMessageRow>(result.Data);
            list.Sort((a, b) => string.CompareOrdinal(a?.created_at, b?.created_at));

            foreach (var row in list)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.id))
                    continue;
                if (_seenIds.Add(row.id) == false)
                    continue;
                _lastCreatedAtIso = MaxIso(_lastCreatedAtIso, row.created_at);
                OnMessageReceived?.Invoke(row);
            }

            return true;
        }

        public async Task<bool> SendAsync(string content)
        {
            var session = _sessionGetter?.Invoke();
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                return false;

            var userId = session.User?.Id;
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var name = string.IsNullOrWhiteSpace(_displayName) ? (session.User?.Email ?? userId) : _displayName;
            var result = await _chat.SendAsync(session.AccessToken, _channelId, userId, name, content);
            return result.IsSuccess;
        }

        /// <summary>MonoBehaviour에서 코루틴으로 폴링을 돌립니다.</summary>
        public void StartPolling(MonoBehaviour host, float intervalSeconds = 1.5f)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            StopPolling();
            _pollHost = host;
            _pollCoroutine = host.StartCoroutine(PollLoop(Mathf.Max(0.3f, intervalSeconds)));
        }

        public void StopPolling()
        {
            if (_pollHost != null && _pollCoroutine != null)
            {
                _pollHost.StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
                _pollHost = null;
            }
        }

        private IEnumerator PollLoop(float interval)
        {
            var init = EnsureWatermarkAsync();
            yield return new WaitUntil(() => init.IsCompleted);

            while (_pollHost != null)
            {
                var task = PollNewAsync();
                yield return new WaitUntil(() => task.IsCompleted);
                yield return new WaitForSeconds(interval);
            }
        }

        /// <summary>히스토리 없이 폴링만 할 때, 최신 1건 기준으로 워터마크 설정.</summary>
        private async Task EnsureWatermarkAsync()
        {
            if (string.IsNullOrWhiteSpace(_lastCreatedAtIso) == false)
                return;

            var session = _sessionGetter?.Invoke();
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                return;

            var result = await _chat.FetchRecentAsync(session.AccessToken, _channelId, 1);
            if (result.IsSuccess && result.Data != null && result.Data.Length > 0 && result.Data[0] != null
                && string.IsNullOrWhiteSpace(result.Data[0].created_at) == false)
                _lastCreatedAtIso = result.Data[0].created_at;
            else
                _lastCreatedAtIso = DateTime.UtcNow.ToString("o");
        }

        private async Task PollNewAsync()
        {
            var session = _sessionGetter?.Invoke();
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                return;

            if (string.IsNullOrWhiteSpace(_lastCreatedAtIso))
                return;

            var result = await _chat.FetchAfterAsync(session.AccessToken, _channelId, _lastCreatedAtIso);
            if (result.IsSuccess == false || result.Data == null)
                return;

            foreach (var row in result.Data)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.id))
                    continue;
                if (_seenIds.Add(row.id) == false)
                    continue;
                _lastCreatedAtIso = MaxIso(_lastCreatedAtIso, row.created_at);
                OnMessageReceived?.Invoke(row);
            }
        }

        private static string MaxIso(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(b))
                return a;
            if (string.IsNullOrWhiteSpace(a))
                return b;
            return string.CompareOrdinal(b, a) > 0 ? b : a;
        }
    }
}
