using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게이지 임계점 도달 후속 처리(Triggered HP 적용, Affect 부여)를 담당합니다.
    /// </summary>
    internal sealed class ElementGaugeThresholdProcessor
    {
        private readonly CharacterBase _owner;
        private readonly ElementTriggeredHpService _triggeredHpService;

        public ElementGaugeThresholdProcessor(CharacterBase owner, ElementTriggeredHpService triggeredHpService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
        }

        public ElementGaugeThresholdResult ProcessThreshold(
            ElementGaugeRuntime runtime,
            ConfigCommon.DamageType damageType,
            GameObject source)
        {
            if (_owner == null || runtime == null)
                return ElementGaugeThresholdResult.None;

            if (!runtime.TryGetRule(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeThresholdResult.None;

            bool triggeredHpChanged = _triggeredHpService != null &&
                                      _triggeredHpService.ApplyTriggeredHp(runtime, damageType, rule.corruptionHpAmount);

            if (rule.thresholdAffectUid > 0)
            {
                AffectApi.Apply(_owner.gameObject, rule.thresholdAffectUid, source, rule.thresholdAffectDurationSeconds);
            }

            return new ElementGaugeThresholdResult(triggeredHpChanged);
        }
    }
}
