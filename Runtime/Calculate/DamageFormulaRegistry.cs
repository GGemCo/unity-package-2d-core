using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// damage_formula 테이블에서 컴파일된 Poly 공식과 후처리 정책을 함께 보관하는 항목입니다.
    /// </summary>
    public sealed class DamageFormulaEntry
    {
        /// <summary>컴파일된 Poly 공식입니다.</summary>
        public readonly PolyDamageFormula Formula;

        /// <summary>공식 계산 결과를 정수 데미지로 변환할 때 사용할 반올림 정책입니다.</summary>
        public readonly string RoundingMode;

        /// <summary>공식 결과가 0보다 클 때 보장할 최소 데미지입니다.</summary>
        public readonly long MinDamage;

        /// <summary>
        /// 데미지 공식 항목을 생성합니다.
        /// </summary>
        /// <param name="formula">컴파일된 Poly 공식입니다.</param>
        /// <param name="roundingMode">정수 변환 정책입니다.</param>
        /// <param name="minDamage">공식 결과가 0보다 클 때 보장할 최소 데미지입니다.</param>
        public DamageFormulaEntry(PolyDamageFormula formula, string roundingMode, long minDamage)
        {
            Formula = formula;
            RoundingMode = string.IsNullOrWhiteSpace(roundingMode) ? "Round" : roundingMode.Trim();
            MinDamage = System.Math.Max(0L, minDamage);
        }
    }

    /// <summary>
    /// damage_formula 테이블의 Poly 공식을 컴파일하고 키 기반으로 조회하는 저장소입니다.
    /// </summary>
    public sealed class DamageFormulaRegistry
    {
        private readonly Dictionary<string, DamageFormulaEntry> _formulas = new(System.StringComparer.OrdinalIgnoreCase);

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
                    // 공식 자체와 함께 반올림/최소 데미지 정책을 캐시하여 런타임 계산 시 테이블을 다시 조회하지 않습니다.
                    _formulas[row.FormulaKey.Trim()] = new DamageFormulaEntry(formula, row.RoundingMode, row.MinDamage);
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
        /// <param name="entry">조회된 공식 항목입니다.</param>
        /// <returns>공식을 찾으면 <see langword="true"/>입니다.</returns>
        public bool TryGet(string formulaKey, out DamageFormulaEntry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(formulaKey) && _formulas.TryGetValue(formulaKey.Trim(), out entry);
        }
    }
}
