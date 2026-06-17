using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 피격 이후 Triggered HP를 즉시 소모해야 하는지 판정하고 처리합니다.
    /// </summary>
    internal sealed class ElementTriggeredHpConsumeProcessor
    {
        private readonly CharacterBase _owner;
        private readonly ElementTriggeredHpService _triggeredHpService;
        private readonly ElementGaugeThresholdAffectService _thresholdAffectService;

        public ElementTriggeredHpConsumeProcessor(
            CharacterBase owner,
            ElementTriggeredHpService triggeredHpService,
            ElementGaugeThresholdAffectService thresholdAffectService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
            _thresholdAffectService = thresholdAffectService;
        }

        /// <summary>
        /// 피격 후 오염 HP 즉시 소모 정책을 평가하고, 오염 HP가 0이 된 경우 임계 Affect와 게이지 UI 상태를 정리합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="metadataDamage">피격 메타데이터입니다.</param>
        /// <returns>오염 HP/게이지 변경과 사망 확정 필요 여부를 포함한 처리 결과입니다.</returns>
        public ElementTriggeredHpConsumeResult HandleAfterIncomingDamage(ElementGaugeRuntime runtime, MetadataDamage metadataDamage)
        {
            if (_owner == null || runtime == null || metadataDamage == null || _triggeredHpService == null)
                return ElementTriggeredHpConsumeResult.None;

            bool triggeredHpChanged = false;
            bool gaugeChanged = false;
            bool requiresDeathFinalize = false;

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

                if (!_triggeredHpService.HasTriggeredHp(runtime, damageType))
                    continue;

                if (!ShouldConsumeTriggeredHp(rule, metadataDamage))
                    continue;

                long consumed = _triggeredHpService.ConsumeTriggeredHp(
                    runtime,
                    damageType,
                    _triggeredHpService.GetTriggeredHpTotal(runtime, damageType));

                if (consumed <= 0)
                    continue;

                triggeredHpChanged = true;
                if (!_triggeredHpService.HasTriggeredHp(runtime, damageType))
                    gaugeChanged |= EndThresholdAffectIfNeeded(runtime, rule, damageType);

                if (_owner.CurrentHp.Value <= 0)
                    requiresDeathFinalize = true;
            }

            return new ElementTriggeredHpConsumeResult(triggeredHpChanged, gaugeChanged, requiresDeathFinalize);
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

        private static bool ShouldConsumeTriggeredHp(ElementGaugeRuleDefinition rule, MetadataDamage metadataDamage)
        {
            if (rule == null || metadataDamage == null)
                return false;

            bool hasExplicitPolicies = rule.consumePolicies != null && rule.consumePolicies.Count > 0;
            if (!hasExplicitPolicies && !rule.consumeCorruptedHpOnMatchingDamage)
                return false;

            if (hasExplicitPolicies)
            {
                for (int i = 0; i < rule.consumePolicies.Count; i++)
                {
                    ElementGaugeCorruptedHpConsumePolicyDefinition policy = rule.consumePolicies[i];
                    if (policy == null)
                        continue;

                    if (IsMatchedConsumePolicy(policy, metadataDamage))
                        return true;
                }

                return false;
            }

            return MatchesLegacyConsumePolicy(rule.damageType, metadataDamage);
        }

        private static bool IsMatchedConsumePolicy(ElementGaugeCorruptedHpConsumePolicyDefinition policy, MetadataDamage metadataDamage)
        {
            if (policy == null || metadataDamage == null)
                return false;

            switch (policy.triggerType)
            {
                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageType:
                    return metadataDamage.damageType == policy.damageType;

                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingGaugeApplication:
                    return HasMatchingGaugeApplication(metadataDamage, policy.damageType);

                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageIfAttackerHasAffect:
                    return HasRequiredAttackerAffect(metadataDamage.attacker, policy.requiredAttackerAffectUid);
            }

            return false;
        }

        private static bool MatchesLegacyConsumePolicy(ConfigCommon.DamageType damageType, MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (metadataDamage.damageType == damageType)
                return true;

            return HasMatchingGaugeApplication(metadataDamage, damageType);
        }

        private static bool HasMatchingGaugeApplication(MetadataDamage metadataDamage, ConfigCommon.DamageType damageType)
        {
            if (metadataDamage == null)
                return false;

            ElementGaugeApplication[] applications = metadataDamage.ElementGaugeApplications;
            if (applications == null || applications.Length == 0)
                return false;

            for (int i = 0; i < applications.Length; i++)
            {
                if (applications[i].damageType == damageType && applications[i].gaugeValue > 0f)
                    return true;
            }

            return false;
        }

        private static bool HasRequiredAttackerAffect(GameObject attacker, int affectUid)
        {
            if (attacker == null || affectUid <= 0)
                return false;

            return AffectApi.HasAttached(attacker, affectUid);
        }
    }
}
