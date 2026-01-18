using System.Collections.Generic;
using GGemCo2DAffect;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어의 <see cref="AffectComponent"/>를 관찰하여 <see cref="UIWindowPlayerBuffInfo"/>에 표시를 위임한다.
    /// </summary>
    /// <remarks>
    /// Core(UI)는 어펙트 적용 규칙을 알지 못한다. 실제 적용/만료/스택 정책은 AffectComponent가 단일 진실 소스다.
    /// 따라서 UI는 AffectComponent의 현재 상태를 스냅샷으로 받아 렌더링한다.
    /// </remarks>
    public sealed class PlayerAffectUiPresenter : MonoBehaviour
    {
        private const float DefaultSyncInterval = 0.10f;

        private AffectComponent _affectComponent;
        private UIWindowPlayerBuffInfo _view;

        private readonly List<AffectInstance> _instancesBuffer = new(64);
        private readonly List<AffectUiItem> _itemsBuffer = new(64);
        private readonly Dictionary<int, Aggregate> _aggregateByAffectUid = new(64);

        private float _syncInterval = DefaultSyncInterval;
        private float _syncTimer;
        private bool _dirty;

        private struct Aggregate
        {
            public int Stacks;
            public float RemainingMax;
            public float TotalDurationMax;
            public string IconKey;
        }

        /// <summary>
        /// 바인딩.
        /// </summary>
        public void Bind(AffectComponent affectComponent, UIWindowPlayerBuffInfo view, float syncIntervalSeconds = DefaultSyncInterval)
        {
            Unbind();

            _affectComponent = affectComponent;
            _view = view;
            _syncInterval = Mathf.Max(0.02f, syncIntervalSeconds);

            if (_affectComponent != null)
            {
                _affectComponent.Changed += OnAffectChanged;
                _dirty = true;
            }
        }

        public void Unbind()
        {
            if (_affectComponent != null)
                _affectComponent.Changed -= OnAffectChanged;

            _affectComponent = null;
            _view = null;
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();
            _syncTimer = 0f;
            _dirty = false;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void OnAffectChanged()
        {
            _dirty = true;
        }

        private void Update()
        {
            if (_view == null || _affectComponent == null)
                return;

            // 구조 변경이 없더라도 남은 시간은 주기적으로 동기화한다.
            _syncTimer += Time.unscaledDeltaTime;
            if (!_dirty && _syncTimer < _syncInterval)
                return;

            _syncTimer = 0f;
            _dirty = false;

            RenderSnapshot();
        }

        private void RenderSnapshot()
        {
            _instancesBuffer.Clear();
            _itemsBuffer.Clear();
            _aggregateByAffectUid.Clear();

            _affectComponent.CollectActiveInstances(_instancesBuffer);

            // UI는 "AffectUid" 단위로 집계하여 1개 아이콘으로 표현한다.
            // (StackPolicy.Independent 로 여러 인스턴스가 존재할 수 있어도 UX는 보통 1개로 합친다.)
            for (int i = 0; i < _instancesBuffer.Count; i++)
            {
                var inst = _instancesBuffer[i];
                if (inst == null || inst.Definition == null) continue;

                int uid = inst.Definition.Uid;
                if (!_aggregateByAffectUid.TryGetValue(uid, out var agg))
                {
                    agg = new Aggregate
                    {
                        Stacks = 0,
                        RemainingMax = 0f,
                        TotalDurationMax = 0f,
                        IconKey = inst.Definition.IconKey
                    };
                }

                agg.Stacks += Mathf.Max(1, inst.Stacks);

                // 남은 시간은 가장 큰 값을 사용(가장 최근에 적용된/리프레시된 인스턴스를 대표로 표시)
                if (inst.RemainingTime > agg.RemainingMax)
                    agg.RemainingMax = inst.RemainingTime;

                // TotalDuration(게이지 분모)은 BaseDuration 또는 RemainingTime 중 큰 값으로 보정(0 분모 방지)
                float total = Mathf.Max(inst.Definition.BaseDuration, inst.RemainingTime);
                if (total > agg.TotalDurationMax)
                    agg.TotalDurationMax = total;

                // 아이콘 키가 비어있다면 유지
                if (!string.IsNullOrEmpty(inst.Definition.IconKey))
                    agg.IconKey = inst.Definition.IconKey;

                _aggregateByAffectUid[uid] = agg;
            }

            foreach (var kv in _aggregateByAffectUid)
            {
                int uid = kv.Key;
                var agg = kv.Value;

                // 아이콘이 없는 어펙트는 UI에서 스킵
                if (string.IsNullOrEmpty(agg.IconKey))
                    continue;

                _itemsBuffer.Add(new AffectUiItem(
                    uid,
                    Mathf.Max(1, agg.Stacks),
                    Mathf.Max(0f, agg.RemainingMax),
                    Mathf.Max(0f, agg.TotalDurationMax),
                    agg.IconKey
                ));
            }

            // 정렬(선택): 남은 시간이 짧은 순서로 나열(원하면 정책 변경 가능)
            _itemsBuffer.Sort(static (a, b) => a.RemainingTime.CompareTo(b.RemainingTime));

            _view.Render(_itemsBuffer);
        }
    }
}
