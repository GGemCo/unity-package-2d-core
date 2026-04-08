using System;
using System.Collections.Generic;
using R3;

namespace GGemCo2DCore
{
    public partial class CharacterStat
    {
        /// <summary>
        /// 최종 임시 최대 HP(Temporary Max HP, 계산 결과)를 스트림으로 제공합니다.
        /// - 추가 하트/보호막 등의 "최대치"로 사용됩니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalHpTemp = new(0);
        public readonly BehaviorSubject<long> CurrentHpTemp = new(0);

        /// <summary>
        /// 아이템 사용 등으로 얻는 "소모형 추가 최대 HP(추가 하트)".
        /// - 데미지를 먼저 흡수하고, 0이 되면 즉시 소멸합니다.
        /// - 회복/리젠으로 다시 채워지지 않습니다.
        /// - 플레이어는 저장/로드 대상입니다(세이브 연동은 Player에서 처리).
        /// </summary>
        protected long TotalHpTempItem;
        protected long CurrentHpTempItem;

        protected long TotalHpTempPassive;
        protected long CurrentHpTempPassive;

        private readonly Dictionary<int, RuntimeTempHpEntry> _runtimeTempHpBySource = new();

        private struct RuntimeTempHpEntry
        {
            public long Max;
            public long Current;

            public RuntimeTempHpEntry(long max, long current)
            {
                Max = max;
                Current = current;
            }
        }


        #region 일반 HP

        public long GetItemBonusHpNormal() => _itemBonusProvider?.GetHpBonusNormal() ?? 0;
        /// <summary>
        /// 아이템 사용으로 "일반 최대 HP" 누적치를 증가시킵니다(저장 + 스탯 반영).
        /// </summary>
        public virtual void AddItemBonusMaxHpNormal(long amount, bool raiseEvent = true)
        {
            if (amount <= 0) return;

            // 스탯 Provider 갱신
            _itemBonusProvider?.AddHpBonusNormal(amount, raiseEvent);
        }
        public virtual void SetItemBonusMaxHpNormal(long amount, bool raiseEvent = true)
        {
            if (amount <= 0) return;

            // 스탯 Provider 갱신
            _itemBonusProvider?.SetHpBonusNormal(amount, raiseEvent);
        }

        #endregion

        #region 임시 HP

        #region 아이템 임시 HP
        public long GetItemBonusHpTemp() => _itemBonusProvider?.GetHpBonusTemp() ?? 0;
        public long GetItemBonusHpTempCurrent() => CurrentHpTempItem;

        /// <summary>
        /// 아이템 사용했을 때 CharacterStat.TotalHpTemp 업데이트 하기
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="raiseEvent"></param>
        /// <param name="fillCurrent"></param>
        public virtual void AddItemBonusMaxHpTemp(long amount, bool raiseEvent = true, bool fillCurrent = true)
        {
            if (amount <= 0) return;

            // 스탯 Provider 갱신. CharacterStat._totalHpTemp 가 갱신된다.
            _itemBonusProvider?.AddHpBonusTemp(amount, raiseEvent);

            // 구독 처리로 인해서 PlayerData 저장 됨
            TotalHpTempItem = TotalHpTempItem + amount;

            if (fillCurrent)
            {
                var newValue = CurrentHpTempItem + amount;
                SetItemBonusHpCurrent(newValue);
            }
        }
        public virtual void SetItemBonusMaxHpTemp(long amount, bool raiseEvent = true)
        {
            if (amount <= 0) return;

            // 스탯 Provider 갱신
            _itemBonusProvider?.SetHpBonusTemp(amount, raiseEvent);
        }
        /// <summary>
        /// 아이템 사용으로 인해 증가한 "일반 최대 HP / 임시 최대 HP" 누적치를 설정합니다(저장값 복원 등).
        /// </summary>
        public void SetItemBonusHpBonuses(long normalHpDelta, long tempHpDelta, bool raiseEvent = true)
        {
            _itemBonusProvider?.SetHpBonuses(normalHpDelta, tempHpDelta, raiseEvent);
        }
        /// <summary>
        /// 데미지 처리에서 사용: ItemBonusHpCurrent를 먼저 소모하고, 남은 데미지를 반환합니다.
        /// </summary>
        public long ConsumeHpTempItem(long incomingDamage)
        {
            if (incomingDamage <= 0) return 0;

            long beforeCurrent = CurrentHpTempItem;
            if (beforeCurrent <= 0) return incomingDamage;

            long consume = System.Math.Min(beforeCurrent, incomingDamage);
            long remainingBonus = beforeCurrent - consume;
            long remainingDamage = incomingDamage - consume;

            bool depleted = remainingBonus <= 0;
            SetCurrentHpTempItem(depleted ? 0 : remainingBonus, invokeDepleted: depleted);

            // NOTE:
            // - 소모형 추가 최대 HP(아이템 보너스 HP)의 “현재치”가 감소한 시점을 외부에서 해석할 수 있도록 훅을 제공합니다.
            // - 기본 구현은 no-op이며, 플레이어는 여기에서 “하트 1개 소모 → 최대치 영구 감소(저장)” 같은 규칙을 적용할 수 있습니다.
            OnConsumedHpTempItem(beforeCurrent, depleted ? 0 : remainingBonus, consume);
            return remainingDamage;
        }

