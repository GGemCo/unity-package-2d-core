using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어용 속성 게이지 규칙 1건입니다.
    /// ScriptableObject 설정 자산(GGemCoPlayerSettings)에서 직렬화하여 사용합니다.
    /// </summary>
    [Serializable]
    public sealed class ElementGaugeRuleDefinition
    {
        [Tooltip("게이지를 누적시킬 데미지 타입입니다.")]
        public ConfigCommon.DamageType damageType = ConfigCommon.DamageType.None;

        [Min(1f)]
        [Tooltip("게이지 최대값입니다. 이 값에 도달하면 임계 효과가 발동됩니다.")]
        public float gaugeMax = 100f;

        [Min(0f)]
        [Tooltip("마지막 누적 이후 감소가 시작되기까지의 지연 시간(초)입니다.")]
        public float decayDelaySeconds = 2f;

        [Min(0.01f)]
        [Tooltip("게이지 감소가 실행되는 주기(초)입니다.")]
        public float decayTickSeconds = 0.1f;

        [Min(0f)]
        [Tooltip("감소 Tick마다 줄어드는 비율(%)입니다. (예: 0.5 = 0.5%)")]
        public float decayPercentPerTick = 0.5f;

        [FormerlySerializedAs("corruptionHeartCount")]
        [Min(0)]
        [Tooltip("임계 도달 시 오염(또는 변질)시킬 HP 양입니다.")]
        public int corruptionHpAmount = 0;

        [Tooltip("임계 상태가 유지되는 동안 게이지 누적을 차단할지 여부입니다.")]
        public bool blockAccumulationWhileTriggered = false;

        [Tooltip("[Legacy] 같은 속성 데미지를 받을 때 오염된 HP를 즉시 소모할지 여부입니다. consumePolicies가 비어 있을 때만 사용됩니다.")]
        public bool consumeCorruptedHpOnMatchingDamage = false;

        [Tooltip("오염된 HP를 즉시 소모할 정책 목록입니다. 여러 정책이 있으면 OR 조건으로 평가합니다.")]
        public List<ElementGaugeCorruptedHpConsumePolicyDefinition> consumePolicies = new();

        [Min(0)]
        [Tooltip("게이지가 최대에 도달했을 때 적용할 Affect UID입니다. (0이면 미사용)")]
        public int thresholdAffectUid = 0;

        [Min(0f)]
        [Tooltip("임계 도달 시 적용되는 Affect의 지속 시간(초)입니다.")]
        public float thresholdAffectDurationSeconds = 0f;

        public ElementGaugeRuleDefinition Clone()
        {
            return new ElementGaugeRuleDefinition
            {
                damageType = damageType,
                gaugeMax = gaugeMax,
                decayDelaySeconds = decayDelaySeconds,
                decayTickSeconds = decayTickSeconds,
                decayPercentPerTick = decayPercentPerTick,
                corruptionHpAmount = corruptionHpAmount,
                blockAccumulationWhileTriggered = blockAccumulationWhileTriggered,
                consumeCorruptedHpOnMatchingDamage = consumeCorruptedHpOnMatchingDamage,
                consumePolicies = ClonePolicies(consumePolicies),
                thresholdAffectUid = thresholdAffectUid,
                thresholdAffectDurationSeconds = thresholdAffectDurationSeconds,
            };
        }

        private static List<ElementGaugeCorruptedHpConsumePolicyDefinition> ClonePolicies(List<ElementGaugeCorruptedHpConsumePolicyDefinition> policies)
        {
            var cloned = new List<ElementGaugeCorruptedHpConsumePolicyDefinition>();
            if (policies == null || policies.Count == 0)
                return cloned;

            for (int i = 0; i < policies.Count; i++)
            {
                var policy = policies[i];
                if (policy == null)
                    continue;

                cloned.Add(policy.Clone());
            }

            return cloned;
        }

        private static List<ElementGaugeCorruptedHpConsumePolicyDefinition> CreateDefaultConsumePolicies(ConfigCommon.DamageType damageType)
        {
            return new List<ElementGaugeCorruptedHpConsumePolicyDefinition>
            {
                ElementGaugeCorruptedHpConsumePolicyDefinition.CreateIncomingDamageType(damageType),
                ElementGaugeCorruptedHpConsumePolicyDefinition.CreateIncomingGaugeApplication(damageType),
            };
        }

        public static List<ElementGaugeRuleDefinition> CreateDefaultPlayerRules()
        {
            return new List<ElementGaugeRuleDefinition>
            {
                CreateDefault(ConfigCommon.DamageType.Fire, false),
                CreateDefault(ConfigCommon.DamageType.Cold, false),
                CreateDefault(ConfigCommon.DamageType.Lightning, false),
                CreateDefault(ConfigCommon.DamageType.Poison, true),
            };
        }

        private static ElementGaugeRuleDefinition CreateDefault(ConfigCommon.DamageType damageType, bool createPoisonDefaults)
        {
            return new ElementGaugeRuleDefinition
            {
                damageType = damageType,
                gaugeMax = 100f,
                decayDelaySeconds = 2f,
                decayTickSeconds = 0.1f,
                decayPercentPerTick = 0.5f,
                // 기본 Heart 설정(100 x 4) 기준 2하트 = 800 HP
                corruptionHpAmount = createPoisonDefaults ? 800 : 0,
                blockAccumulationWhileTriggered = createPoisonDefaults,
                consumeCorruptedHpOnMatchingDamage = createPoisonDefaults,
                consumePolicies = createPoisonDefaults ? CreateDefaultConsumePolicies(damageType) : new List<ElementGaugeCorruptedHpConsumePolicyDefinition>(),
            };
        }
    }
}
