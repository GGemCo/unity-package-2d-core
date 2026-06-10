using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 영구 Modifier Provider입니다.
    /// - 스탯 포인트/영구 성장 등, 장비 갱신과 무관하게 유지되는 modifier 출처를 담당합니다.
    /// - Flat/Percent 버킷을 유지하며, 변경 시 <see cref="Changed"/> 이벤트로 상위(<see cref="CharacterStat"/>) 재계산을 트리거합니다.
    /// </summary>
    public sealed class PersistentModifierProvider : IStatModifierProvider, IStatModifierDebugSource
    {
        /// <summary>
        /// 영구 성장으로부터 누적되는 스탯 변경 버킷(Flat/Percent)입니다.
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

        /// <summary>영구 성장/스탯 포인트로 인한 스탯 증가임을 표시합니다.</summary>
        public StatModifierDebugSourceType DebugSourceType => StatModifierDebugSourceType.Persistent;

        /// <summary>디버그 HUD에 표시할 Provider 이름입니다.</summary>
        public string DebugSourceName => "Persistent";

        /// <summary>
        /// 영구 modifier를 “전체 재구성” 방식으로 설정합니다.
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
        /// 내부 버킷을 읽기 전용 스냅샷 용도로 반환합니다(프로젝션/시뮬레이션 계산 등).
        /// </summary>
        /// <returns>내부에서 유지 중인 <see cref="StatModifierBucket"/> 인스턴스 참조입니다.</returns>
        /// <remarks>
        /// 주의: 실제로는 내부 참조를 그대로 반환하므로 외부에서 수정하면 Provider 상태가 변경됩니다.
        /// 스냅샷이 필요하다면 복사본을 반환하는 API로 대체하는 것을 권장합니다.
        /// </remarks>
        public StatModifierBucket GetBucketUnsafe() => _bucket;
    }
}