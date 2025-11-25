using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public static class SaveRegistry
    {
        private static readonly List<ISaveContributor> List = new();
        private static SaveEnvelope _pendingRestore;

        public static event Action<ISaveContributor> Registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInitialized()
        {
            List.Clear();
            _pendingRestore = null;
            Registered = null;
        }

        public static void Register(ISaveContributor contributor)
        {
            if (contributor == null || List.Contains(contributor)) return;
            List.Add(contributor);
            List.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            // 등록 이벤트 알림
            Registered?.Invoke(contributor);

            // 로드시점에 envelope이 있었다면 늦게 온 기여자에게도 즉시 Restore
            if (_pendingRestore != null)
                contributor.Restore(_pendingRestore);
        }

        public static IReadOnlyList<ISaveContributor> All => List;

        /// <summary>로드 직후 SaveDataManager에서 호출. 현재 등록분엔 즉시 Restore, 이후 등록분엔 자동 적용.</summary>
        public static void ApplyRestore(SaveEnvelope env)
        {
            _pendingRestore = env;
            var list = All;
            for (int i = 0; i < list.Count; i++)
                list[i].Restore(env);
        }
        public static void Unregister(ISaveContributor contributor)
        {
            if (contributor == null) return;
            List.Remove(contributor);
        }
        /// <summary>새 게임 시작 등으로 보류 상태 초기화가 필요할 때 호출.</summary>
        public static void ClearPendingRestore() => _pendingRestore = null;

        public static void Clear()
        {
            List.Clear();
            _pendingRestore = null;
            Registered = null;
        }
    }
}