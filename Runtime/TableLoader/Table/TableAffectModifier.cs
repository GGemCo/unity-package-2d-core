using System;
using System.Collections.Generic;
using GGemCo2DAffect;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 Modifier 서브테이블 파서.
    /// - key: affect_modifier
    /// - 1행 헤더, 이후 탭(\t) 구분
    /// - AffectUid를 기준으로 여러 Modifier가 존재할 수 있으므로, 내부적으로 List로 보관한다.
    /// </summary>
    public sealed class TableAffectModifier : ITableParser
    {
        public string Key => ConfigAddressableTable.AffectModifier;

        private readonly Dictionary<int, List<AffectModifierDefinition>> _byAffectUid = new();

        public void LoadData(string content)
        {
            _byAffectUid.Clear();

            if (string.IsNullOrWhiteSpace(content))
                return;

            var lines = content.Split('\n');
            if (lines.Length <= 1) return;

            var headers = lines[0].Trim().Split('\t');
            if (headers.Length == 0) return;

            for (int i = 1; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var values = rawLine.Split('\t');
                if (values.Length < headers.Length)
                    Array.Resize(ref values, headers.Length);

                var row = new Dictionary<string, string>(headers.Length);
                for (int j = 0; j < headers.Length; j++)
                {
                    var v = values[j] ?? string.Empty;
                    row[headers[j].Trim()] = v.Trim();
                }

                int affectUid = MathHelper.ParseInt(row.GetValueOrDefault("AffectUid"));
                if (affectUid <= 0) continue;

                var def = BuildModifier(row);
                if (!_byAffectUid.TryGetValue(affectUid, out var list))
                {
                    list = new List<AffectModifierDefinition>(4);
                    _byAffectUid.Add(affectUid, list);
                }
                list.Add(def);
            }
        }

        public IReadOnlyList<AffectModifierDefinition> GetModifiers(int affectUid)
        {
            if (_byAffectUid.TryGetValue(affectUid, out var list))
                return list;
            return Array.Empty<AffectModifierDefinition>();
        }

        private static AffectModifierDefinition BuildModifier(Dictionary<string, string> row)
        {
            var mod = new AffectModifierDefinition
            {
                modifierId = MathHelper.ParseInt(row.GetValueOrDefault("ModifierId")),
                phase = EnumHelper.ConvertEnum<AffectPhase>(row.GetValueOrDefault("Phase")),
                kind = EnumHelper.ConvertEnum<ModifierKind>(row.GetValueOrDefault("Kind")),

                statId = row.GetValueOrDefault("StatId"),
                statValue = MathHelper.ParseFloat(row.GetValueOrDefault("StatValue")),
                statValueType = EnumHelper.ConvertEnum<StatValueType>(row.GetValueOrDefault("StatValueType")),
                statOperation = EnumHelper.ConvertEnum<StatOperation>(row.GetValueOrDefault("StatOperation")),

                damageTypeId = row.GetValueOrDefault("DamageTypeId"),
                damageBaseValue = MathHelper.ParseFloat(row.GetValueOrDefault("DamageBaseValue")),
                scalingStatId = row.GetValueOrDefault("ScalingStatId"),
                scalingCoefficient = MathHelper.ParseFloat(row.GetValueOrDefault("ScalingCoefficient")),
                canCrit = MathHelper.ParseInt(row.GetValueOrDefault("CanCrit")) != 0,
                isDot = MathHelper.ParseInt(row.GetValueOrDefault("IsDot")) != 0,

                stateId = row.GetValueOrDefault("StateId"),
                stateChance = MathHelper.ParseFloat(row.GetValueOrDefault("StateChance")),
                stateDurationOverride = MathHelper.ParseFloat(row.GetValueOrDefault("StateDurationOverride")),
            };

            return mod;
        }
    }
}
