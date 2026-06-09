using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기본 콤보 공격 단계에서 사용할 데미지 공식 설정입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Control 패키지의 공격 콤보 설정 자산을 직접 참조하지 않기 때문에,
    /// 상위 패키지는 이 값 구조체를 통해 현재 콤보 단계의 공식 설정만 전달합니다.
    /// </remarks>
    [Serializable]
    public struct AttackComboDamageFormulaSettings
    {
        [Tooltip("기본 물리 공격 공식 대신 커스텀 공식 설정을 사용할지 여부입니다.")]
        public bool useCustomFormula;

        [Tooltip("damage_formula 테이블에 등록된 Poly 공식 키입니다. 비어 있으면 기본 배율 공식을 사용합니다.")]
        public string formulaKey;

        [Tooltip("공식에 전달할 기준 데미지입니다. 0 이하이면 공격자의 ResolvedAtk를 사용합니다.")]
        public float baseDamage;

        [Tooltip("기본 공격 데미지 배율입니다. 1은 100%입니다.")]
        public float damageRate;

        [Tooltip("이벤트 단위 추가 배율입니다. 1은 100%입니다.")]
        public float eventMultiplier;

        [Tooltip("실행 옵션 단위 추가 배율입니다. 1은 100%입니다.")]
        public float optionMultiplier;

        [Tooltip("Poly 공식 변수로 전달할 버프 배율입니다. 기본 배율 공식에는 직접 곱하지 않습니다.")]
        public float buffRate;

        [Tooltip("이 공격의 데미지 타입입니다. None이면 Physic으로 보정합니다.")]
        public ConfigCommon.DamageType damageType;

        [Tooltip("크리티컬 판정을 적용할지 여부입니다.")]
        public bool rollCritical;

        /// <summary>
        /// 기존 기본 물리 공격과 동일하게 동작하는 기본 설정을 반환합니다.
        /// </summary>
        public static AttackComboDamageFormulaSettings Default => new AttackComboDamageFormulaSettings
        {
            useCustomFormula = false,
            formulaKey = string.Empty,
            baseDamage = 0f,
            damageRate = 1f,
            eventMultiplier = 1f,
            optionMultiplier = 1f,
            buffRate = 0f,
            damageType = ConfigCommon.DamageType.Physic,
            rollCritical = true
        };

        /// <summary>
        /// 공격자의 현재 스탯을 기준으로 공식에 전달할 기준 데미지를 계산합니다.
        /// </summary>
        /// <param name="attacker">공격자 스탯입니다.</param>
        /// <returns>공식 입력용 기준 데미지입니다.</returns>
        public readonly double ResolveBaseDamage(CharacterStat attacker)
        {
            if (baseDamage > 0f)
                return baseDamage;

            return attacker != null ? Math.Max(0d, attacker.ResolvedAtk.Value) : 0d;
        }

        /// <summary>
        /// 기본 공격 데미지 배율을 1 이상 기본값으로 보정합니다.
        /// </summary>
        public readonly double ResolveDamageRate()
        {
            return damageRate > 0f ? damageRate : 1d;
        }

        /// <summary>
        /// 이벤트 배율을 1 이상 기본값으로 보정합니다.
        /// </summary>
        public readonly double ResolveEventMultiplier()
        {
            return eventMultiplier > 0f ? eventMultiplier : 1d;
        }

        /// <summary>
        /// 실행 옵션 배율을 1 이상 기본값으로 보정합니다.
        /// </summary>
        public readonly double ResolveOptionMultiplier()
        {
            return optionMultiplier > 0f ? optionMultiplier : 1d;
        }

        /// <summary>
        /// 버프 배율을 0 이상 값으로 보정합니다.
        /// </summary>
        public readonly double ResolveBuffRate()
        {
            return buffRate > 0f ? buffRate : 0d;
        }

        /// <summary>
        /// 데미지 타입이 비어 있을 때 기본 물리 타입으로 보정합니다.
        /// </summary>
        public readonly ConfigCommon.DamageType ResolveDamageType()
        {
            return damageType == ConfigCommon.DamageType.None ? ConfigCommon.DamageType.Physic : damageType;
        }

        /// <summary>
        /// 현재 설정에 유효한 Poly 공식 키가 있는지 확인합니다.
        /// </summary>
        public readonly bool HasFormulaKey()
        {
            return !string.IsNullOrWhiteSpace(formulaKey);
        }
    }
}
