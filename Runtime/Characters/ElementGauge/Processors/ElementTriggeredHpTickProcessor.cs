using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Triggered HP의 주기 소모(Tick)와 Clamp 후속 정리를 담당합니다.
    /// </summary>
    internal sealed class ElementTriggeredHpTickProcessor
    {
        private readonly CharacterBase _owner;
        private readonly ElementTriggeredHpService _triggeredHpService;
        private readonly ElementGaugeThresholdAffectService _thresholdAffectService;

        public ElementTriggeredHpTickProcessor(
            CharacterBase owner,
            ElementTriggeredHpService triggeredHpService,
            ElementGaugeThresholdAffectService thresholdAffectService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
            _thresholdAffectService = thresholdAffectService;
        }

        /// <summary>
        /// 오염 HP Tick 소모를 진행하고, Tick/Clamp로 오염 HP가 0이 되면 임계 Affect와 게이지 UI 상태를 정리합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="deltaTime">프레임 경과 시간입니다.</param>
        /// <returns>오염 HP/게이지 변경과 사망 확정 필요 여부를 포함한 처리 결과입니다.</returns>
        public ElementTriggeredHpTickResult UpdateTick(ElementGaugeRuntime runtime, float deltaTime)
        {
            if (_owner == null || runtime == null || deltaTime <= 0f)
                return ElementTriggeredHpTickResult.None;

            bool triggeredHpChanged = false;
            bool gaugeChanged = false;
            bool requiresDeathFinalize = false;

            if (_triggeredHpService == null)
                return new ElementTriggeredHpTickResult(false, false, false);

            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                ConfigCommon.DamageType damageType = rule.damageType;
                bool clamped = _triggeredHpService.ClampTriggeredHpToCurrentResources(runtime, damageType);
                triggeredHpChanged |= clamped;

                if (clamped && !_triggeredHpService.HasTriggeredHp(runtime, damageType))
                    gaugeChanged |= EndThresholdAffectIfNeeded(runtime, rule, damageType);

                TriggeredHpTickState tickState = runtime.GetOrCreateTriggeredHpTickState(damageType);

                if (!rule.useCorruptedHpTickDamage)
                {
                    tickState.Elapsed = 0f;
                    continue;
                }

                if (!_triggeredHpService.HasTriggeredHp(runtime, damageType))
                {
                    tickState.Elapsed = 0f;
                    continue;
                }

                long tickHpAmount = Math.Max(0, rule.corruptedHpTickHpAmount);
                if (tickHpAmount <= 0)
                {
                    tickState.Elapsed = 0f;
                    continue;
                }

                tickState.Elapsed += deltaTime;
                float interval = UnityEngine.Mathf.Max(0.01f, rule.corruptedHpTickIntervalSeconds);

                while (tickState.Elapsed >= interval)
                {
                    tickState.Elapsed -= interval;
                    long consumed = _triggeredHpService.ConsumeTriggeredHp(runtime, damageType, tickHpAmount);
                    if (consumed > 0)
                    {
                        triggeredHpChanged = true;
                        if (!_triggeredHpService.HasTriggeredHp(runtime, damageType))
                            gaugeChanged |= EndThresholdAffectIfNeeded(runtime, rule, damageType);

                        if (_owner.CurrentHp.Value <= 0)
                            requiresDeathFinalize = true;
                    }

                    if (!_triggeredHpService.HasTriggeredHp(runtime, damageType) || _owner.IsStatusDead())
                    {
                        tickState.Elapsed = 0f;
                        break;
                    }
                }
            }

            return new ElementTriggeredHpTickResult(triggeredHpChanged, gaugeChanged, requiresDeathFinalize);
        }

        /// <summary>
        /// 오염 HP가 남아 있지 않을 때 임계 Affect와 게이지 상태를 종료합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="rule">현재 평가 중인 속성 게이지 규칙입니다.</param>
        /// <param name="damageType">정리할 속성 타입입니다.</param>
        /// <returns>게이지 UI 갱신이 필요한지 여부입니다.</returns>
        private bool EndThresholdAffectIfNeeded(
            ElementGaugeRuntime runtime,
            ElementGaugeRuleDefinition rule,
            ConfigCommon.DamageType damageType)
        {
            return _thresholdAffectService != null &&
                   _thresholdAffectService.EndIfTriggeredHpCleared(runtime, _triggeredHpService, rule, damageType);
        }
    }
}
