using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인에서 사용하는 targetKey와 실제 <see cref="UIEffectTarget"/>을 연결하는 레지스트리입니다.
    /// </summary>
    public sealed class UIEffectTimelineTargetRegistry : MonoBehaviour, IUIEffectTimelineTargetResolver
    {
        [Serializable]
        public struct TargetEntry
        {
            /// <summary>
            /// Timeline Clip 또는 RuntimeSequence Payload에서 사용하는 대상 키입니다.
            /// </summary>
            public string targetKey;

            /// <summary>
            /// 실제 UI 효과 적용 대상입니다.
            /// </summary>
            public UIEffectTarget target;
        }

        [SerializeField] private List<TargetEntry> targets = new List<TargetEntry>();

        private readonly Dictionary<string, UIEffectTarget> _targetMap = new Dictionary<string, UIEffectTarget>();
        private bool _isDirty = true;

        private void Awake()
        {
            RebuildCache();
        }

        private void OnValidate()
        {
            _isDirty = true;
        }

        /// <summary>
        /// targetKey에 해당하는 UI 효과 대상을 조회합니다.
        /// </summary>
        /// <param name="targetKey">조회할 대상 키입니다.</param>
        /// <param name="target">조회된 UI 효과 대상입니다.</param>
        /// <returns>대상을 찾았으면 true입니다.</returns>
        public bool TryResolve(string targetKey, out UIEffectTarget target)
        {
            if (_isDirty)
            {
                RebuildCache();
            }

            if (string.IsNullOrWhiteSpace(targetKey))
            {
                target = null;
                return false;
            }

            return _targetMap.TryGetValue(targetKey, out target) && target != null;
        }

        /// <summary>
        /// 인스펙터에 등록된 대상 목록을 캐시 딕셔너리로 재구성합니다.
        /// </summary>
        public void RebuildCache()
        {
            _targetMap.Clear();
            foreach (TargetEntry entry in targets)
            {
                if (string.IsNullOrWhiteSpace(entry.targetKey) || entry.target == null)
                {
                    continue;
                }

                entry.target.AutoBind();
                _targetMap[entry.targetKey] = entry.target;
            }

            _isDirty = false;
        }
    }
}
