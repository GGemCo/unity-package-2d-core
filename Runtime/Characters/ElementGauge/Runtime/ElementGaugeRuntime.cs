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
        public bool IsThresholdReached;

        /// <summary>
        /// 게이지 누적값과 임계 상태를 초기화합니다.
        /// </summary>
        /// <returns>초기화 전 표시 값 또는 임계 상태가 남아 있었으면 <see langword="true"/>입니다.</returns>
        public bool Reset()
        {
            bool changed = !Mathf.Approximately(CurrentValue, 0f) || !Mathf.Approximately(DecayElapsed, 0f) || IsThresholdReached;
            CurrentValue = 0f;
            LastAccumulatedTime = float.MinValue;
            DecayElapsed = 0f;
            IsThresholdReached = false;
            return changed;
        }
    }

    /// <summary>
    /// 속성 게이지 시스템이 공유하는 런타임 상태 저장소입니다.
    /// </summary>
    internal sealed class ElementGaugeRuntime
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _gaugeStates = new();

        /// <summary>
        /// 규칙 목록을 기준으로 런타임 상태 저장소를 생성합니다.
        /// </summary>
        /// <param name="rules">속성별 누적 규칙 목록입니다.</param>
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
        /// 지정한 속성 게이지를 초기화합니다.
        /// </summary>
        /// <param name="damageType">초기화할 속성 타입입니다.</param>
        /// <returns>초기화로 인해 표시 갱신이 필요하면 <see langword="true"/>입니다.</returns>
        public bool ResetGaugeState(ConfigCommon.DamageType damageType)
        {
            return GetOrCreateGaugeState(damageType).Reset();
        }
    }
}
