using System;
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

        /// <summary>
        /// 아이템 사용 등으로 얻는 "소모형 추가 최대 HP(추가 하트)".
        /// - 데미지를 먼저 흡수하고, 0이 되면 즉시 소멸합니다.
        /// - 회복/리젠으로 다시 채워지지 않습니다.
        /// - 플레이어는 저장/로드 대상입니다(세이브 연동은 Player에서 처리).
        /// </summary>
        public readonly BehaviorSubject<long> TotalItemBonusHpTemp = new(0);
        public readonly BehaviorSubject<long> CurrentItemBonusHpTemp = new(0);
        /// <summary>
        /// ItemBonusHpCurrent 변경 알림(저장/UI 갱신 등 외부 구독용).
        /// </summary>
        public event Action<long> ItemBonusHpChanged;

        /// <summary>
        /// ItemBonusHpCurrent가 0이 되어 소멸한 순간 1회 호출됩니다.
        /// </summary>
        public event Action ItemBonusHpDepleted;

        /// <summary>
        /// 리소스 동기화(최대치 변경 시 현재값 보정)
        /// 특정 구간에서 Current 값을 직접 세팅하는 경우, 자동 보정을 잠시 비활성화할 수 있습니다.
        /// </summary>
        private int _suppressAutoResourceSyncCount;

        private bool IsAutoResourceSyncSuppressed => _suppressAutoResourceSyncCount > 0;
        
        /// <summary>
        /// 특정 구간에서 Current 값을 직접 세팅해야 할 때, 최대치 변경 자동 보정을 잠시 비활성화합니다.
        /// - 예) 스탯 포인트 재분배 후 현재값을 비율 유지로 직접 세팅하는 경우
        /// </summary>
        protected IDisposable SuppressAutoResourceSync()
        {
            _suppressAutoResourceSyncCount++;
            return new AutoResourceSyncScope(this);
        }

        private readonly struct AutoResourceSyncScope : IDisposable
        {
            private readonly CharacterStat _owner;
            public AutoResourceSyncScope(CharacterStat owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                _owner._suppressAutoResourceSyncCount = Math.Max(0, _owner._suppressAutoResourceSyncCount - 1);
            }
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
        public long ConsumeItemBonusHp(long incomingDamage)
        {
            if (incomingDamage <= 0) return 0;

            long beforeCurrent = CurrentItemBonusHpTemp.Value;
            if (beforeCurrent <= 0) return incomingDamage;

            long consume = System.Math.Min(beforeCurrent, incomingDamage);
            long remainingBonus = beforeCurrent - consume;
            long remainingDamage = incomingDamage - consume;

            bool depleted = remainingBonus <= 0;
            SetItemBonusHpCurrentInternal(depleted ? 0 : remainingBonus, invokeDepleted: depleted);

            // NOTE:
            // - 소모형 추가 최대 HP(아이템 보너스 HP)의 “현재치”가 감소한 시점을 외부에서 해석할 수 있도록 훅을 제공합니다.
            // - 기본 구현은 no-op이며, 플레이어는 여기에서 “하트 1개 소모 → 최대치 영구 감소(저장)” 같은 규칙을 적용할 수 있습니다.
            OnItemBonusHpConsumed(beforeCurrent, depleted ? 0 : remainingBonus, consume);
            return remainingDamage;
        }

        /// <summary>
        /// ItemBonusHpCurrent(소모형 추가 HP)가 감소했을 때 호출되는 훅.
        /// </summary>
        /// <remarks>
        /// - <see cref="ConsumeItemBonusHp"/> 경로에서만 호출됩니다.
        /// - 기본 구현은 아무 것도 하지 않습니다.
        /// - 예: 플레이어는 “하트 단위 소모가 완료되면 ItemBonusHpTemp(최대치) 자체를 영구 감소” 같은 규칙을 적용할 수 있습니다.
        /// </remarks>
        protected virtual void OnItemBonusHpConsumed(long beforeCurrent, long afterCurrent, long consumedAmount)
        {
        }

        /// <summary>
        /// 저장/로드 또는 사망 처리 등에서 직접 값을 세팅할 때 사용합니다.
        /// </summary>
        public void SetItemBonusHpCurrent(long value)
        {
            SetItemBonusHpCurrentInternal(System.Math.Max(0, value), invokeDepleted: value <= 0 && TotalItemBonusHpTemp.Value > 0);
        }

        private void SetItemBonusHpCurrentInternal(long value, bool invokeDepleted)
        {
            value = System.Math.Max(0, value);
            // 임시 최대 HP(TotalTempHp)를 초과하지 않도록 클램프
            long tempMax = TotalItemBonusHpTemp.Value;
            if (tempMax > 0)
                value = System.Math.Min(value, tempMax);
            if (CurrentItemBonusHpTemp.Value == value)
                return;

            CurrentItemBonusHpTemp.OnNext(value);
            ItemBonusHpChanged?.Invoke(value);

            if (invokeDepleted)
            {
                // ItemBonus가 0이 되는 순간: 최대치(표시) 변화에 따른 클램프/리빌드 트리거
                if (CurrentHp.Value > TotalHp.Value)
                {
                    CurrentHp.OnNext(TotalHp.Value);
                }
                ItemBonusHpDepleted?.Invoke();
            }
        }
        
        #region 일반 HP
        
        public long GetItemBonusHpNormal() => _itemBonusProvider?.GetHpBonusNormal() ?? 0;
        protected void AddItemBonusHpNormal(long add, bool raiseEvent = true)
        {
            _itemBonusProvider?.AddHpBonusNormal(add, raiseEvent);
        }
        #endregion

        #region 임시 HP

        public long GetItemBonusHpTemp() => _itemBonusProvider?.GetHpBonusTemp() ?? 0;

        protected void AddItemBonusHpTemp(long add, bool raiseEvent = true)
        {
            _itemBonusProvider?.AddHpBonusTemp(add, raiseEvent);
        }

        /// <summary>
        /// 아이템 보너스 HP를 추가합니다.
        /// - 유일한 증가 경로(회복/리젠은 Base HP만 회복)
        /// </summary>
        public void AddItemBonusHp(long amount)
        {
            if (amount <= 0) return;

            long next = TotalItemBonusHpTemp.Value + amount;
            if (next < 0) next = long.MaxValue; // overflow 방어
            SetItemBonusHpCurrentInternal(next, invokeDepleted: false);
        }
        
        #endregion
        
    }
}