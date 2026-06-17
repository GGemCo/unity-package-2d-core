using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어의 기본 항목, 성장 스탯, 스탯 포인트 정책을 보관하는 설정 자산입니다.
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObject.PlayerStat.FileName, menuName = ConfigScriptableObject.PlayerStat.MenuName, order = ConfigScriptableObject.PlayerStat.Ordering)]
    public class GGemCoPlayerStatSettings : ScriptableObject
    {
        /// <summary>
        /// 스탯 포인트 획득 경로 정책입니다.
        /// </summary>
        public enum StatPointAcquirePolicy
        {
            [Tooltip("경험치 레벨업으로만 스탯 포인트를 획득합니다.")]
            LevelUpOnly = 0,
            [Tooltip("골드 구매로만 스탯 포인트를 획득합니다.")]
            GoldPurchaseOnly = 1,
            [Tooltip("경험치 레벨업과 골드 구매를 모두 허용합니다.")]
            LevelUpAndGoldPurchase = 2,
        }

        /// <summary>
        /// 스탯 포인트 투자 시 플레이어 레벨 증가 정책입니다.
        /// </summary>
        public enum StatPointLevelUpOnInvestPolicy
        {
            [Tooltip("스탯 포인트를 투자해도 플레이어 레벨은 오르지 않습니다.")]
            None = 0,
            [Tooltip("스탯 포인트를 1 투자할 때마다 플레이어 레벨을 1 올립니다.")]
            IncreaseLevelByInvestedPoints = 1,
        }

        /// <summary>
        /// 이미 적용된 스탯 포인트 회수 정책입니다.
        /// </summary>
        public enum StatPointRefundPolicy
        {
            [Tooltip("이미 커밋된 스탯 포인트를 다시 회수할 수 있습니다.")]
            AllowCommittedRefund = 0,
            [Tooltip("이미 커밋된 스탯 포인트는 회수할 수 없고, 이번 드래프트에서 새로 넣은 포인트만 취소할 수 있습니다.")]
            DisallowCommittedRefund = 1,
        }

        /// <summary>
        /// TotalStat* 값을 Base* 계열 파생값에 더하는 변환 규칙입니다.
        /// </summary>
        [Serializable]
        public struct StatPointBonus
        {
            [Tooltip("TotalStat* 1당 Base* 계열에 더할 변환 방식입니다. Flat은 고정값, PercentOfMax는 TotalBase* 기준 % 보너스입니다.")]
            public ConfigCommon.CalculateType mode;

            [Tooltip("TotalStat* 1당 Base* 계열에 더할 값입니다. Flat 예) 10 = TotalStat 1당 +10, PercentOfMax 예) 1.5 = TotalBase의 1.5%")]
            public float valuePerPoint;
        }

        [Header("기본 항목 시작값")]
        [Tooltip("장비/패시브의 BASE_* 옵션이 누적되는 플레이어 기본 항목 시작값입니다.")]
        public CharacterBaseAttributeValues baseAttributes;

        [Header("스탯 항목 시작값")]
        [Tooltip("스탯 포인트와 STAT_* 옵션이 누적되는 플레이어 스탯 항목 시작값입니다.")]
        public CharacterGrowthStatValues stats;

        [Header("스탯 포인트")]
        [Tooltip("스탯 포인트 리셋 비용")]
        public int statPointResetCost;

        [Tooltip("스탯 포인트 획득 경로 정책입니다.")]
        public StatPointAcquirePolicy statPointAcquirePolicy = StatPointAcquirePolicy.LevelUpOnly;

        [Tooltip("스탯 포인트 투자 시 플레이어 레벨 증가 정책입니다.")]
        public StatPointLevelUpOnInvestPolicy statPointLevelUpOnInvestPolicy = StatPointLevelUpOnInvestPolicy.None;

        [Tooltip("이미 적용된 스탯 포인트를 다시 회수할 수 있는지 결정합니다.")]
        public StatPointRefundPolicy statPointRefundPolicy = StatPointRefundPolicy.AllowCommittedRefund;

        [Tooltip("GoldPurchaseOnly 정책에서는 런타임에서 Gold로 고정됩니다. LevelUpAndGoldPurchase 정책의 직접 구매 버튼에서 사용할 재화 타입입니다.")]
        public CurrencyConstants.Type statPointPurchaseCurrencyType = CurrencyConstants.Type.Gold;

        [Tooltip("LevelUpAndGoldPurchase의 직접 구매 버튼 기본 가격입니다. GoldPurchaseOnly에서는 exp 테이블의 NeedStatPointGold 값을 우선 사용하고, 값이 없을 때 fallback으로 사용합니다.")]
        [Min(0)]
        public int statPointPurchaseCurrencyValue = 0;

        [Tooltip("새 게임 시작 시 지급되는 스탯 포인트")]
        public int statPointInitial;

        [Tooltip("레벨업 1회당 지급되는 스탯 포인트")]
        public int statPointPerLevel;

        [Tooltip("TotalStatAtk 1당 ResolvedAtk에 더할 BaseAtk 계열 변환량입니다.")]
        public StatPointBonus statPointAtk;

        [Tooltip("TotalStatDef 1당 ResolvedDef에 더할 BaseDef 계열 변환량입니다.")]
        public StatPointBonus statPointDef;

        [Tooltip("TotalStatHp 1당 MaxHp에 더할 BaseHp 계열 변환량입니다.")]
        public StatPointBonus statPointHp;

        [Tooltip("TotalStatMp 1당 MaxMp에 더할 BaseMp 계열 변환량입니다.")]
        public StatPointBonus statPointMp;

        [Tooltip("TotalStatStamina 1당 MaxStamina에 더할 BaseStamina 계열 변환량입니다.")]
        public StatPointBonus statPointStamina;

        [Header("Player Stat Debug")]
        [SerializeField, Tooltip("플레이어 스탯 디버그 기능 전체 On/Off")]
        private bool enablePlayerStatDebug;

        /// <summary>플레이어 스탯 디버그 기능 전체 사용 여부입니다.</summary>
        public bool EnablePlayerStatDebug => DebugOptionRuntimeUtility.Resolve(enablePlayerStatDebug);

        [SerializeField, Tooltip("플레이어 스탯 디버그 HUD 출력 On/Off")]
        private bool enablePlayerStatDebugHud;

        /// <summary>디버그 HUD에 플레이어 스탯 정보를 표시할지 여부입니다.</summary>
        public bool EnablePlayerStatDebugHud => EnablePlayerStatDebug && DebugOptionRuntimeUtility.Resolve(enablePlayerStatDebugHud);

        [SerializeField, Tooltip("공격력/방어력/스태미나의 Item/Skill/Affect 증가량 출력")]
        private bool enablePlayerStatContributionDebug;

        /// <summary>스탯 출처별 증가량을 표시할지 여부입니다.</summary>
        public bool EnablePlayerStatContributionDebug => EnablePlayerStatDebugHud && DebugOptionRuntimeUtility.Resolve(enablePlayerStatContributionDebug);

        [SerializeField, Tooltip("어펙트/패시브 스킬 공식 변수 출력")]
        private bool enableFormulaVariableDebug;

        /// <summary>현재 적용 중인 공식 변수를 표시할지 여부입니다.</summary>
        public bool EnableFormulaVariableDebug => EnablePlayerStatDebugHud && DebugOptionRuntimeUtility.Resolve(enableFormulaVariableDebug);

        [SerializeField, Tooltip("공식 변수의 Item/Skill/Affect 출처별 증가량 출력")]
        private bool enableFormulaVariableContributionDebug;

        /// <summary>공식 변수의 출처별 증가량을 표시할지 여부입니다.</summary>
        public bool EnableFormulaVariableContributionDebug => EnableFormulaVariableDebug && DebugOptionRuntimeUtility.Resolve(enableFormulaVariableContributionDebug);

        [SerializeField, Tooltip("플레이어 점프/공중 상태 출력")]
        private bool enablePlayerJumpStateDebug;

        /// <summary>플레이어의 점프 및 공중 상태를 표시할지 여부입니다.</summary>
        public bool EnablePlayerJumpStateDebug => EnablePlayerStatDebugHud && DebugOptionRuntimeUtility.Resolve(enablePlayerJumpStateDebug);

        [SerializeField, Tooltip("마지막 최종 데미지 계산 결과 출력")]
        private bool enablePlayerFinalDamageDebug;

        /// <summary>마지막 최종 데미지 결과를 표시할지 여부입니다.</summary>
        public bool EnablePlayerFinalDamageDebug => EnablePlayerStatDebugHud && DebugOptionRuntimeUtility.Resolve(enablePlayerFinalDamageDebug);

        [SerializeField, Tooltip("마지막 최종 데미지에 사용된 공식 변수 출력")]
        private bool enableLastDamageFormulaVariableDebug;

        /// <summary>마지막 데미지 계산에 실제 사용된 공식 변수를 표시할지 여부입니다.</summary>
        public bool EnableLastDamageFormulaVariableDebug => EnablePlayerFinalDamageDebug && DebugOptionRuntimeUtility.Resolve(enableLastDamageFormulaVariableDebug);

        [Tooltip("플레이어 스탯 디버그 HUD 갱신 주기입니다.")]
        [Min(0.05f)]
        public float playerStatDebugHudUpdateInterval = 0.2f;

        /// <summary>
        /// 생성 직후 기본 스탯 값을 초기화합니다.
        /// </summary>
        private void Reset()
        {
            baseAttributes = CreateDefaultBaseAttributes();
            stats = CreateDefaultGrowthStats();

            statPointAcquirePolicy = StatPointAcquirePolicy.LevelUpOnly;
            statPointLevelUpOnInvestPolicy = StatPointLevelUpOnInvestPolicy.None;
            statPointRefundPolicy = StatPointRefundPolicy.AllowCommittedRefund;
            statPointPurchaseCurrencyType = CurrencyConstants.Type.Gold;
            statPointPurchaseCurrencyValue = 0;
            statPointInitial = 0;
            statPointPerLevel = 0;

            statPointAtk = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 1f };
            statPointDef = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 1f };
            statPointHp = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 10f };
            statPointMp = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 5f };
            statPointStamina = new StatPointBonus { mode = ConfigCommon.CalculateType.Flat, valuePerPoint = 5f };

            enablePlayerStatDebug = false;
            enablePlayerStatDebugHud = false;
            enablePlayerStatContributionDebug = true;
            enableFormulaVariableDebug = true;
            enableFormulaVariableContributionDebug = true;
            enablePlayerJumpStateDebug = true;
            enablePlayerFinalDamageDebug = true;
            enableLastDamageFormulaVariableDebug = true;
            playerStatDebugHudUpdateInterval = 0.2f;
        }

        /// <summary>
        /// 플레이어 기본 항목의 런타임 기본값을 생성합니다.
        /// </summary>
        /// <returns>기본 항목 시작값입니다.</returns>
        public static CharacterBaseAttributeValues CreateDefaultBaseAttributes()
        {
            return new CharacterBaseAttributeValues
            {
                atk = 100,
                def = 100,
                hp = 100,
                mp = 100,
                stamina = 100,
                superArmor = 0,
                moveSpeed = 100,
                attackSpeed = 100,
                criticalDamage = 100,
                criticalProbability = 0,
                registFire = 0,
                registCold = 0,
                registLightning = 0,
                registPoison = 0,
                damageFire = 0,
                damageCold = 0,
                damageLightning = 0,
                damagePoison = 0,
                moveStep = 100,
            };
        }

        /// <summary>
        /// 플레이어 성장 스탯의 런타임 기본값을 생성합니다.
        /// </summary>
        /// <returns>성장 스탯 시작값입니다.</returns>
        public static CharacterGrowthStatValues CreateDefaultGrowthStats()
        {
            return new CharacterGrowthStatValues
            {
                atk = 100,
                def = 100,
                hp = 100,
                mp = 100,
                stamina = 100,
            };
        }
    }
}
