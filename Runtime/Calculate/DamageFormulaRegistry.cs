using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// damage_formula 테이블의 Poly 공식을 컴파일하고 키 기반으로 조회하는 저장소입니다.
    /// </summary>
    public sealed class DamageFormulaRegistry
    {
        private readonly Dictionary<string, PolyDamageFormula> _formulas = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 테이블 데이터를 기준으로 공식 캐시를 다시 구성합니다.
        /// </summary>
        /// <param name="table">데미지 공식 테이블입니다.</param>
        public void Rebuild(TableDamageFormula table)
        {
            _formulas.Clear();
            if (table == null)
                return;

            foreach (KeyValuePair<int, StruckTableDamageFormula> pair in table.GetDatas())
            {
                StruckTableDamageFormula row = pair.Value;
                if (row == null || string.IsNullOrWhiteSpace(row.FormulaKey) || string.IsNullOrWhiteSpace(row.Expression))
                    continue;

                if (PolyDamageFormula.TryCompile(row.Expression, out PolyDamageFormula formula, out string error))
                {
                    _formulas[row.FormulaKey] = formula;
                }
                else
                {
                    Debug.LogWarning($"[DamageFormulaRegistry] Poly 공식 컴파일 실패. key={row.FormulaKey}, error={error}");
                }
            }
        }

        /// <summary>
        /// 공식 키로 컴파일된 Poly 공식을 조회합니다.
        /// </summary>
        /// <param name="formulaKey">조회할 공식 키입니다.</param>
        /// <param name="formula">조회된 공식입니다.</param>
        /// <returns>공식을 찾으면 <see langword="true"/>입니다.</returns>
        public bool TryGet(string formulaKey, out PolyDamageFormula formula)
        {
            formula = null;
            return !string.IsNullOrWhiteSpace(formulaKey) && _formulas.TryGetValue(formulaKey.Trim(), out formula);
        }
    }
}
