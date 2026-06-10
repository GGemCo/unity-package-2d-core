using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임 전용 Temp HP(보호막/임시 하트) 최대치를 source key 단위로 관리하는 Provider입니다.
    /// - 저장하지 않습니다.
    /// - 같은 source key가 다시 설정되면 누적하지 않고 해당 값으로 교체합니다.
    /// - CharacterStat에서는 Current 값만 별도로 관리하고, 이 Provider는 BASE_HP_TEMP 기반 최종 Temp HP 최대치 계산에만 참여합니다.
    /// </summary>
    public sealed class RuntimeTempHpModifierProvider : IStatModifierProvider, IStatModifierDebugSource
    {
        private readonly StatModifierBucket _bucket = new();
        private readonly Dictionary<int, int> _hpTempBySource = new();

        public IReadOnlyDictionary<string, int> Flat => _bucket.Flat;
        public IReadOnlyDictionary<string, float> Percent => _bucket.Percent;

        public event Action Changed;

        /// <summary>런타임 임시 효과로 인한 스탯 증가임을 표시합니다.</summary>
        public StatModifierDebugSourceType DebugSourceType => StatModifierDebugSourceType.Runtime;

        /// <summary>디버그 HUD에 표시할 Provider 이름입니다.</summary>
        public string DebugSourceName => "Runtime";

        public long GetHpBonusTemp() => GetFlatAsLong(ConfigCommon.BaseStatHpTemp);

        public void SetHpBonusTempBySource(int sourceKey, long value, bool raiseEvent = true)
        {
            int clamped = ClampToIntNonNegative(value);

            if (clamped <= 0)
            {
                RemoveHpBonusTempSource(sourceKey, raiseEvent);
                return;
            }

            if (_hpTempBySource.TryGetValue(sourceKey, out int before) && before == clamped)
                return;

            _hpTempBySource[sourceKey] = clamped;
            RebuildBucket();

            if (raiseEvent)
                Changed?.Invoke();
        }

        public void RemoveHpBonusTempSource(int sourceKey, bool raiseEvent = true)
        {
            if (!_hpTempBySource.Remove(sourceKey))
                return;

            RebuildBucket();

            if (raiseEvent)
                Changed?.Invoke();
        }

        public void Clear(bool raiseEvent = true)
        {
            if (_hpTempBySource.Count == 0 && _bucket.Flat.Count == 0 && _bucket.Percent.Count == 0)
                return;

            _hpTempBySource.Clear();
            _bucket.Clear();

            if (raiseEvent)
                Changed?.Invoke();
        }

        private void RebuildBucket()
        {
            long totalTempHp = 0;
            foreach (var pair in _hpTempBySource)
            {
                totalTempHp += Mathf.Max(0, pair.Value);
                if (totalTempHp >= int.MaxValue)
                {
                    totalTempHp = int.MaxValue;
                    break;
                }
            }

            _bucket.SetFlat(ConfigCommon.BaseStatHpTemp, ClampToIntNonNegative(totalTempHp));
        }

        private long GetFlatAsLong(string statKey)
        {
            if (string.IsNullOrEmpty(statKey)) return 0;
            return _bucket.Flat.TryGetValue(statKey, out int v) ? Mathf.Max(0, v) : 0;
        }

        private static int ClampToIntNonNegative(long value)
        {
            if (value <= 0) return 0;
            if (value >= int.MaxValue) return int.MaxValue;
            return (int)value;
        }
    }
}
