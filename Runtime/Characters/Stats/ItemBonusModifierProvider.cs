using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 사용으로 인해 증가하는 modifier Provider입니다.
    /// - 저장/로드 대상(플레이어 기준)이며, 런타임에 누적될 수 있습니다.
    /// - 현재는 HP(일반/임시) 확장을 우선 지원합니다.
    /// </summary>
    public sealed class ItemBonusModifierProvider : IStatModifierProvider
    {
        private readonly StatModifierBucket _bucket = new();

        public IReadOnlyDictionary<string, int> Flat => _bucket.Flat;
        public IReadOnlyDictionary<string, float> Percent => _bucket.Percent;

        public event Action Changed;

        /// <summary>
        /// 아이템 보너스 HP(일반/임시)를 "전체 재구성" 방식으로 설정합니다.
        /// </summary>
        public void SetHpBonuses(long normalHpDelta, long tempHpDelta, bool raiseEvent = true)
        {
            // 다른 스탯 키가 이후 확장될 수 있으므로, 현재는 HP 관련 키만 선택적으로 갱신합니다.
            SetFlatInternal(ConfigCommon.StatusStatHp, normalHpDelta);
            SetFlatInternal(ConfigCommon.StatusStatHpTemp, tempHpDelta);

            if (raiseEvent)
                Changed?.Invoke();
        }

        public long GetHpBonusNormal() => GetFlatAsLong(ConfigCommon.StatusStatHp);
        public long GetHpBonusTemp() => GetFlatAsLong(ConfigCommon.StatusStatHpTemp);

        public void AddHpBonusNormal(long add, bool raiseEvent = true)
        {
            if (add <= 0) return;
            AddFlatInternal(ConfigCommon.StatusStatHp, add);
            if (raiseEvent) Changed?.Invoke();
        }
        public void SetHpBonusNormal(long value, bool raiseEvent = true)
        {
            if (value <= 0) return;
            SetFlatInternal(ConfigCommon.StatusStatHp, value);
            if (raiseEvent) Changed?.Invoke();
        }

        public void AddHpBonusTemp(long add, bool raiseEvent = true)
        {
            if (add <= 0) return;
            AddFlatInternal(ConfigCommon.StatusStatHpTemp, add);
            // Changed 는 CharacterStat.OnProviderChanged 호출 
            // TotalHpTemp 가 업데이트 된다.
            if (raiseEvent) Changed?.Invoke();
        }
        public void SetHpBonusTemp(long value, bool raiseEvent = true)
        {
            if (value <= 0) return;
            SetFlatInternal(ConfigCommon.StatusStatHpTemp, value);
            // Changed 는 CharacterStat.OnProviderChanged 호출 
            // TotalHpTemp 가 업데이트 된다.
            if (raiseEvent) Changed?.Invoke();
        }
        

        private void SetFlatInternal(string statKey, long value)
        {
            if (string.IsNullOrEmpty(statKey)) return;

            int clamped = ClampToIntNonNegative(value);

            // 0이면 제거하여 데이터 밀도를 낮춘다.
            if (clamped == 0)
            {
                _bucket.SetFlat(statKey, 0);
                return;
            }

            _bucket.SetFlat(statKey, clamped);
        }

        private void AddFlatInternal(string statKey, long add)
        {
            if (string.IsNullOrEmpty(statKey)) return;

            long current = GetFlatAsLong(statKey);
            long next = current + add;
            if (next < 0) next = long.MaxValue; // overflow 방어

            SetFlatInternal(statKey, next);
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
