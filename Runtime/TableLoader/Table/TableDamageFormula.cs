using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Poly 데미지 공식 테이블 row입니다.
    /// </summary>
    public sealed class StruckTableDamageFormula
    {
        public int Uid;
        public string FormulaKey;
        public string Memo;
        public string Expression;
        public string RoundingMode;
        public long MinDamage;
    }

    /// <summary>
    /// damage_formula 테이블을 로드합니다.
    /// </summary>
    public sealed class TableDamageFormula : DefaultTable<StruckTableDamageFormula>
    {
        public override string Key => ConfigAddressableTable.DamageFormula;

        private readonly Dictionary<string, StruckTableDamageFormula> _byKey = new(System.StringComparer.OrdinalIgnoreCase);

        protected override void PreLoad()
        {
            _byKey.Clear();
        }

        protected override void OnLoadedData(StruckTableDamageFormula data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.FormulaKey))
                return;

            _byKey[data.FormulaKey.Trim()] = data;
        }

        /// <summary>
        /// FormulaKey 기준으로 공식 row를 조회합니다.
        /// </summary>
        /// <param name="formulaKey">조회할 공식 키입니다.</param>
        /// <returns>공식 row입니다. 없으면 null입니다.</returns>
        public StruckTableDamageFormula GetDataByFormulaKey(string formulaKey)
        {
            return !string.IsNullOrWhiteSpace(formulaKey) && _byKey.TryGetValue(formulaKey.Trim(), out StruckTableDamageFormula row)
                ? row
                : null;
        }

        protected override StruckTableDamageFormula BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableDamageFormula
            {
                Uid = reader.Int("Uid"),
                FormulaKey = reader.String("FormulaKey", string.Empty),
                Memo = reader.String("Memo", string.Empty),
                Expression = reader.String("Expression", string.Empty),
                RoundingMode = reader.String("RoundingMode", "Round"),
                MinDamage = System.Math.Max(0L, reader.Long("MinDamage", 0L)),
            };
        }
    }
}
