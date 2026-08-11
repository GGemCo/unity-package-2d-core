using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 출처별 분해 정보를 제공할 대표 캐릭터 스탯입니다.
    /// </summary>
    public enum CharacterStatBreakdownType
    {
        /// <summary>최종 공격력입니다.</summary>
        Attack = 0,

        /// <summary>최종 방어력입니다.</summary>
        Defense = 1,

        /// <summary>최대 스태미나입니다.</summary>
        Stamina = 2,
    }

    /// <summary>
    /// 단일 캐릭터 스탯의 출처별 기여도와 최종값을 보관하는 읽기 전용 스냅샷입니다.
    /// </summary>
    /// <remarks>
    /// 기여도는 Base → Growth → Item → Passive → Temporary → Other 순서로 Provider를 누적한
    /// 워터폴 차이값입니다. 퍼센트 Modifier가 다른 출처의 Flat Modifier에 영향을 주는 경우에도
    /// 모든 기여도의 합이 <see cref="FinalValue"/>와 일치하도록 순서를 고정합니다.
    /// </remarks>
    public readonly struct CharacterStatBreakdown
    {
        /// <summary>
        /// 출처별 분해 결과를 생성합니다.
        /// </summary>
        /// <param name="statType">분해 대상 스탯 종류입니다.</param>
        /// <param name="baseValue">Provider 적용 전 기본값입니다.</param>
        /// <param name="growthContribution">성장 스탯과 영구 성장의 기여량입니다.</param>
        /// <param name="itemContribution">장비와 아이템의 기여량입니다.</param>
        /// <param name="passiveContribution">패시브 스킬의 기여량입니다.</param>
        /// <param name="temporaryContribution">Affect와 런타임 임시 효과의 기여량입니다.</param>
        /// <param name="otherContribution">출처가 분류되지 않은 Provider의 기여량입니다.</param>
        /// <param name="finalValue">모든 출처를 반영한 최종값입니다.</param>
        public CharacterStatBreakdown(
            CharacterStatBreakdownType statType,
            long baseValue,
            long growthContribution,
            long itemContribution,
            long passiveContribution,
            long temporaryContribution,
            long otherContribution,
            long finalValue)
        {
            StatType = statType;
            BaseValue = baseValue;
            GrowthContribution = growthContribution;
            ItemContribution = itemContribution;
            PassiveContribution = passiveContribution;
            TemporaryContribution = temporaryContribution;
            OtherContribution = otherContribution;
            FinalValue = finalValue;
        }

        /// <summary>분해 대상 스탯 종류입니다.</summary>
        public CharacterStatBreakdownType StatType { get; }

        /// <summary>Provider가 적용되기 전 기본 항목 값입니다.</summary>
        public long BaseValue { get; }

        /// <summary>성장 스탯 시작값과 영구 스탯 포인트가 더한 최종 기여량입니다.</summary>
        public long GrowthContribution { get; }

        /// <summary>장비 옵션과 영구 아이템 보너스가 더한 최종 기여량입니다.</summary>
        public long ItemContribution { get; }

        /// <summary>장착 패시브 스킬이 더한 최종 기여량입니다.</summary>
        public long PassiveContribution { get; }

        /// <summary>Affect와 런타임 임시 Modifier가 더한 최종 기여량입니다.</summary>
        public long TemporaryContribution { get; }

        /// <summary>출처 정보가 없는 확장 Provider가 더한 최종 기여량입니다.</summary>
        public long OtherContribution { get; }

        /// <summary>모든 Provider와 캐릭터별 파생 공식을 적용한 최종값입니다.</summary>
        public long FinalValue { get; }
    }

    /// <summary>
    /// 출처별 워터폴 계산에 포함할 Modifier Provider 그룹입니다.
    /// </summary>
    [Flags]
    internal enum StatModifierSourceMask
    {
        /// <summary>어떤 Provider도 포함하지 않습니다.</summary>
        None = 0,

        /// <summary>스탯 포인트와 영구 성장 Provider입니다.</summary>
        Persistent = 1 << 0,

        /// <summary>장비와 아이템 Provider입니다.</summary>
        Item = 1 << 1,

        /// <summary>패시브 스킬 Provider입니다.</summary>
        Passive = 1 << 2,

        /// <summary>Affect와 런타임 임시 Provider입니다.</summary>
        Temporary = 1 << 3,

        /// <summary>출처 정보가 없는 확장 Provider입니다.</summary>
        Other = 1 << 4,

        /// <summary>모든 Provider 그룹입니다.</summary>
        All = Persistent | Item | Passive | Temporary | Other,
    }

    public partial class CharacterStat
    {
        /// <summary>
        /// 현재 공격력, 방어력 또는 스태미나를 Base/Growth/Item/Passive/Temporary 출처로 분해합니다.
        /// </summary>
        /// <param name="statType">분해할 대표 스탯 종류입니다.</param>
        /// <param name="breakdown">현재 Provider 상태를 반영한 읽기 전용 분해 결과입니다.</param>
        /// <returns>지원하는 스탯 종류이면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGetStatBreakdown(
            CharacterStatBreakdownType statType,
            out CharacterStatBreakdown breakdown)
        {
            if (!TryGetBreakdownDefinition(
                    statType,
                    out string baseStatKey,
                    out string growthStatKey,
                    out int baseStartValue,
                    out int growthStartValue))
            {
                breakdown = default;
                return false;
            }

            IReadOnlyList<IStatModifierProvider> providers = _allProviders;

            long baseValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                0,
                providers,
                StatModifierSourceMask.None);

            StatModifierSourceMask includedSources = StatModifierSourceMask.Persistent;
            long growthValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                growthStartValue,
                providers,
                includedSources);

            includedSources |= StatModifierSourceMask.Item;
            long itemValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                growthStartValue,
                providers,
                includedSources);

            includedSources |= StatModifierSourceMask.Passive;
            long passiveValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                growthStartValue,
                providers,
                includedSources);

            includedSources |= StatModifierSourceMask.Temporary;
            long temporaryValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                growthStartValue,
                providers,
                includedSources);

            long finalValue = CalculateBreakdownStage(
                statType,
                baseStatKey,
                growthStatKey,
                baseStartValue,
                growthStartValue,
                providers,
                StatModifierSourceMask.All);

            breakdown = new CharacterStatBreakdown(
                statType,
                baseValue,
                growthValue - baseValue,
                itemValue - growthValue,
                passiveValue - itemValue,
                temporaryValue - passiveValue,
                finalValue - temporaryValue,
                finalValue);
            return true;
        }

        /// <summary>
        /// 대표 스탯 종류를 기존 BASE_*/STAT_* 키와 시작값으로 변환합니다.
        /// </summary>
        /// <param name="statType">변환할 대표 스탯 종류입니다.</param>
        /// <param name="baseStatKey">기본 항목에 대응하는 BASE_* 키입니다.</param>
        /// <param name="growthStatKey">성장 항목에 대응하는 STAT_* 키입니다.</param>
        /// <param name="baseStartValue">기본 항목 시작값입니다.</param>
        /// <param name="growthStartValue">성장 항목 시작값입니다.</param>
        /// <returns>지원하는 대표 스탯 종류이면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetBreakdownDefinition(
            CharacterStatBreakdownType statType,
            out string baseStatKey,
            out string growthStatKey,
            out int baseStartValue,
            out int growthStartValue)
        {
            switch (statType)
            {
                case CharacterStatBreakdownType.Attack:
                    baseStatKey = ConfigCommon.BaseStatAtk;
                    growthStatKey = ConfigCommon.StatusStatAtk;
                    baseStartValue = BaseAtk;
                    growthStartValue = StatAtk;
                    return true;

                case CharacterStatBreakdownType.Defense:
                    baseStatKey = ConfigCommon.BaseStatDef;
                    growthStatKey = ConfigCommon.StatusStatDef;
                    baseStartValue = BaseDef;
                    growthStartValue = StatDef;
                    return true;

                case CharacterStatBreakdownType.Stamina:
                    baseStatKey = ConfigCommon.BaseStatStamina;
                    growthStatKey = ConfigCommon.StatusStatStamina;
                    baseStartValue = BaseStamina;
                    growthStartValue = StatStamina;
                    return true;

                default:
                    baseStatKey = null;
                    growthStatKey = null;
                    baseStartValue = 0;
                    growthStartValue = 0;
                    return false;
            }
        }

        /// <summary>
        /// 지정한 출처까지 누적한 BASE_*/STAT_* 값을 캐릭터별 파생 공식에 전달합니다.
        /// </summary>
        /// <param name="statType">파생 공식을 선택할 대표 스탯 종류입니다.</param>
        /// <param name="baseStatKey">기본 항목에 대응하는 BASE_* 키입니다.</param>
        /// <param name="growthStatKey">성장 항목에 대응하는 STAT_* 키입니다.</param>
        /// <param name="baseStartValue">기본 항목 시작값입니다.</param>
        /// <param name="growthStartValue">성장 항목 시작값입니다.</param>
        /// <param name="providers">현재 계산에 참여하는 Provider 목록입니다.</param>
        /// <param name="includedSources">이번 단계에 포함할 Provider 출처입니다.</param>
        /// <returns>지정한 출처까지 누적한 파생 스탯 값입니다.</returns>
        private long CalculateBreakdownStage(
            CharacterStatBreakdownType statType,
            string baseStatKey,
            string growthStatKey,
            int baseStartValue,
            int growthStartValue,
            IReadOnlyList<IStatModifierProvider> providers,
            StatModifierSourceMask includedSources)
        {
            long totalBaseValue = CalculateStatValueForSources(
                baseStatKey,
                baseStartValue,
                providers,
                includedSources);
            long totalGrowthValue = CalculateStatValueForSources(
                growthStatKey,
                growthStartValue,
                providers,
                includedSources);

            return statType switch
            {
                CharacterStatBreakdownType.Attack => CalculateResolvedAtkValue(totalBaseValue, totalGrowthValue),
                CharacterStatBreakdownType.Defense => CalculateResolvedDefValue(totalBaseValue, totalGrowthValue),
                CharacterStatBreakdownType.Stamina => CalculateMaxStaminaValue(totalBaseValue, totalGrowthValue),
                _ => 0L,
            };
        }

        /// <summary>
        /// 지정한 출처에 해당하는 Provider만 합산하여 단일 스탯 키의 값을 계산합니다.
        /// </summary>
        /// <param name="statKey">계산할 BASE_* 또는 STAT_* 키입니다.</param>
        /// <param name="startValue">Provider 적용 전 시작값입니다.</param>
        /// <param name="providers">현재 계산에 참여하는 Provider 목록입니다.</param>
        /// <param name="includedSources">이번 단계에 포함할 Provider 출처입니다.</param>
        /// <returns>선택한 출처의 Flat/Percent Modifier가 반영된 값입니다.</returns>
        private static long CalculateStatValueForSources(
            string statKey,
            int startValue,
            IReadOnlyList<IStatModifierProvider> providers,
            StatModifierSourceMask includedSources)
        {
            int flat = 0;
            float percent = 0f;

            if (providers != null && includedSources != StatModifierSourceMask.None)
            {
                for (int i = 0; i < providers.Count; i++)
                {
                    IStatModifierProvider provider = providers[i];
                    if (provider == null ||
                        (ResolveSourceMask(provider) & includedSources) == StatModifierSourceMask.None)
                    {
                        continue;
                    }

                    if (provider.Flat != null && provider.Flat.TryGetValue(statKey, out int flatValue))
                        flat += flatValue;

                    if (provider.Percent != null && provider.Percent.TryGetValue(statKey, out float percentValue))
                        percent += percentValue;
                }
            }

            return StatCalculator.CalculateFinalFromModifiers(startValue, flat, percent);
        }

        /// <summary>
        /// Modifier Provider의 출처 정보를 Breakdown 누적 단계로 변환합니다.
        /// </summary>
        /// <param name="provider">출처를 확인할 Modifier Provider입니다.</param>
        /// <returns>Provider가 속한 워터폴 출처 그룹입니다.</returns>
        private static StatModifierSourceMask ResolveSourceMask(IStatModifierProvider provider)
        {
            if (provider is not IStatModifierDebugSource source)
                return StatModifierSourceMask.Other;

            return source.DebugSourceType switch
            {
                StatModifierDebugSourceType.Persistent => StatModifierSourceMask.Persistent,
                StatModifierDebugSourceType.Item => StatModifierSourceMask.Item,
                StatModifierDebugSourceType.Skill => StatModifierSourceMask.Passive,
                StatModifierDebugSourceType.Affect => StatModifierSourceMask.Temporary,
                StatModifierDebugSourceType.Runtime => StatModifierSourceMask.Temporary,
                _ => StatModifierSourceMask.Other,
            };
        }
    }
}
