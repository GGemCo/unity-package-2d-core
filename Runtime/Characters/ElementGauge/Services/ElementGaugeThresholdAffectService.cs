using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 게이지 임계 도달로 적용한 Affect의 적용/추적/종료를 담당합니다.
    /// 오염 HP가 0이 되는 모든 경로에서 같은 정리 정책을 사용하기 위한 서비스입니다.
    /// </summary>
    internal sealed class ElementGaugeThresholdAffectService
    {
        private readonly CharacterBase _owner;

        public ElementGaugeThresholdAffectService(CharacterBase owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 임계 Affect를 적용하고, 오염 HP가 남아 있는 경우에만 종료 추적 상태로 기록합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="rule">임계 도달 규칙입니다.</param>
        /// <param name="damageType">임계 도달 속성 타입입니다.</param>
        /// <param name="source">Affect 발생 원천입니다.</param>
        /// <param name="triggeredHpService">오염 HP 상태 조회 서비스입니다.</param>
        public void ApplyAndTrack(
            ElementGaugeRuntime runtime,
            ElementGaugeRuleDefinition rule,
            ConfigCommon.DamageType damageType,
            GameObject source,
            ElementTriggeredHpService triggeredHpService)
        {
            if (_owner == null || runtime == null || rule == null || rule.thresholdAffectUid <= 0)
                return;

            AffectApi.Apply(_owner.gameObject, rule.thresholdAffectUid, source, rule.thresholdAffectDurationSeconds);

            if (triggeredHpService != null && triggeredHpService.HasTriggeredHp(runtime, damageType))
            {
                runtime.GetOrCreateThresholdAffectState(damageType).Activate(rule.thresholdAffectUid);
                return;
            }

            runtime.ClearThresholdAffectState(damageType);
        }

        /// <summary>
        /// 오염 HP가 모두 사라졌을 때 임계 Affect를 종료하고 게이지 상태를 초기화합니다.
        /// </summary>
        /// <param name="runtime">속성 게이지 런타임 상태입니다.</param>
        /// <param name="triggeredHpService">오염 HP 상태 조회 서비스입니다.</param>
        /// <param name="rule">정리할 속성 게이지 규칙입니다.</param>
        /// <param name="damageType">정리할 속성 타입입니다.</param>
        /// <returns>게이지 UI 갱신이 필요한지 여부입니다.</returns>
        public bool EndIfTriggeredHpCleared(
            ElementGaugeRuntime runtime,
            ElementTriggeredHpService triggeredHpService,
            ElementGaugeRuleDefinition rule,
            ConfigCommon.DamageType damageType)
        {
            if (_owner == null || runtime == null || triggeredHpService == null || rule == null)
                return false;

            if (triggeredHpService.HasTriggeredHp(runtime, damageType))
                return false;

            bool gaugeChanged = runtime.ResetGaugeState(damageType);
            runtime.ResetTriggeredHpTickElapsed(damageType);

            if (!runtime.TryGetThresholdAffectState(damageType, out ThresholdAffectState thresholdState) ||
                thresholdState == null ||
                !thresholdState.IsActive)
            {
                return gaugeChanged;
            }

            if (rule.thresholdAffectUid > 0 && thresholdState.AffectUid == rule.thresholdAffectUid)
                AffectApi.Remove(_owner.gameObject, rule.thresholdAffectUid);

            thresholdState.Clear();
            return true;
        }
    }
}