        /// <summary>
        /// ItemBonusHpCurrent(소모형 추가 HP)가 감소했을 때 호출되는 훅.
        /// </summary>
        /// <remarks>
        /// - <see cref="ConsumeHpTempItem"/> 경로에서만 호출됩니다.
        /// - 기본 구현은 아무 것도 하지 않습니다.
        /// - 예: 플레이어는 “하트 단위 소모가 완료되면 ItemBonusHpTemp(최대치) 자체를 영구 감소” 같은 규칙을 적용할 수 있습니다.
        /// </remarks>
        protected virtual void OnConsumedHpTempItem(long beforeCurrent, long afterCurrent, long consumedAmount)
        {
        }

        /// <summary>
        /// 저장/로드 또는 사망 처리 등에서 직접 값을 세팅할 때 사용합니다.
        /// </summary>
        public void SetItemBonusHpCurrent(long value)
        {
            SetCurrentHpTempItem(System.Math.Max(0, value),
                invokeDepleted: value <= 0 && TotalHpTempItem > 0);
        }

        private void SetCurrentHpTempItem(long value, bool invokeDepleted)
        {
            value = System.Math.Max(0, value);
            // 임시 최대 HP(TotalTempHp)를 초과하지 않도록 클램프
            long tempMax = TotalHpTempItem;
            if (tempMax > 0)
                value = System.Math.Min(value, tempMax);
            if (CurrentHpTempItem == value)
                return;

            CurrentHpTempItem = value;

            UpdateCurrentHpTemp();

            if (invokeDepleted)
            {
                // ItemBonus가 0이 되는 순간: 최대치(표시) 변화에 따른 클램프/리빌드 트리거
                if (CurrentHp.Value > TotalHp.Value)
                {
                    CurrentHp.OnNext(TotalHp.Value);
                }
            }
        }

        #endregion

        protected void UpdateCurrentHpTemp()
        {
            var newValue = CurrentHpTempPassive + CurrentHpTempItem + SumRuntimeTempHpCurrent();
            if (newValue > TotalHpTemp.Value)
                newValue = TotalHpTemp.Value;
            if (CurrentHpTemp.Value == newValue) return;
            CurrentHpTemp.OnNext(newValue);
        }
        #endregion

        #region 패시브 스킬
        public long GetPassiveBonusHpTempMax() => TotalHpTempPassive;
        public long GetPassiveBonusHpTempCurrent() => CurrentHpTempPassive;
        public long GetPersistentBonusHpTempMax() => GetItemBonusHpTemp() + GetPassiveBonusHpTempMax();
        public long GetPersistentBonusHpTempCurrent() => GetItemBonusHpTempCurrent() + GetPassiveBonusHpTempCurrent();

        public void SetPassiveBonusHpTempMax(long value)
        {
            value = Math.Max(0, value);

            if (TotalHpTempPassive == value)
                return;

            TotalHpTempPassive = value;

            if (CurrentHpTempPassive > TotalHpTempPassive)
                CurrentHpTempPassive = TotalHpTempPassive;

            UpdateCurrentHpTemp();
        }

        public void AddPassiveBonusHpTempCurrent(long amount)
        {
            if (amount <= 0)
                return;

            var next = CurrentHpTempPassive + amount;
            if (next > TotalHpTempPassive)
                next = TotalHpTempPassive;

            if (CurrentHpTempPassive == next)
                return;

            CurrentHpTempPassive = next;
            UpdateCurrentHpTemp();
        }

        public void SetCurrentHpTempPassive(long value)
        {
            value = Math.Max(0, value);
            if (value > TotalHpTempPassive)
                value = TotalHpTempPassive;

            if (CurrentHpTempPassive == value)
                return;

            CurrentHpTempPassive = value;
            UpdateCurrentHpTemp();
        }
        public void FillPassiveBonusHpTempToMax()
        {
            SetCurrentHpTempPassive(GetPassiveBonusHpTempMax());
        }

        public long ConsumeHpTempPassive(long incomingDamage)
        {
            if (incomingDamage <= 0) return 0;

            long beforeCurrent = CurrentHpTempPassive;
            if (beforeCurrent <= 0) return incomingDamage;

            long consume = Math.Min(beforeCurrent, incomingDamage);
            long remainingPassive = beforeCurrent - consume;
            long remainingDamage = incomingDamage - consume;

            SetCurrentHpTempPassive(remainingPassive);
            return remainingDamage;
        }
        #endregion

