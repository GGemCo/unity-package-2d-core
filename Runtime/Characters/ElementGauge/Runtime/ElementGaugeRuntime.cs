using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    internal sealed class RuntimeGaugeState
    {
        public float CurrentValue;
        public float LastAccumulatedTime = float.MinValue;
        public float DecayElapsed;
    }

    internal sealed class TriggeredHpState
    {
        public long BaseHp;
        public long TempItemHp;
        public long TempRuntimeHp;
        public long TempPassiveHp;

        public long TotalHp => BaseHp + TempItemHp + TempRuntimeHp + TempPassiveHp;

        public void Clear()
        {
            BaseHp = 0;
            TempItemHp = 0;
            TempRuntimeHp = 0;
            TempPassiveHp = 0;
        }
    }

    internal sealed class TriggeredHpTickState
    {
        public float Elapsed;
    }

    /// <summary>
    /// 속성 게이지 시스템이 공유하는 런타임 상태 저장소입니다.
    /// 규칙, 게이지 값, 임계 HP 상태, Tick 타이머를 묶어서 관리합니다.
    /// </summary>
    internal sealed class ElementGaugeRuntime
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _gaugeStates = new();
        private readonly Dictionary<ConfigCommon.DamageType, TriggeredHpState> _triggeredHpStates = new();
        private readonly Dictionary<ConfigCommon.DamageType, TriggeredHpTickState> _triggeredHpTickStates = new();

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
    }
}
