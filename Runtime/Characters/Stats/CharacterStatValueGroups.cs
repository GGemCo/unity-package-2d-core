using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 스탯 값이 속한 계산 그룹입니다.
    /// </summary>
    public enum CharacterStatValueGroup
    {
        /// <summary>
        /// 전투/이동/자원 계산에 직접 사용되는 기본 항목입니다.
        /// </summary>
        BaseAttribute = 0,

        /// <summary>
        /// 스탯 포인트, 성장, 장비/패시브의 스탯 옵션이 누적되는 스탯 항목입니다.
        /// </summary>
        GrowthStat = 1,
    }

    /// <summary>
    /// 캐릭터의 기본 항목 시작값입니다.
    /// </summary>
    /// <remarks>
    /// 기본 항목은 공격력/방어력/HP 같은 전투 계산의 원천 값이며,
    /// 스탯 포인트로 직접 증가하는 성장 스탯과 분리해서 관리합니다.
    /// </remarks>
    [Serializable]
    public struct CharacterBaseAttributeValues
    {
        [Tooltip("기본 공격력")]
        public int atk;
        [Tooltip("기본 방어력")]
        public int def;
        [Tooltip("기본 HP")]
        public int hp;
        [Tooltip("기본 MP")]
        public int mp;
        [Tooltip("기본 스태미나")]
        public int stamina;
        [Tooltip("기본 슈퍼아머")]
        public int superArmor;
        [Tooltip("기본 이동속도")]
        public int moveSpeed;
        [Tooltip("기본 공격속도")]
        public int attackSpeed;
        [Tooltip("기본 크리티컬 피해량")]
        public int criticalDamage;
        [Tooltip("기본 크리티컬 확률")]
        public int criticalProbability;
        [Tooltip("기본 화염 저항")]
        public int registFire;
        [Tooltip("기본 냉기 저항")]
        public int registCold;
        [Tooltip("기본 번개 저항")]
        public int registLightning;
        [Tooltip("기본 독 저항")]
        public int registPoison;
        [Tooltip("기본 화염 데미지")]
        public int damageFire;
        [Tooltip("기본 냉기 데미지")]
        public int damageCold;
        [Tooltip("기본 번개 데미지")]
        public int damageLightning;
        [Tooltip("기본 독 데미지")]
        public int damagePoison;
        [Tooltip("기본 이동 스텝")]
        public int moveStep;
    }

    /// <summary>
    /// 캐릭터의 스탯 항목 시작값입니다.
    /// </summary>
    /// <remarks>
    /// 스탯 항목은 스탯 포인트와 장비/패시브의 성장 스탯 옵션이 누적되는 값입니다.
    /// 실제 데미지 반영 비율은 <see cref="CalculateManager"/>의 데미지 공식에서 결정합니다.
    /// </remarks>
    [Serializable]
    public struct CharacterGrowthStatValues
    {
        [Tooltip("공격 계열 스탯")]
        public int atk;
        [Tooltip("방어 계열 스탯")]
        public int def;
        [Tooltip("HP 계열 스탯")]
        public int hp;
        [Tooltip("MP 계열 스탯")]
        public int mp;
        [Tooltip("스태미나 계열 스탯")]
        public int stamina;
    }
}
