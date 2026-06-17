using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 데미지 누적/감쇠 계산만 담당하는 프로세서입니다.
    /// </summary>
    internal sealed class ElementGaugeProcessor
    {
        /// <summary>
        /// 실제로 받은 속성 데미지를 게이지에 누적합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 저장소입니다.</param>
        /// <param name="damageType">누적할 속성 타입입니다.</param>
        /// <param name="damageAmount">실제로 받은 데미지량입니다.</param>
        /// <param name="now">현재 시간입니다.</param>
        /// <returns>누적 처리 결과입니다.</returns>
        public ElementGaugeApplyResult AccumulateDamage(
            ElementGaugeRuntime runtime,
            ConfigCommon.DamageType damageType,
            long damageAmount,
            float now)
        {
            if (runtime == null || damageAmount <= 0L)
                return ElementGaugeApplyResult.None;

            if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                return ElementGaugeApplyResult.None;

            if (!runtime.TryGetRule(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeApplyResult.None;

            RuntimeGaugeState gaugeState = runtime.GetOrCreateGaugeState(damageType);
            float maxValue = Mathf.Max(1f, rule.gaugeMax);
            bool wasThresholdReached = gaugeState.IsThresholdReached;

            gaugeState.LastAccumulatedTime = now;
            gaugeState.DecayElapsed = 0f;

            if (wasThresholdReached)
            {
                gaugeState.CurrentValue = maxValue;
                return new ElementGaugeApplyResult(false, false, true, damageType, BuildSnapshot(rule, gaugeState));
            }

            float previousValue = gaugeState.CurrentValue;
            gaugeState.CurrentValue = Mathf.Clamp(gaugeState.CurrentValue + damageAmount, 0f, maxValue);

            bool reachedNow = !wasThresholdReached && gaugeState.CurrentValue >= maxValue;
            if (reachedNow)
                gaugeState.IsThresholdReached = true;

            bool changed = !Mathf.Approximately(previousValue, gaugeState.CurrentValue) || reachedNow;
            return new ElementGaugeApplyResult(changed, reachedNow, false, damageType, BuildSnapshot(rule, gaugeState));
        }

        /// <summary>
        /// 설정된 감쇠 규칙에 따라 게이지 값을 감소시킵니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 저장소입니다.</param>
        /// <param name="now">현재 시간입니다.</param>
        /// <param name="deltaTime">프레임 경과 시간입니다.</param>
        /// <returns>감쇠 처리 결과입니다.</returns>
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

                    if (state.IsThresholdReached && state.CurrentValue < Mathf.Max(1f, rule.gaugeMax))
                    {
                        state.IsThresholdReached = false;
                        gaugeChanged = true;
                    }

                    if (state.CurrentValue <= 0f)
                    {
                        state.CurrentValue = 0f;
                        state.DecayElapsed = 0f;
                        state.IsThresholdReached = false;
                        break;
                    }
                }
            }

            return new ElementGaugeDecayResult(gaugeChanged);
        }

        /// <summary>
        /// 현재 게이지 상태를 UI에서 사용할 스냅샷 목록으로 변환합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 저장소입니다.</param>
        /// <param name="buffer">재사용할 결과 버퍼입니다.</param>
        /// <returns>속성별 게이지 스냅샷 목록입니다.</returns>
        public IReadOnlyList<ElementGaugeSnapshot> BuildSnapshots(ElementGaugeRuntime runtime, List<ElementGaugeSnapshot> buffer)
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

                buffer.Add(BuildSnapshot(rule, state));
            }

            return buffer;
        }

        /// <summary>
        /// 단일 속성 게이지 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="rule">속성 게이지 규칙입니다.</param>
        /// <param name="state">현재 런타임 상태입니다.</param>
        /// <returns>현재 표시 상태를 담은 스냅샷입니다.</returns>
        private static ElementGaugeSnapshot BuildSnapshot(ElementGaugeRuleDefinition rule, RuntimeGaugeState state)
        {
            return new ElementGaugeSnapshot(
                rule.damageType,
                state != null ? state.CurrentValue : 0f,
                Mathf.Max(1f, rule.gaugeMax),
                state != null && state.IsThresholdReached);
        }
    }
}