        #region 런타임 Temp HP

        /// <summary>
        /// 런타임 전용 Temp HP 최대치를 합산한 값을 반환합니다.
        /// 저장하지 않는 보호막/스킬 Temp HP 용도입니다.
        /// </summary>
        public long GetRuntimeBonusHpTempMax() => _runtimeTempHpProvider?.GetHpBonusTemp() ?? 0;

        /// <summary>
        /// 런타임 전용 Temp HP 현재치를 합산한 값을 반환합니다.
        /// </summary>
        public long GetRuntimeBonusHpTempCurrent() => SumRuntimeTempHpCurrent();

        /// <summary>
        /// source key 단위 런타임 Temp HP를 설정합니다.
        /// - 같은 key가 다시 들어오면 누적하지 않고 해당 값으로 교체합니다.
        /// - fillToMax=true 이면 현재치를 설정값까지 즉시 채웁니다.
        /// - 값이 0 이하이면 해당 source를 제거합니다.
        /// </summary>
        public void SetRuntimeBonusHpTemp(int sourceKey, long amount, bool fillToMax = true)
        {
            if (amount <= 0)
            {
                ClearRuntimeBonusHpTemp(sourceKey);
                return;
            }

            amount = Math.Max(0, amount);

            if (_runtimeTempHpBySource.TryGetValue(sourceKey, out var existing))
            {
                existing.Max = amount;
                existing.Current = fillToMax ? amount : Math.Min(existing.Current, amount);
                _runtimeTempHpBySource[sourceKey] = existing;
            }
            else
            {
                _runtimeTempHpBySource[sourceKey] = new RuntimeTempHpEntry(amount, fillToMax ? amount : 0);
            }

            _runtimeTempHpProvider?.SetHpBonusTempBySource(sourceKey, amount, raiseEvent: true);
            UpdateCurrentHpTemp();
        }

        /// <summary>
        /// source key 단위 런타임 Temp HP를 제거합니다.
        /// </summary>
        public void ClearRuntimeBonusHpTemp(int sourceKey)
        {
            bool removed = _runtimeTempHpBySource.Remove(sourceKey);
            _runtimeTempHpProvider?.RemoveHpBonusTempSource(sourceKey, raiseEvent: true);

            if (removed)
                UpdateCurrentHpTemp();
        }

        /// <summary>
        /// 모든 런타임 Temp HP를 제거합니다.
        /// </summary>
        public void ClearAllRuntimeBonusHpTemp()
        {
            if (_runtimeTempHpBySource.Count == 0)
                return;

            _runtimeTempHpBySource.Clear();
            _runtimeTempHpProvider?.Clear(raiseEvent: true);
            UpdateCurrentHpTemp();
        }

        /// <summary>
        /// 데미지 처리에서 사용: 런타임 Temp HP를 소모하고 남은 데미지를 반환합니다.
        /// - source key 별 현재치를 먼저 소모합니다.
        /// - 어떤 source의 현재치가 0이 되면 해당 source의 최대치까지 함께 제거합니다.
        /// </summary>
        public long ConsumeHpTempRuntime(long incomingDamage)
        {
            if (incomingDamage <= 0)
                return 0;

            if (_runtimeTempHpBySource.Count == 0)
                return incomingDamage;

            long remainingDamage = incomingDamage;
            var sourceKeys = new List<int>(_runtimeTempHpBySource.Keys);
            sourceKeys.Sort();

            for (int i = 0; i < sourceKeys.Count; i++)
            {
                if (remainingDamage <= 0)
                    break;

                int sourceKey = sourceKeys[i];
                if (!_runtimeTempHpBySource.TryGetValue(sourceKey, out var entry))
                    continue;

                if (entry.Current <= 0)
                {
                    RemoveRuntimeTempHpSource(sourceKey, updateCurrent: false);
                    continue;
                }

                long consume = Math.Min(entry.Current, remainingDamage);
                entry.Current -= consume;
                remainingDamage -= consume;

                if (entry.Current <= 0)
                {
                    RemoveRuntimeTempHpSource(sourceKey, updateCurrent: false);
                }
                else
                {
                    _runtimeTempHpBySource[sourceKey] = entry;
                }
            }

            UpdateCurrentHpTemp();
            return remainingDamage;
        }

        private void RemoveRuntimeTempHpSource(int sourceKey, bool updateCurrent)
        {
            if (!_runtimeTempHpBySource.Remove(sourceKey))
                return;

            _runtimeTempHpProvider?.RemoveHpBonusTempSource(sourceKey, raiseEvent: true);

            if (updateCurrent)
                UpdateCurrentHpTemp();
        }

        private long SumRuntimeTempHpCurrent()
        {
            long total = 0;
            foreach (var pair in _runtimeTempHpBySource)
            {
                total += Math.Max(0, pair.Value.Current);
            }

            return total;
        }

        #endregion
    }
}
