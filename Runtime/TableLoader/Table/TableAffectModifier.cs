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
                ModifierId = MathHelper.ParseInt(row.GetValueOrDefault("ModifierId")),
                Phase = EnumHelper.ConvertEnum<AffectPhase>(row.GetValueOrDefault("Phase")),
                Kind = EnumHelper.ConvertEnum<ModifierKind>(row.GetValueOrDefault("Kind")),

                StatId = row.GetValueOrDefault("StatId"),
                StatValue = MathHelper.ParseFloat(row.GetValueOrDefault("StatValue")),
                StatValueType = EnumHelper.ConvertEnum<ValueType>(row.GetValueOrDefault("StatValueType")),
                StatOperation = EnumHelper.ConvertEnum<StatOperation>(row.GetValueOrDefault("StatOperation")),

                DamageTypeId = row.GetValueOrDefault("DamageTypeId"),
                DamageBaseValue = MathHelper.ParseFloat(row.GetValueOrDefault("DamageBaseValue")),
                ScalingStatId = row.GetValueOrDefault("ScalingStatId"),
                ScalingCoefficient = MathHelper.ParseFloat(row.GetValueOrDefault("ScalingCoefficient")),
                CanCrit = MathHelper.ParseInt(row.GetValueOrDefault("CanCrit")) != 0,
                IsDot = MathHelper.ParseInt(row.GetValueOrDefault("IsDot")) != 0,

                StateId = row.GetValueOrDefault("StateId"),
                StateChance = MathHelper.ParseFloat(row.GetValueOrDefault("StateChance")),
                StateDurationOverride = MathHelper.ParseFloat(row.GetValueOrDefault("StateDurationOverride")),
            };

            return mod;
        }
    }
}
