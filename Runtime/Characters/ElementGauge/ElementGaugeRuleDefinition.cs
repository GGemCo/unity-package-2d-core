using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 데미지 누적 게이지 규칙 1건입니다.
    /// </summary>
    /// <remarks>
    /// Core는 이 규칙으로 누적량, 감쇠, 임계 도달 여부만 계산합니다.
    /// 임계 도달 후 효과와 임계 상태 재피격 처리는 프로젝트별 핸들러에서 확장합니다.
    /// </remarks>
    [Serializable]
    public sealed class ElementGaugeRuleDefinition
    {
        [Tooltip("게이지를 누적시킬 데미지 타입입니다.")]
        public ConfigCommon.DamageType damageType = ConfigCommon.DamageType.None;

        [Min(1f)]
        [Tooltip("게이지 최대값입니다. 실제로 받은 같은 속성 데미지가 이 값까지 누적되면 임계 이벤트가 발생합니다.")]
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

        /// <summary>
        /// 런타임에서 사용할 복사본을 생성합니다.
        /// </summary>
        /// <returns>동일한 값을 가진 새 규칙 인스턴스입니다.</returns>
        public ElementGaugeRuleDefinition Clone()
        {
            return new ElementGaugeRuleDefinition
            {
                damageType = damageType,
                gaugeMax = gaugeMax,
                decayDelaySeconds = decayDelaySeconds,
                decayTickSeconds = decayTickSeconds,
                decayPercentPerTick = decayPercentPerTick,
            };
        }

        /// <summary>
        /// 기본 플레이어 속성 게이지 규칙을 생성합니다.
        /// </summary>
        /// <returns>화염, 냉기, 번개, 독 속성 게이지 기본 규칙 목록입니다.</returns>
        public static List<ElementGaugeRuleDefinition> CreateDefaultPlayerRules()
        {
            return new List<ElementGaugeRuleDefinition>
            {
                CreateDefault(ConfigCommon.DamageType.Fire),
                CreateDefault(ConfigCommon.DamageType.Cold),
                CreateDefault(ConfigCommon.DamageType.Lightning),
                CreateDefault(ConfigCommon.DamageType.Poison),
            };
        }

        /// <summary>
        /// 지정한 속성 타입의 기본 규칙을 생성합니다.
        /// </summary>
        /// <param name="damageType">누적할 속성 타입입니다.</param>
        /// <returns>기본 감쇠 정책이 적용된 규칙입니다.</returns>
        private static ElementGaugeRuleDefinition CreateDefault(ConfigCommon.DamageType damageType)
        {
            return new ElementGaugeRuleDefinition
            {
                damageType = damageType,
                gaugeMax = 100f,
                decayDelaySeconds = 2f,
                decayTickSeconds = 0.1f,
                decayPercentPerTick = 0.5f,
            };
        }
    }
}
