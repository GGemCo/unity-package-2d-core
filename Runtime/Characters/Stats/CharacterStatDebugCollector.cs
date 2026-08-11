using System;
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
            /// <summary>
            /// 기존 디버그 표시 계약을 유지하면서 단일 스탯 항목을 생성합니다.
            /// </summary>
            /// <param name="displayName">HUD에 표시할 스탯 이름입니다.</param>
            /// <param name="baseStart">기본 항목 시작값입니다.</param>
            /// <param name="statStart">성장 항목 시작값입니다.</param>
            /// <param name="baseTotal">모든 Provider가 반영된 기본 항목 총합입니다.</param>
            /// <param name="statTotal">모든 Provider가 반영된 성장 항목 총합입니다.</param>
            /// <param name="finalValue">캐릭터별 파생 공식이 반영된 최종값입니다.</param>
            /// <param name="itemContribution">아이템 출처의 기여량입니다.</param>
            /// <param name="skillContribution">패시브 스킬 출처의 기여량입니다.</param>
            /// <param name="affectContribution">Affect 출처의 기여량입니다.</param>
            public StatLine(string displayName, int baseStart, int statStart, long baseTotal, long statTotal, long finalValue,
                long itemContribution, long skillContribution, long affectContribution)
                : this(
                    displayName,
                    baseStart,
                    statStart,
                    baseTotal,
                    statTotal,
                    finalValue,
                    baseStart,
                    0L,
                    itemContribution,
                    skillContribution,
                    affectContribution,
                    0L)
            {
            }

            /// <summary>
            /// 공용 Breakdown 결과가 반영된 단일 스탯 항목을 생성합니다.
            /// </summary>
            /// <param name="displayName">HUD에 표시할 스탯 이름입니다.</param>
            /// <param name="baseStart">기본 항목 시작값입니다.</param>
            /// <param name="statStart">성장 항목 시작값입니다.</param>
            /// <param name="baseTotal">모든 Provider가 반영된 기본 항목 총합입니다.</param>
            /// <param name="statTotal">모든 Provider가 반영된 성장 항목 총합입니다.</param>
            /// <param name="finalValue">캐릭터별 파생 공식이 반영된 최종값입니다.</param>
            /// <param name="baseValue">Provider 적용 전 기본값입니다.</param>
            /// <param name="growthContribution">성장 및 영구 출처의 기여량입니다.</param>
            /// <param name="itemContribution">아이템 출처의 기여량입니다.</param>
            /// <param name="passiveContribution">패시브 스킬 출처의 기여량입니다.</param>
            /// <param name="temporaryContribution">Affect와 런타임 임시 출처의 기여량입니다.</param>
            /// <param name="otherContribution">분류되지 않은 출처의 기여량입니다.</param>
            public StatLine(
                string displayName,
                int baseStart,
                int statStart,
                long baseTotal,
                long statTotal,
                long finalValue,
                long baseValue,
                long growthContribution,
                long itemContribution,
                long passiveContribution,
                long temporaryContribution,
                long otherContribution)
            {
                DisplayName = displayName;
                BaseStart = baseStart;
                StatStart = statStart;
                BaseTotal = baseTotal;
                StatTotal = statTotal;
                FinalValue = finalValue;
                BaseValue = baseValue;
                GrowthContribution = growthContribution;
                ItemContribution = itemContribution;
                PassiveContribution = passiveContribution;
                TemporaryContribution = temporaryContribution;
                OtherContribution = otherContribution;

                // 기존 외부 소비자가 사용하는 이름은 공용 Breakdown 출처에 대한 호환 별칭으로 유지합니다.
                SkillContribution = passiveContribution;
                AffectContribution = temporaryContribution;
            }

            /// <summary>HUD에 표시할 스탯 이름입니다.</summary>
            public string DisplayName { get; }

            /// <summary>기본 항목 시작값입니다.</summary>
            public int BaseStart { get; }

            /// <summary>성장 항목 시작값입니다.</summary>
            public int StatStart { get; }

            /// <summary>모든 Provider가 반영된 기본 항목 총합입니다.</summary>
            public long BaseTotal { get; }

            /// <summary>모든 Provider가 반영된 성장 항목 총합입니다.</summary>
            public long StatTotal { get; }

            /// <summary>캐릭터별 파생 공식이 반영된 최종값입니다.</summary>
            public long FinalValue { get; }

            /// <summary>Provider 적용 전 기본값입니다.</summary>
            public long BaseValue { get; }

            /// <summary>성장 스탯과 영구 스탯 포인트의 기여량입니다.</summary>
            public long GrowthContribution { get; }

            /// <summary>장비와 아이템의 기여량입니다.</summary>
            public long ItemContribution { get; }

            /// <summary>패시브 스킬의 기여량입니다.</summary>
            public long PassiveContribution { get; }

            /// <summary>Affect와 런타임 임시 효과의 기여량입니다.</summary>
            public long TemporaryContribution { get; }

            /// <summary>출처를 분류할 수 없는 Provider의 기여량입니다.</summary>
            public long OtherContribution { get; }

            /// <summary><see cref="PassiveContribution"/>의 기존 호환 별칭입니다.</summary>
            public long SkillContribution { get; }

            /// <summary><see cref="TemporaryContribution"/>의 기존 호환 별칭입니다.</summary>
            public long AffectContribution { get; }
        }

        /// <summary>
        /// 공격력/방어력/스태미나 디버그 스냅샷입니다.
        /// </summary>
        public readonly struct Snapshot
        {
            public Snapshot(StatLine atk, StatLine def, StatLine stamina,
                IReadOnlyList<DamageFormulaVariableDebugLine> formulaVariables)
            {
                Atk = atk;
                Def = def;
                Stamina = stamina;
                FormulaVariables = formulaVariables ?? Array.Empty<DamageFormulaVariableDebugLine>();
            }

            public StatLine Atk { get; }
            public StatLine Def { get; }
            public StatLine Stamina { get; }
            public IReadOnlyList<DamageFormulaVariableDebugLine> FormulaVariables { get; }
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

            return new Snapshot(
                BuildLine("ATK", CharacterStatBreakdownType.Attack, stat.BaseAtk, stat.StatAtk,
                    stat.TotalBaseAtk.Value, stat.TotalStatAtk.Value, stat.ResolvedAtk.Value, stat),
                BuildLine("DEF", CharacterStatBreakdownType.Defense, stat.BaseDef, stat.StatDef,
                    stat.TotalBaseDef.Value, stat.TotalStatDef.Value, stat.ResolvedDef.Value, stat),
                BuildLine("STAMINA", CharacterStatBreakdownType.Stamina, stat.BaseStamina, stat.StatStamina,
                    stat.TotalBaseStamina.Value, stat.TotalStatStamina.Value, stat.MaxStamina.Value, stat),
                BuildFormulaVariableLines(stat));
        }

        /// <summary>
        /// 공용 출처별 Breakdown을 디버그 HUD의 단일 표시 항목으로 변환합니다.
        /// </summary>
        /// <param name="displayName">HUD에 표시할 스탯 이름입니다.</param>
        /// <param name="statType">공용 Breakdown 대상 스탯 종류입니다.</param>
        /// <param name="baseStart">기본 항목 시작값입니다.</param>
        /// <param name="statStart">성장 항목 시작값입니다.</param>
        /// <param name="baseTotal">모든 Provider가 반영된 기본 항목 총합입니다.</param>
        /// <param name="statTotal">모든 Provider가 반영된 성장 항목 총합입니다.</param>
        /// <param name="finalValue">Breakdown 조회 실패 시 사용할 현재 최종값입니다.</param>
        /// <param name="stat">Breakdown을 조회할 캐릭터 스탯입니다.</param>
        /// <returns>HUD 출력에 사용할 단일 스탯 항목입니다.</returns>
        private static StatLine BuildLine(
            string displayName,
            CharacterStatBreakdownType statType,
            int baseStart,
            int statStart,
            long baseTotal,
            long statTotal,
            long finalValue,
            CharacterStat stat)
        {
            if (stat == null || !stat.TryGetStatBreakdown(statType, out CharacterStatBreakdown breakdown))
                return new StatLine(displayName, baseStart, statStart, baseTotal, statTotal, finalValue, 0L, 0L, 0L);

            return new StatLine(
                displayName,
                baseStart,
                statStart,
                baseTotal,
                statTotal,
                breakdown.FinalValue,
                breakdown.BaseValue,
                breakdown.GrowthContribution,
                breakdown.ItemContribution,
                breakdown.PassiveContribution,
                breakdown.TemporaryContribution,
                breakdown.OtherContribution);
        }

        /// <summary>
        /// 현재 캐릭터에 부착된 공식 변수 Provider를 출처별 합산 표시 항목으로 변환합니다.
        /// </summary>
        /// <param name="stat">공식 변수를 수집할 캐릭터 스탯입니다.</param>
        /// <returns>공식 변수 ID별 출처 합산 목록입니다.</returns>
        private static IReadOnlyList<DamageFormulaVariableDebugLine> BuildFormulaVariableLines(CharacterStat stat)
        {
            CharacterBase character = stat as CharacterBase;
            if (character == null)
                return Array.Empty<DamageFormulaVariableDebugLine>();

            var records = new List<DamageFormulaVariableDebugRecord>(8);
            IDamageFormulaVariableDebugProvider[] providers = character.GetComponents<IDamageFormulaVariableDebugProvider>();
            if (providers == null || providers.Length == 0)
                return Array.Empty<DamageFormulaVariableDebugLine>();

            for (int i = 0; i < providers.Length; i++)
            {
                providers[i]?.CollectDamageFormulaVariableDebugRecords(character, null, records);
            }

            if (records.Count == 0)
                return Array.Empty<DamageFormulaVariableDebugLine>();

            Dictionary<string, FormulaVariableAggregate> aggregates = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < records.Count; i++)
            {
                DamageFormulaVariableDebugRecord record = records[i];
                if (string.IsNullOrWhiteSpace(record.VariableKey))
                    continue;

                if (!aggregates.TryGetValue(record.VariableKey, out FormulaVariableAggregate aggregate))
                    aggregate = default;

                aggregate.Add(record.SourceType, record.Value);
                aggregates[record.VariableKey] = aggregate;
            }

            var lines = new List<DamageFormulaVariableDebugLine>(aggregates.Count);
            foreach (KeyValuePair<string, FormulaVariableAggregate> pair in aggregates)
            {
                FormulaVariableAggregate aggregate = pair.Value;
                lines.Add(new DamageFormulaVariableDebugLine(
                    pair.Key,
                    aggregate.Item,
                    aggregate.Skill,
                    aggregate.Affect,
                    aggregate.Item + aggregate.Skill + aggregate.Affect));
            }

            lines.Sort((left, right) => string.Compare(left.VariableKey, right.VariableKey, StringComparison.OrdinalIgnoreCase));
            return lines;
        }

        /// <summary>
        /// 공식 변수 출처별 합산값을 임시로 보관합니다.
        /// </summary>
        private struct FormulaVariableAggregate
        {
            public double Item;
            public double Skill;
            public double Affect;

            /// <summary>
            /// 출처 타입에 맞는 버킷에 값을 더합니다.
            /// </summary>
            public void Add(StatModifierDebugSourceType sourceType, double value)
            {
                switch (sourceType)
                {
                    case StatModifierDebugSourceType.Item:
                        Item += value;
                        break;
                    case StatModifierDebugSourceType.Skill:
                        Skill += value;
                        break;
                    case StatModifierDebugSourceType.Affect:
                        Affect += value;
                        break;
                }
            }
        }

    }
}
