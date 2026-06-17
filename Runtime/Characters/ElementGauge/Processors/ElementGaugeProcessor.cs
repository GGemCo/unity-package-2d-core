using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 게이지 누적/감쇠 계산만 담당하는 프로세서입니다.
    /// </summary>
    internal sealed class ElementGaugeProcessor
    {
        private readonly CharacterBase _owner;

        public ElementGaugeProcessor(CharacterBase owner)
        {
            _owner = owner;
        }

        public ElementGaugeApplyResult ApplyGauge(ElementGaugeRuntime runtime, ElementGaugeApplication application, float now)
        {
            if (_owner == null || _owner.IsStatusDead() || runtime == null || !application.IsValid)
                return ElementGaugeApplyResult.None;

            if (!runtime.TryGetRule(application.damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeApplyResult.None;

            if (rule.blockAccumulationWhileTriggered &&
                runtime.TryGetTriggeredHpState(application.damageType, out TriggeredHpState triggeredState) &&
                triggeredState != null &&
                triggeredState.TotalHp > 0)
            {
                return ElementGaugeApplyResult.None;
            }

            RuntimeGaugeState gaugeState = runtime.GetOrCreateGaugeState(application.damageType);
            float appliedValue = Mathf.Max(0f, application.gaugeValue * ResolveResistanceMultiplier(application.damageType));
            if (appliedValue <= 0f)
                return ElementGaugeApplyResult.None;

            gaugeState.CurrentValue += appliedValue;
            gaugeState.LastAccumulatedTime = now;
            gaugeState.DecayElapsed = 0f;

            if (gaugeState.CurrentValue >= Mathf.Max(1f, rule.gaugeMax))
            {
                gaugeState.CurrentValue = 0f;
                gaugeState.DecayElapsed = 0f;
                return new ElementGaugeApplyResult(true, true, application.damageType);
            }

            return new ElementGaugeApplyResult(true, false, application.damageType);
        }

        public ElementGaugeDecayResult UpdateDecay(ElementGaugeRuntime runtime, float now, float deltaTime)
        {
            if (runtime == null || deltaTime <= 0f)
                return ElementGaugeDecayResult.None;

            bool gaugeChanged = false;

            foreach (KeyValuePair<ConfigCommon.DamageType, ElementGaugeRuleDefinition> pair in runtime.RulePairs)
            {
                ElementGaugeRuleDefinition rule = pair.Value;
                if (rule == null)
                    continue;

                if (!runtime.TryGetGaugeState(pair.Key, out RuntimeGaugeState state) || state == null || state.CurrentValue <= 0f)
                    continue;

                if (now - state.LastAccumulatedTime < Mathf.Max(0f, rule.decayDelaySeconds))
                    continue;

                state.DecayElapsed += deltaTime;
                float tick = Mathf.Max(0.01f, rule.decayTickSeconds);
                float decayPerTick = Mathf.Max(0f, rule.gaugeMax) * Mathf.Max(0f, rule.decayPercentPerTick) * 0.01f;
                if (decayPerTick <= 0f)
                    continue;

                while (state.DecayElapsed >= tick)
                {
                    state.DecayElapsed -= tick;
                    float nextValue = Mathf.Max(0f, state.CurrentValue - decayPerTick);
                    if (!Mathf.Approximately(nextValue, state.CurrentValue))
                    {
                        state.CurrentValue = nextValue;
                        gaugeChanged = true;
                    }

                    if (state.CurrentValue <= 0f)
                    {
                        state.CurrentValue = 0f;
                        state.DecayElapsed = 0f;
                        break;
                    }
                }
            }

            return new ElementGaugeDecayResult(gaugeChanged);
        }

        public IReadOnlyList<ElementGaugeSnapshot> BuildSnapshots(
            ElementGaugeRuntime runtime,
            ElementTriggeredHpService triggeredHpService,
            List<ElementGaugeSnapshot> buffer)
        {
            buffer.Clear();
            if (runtime == null)
                return buffer;

            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                if (!runtime.TryGetGaugeState(rule.damageType, out RuntimeGaugeState state) || state == null)
                    continue;

                bool isBlocked = triggeredHpService != null && triggeredHpService.HasTriggeredHp(runtime, rule.damageType);
                buffer.Add(new ElementGaugeSnapshot(
                    rule.damageType,
                    state.CurrentValue,
                    Mathf.Max(1f, rule.gaugeMax),
                    isBlocked));
            }

            return buffer;
        }

        private float ResolveResistanceMultiplier(ConfigCommon.DamageType damageType)
        {
            if (_owner == null)
                return 1f;

            long resistance = damageType switch
            {
                ConfigCommon.DamageType.Fire => _owner.TotalRegistFire.Value,
                ConfigCommon.DamageType.Cold => _owner.TotalRegistCold.Value,
                ConfigCommon.DamageType.Lightning => _owner.TotalRegistLightning.Value,
                ConfigCommon.DamageType.Poison => _owner.TotalRegistPoison.Value,
                _ => 0L,
            };

            return Mathf.Clamp01((100f - Mathf.Clamp(resistance, 0f, 100f)) / 100f);
        }
    }
}
