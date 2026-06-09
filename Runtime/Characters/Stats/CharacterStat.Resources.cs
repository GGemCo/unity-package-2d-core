using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    public partial class CharacterStat
    {
        [Header("상태 및 스탯")] public readonly BehaviorSubject<long> CurrentHp = new(0);
        public readonly BehaviorSubject<long> CurrentMp = new(0);
        public readonly BehaviorSubject<long> CurrentStamina = new(0);
        public readonly BehaviorSubject<int> CurrentSuperArmor = new(0);

        #region 생명력

        /// <summary>
        /// 현재 생명력이 최대치인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxHp()
        {
            return CurrentHp.Value >= MaxHp.Value;
        }
        /// <summary>
        /// 현재 생명력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddHp(int value)
        {
            long newVale = CurrentHp.Value + value;
            if (newVale > MaxHp.Value)
            {
                newVale = MaxHp.Value;
            }
            if (CurrentHp.Value == newVale) return;
            CurrentHp.OnNext(newVale);
        }

        /// <summary>
        /// 일반 HP를 최대치까지 회복합니다.
        /// 임시 HP(CurrentHpTemp)는 변경하지 않으므로, 휴식/여관처럼 일반 체력만 채워야 하는 상황에서 사용합니다.
        /// </summary>
        /// <param name="reviveIfDead">사망 상태에서도 HP를 회복할지 여부입니다.</param>
        /// <returns>일반 HP 값이 변경되었으면 <see langword="true"/>입니다.</returns>
        public bool RestoreNormalHpToMax(bool reviveIfDead = false)
        {
            long maxHp = MaxHp.Value;
            if (maxHp <= 0)
            {
                return false;
            }

            if (!reviveIfDead && CurrentHp.Value <= 0)
            {
                return false;
            }

            long nextHp = maxHp;

            if (CurrentHp.Value == nextHp)
            {
                return false;
            }

            CurrentHp.OnNext(nextHp);
            return true;
        }

        #endregion

        #region 마력

        /// <summary>
        /// 현재 마력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddMp(int value)
        {
            long newVale = CurrentMp.Value + value;
            if (newVale > MaxMp.Value)
            {
                newVale = MaxMp.Value;
            }
            if (CurrentMp.Value == newVale) return;
            CurrentMp.OnNext(newVale);
        }
        /// <summary>
        /// 현재 마력 빼기
        /// </summary>
        /// <param name="value"></param>
        public void MinusMp(int value)
        {
            long newVale = CurrentMp.Value - value;
            if (newVale < 0)
            {
                newVale = 0;
            }
            if (CurrentMp.Value == newVale) return;
            CurrentMp.OnNext(newVale);
        }

        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool IsMaxMp()
        {
            return CurrentMp.Value >= MaxMp.Value;
        }
        /// <summary>
        /// 현재 마력이 최대치 인지
        /// </summary>
        /// <returns></returns>
        public bool CheckNeedMp(int needMp)
        {
            return CurrentMp.Value >= needMp;
        }

        #endregion
        
        #region 슈퍼 아머
        public bool CanSpendSuperArmor(int amount)
        {
            if (amount <= 0) return true;
            return CurrentSuperArmor.Value >= amount;
        }

        /// <summary>
        /// 스테미나를 즉시 차감합니다.
        /// - 부족하면 차감하지 않고 false
        /// - 성공 시 0~TotalSuperArmor로 Clamp 합니다.
        /// </summary>
        public bool TrySpendSuperArmor(int amount)
        {
            if (amount <= 0) return true;

            int cur = CurrentSuperArmor.Value;
            if (cur < amount) return false;

            SetCurrentSuperArmorInternal(cur - amount);
            return true;
        }

        /// <summary>
        /// 스테미나를 회복합니다.
        /// - amount가 0 이하이면 아무 처리도 하지 않습니다.
        /// </summary>
        public void RestoreSuperArmor(int amount)
        {
            if (amount <= 0) return;
            SetCurrentSuperArmorInternal(CurrentSuperArmor.Value + amount);
        }

        private void SetCurrentSuperArmorInternal(int value)
        {
            int max = TotalSuperArmor.Value;
            if (max < 0) max = 0;

            if (value < 0) value = 0;
            if (value > max) value = max;

            if (CurrentSuperArmor.Value == value) return;
            CurrentSuperArmor.OnNext(value);
        }
        #endregion
        
        #region 스테미나

        /// <summary>
        /// 스테미나 소비 가능 여부를 반환합니다.
        /// - amount가 0 이하이면 항상 가능으로 처리합니다.
        /// </summary>
        public bool CanSpendStamina(long amount)
        {
            if (amount <= 0) return true;
            return CurrentStamina.Value >= amount;
        }

        /// <summary>
        /// 스테미나를 즉시 차감합니다.
        /// - 부족하면 차감하지 않고 false
        /// - 성공 시 0~MaxStamina로 Clamp 합니다.
        /// </summary>
        public bool TrySpendStamina(long amount)
        {
            if (amount <= 0) return true;

            long cur = CurrentStamina.Value;
            if (cur < amount) return false;

            SetCurrentStaminaInternal(cur - amount);
            return true;
        }

        /// <summary>
        /// 스테미나를 회복합니다.
        /// - amount가 0 이하이면 아무 처리도 하지 않습니다.
        /// </summary>
        public void RestoreStamina(long amount)
        {
            if (amount <= 0) return;
            SetCurrentStaminaInternal(CurrentStamina.Value + amount);
        }

        private void SetCurrentStaminaInternal(long value)
        {
            long max = MaxStamina.Value;
            if (max < 0) max = 0;

            if (value < 0) value = 0;
            if (value > max) value = max;

            if (CurrentStamina.Value == value) return;
            CurrentStamina.OnNext(value);
        }
        
        #endregion
    }
}