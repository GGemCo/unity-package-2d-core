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

        public ElementTriggeredHpConsumeProcessor(CharacterBase owner, ElementTriggeredHpService triggeredHpService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
        }

        public ElementTriggeredHpConsumeResult HandleAfterIncomingDamage(ElementGaugeRuntime runtime, MetadataDamage metadataDamage)
        {
            if (_owner == null || runtime == null || metadataDamage == null || _triggeredHpService == null)
                return ElementTriggeredHpConsumeResult.None;

            bool triggeredHpChanged = false;
            bool requiresDeathFinalize = false;

            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                ConfigCommon.DamageType damageType = rule.damageType;
                triggeredHpChanged |= _triggeredHpService.ClampTriggeredHpToCurrentResources(runtime, damageType);

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
                if (_owner.CurrentHp.Value <= 0)
                    requiresDeathFinalize = true;
            }

            return new ElementTriggeredHpConsumeResult(triggeredHpChanged, requiresDeathFinalize);
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
                if (applications[i].DamageType == damageType && applications[i].GaugeValue > 0f)
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
