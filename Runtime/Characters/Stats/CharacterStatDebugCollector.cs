using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 스탯 디버그 HUD에서 사용할 공격력/방어력/스태미나 스냅샷을 구성합니다.
    /// </summary>
    public static class CharacterStatDebugCollector
    {
        /// <summary>
        /// 디버그 HUD에 표시할 단일 스탯 항목입니다.
        /// </summary>
        public readonly struct StatLine
        {
            public StatLine(string displayName, int baseStart, int statStart, long baseTotal, long statTotal, long finalValue,
                long itemContribution, long skillContribution, long affectContribution)
            {
                DisplayName = displayName;
                BaseStart = baseStart;
                StatStart = statStart;
                BaseTotal = baseTotal;
                StatTotal = statTotal;
                FinalValue = finalValue;
                ItemContribution = itemContribution;
                SkillContribution = skillContribution;
                AffectContribution = affectContribution;
            }

            public string DisplayName { get; }
            public int BaseStart { get; }
            public int StatStart { get; }
            public long BaseTotal { get; }
            public long StatTotal { get; }
            public long FinalValue { get; }
            public long ItemContribution { get; }
            public long SkillContribution { get; }
            public long AffectContribution { get; }
        }

        /// <summary>
        /// 공격력/방어력/스태미나 디버그 스냅샷입니다.
        /// </summary>
        public readonly struct Snapshot
        {
            public Snapshot(StatLine atk, StatLine def, StatLine stamina)
            {
                Atk = atk;
                Def = def;
                Stamina = stamina;
            }

            public StatLine Atk { get; }
            public StatLine Def { get; }
            public StatLine Stamina { get; }
        }

        /// <summary>
        /// 현재 캐릭터 스탯에서 디버그 표시용 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="stat">스냅샷을 생성할 캐릭터 스탯입니다.</param>
        /// <returns>공격력/방어력/스태미나 스냅샷입니다.</returns>
        public static Snapshot BuildSnapshot(CharacterStat stat)
        {
            if (stat == null)
                return default;

            IReadOnlyList<IStatModifierProvider> providers = stat.GetStatModifierProvidersForDebug();

            return new Snapshot(
                BuildLine("ATK", stat.BaseAtk, stat.StatAtk, ConfigCommon.BaseStatAtk, ConfigCommon.StatusStatAtk,
                    stat.TotalBaseAtk.Value, stat.TotalStatAtk.Value, stat.ResolvedAtk.Value, providers),
                BuildLine("DEF", stat.BaseDef, stat.StatDef, ConfigCommon.BaseStatDef, ConfigCommon.StatusStatDef,
                    stat.TotalBaseDef.Value, stat.TotalStatDef.Value, stat.ResolvedDef.Value, providers),
                BuildLine("STAMINA", stat.BaseStamina, stat.StatStamina, ConfigCommon.BaseStatStamina, ConfigCommon.StatusStatStamina,
                    stat.TotalBaseStamina.Value, stat.TotalStatStamina.Value, stat.MaxStamina.Value, providers));
        }

        /// <summary>
        /// Base/Stat 키 쌍을 하나의 표시 항목으로 변환합니다.
        /// </summary>
        private static StatLine BuildLine(string displayName, int baseStart, int statStart, string baseKey, string statKey,
            long baseTotal, long statTotal, long finalValue, IReadOnlyList<IStatModifierProvider> providers)
        {
            long item = CalculateSourceContribution(baseKey, baseStart, providers, StatModifierDebugSourceType.Item)
                + CalculateSourceContribution(statKey, statStart, providers, StatModifierDebugSourceType.Item);
            long skill = CalculateSourceContribution(baseKey, baseStart, providers, StatModifierDebugSourceType.Skill)
                + CalculateSourceContribution(statKey, statStart, providers, StatModifierDebugSourceType.Skill);
            long affect = CalculateSourceContribution(baseKey, baseStart, providers, StatModifierDebugSourceType.Affect)
                + CalculateSourceContribution(statKey, statStart, providers, StatModifierDebugSourceType.Affect);

            return new StatLine(displayName, baseStart, statStart, baseTotal, statTotal, finalValue, item, skill, affect);
        }

        /// <summary>
        /// 특정 출처가 지정 스탯 키에 기여한 증가량을 계산합니다.
        /// </summary>
        /// <remarks>
        /// Percent 기여량은 전체 Flat이 반영된 기준값에 해당 출처 Percent만 적용하여 계산합니다.
        /// 이 방식은 Provider별 Percent가 합산되는 현재 StatCalculator 규칙과 동일한 기준입니다.
        /// </remarks>
        private static long CalculateSourceContribution(string statKey, int startValue, IReadOnlyList<IStatModifierProvider> providers,
            StatModifierDebugSourceType sourceType)
        {
            if (providers == null || string.IsNullOrEmpty(statKey))
                return 0L;

            int totalFlat = 0;
            int sourceFlat = 0;
            float sourcePercent = 0f;

            for (int i = 0; i < providers.Count; i++)
            {
                IStatModifierProvider provider = providers[i];
                if (provider == null)
                    continue;

                int flat = 0;
                if (provider.Flat != null && provider.Flat.TryGetValue(statKey, out int providerFlat))
                    flat = providerFlat;

                totalFlat += flat;

                if (provider is not IStatModifierDebugSource debugSource || debugSource.DebugSourceType != sourceType)
                    continue;

                sourceFlat += flat;
                if (provider.Percent != null && provider.Percent.TryGetValue(statKey, out float providerPercent))
                    sourcePercent += providerPercent;
            }

            double percentContribution = (startValue + totalFlat) * (sourcePercent / 100d);
            double resolved = sourceFlat + percentContribution;
            if (resolved >= long.MaxValue)
                return long.MaxValue;
            if (resolved <= long.MinValue)
                return long.MinValue;

            return (long)System.Math.Round(resolved);
        }
    }
}
