using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임에서 특정 출처의 Stat Modifier를 분리 보관하는 범용 Provider입니다.
    /// </summary>
    /// <remarks>
    /// Affect처럼 Core 외부 패키지에서 일시적으로 적용/해제하는 Modifier를 장비 Provider와 섞지 않고,
    /// 디버그 HUD에서 출처별로 구분하기 위해 사용합니다.
    /// </remarks>
    public sealed class RuntimeStatModifierProvider : IStatModifierProvider, IStatModifierDebugSource
    {
        private readonly StatModifierBucket _bucket = new();

        /// <summary>
        /// Provider를 생성합니다.
        /// </summary>
        /// <param name="debugSourceType">디버그 HUD에 표시할 출처 타입입니다.</param>
        /// <param name="debugSourceName">디버그 HUD에 표시할 출처 이름입니다.</param>
        public RuntimeStatModifierProvider(StatModifierDebugSourceType debugSourceType, string debugSourceName)
        {
            DebugSourceType = debugSourceType;
            DebugSourceName = string.IsNullOrWhiteSpace(debugSourceName) ? debugSourceType.ToString() : debugSourceName;
        }

        /// <summary>스탯 키별 Flat(고정) 누적값입니다.</summary>
        public IReadOnlyDictionary<string, int> Flat => _bucket.Flat;

        /// <summary>스탯 키별 Percent(비율) 누적값입니다.</summary>
        public IReadOnlyDictionary<string, float> Percent => _bucket.Percent;

        /// <summary>디버그 HUD에 표시할 출처 타입입니다.</summary>
        public StatModifierDebugSourceType DebugSourceType { get; }

        /// <summary>디버그 HUD에 표시할 출처 이름입니다.</summary>
        public string DebugSourceName { get; }

        /// <summary>버킷 변경 시 발생합니다.</summary>
        public event Action Changed;

        /// <summary>
        /// 스탯 변경 목록을 누적 적용합니다.
        /// </summary>
        /// <param name="modifiers">적용할 스탯 변경 목록입니다.</param>
        /// <param name="raiseEvent">true이면 적용 후 재계산 이벤트를 발생시킵니다.</param>
        public void ApplyStatModifiers(List<ConfigCommon.StruckStatus> modifiers, bool raiseEvent = true)
        {
            ModifyStatModifiers(modifiers, isAdding: true, raiseEvent: raiseEvent);
        }

        /// <summary>
        /// 스탯 변경 목록을 제거합니다.
        /// </summary>
        /// <param name="modifiers">제거할 스탯 변경 목록입니다.</param>
        /// <param name="raiseEvent">true이면 제거 후 재계산 이벤트를 발생시킵니다.</param>
        public void RemoveStatModifiers(List<ConfigCommon.StruckStatus> modifiers, bool raiseEvent = true)
        {
            ModifyStatModifiers(modifiers, isAdding: false, raiseEvent: raiseEvent);
        }

        /// <summary>
        /// 모든 Modifier를 제거합니다.
        /// </summary>
        /// <param name="raiseEvent">true이면 제거 후 재계산 이벤트를 발생시킵니다.</param>
        public void Clear(bool raiseEvent = true)
        {
            if (_bucket.Flat.Count == 0 && _bucket.Percent.Count == 0)
                return;

            _bucket.Clear();
            if (raiseEvent)
                Changed?.Invoke();
        }

        /// <summary>
        /// 적용/제거 방향에 따라 Modifier 목록을 버킷에 반영합니다.
        /// </summary>
        /// <param name="modifiers">처리할 스탯 변경 목록입니다.</param>
        /// <param name="isAdding">true이면 적용, false이면 제거입니다.</param>
        /// <param name="raiseEvent">true이면 처리 후 재계산 이벤트를 발생시킵니다.</param>
        private void ModifyStatModifiers(List<ConfigCommon.StruckStatus> modifiers, bool isAdding, bool raiseEvent)
        {
            if (modifiers == null || modifiers.Count <= 0)
                return;

            for (int i = 0; i < modifiers.Count; i++)
            {
                ConfigCommon.StruckStatus modifier = modifiers[i];
                ModifyStat(modifier.ID, modifier, isAdding);
            }

            if (raiseEvent)
                Changed?.Invoke();
        }

        /// <summary>
        /// 접미사 정책에 따라 Flat 또는 Percent 버킷을 갱신합니다.
        /// </summary>
        /// <param name="statType">스탯 키입니다.</param>
        /// <param name="struckStatus">스탯 변경 정보입니다.</param>
        /// <param name="isAdding">true이면 적용, false이면 역적용입니다.</param>
        private void ModifyStat(string statType, ConfigCommon.StruckStatus struckStatus, bool isAdding)
        {
            if (string.IsNullOrEmpty(statType))
                return;

            float value = struckStatus.Value;
            ConfigCommon.SuffixType suffixType = struckStatus.SuffixType;

            switch (suffixType)
            {
                case ConfigCommon.SuffixType.Plus:
                    _bucket.AddFlat(statType, isAdding ? (int)value : -(int)value);
                    break;
                case ConfigCommon.SuffixType.Minus:
                    _bucket.AddFlat(statType, isAdding ? -(int)value : (int)value);
                    break;
                case ConfigCommon.SuffixType.Increase:
                    _bucket.AddPercent(statType, isAdding ? value : -value);
                    break;
                case ConfigCommon.SuffixType.Decrease:
                    _bucket.AddPercent(statType, isAdding ? -value : value);
                    break;
                case ConfigCommon.SuffixType.None:
                default:
                    _bucket.AddFlat(statType, isAdding ? (int)value : -(int)value);
                    break;
            }
        }
    }
}
