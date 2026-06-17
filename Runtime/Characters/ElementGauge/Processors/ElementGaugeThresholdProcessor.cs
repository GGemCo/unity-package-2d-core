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
        private readonly ElementGaugeThresholdAffectService _thresholdAffectService;

        public ElementGaugeThresholdProcessor(
            CharacterBase owner,
            ElementTriggeredHpService triggeredHpService,
            ElementGaugeThresholdAffectService thresholdAffectService)
        {
            _owner = owner;
            _triggeredHpService = triggeredHpService;
            _thresholdAffectService = thresholdAffectService;
        }

        /// <summary>
        /// 속성 게이지가 임계치에 도달했을 때 오염 HP와 임계 Affect를 적용합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="damageType">임계치에 도달한 속성 타입입니다.</param>
        /// <param name="source">임계 반응을 발생시킨 원천 오브젝트입니다.</param>
        /// <returns>오염 HP 변경 여부를 포함한 처리 결과입니다.</returns>
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

            _thresholdAffectService?.ApplyAndTrack(runtime, rule, damageType, source, _triggeredHpService);
            return new ElementGaugeThresholdResult(triggeredHpChanged);
        }
    }
}
