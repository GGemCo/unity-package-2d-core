using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터 스탯 관리
    /// </summary>
    public class CharacterStat : MonoBehaviour
    {
        // 기본 스탯
        private int BaseAtk { get; set; }
        private int BaseDef { get; set; }
        protected int BaseHp { get; set; }
        private int BaseMp { get; set; }
        private int BaseMoveSpeed { get; set; }
        private int BaseAttackSpeed { get; set; }
        private int BaseCriticalDamage { get; set; }
        private int BaseCriticalProbability { get; set; }
        private int BaseRegistFire { get; set; }
        private int BaseRegistCold { get; set; }
        private int BaseRegistLightning { get; set; }

        private readonly Dictionary<string, int> flatModifiers = new();
        private readonly Dictionary<string, float> percentModifiers = new();

        private long totalAtk,
            totalDef,
            totalHp,
            totalMp,
            totalMoveSpeed,
            totalAttackSpeed,
            totalCriticalDamage,
            totalCriticalProbability,
            totalRegistFire,
            totalRegistCold,
            totalRegistLightning;
        // 최종 적용된 스탯 (캐싱)
        public readonly BehaviorSubject<long> TotalAtk = new(1);
        public readonly BehaviorSubject<long> TotalDef = new(1);
        public readonly BehaviorSubject<long> TotalHp = new(100);
        public readonly BehaviorSubject<long> TotalMp = new(100);
        public readonly BehaviorSubject<long> TotalMoveSpeed = new(100);
        public readonly BehaviorSubject<long> TotalAttackSpeed = new(100);
        public readonly BehaviorSubject<long> TotalCriticalDamage = new(100);
        public readonly BehaviorSubject<long> TotalCriticalProbability = new(100);
        public readonly BehaviorSubject<long> TotalRegistFire = new(100);
        public readonly BehaviorSubject<long> TotalRegistCold = new(100);
        public readonly BehaviorSubject<long> TotalRegistLightning = new(100);

        protected AffectController AffectController;

        protected virtual void Awake()
        {
        }
        protected virtual void Start()
        {
        }

        /// <summary>
        /// 스크립터블 오브젝트에 설정된 base 값 셋팅 
        /// </summary>
        /// <param name="statAtk"></param>
        /// <param name="statDef"></param>
        /// <param name="statHp"></param>
        /// <param name="statMp"></param>
        /// <param name="statMoveSpeed"></param>
        /// <param name="statAttackSpeed"></param>
        /// <param name="statRegistFire"></param>
        /// <param name="statRegistCold"></param>
        /// <param name="statRegistLightning"></param>
        protected void SetBaseInfos(int statAtk, int statDef, int statHp, int statMp, int statMoveSpeed,
            int statAttackSpeed, int statRegistFire, int statRegistCold, int statRegistLightning)
        {
            BaseAtk = statAtk;
            BaseDef = statDef;
            BaseHp = statHp;
            BaseMp = statMp;
            BaseMoveSpeed = statMoveSpeed;
            BaseAttackSpeed = statAttackSpeed;
            BaseRegistFire = statRegistFire;
            BaseRegistCold = statRegistCold;
            BaseRegistLightning = statRegistLightning;
            RecalculateStats();
        }

        /// <summary>
        /// 값 업데이트
        /// </summary>
        /// <param name="equippedItems"></param>
        public void UpdateStatCache(Dictionary<int, StruckTableItem> equippedItems)
        {
            flatModifiers.Clear();
            percentModifiers.Clear();

            List<ConfigCommon.StruckStatus> modifiers = new List<ConfigCommon.StruckStatus>();
            // 아이템 효과 적용
            foreach (var item in equippedItems.Select(items => items.Value))
            {
                if (item == null) continue;
                modifiers.Add(new ConfigCommon.StruckStatus(item.StatusID1, item.StatusSuffix1, item.StatusValue1));
                modifiers.Add(new ConfigCommon.StruckStatus(item.StatusID2, item.StatusSuffix2, item.StatusValue2));
                modifiers.Add(new ConfigCommon.StruckStatus(item.OptionType1, ConfigCommon.SuffixType.None, item.OptionValue1));
                modifiers.Add(new ConfigCommon.StruckStatus(item.OptionType2, ConfigCommon.SuffixType.None, item.OptionValue2));
                modifiers.Add(new ConfigCommon.StruckStatus(item.OptionType3, ConfigCommon.SuffixType.None, item.OptionValue3));
                modifiers.Add(new ConfigCommon.StruckStatus(item.OptionType4, ConfigCommon.SuffixType.None, item.OptionValue4));
                modifiers.Add(new ConfigCommon.StruckStatus(item.OptionType5, ConfigCommon.SuffixType.None, item.OptionValue5));
            }

            ApplyStatModifiers(modifiers);
            RecalculateStats();
        }
        /// <summary>
        /// 버프 적용하기
        /// </summary>
        /// <param name="affectUid"></param>
        protected void ApplyAffect(int affectUid) => AffectController.ApplyAffect(affectUid);
        /// <summary>
        /// 스탯 변경값 적용하기
        /// </summary>
        /// <param name="modifiers"></param>
        public void ApplyStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            foreach (var kvp in modifiers)
            {
                ModifyStat(kvp.ID, kvp, true);
            }
        }

        public void RemoveStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            foreach (var kvp in modifiers)
            {
                ModifyStat(kvp.ID, kvp, false);
            }
        }
        /// <summary>
        /// 접미사에 따라 적용할 값 배열에 넣기
        /// </summary>
        /// <param name="statType"></param>
        /// <param name="struckStatus"></param>
        /// <param name="isAdding"></param>
        private void ModifyStat(string statType, ConfigCommon.StruckStatus struckStatus, bool isAdding)
        {
            if (string.IsNullOrEmpty(statType)) return;

            string baseStat = statType;

            float value = struckStatus.Value;
            ConfigCommon.SuffixType suffixType = struckStatus.SuffixType;
            switch (suffixType)
            {
                case ConfigCommon.SuffixType.Plus:
                {
                    flatModifiers[baseStat] = flatModifiers.GetValueOrDefault(baseStat, 0) + (isAdding ? (int)value : -(int)value);
                    if (flatModifiers[baseStat] == 0) flatModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Minus:
                {
                    flatModifiers[baseStat] = flatModifiers.GetValueOrDefault(baseStat, 0) - (isAdding ? (int)value : -(int)value);
                    if (flatModifiers[baseStat] == 0) flatModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Increase:
                {
                    percentModifiers[baseStat] = percentModifiers.GetValueOrDefault(baseStat, 0) + (isAdding ? value : -value);
                    if (Mathf.Approximately(percentModifiers[baseStat], 0)) percentModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Decrease:
                {
                    percentModifiers[baseStat] = percentModifiers.GetValueOrDefault(baseStat, 0) - (isAdding ? value : -value);
                    if (Mathf.Approximately(percentModifiers[baseStat], 0)) percentModifiers.Remove(baseStat);
                    break;
                }
            }
        }
        /// <summary>
        /// 스탯별 최종 계산하기
        /// </summary>
        /// <param name="statKey"></param>
        /// <param name="baseValue"></param>
        /// <returns></returns>
        private long CalculateFinalStat(string statKey, int baseValue)
        {
            int flatBonus = flatModifiers.GetValueOrDefault(statKey, 0);
            float percentBonus = percentModifiers.GetValueOrDefault(statKey, 0);

            float finalMultiplier = 1 + (percentBonus / 100f);
            if (finalMultiplier < 0) finalMultiplier = 0; // 최소 0으로 제한

            return (long)((baseValue + flatBonus) * finalMultiplier);
        }
        /// <summary>
        /// 최종 계산하기
        /// </summary>
        public void RecalculateStats()
        {
            totalAtk = CalculateFinalStat(ConfigCommon.StatusStatAtk, BaseAtk);
            totalDef = CalculateFinalStat(ConfigCommon.StatusStatDef, BaseDef);
            totalHp = CalculateFinalStat(ConfigCommon.StatusStatHp, BaseHp);
            totalMp = CalculateFinalStat(ConfigCommon.StatusStatMp, BaseMp);
            totalMoveSpeed = CalculateFinalStat(ConfigCommon.StatusStatMoveSpeed, BaseMoveSpeed);
            totalAttackSpeed = CalculateFinalStat(ConfigCommon.StatusStatAttackSpeed, BaseAttackSpeed);
            totalCriticalDamage = CalculateFinalStat(ConfigCommon.StatusStatCriticalDamage, BaseCriticalDamage);
            totalCriticalProbability = CalculateFinalStat(ConfigCommon.StatusStatCriticalProbability, BaseCriticalProbability);
            totalRegistFire = CalculateFinalStat(ConfigCommon.StatusStatResistanceFire, BaseRegistFire);
            totalRegistCold = CalculateFinalStat(ConfigCommon.StatusStatResistanceCold, BaseRegistCold);
            totalRegistLightning = CalculateFinalStat(ConfigCommon.StatusStatResistanceLightning, BaseRegistLightning);

            ApplyStatChanges();
        }
        /// <summary>
        /// 변경한 스탯 r3 에 적용하기
        /// </summary>
        private void ApplyStatChanges()
        {
            TotalAtk.OnNext(totalAtk);
            TotalDef.OnNext(totalDef);
            TotalHp.OnNext(totalHp);
            TotalMp.OnNext(totalMp);
            TotalMoveSpeed.OnNext(totalMoveSpeed);
            TotalAttackSpeed.OnNext(totalAttackSpeed);
            TotalCriticalDamage.OnNext(totalCriticalDamage);
            TotalCriticalProbability.OnNext(totalCriticalProbability);
            TotalRegistFire.OnNext(totalRegistFire);
            TotalRegistCold.OnNext(totalRegistCold);
            TotalRegistLightning.OnNext(totalRegistLightning);
        }

        public float GetCurrentMoveSpeed(bool isPercent = true) => isPercent ? TotalMoveSpeed.Value / 100f : TotalMoveSpeed.Value;
        public float GetCurrentAttackSpeed() => TotalAttackSpeed.Value / 100f;

        public void SetCurrentMoveSpeed(int value)
        {
            if (value <= 0) return;
            BaseMoveSpeed = value;
            RecalculateStats();
        }
    }
}
