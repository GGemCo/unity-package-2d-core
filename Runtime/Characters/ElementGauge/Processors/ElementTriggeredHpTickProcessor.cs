using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Triggered HP의 주기 소모(Tick)만 담당합니다.
    /// </summary>
    internal sealed class ElementTriggeredHpTickProcessor
    {
        private readonly CharacterBase _owner;
        private readonly ElementTriggeredHpService _triggeredHpService;

        public ElementTriggeredHpTickProcessor(CharacterBase owner, ElementTriggeredHpService triggeredHpService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
        }

        public ElementTriggeredHpTickResult UpdateTick(ElementGaugeRuntime runtime, float deltaTime)
        {
            if (_owner == null || runtime == null || deltaTime <= 0f)
                return ElementTriggeredHpTickResult.None;

            bool triggeredHpChanged = _triggeredHpService != null &&
                                      _triggeredHpService.ClampAllTriggeredHpToCurrentResources(runtime);
            bool requiresDeathFinalize = false;

            if (_triggeredHpService == null)
                return new ElementTriggeredHpTickResult(triggeredHpChanged, false);

            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                ConfigCommon.DamageType damageType = rule.damageType;
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

            return new ElementTriggeredHpTickResult(triggeredHpChanged, requiresDeathFinalize);
        }
    }
}
