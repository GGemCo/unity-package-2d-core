using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 게이지 1종의 현재 누적 상태입니다.
    /// </summary>
    internal sealed class RuntimeGaugeState
    {
        public float CurrentValue;
        public float LastAccumulatedTime = float.MinValue;
        public float DecayElapsed;

        /// <summary>
        /// 게이지 누적값과 감쇠 타이머를 초기 상태로 되돌립니다.
        /// </summary>
        /// <returns>초기화 전 표시 값이 남아 있어 UI 갱신이 필요한지 여부입니다.</returns>
        public bool Reset()
        {
            bool changed = !Mathf.Approximately(CurrentValue, 0f) || !Mathf.Approximately(DecayElapsed, 0f);
            CurrentValue = 0f;
            LastAccumulatedTime = float.MinValue;
            DecayElapsed = 0f;
            return changed;
        }
    }

    /// <summary>
    /// 속성 임계 반응으로 오염된 HP 구간 상태입니다.
    /// </summary>
    internal sealed class TriggeredHpState
    {
        public long BaseHp;
        public long TempItemHp;
        public long TempRuntimeHp;
        public long TempPassiveHp;

        public long TotalHp => BaseHp + TempItemHp + TempRuntimeHp + TempPassiveHp;

        /// <summary>
        /// 오염 HP 구간을 모두 초기화합니다.
        /// </summary>
        public void Clear()
        {
            BaseHp = 0;
            TempItemHp = 0;
            TempRuntimeHp = 0;
            TempPassiveHp = 0;
        }
    }

    /// <summary>
    /// 오염 HP 주기 소모 Tick 누적 상태입니다.
    /// </summary>
    internal sealed class TriggeredHpTickState
    {
        public float Elapsed;
    }

    /// <summary>
    /// 속성 임계 도달로 적용한 Affect 추적 상태입니다.
    /// </summary>
    internal sealed class ThresholdAffectState
    {
        public int AffectUid;
        public bool IsActive;

        /// <summary>
        /// 임계 Affect가 현재 속성 게이지에서 적용된 상태임을 기록합니다.
        /// </summary>
        /// <param name="affectUid">적용한 Affect UID입니다.</param>
        public void Activate(int affectUid)
        {
            AffectUid = Mathf.Max(0, affectUid);
            IsActive = AffectUid > 0;
        }

        /// <summary>
        /// 임계 Affect 추적 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            AffectUid = 0;
            IsActive = false;
        }
    }

    /// <summary>
    /// 속성 게이지 시스템이 공유하는 런타임 상태 저장소입니다.
    /// 규칙, 게이지 값, 임계 HP 상태, Tick 타이머, 임계 Affect 추적 상태를 묶어서 관리합니다.
    /// </summary>
    internal sealed class ElementGaugeRuntime
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _gaugeStates = new();
        private readonly Dictionary<ConfigCommon.DamageType, TriggeredHpState> _triggeredHpStates = new();
        private readonly Dictionary<ConfigCommon.DamageType, TriggeredHpTickState> _triggeredHpTickStates = new();
        private readonly Dictionary<ConfigCommon.DamageType, ThresholdAffectState> _thresholdAffectStates = new();

        public ElementGaugeRuntime(IReadOnlyList<ElementGaugeRuleDefinition> rules)
        {
            if (rules == null)
                return;

            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition source = rules[i];
                if (source == null)
                    continue;

                ConfigCommon.DamageType damageType = source.damageType;
                if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                    continue;

                ElementGaugeRuleDefinition cloned = source.Clone();
                _rules.Add(cloned);
                _ruleMap[damageType] = cloned;
                _gaugeStates[damageType] = new RuntimeGaugeState();
                _triggeredHpStates[damageType] = new TriggeredHpState();
                _triggeredHpTickStates[damageType] = new TriggeredHpTickState();
                _thresholdAffectStates[damageType] = new ThresholdAffectState();
            }
        }

        public IReadOnlyList<ElementGaugeRuleDefinition> Rules => _rules;
        public IEnumerable<KeyValuePair<ConfigCommon.DamageType, ElementGaugeRuleDefinition>> RulePairs => _ruleMap;

        public bool TryGetRule(ConfigCommon.DamageType damageType, out ElementGaugeRuleDefinition rule)
        {
            return _ruleMap.TryGetValue(damageType, out rule);
        }

        public bool TryGetGaugeState(ConfigCommon.DamageType damageType, out RuntimeGaugeState state)
        {
            return _gaugeStates.TryGetValue(damageType, out state);
        }

        public RuntimeGaugeState GetOrCreateGaugeState(ConfigCommon.DamageType damageType)
        {
            if (_gaugeStates.TryGetValue(damageType, out RuntimeGaugeState state) && state != null)
                return state;

            state = new RuntimeGaugeState();
            _gaugeStates[damageType] = state;
            return state;
        }

        /// <summary>
        /// 지정한 속성 게이지 값을 0으로 초기화합니다.
        /// </summary>
        /// <param name="damageType">초기화할 속성 타입입니다.</param>
        /// <returns>초기화 전 값이 남아 있어 UI 갱신이 필요한지 여부입니다.</returns>
        public bool ResetGaugeState(ConfigCommon.DamageType damageType)
        {
            return GetOrCreateGaugeState(damageType).Reset();
        }

        public bool TryGetTriggeredHpState(ConfigCommon.DamageType damageType, out TriggeredHpState state)
        {
            return _triggeredHpStates.TryGetValue(damageType, out state);
        }

        public TriggeredHpState GetOrCreateTriggeredHpState(ConfigCommon.DamageType damageType)
        {
            if (_triggeredHpStates.TryGetValue(damageType, out TriggeredHpState state) && state != null)
                return state;

            state = new TriggeredHpState();
            _triggeredHpStates[damageType] = state;
            return state;
        }

        public bool TryGetTriggeredHpTickState(ConfigCommon.DamageType damageType, out TriggeredHpTickState state)
        {
            return _triggeredHpTickStates.TryGetValue(damageType, out state);
        }

        public TriggeredHpTickState GetOrCreateTriggeredHpTickState(ConfigCommon.DamageType damageType)
        {
            if (_triggeredHpTickStates.TryGetValue(damageType, out TriggeredHpTickState state) && state != null)
                return state;

            state = new TriggeredHpTickState();
            _triggeredHpTickStates[damageType] = state;
            return state;
        }

        public void ResetTriggeredHpTickElapsed(ConfigCommon.DamageType damageType)
        {
            GetOrCreateTriggeredHpTickState(damageType).Elapsed = 0f;
        }

        public bool TryGetThresholdAffectState(ConfigCommon.DamageType damageType, out ThresholdAffectState state)
        {
            return _thresholdAffectStates.TryGetValue(damageType, out state);
        }

        public ThresholdAffectState GetOrCreateThresholdAffectState(ConfigCommon.DamageType damageType)
        {
            if (_thresholdAffectStates.TryGetValue(damageType, out ThresholdAffectState state) && state != null)
                return state;

            state = new ThresholdAffectState();
            _thresholdAffectStates[damageType] = state;
            return state;
        }

        /// <summary>
        /// 임계 Affect 추적 상태를 초기화합니다.
        /// </summary>
        /// <param name="damageType">초기화할 속성 타입입니다.</param>
        public void ClearThresholdAffectState(ConfigCommon.DamageType damageType)
        {
            GetOrCreateThresholdAffectState(damageType).Clear();
        }
    }
}
