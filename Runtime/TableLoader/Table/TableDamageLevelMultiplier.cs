using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공격자와 대상의 레벨 차이에 따른 데미지 배율 row입니다.
    /// </summary>
    public sealed class StruckTableDamageLevelMultiplier
    {
        public int Uid;
        public int MinLevelDiff;
        public int MaxLevelDiff;
        public float Multiplier;
    }

    /// <summary>
    /// damage_level_multiplier 테이블을 로드합니다.
    /// </summary>
    public sealed class TableDamageLevelMultiplier : DefaultTable<StruckTableDamageLevelMultiplier>
    {
        public override string Key => ConfigAddressableTable.DamageLevelMultiplier;

        /// <summary>
        /// 레벨 차이에 해당하는 데미지 배율을 반환합니다.
        /// </summary>
        /// <param name="levelDiff">공격자 레벨 - 대상 레벨 값입니다.</param>
        /// <returns>데이터에 설정된 배율입니다. 매칭 row가 없으면 1입니다.</returns>
        public float ResolveMultiplier(int levelDiff)
        {
            foreach (KeyValuePair<int, StruckTableDamageLevelMultiplier> pair in GetDatas())
            {
                StruckTableDamageLevelMultiplier row = pair.Value;
                if (row == null)
                    continue;

                if (levelDiff >= row.MinLevelDiff && levelDiff <= row.MaxLevelDiff)
                    return row.Multiplier > 0f ? row.Multiplier : 1f;
            }

            return 1f;
        }

        protected override StruckTableDamageLevelMultiplier BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableDamageLevelMultiplier
            {
                Uid = reader.Int("Uid"),
                MinLevelDiff = reader.Int("MinLevelDiff"),
                MaxLevelDiff = reader.Int("MaxLevelDiff"),
                Multiplier = System.Math.Max(0f, reader.Float("Multiplier", 1f)),
            };
        }
    }
}
