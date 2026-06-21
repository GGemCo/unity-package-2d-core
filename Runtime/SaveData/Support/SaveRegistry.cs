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

        /// <summary>
        /// 저장 파일 로드 직후 현재 등록된 기여자에게 복원 데이터를 전달합니다.
        /// 여러 패키지 저장 파일이 순차적으로 로드될 수 있으므로 보류 봉투는 섹션 단위로 병합합니다.
        /// 이후 등록되는 기여자에게도 병합된 전체 봉투가 자동 적용됩니다.
        /// </summary>
        /// <param name="env">이번 저장 파일에서 복원한 확장 섹션 봉투입니다.</param>
        public static void ApplyRestore(SaveEnvelope env)
        {
            if (env == null)
            {
                return;
            }

            _pendingRestore ??= new SaveEnvelope();
            foreach (KeyValuePair<string, Newtonsoft.Json.Linq.JToken> section in env.Sections)
            {
                _pendingRestore.Sections[section.Key] = section.Value;
            }

            var list = All;
            for (int i = 0; i < list.Count; i++)
                list[i].Restore(_pendingRestore);
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
