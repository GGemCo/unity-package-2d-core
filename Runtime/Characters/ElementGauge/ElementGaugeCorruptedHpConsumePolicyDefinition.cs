using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 오염된 HP를 언제 즉시 소모할지 정의하는 트리거 종류입니다.
    /// </summary>
    [Serializable]
    public enum ElementGaugeCorruptedHpConsumeTriggerType
    {
        IncomingDamageType = 0,
        IncomingGaugeApplication = 1,
        IncomingDamageIfAttackerHasAffect = 2,
    }

    /// <summary>
    /// 오염된 HP 즉시 소모 정책 1건입니다.
    /// 여러 정책이 있을 경우 OR 조건으로 평가합니다.
    /// </summary>
    [Serializable]
    public sealed class ElementGaugeCorruptedHpConsumePolicyDefinition
    {
        [Tooltip("피격 후 오염된 HP를 즉시 소모할 조건 종류입니다.")]
        public ElementGaugeCorruptedHpConsumeTriggerType triggerType = ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageType;

        [Tooltip("DamageType 기반 정책에서 사용할 속성 타입입니다.")]
        public ConfigCommon.DamageType damageType = ConfigCommon.DamageType.None;

        [Min(0)]
        [Tooltip("공격자가 이 Affect를 가지고 있으면 오염된 HP를 즉시 소모합니다. (0이면 미사용)")]
        public int requiredAttackerAffectUid = 0;

        public ElementGaugeCorruptedHpConsumePolicyDefinition Clone()
        {
            return new ElementGaugeCorruptedHpConsumePolicyDefinition
            {
                triggerType = triggerType,
                damageType = damageType,
                requiredAttackerAffectUid = requiredAttackerAffectUid,
            };
        }

        public static ElementGaugeCorruptedHpConsumePolicyDefinition CreateIncomingDamageType(ConfigCommon.DamageType damageType)
        {
            return new ElementGaugeCorruptedHpConsumePolicyDefinition
            {
                triggerType = ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageType,
                damageType = damageType,
            };
        }

        public static ElementGaugeCorruptedHpConsumePolicyDefinition CreateIncomingGaugeApplication(ConfigCommon.DamageType damageType)
        {
            return new ElementGaugeCorruptedHpConsumePolicyDefinition
            {
                triggerType = ElementGaugeCorruptedHpConsumeTriggerType.IncomingGaugeApplication,
                damageType = damageType,
            };
        }

        public static ElementGaugeCorruptedHpConsumePolicyDefinition CreateIncomingDamageIfAttackerHasAffect(int affectUid)
        {
            return new ElementGaugeCorruptedHpConsumePolicyDefinition
            {
                triggerType = ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageIfAttackerHasAffect,
                requiredAttackerAffectUid = Mathf.Max(0, affectUid),
            };
        }
    }
}
