using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Flat/Percent Modifier 딕셔너리 1쌍을 관리하는 경량 컨테이너입니다.
    /// </summary>
    /// <remarks>
    /// - Key: 스탯 키 문자열(예: BASE_ATK, STAT_ATK 등)
    /// - Flat: 고정(가산) 값
    /// - Percent: 100 기준 퍼센트 값(예: 10 = +10%)
    /// - 값이 0이 되면 내부 딕셔너리에서 제거하여 메모리 및 조회 비용을 최소화합니다.
    /// </remarks>
    public sealed class StatModifierBucket
    {
        /// <summary>
        /// Flat(가산) modifier 저장소입니다.
        /// </summary>
        private readonly Dictionary<string, int> _flat = new();

        /// <summary>
        /// Percent(%) modifier 저장소입니다.
        /// </summary>
        private readonly Dictionary<string, float> _percent = new();

        /// <summary>
        /// 스탯 키별 Flat(가산) 누적값을 읽기 전용으로 반환합니다.
        /// </summary>
        public IReadOnlyDictionary<string, int> Flat => _flat;

        /// <summary>
        /// 스탯 키별 Percent(%) 누적값을 읽기 전용으로 반환합니다.
        /// </summary>
        public IReadOnlyDictionary<string, float> Percent => _percent;

        /// <summary>
        /// 모든 Flat/Percent modifier를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _flat.Clear();
            _percent.Clear();
        }

        /// <summary>
        /// 지정한 스탯 키의 Flat 값을 설정합니다(덮어쓰기).
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <param name="value">설정할 Flat 값입니다.</param>
        /// <remarks>
        /// value가 0이면 해당 키를 제거합니다.
        /// </remarks>
        public void SetFlat(string statKey, int value)
        {
            if (string.IsNullOrEmpty(statKey)) return;

            if (value == 0)
                _flat.Remove(statKey);
            else
                _flat[statKey] = value;
        }

        /// <summary>
        /// 지정한 스탯 키의 Percent 값을 설정합니다(덮어쓰기).
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <param name="value">설정할 Percent 값(100 기준)입니다.</param>
        /// <remarks>
        /// value가 0에 가까우면(<see cref="Mathf.Approximately(float, float)"/>) 해당 키를 제거합니다.
        /// </remarks>
        public void SetPercent(string statKey, float value)
        {
            if (string.IsNullOrEmpty(statKey)) return;

            if (Mathf.Approximately(value, 0f))
                _percent.Remove(statKey);
            else
                _percent[statKey] = value;
        }

        /// <summary>
        /// 지정한 스탯 키의 Flat 값을 누적(가산)합니다.
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <param name="delta">증감시킬 Flat 값입니다.</param>
        /// <remarks>
        /// - delta가 0이면 아무 동작도 하지 않습니다.
        /// - 누적 결과가 0이 되면 해당 키를 제거합니다.
        /// </remarks>
        public void AddFlat(string statKey, int delta)
        {
            if (string.IsNullOrEmpty(statKey) || delta == 0) return;

            _flat.TryGetValue(statKey, out var v);
            v += delta;

            if (v == 0)
                _flat.Remove(statKey);
            else
                _flat[statKey] = v;
        }

        /// <summary>
        /// 지정한 스탯 키의 Percent 값을 누적(가산)합니다.
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <param name="delta">증감시킬 Percent 값(100 기준)입니다.</param>
        /// <remarks>
        /// - delta가 0에 가까우면 아무 동작도 하지 않습니다.
        /// - 누적 결과가 0에 가까우면 해당 키를 제거합니다.
        /// </remarks>
        public void AddPercent(string statKey, float delta)
        {
            if (string.IsNullOrEmpty(statKey) || Mathf.Approximately(delta, 0f)) return;

            _percent.TryGetValue(statKey, out var v);
            v += delta;

            if (Mathf.Approximately(v, 0f))
                _percent.Remove(statKey);
            else
                _percent[statKey] = v;
        }

        /// <summary>
        /// 지정한 스탯 키의 Flat 값을 반환합니다.
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <returns>존재하면 해당 값, 없으면 0입니다.</returns>
        public int GetFlatOrZero(string statKey)
        {
            if (string.IsNullOrEmpty(statKey)) return 0;
            return _flat.GetValueOrDefault(statKey, 0);
        }

        /// <summary>
        /// 지정한 스탯 키의 Percent 값을 반환합니다.
        /// </summary>
        /// <param name="statKey">스탯 키입니다.</param>
        /// <returns>존재하면 해당 값, 없으면 0f입니다.</returns>
        public float GetPercentOrZero(string statKey)
        {
            if (string.IsNullOrEmpty(statKey)) return 0f;
            return _percent.GetValueOrDefault(statKey, 0f);
        }
    }
}