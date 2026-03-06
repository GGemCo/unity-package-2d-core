using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public enum PassiveTempHpApplyPolicy
    {
        KeepCurrent = 0,
        FillDelta = 1,
    }

    public enum PassiveTempHpApplyMode
    {
        UsePolicy = 0,
        KeepCurrent = 1,
        FillDelta = 2,
        FillToMax = 3,
    }
    /// <summary>
    /// 패시브 스킬(장착형) Modifier Provider입니다.
    /// - 장착/해제/레벨 변경 시 “전체를 재구성(Set)”하는 방식으로 갱신하는 것을 권장합니다.
    /// - Flat/Percent 버킷을 유지하며, 변경 시 <see cref="Changed"/> 이벤트로 상위(<see cref="CharacterStat"/>) 재계산을 트리거합니다.
    /// </summary>
    public sealed class PassiveSkillModifierProvider : IStatModifierProvider
    {
        /// <summary>
        /// 패시브 스킬로부터 누적되는 스탯 변경 버킷(Flat/Percent)입니다.
        /// </summary>
        private readonly StatModifierBucket _bucket = new();

        /// <summary>
        /// 스탯 키별 Flat(고정) 누적값입니다.
        /// </summary>
        public IReadOnlyDictionary<string, int> Flat => _bucket.Flat;

        /// <summary>
        /// 스탯 키별 Percent(비율) 누적값입니다.
        /// </summary>
        public IReadOnlyDictionary<string, float> Percent => _bucket.Percent;

        /// <summary>
        /// 버킷(Flat/Percent)이 변경되었을 때 발생합니다.
        /// </summary>
        public event Action Changed;

        public long GetHpBonusNormal() => GetFlatAsLong(ConfigCommon.StatusStatHp);
        public long GetHpBonusTemp() => GetFlatAsLong(ConfigCommon.StatusStatHpTemp);
        
        /// <summary>
        /// 패시브 스킬 modifier를 “전체 재구성” 방식으로 설정합니다.
        /// </summary>
        /// <param name="flatByStatKey">스탯 키별 Flat(고정) 증가량입니다.</param>
        /// <param name="percentByStatKey">스탯 키별 Percent(비율) 증가율입니다.</param>
        /// <param name="raiseEvent">true이면 설정 후 <see cref="Changed"/> 이벤트를 발생시킵니다.</param>
        /// <remarks>
        /// - 내부 버킷을 먼저 비운 뒤, 0 값(의미 없는 값)은 저장하지 않습니다.
        /// - Percent는 <see cref="Mathf.Approximately(float, float)"/>로 0에 가까운 값을 필터링합니다.
        /// </remarks>
        public void SetModifiers(Dictionary<string, int> flatByStatKey, Dictionary<string, float> percentByStatKey, bool raiseEvent = true)
        {
            _bucket.Clear();

            if (flatByStatKey != null)
            {
                foreach (var kv in flatByStatKey)
                {
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    if (kv.Value == 0) continue;
                    _bucket.SetFlat(kv.Key, kv.Value);
                }
            }

            if (percentByStatKey != null)
            {
                foreach (var kv in percentByStatKey)
                {
                    if (string.IsNullOrEmpty(kv.Key)) continue;
                    if (Mathf.Approximately(kv.Value, 0f)) continue;
                    _bucket.SetPercent(kv.Key, kv.Value);
                }
            }

            if (raiseEvent)
                Changed?.Invoke();
        }

        /// <summary>
        /// 패시브 스킬 modifier를 모두 제거합니다.
        /// </summary>
        /// <param name="raiseEvent">true이면 제거 후 <see cref="Changed"/> 이벤트를 발생시킵니다.</param>
        /// <remarks>
        /// 이미 비어있는 경우에는 불필요한 이벤트/연산을 피하기 위해 아무 동작도 하지 않습니다.
        /// </remarks>
        public void Clear(bool raiseEvent = true)
        {
            if (_bucket.Flat.Count == 0 && _bucket.Percent.Count == 0) return;
            _bucket.Clear();
            if (raiseEvent)
                Changed?.Invoke();
        }
        
        private long GetFlatAsLong(string statKey)
        {
            if (string.IsNullOrEmpty(statKey)) return 0;
            return _bucket.Flat.TryGetValue(statKey, out int v) ? Mathf.Max(0, v) : 0;
        }
    }
}