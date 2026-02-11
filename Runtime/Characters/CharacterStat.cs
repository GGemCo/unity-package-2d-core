using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 스탯 관리
    /// </summary>
    public class CharacterStat : MonoBehaviour
    {
        // 기본 스탯
        private int BaseAtk { get; set; }
        private int BaseDef { get; set; }
        public int BaseHp { get; set; }
        private int BaseMp { get; set; }
        private int BaseStamina { get; set; }
        private int BaseMoveSpeed { get; set; }
        private int BaseAttackSpeed { get; set; }
        private int BaseCriticalDamage { get; set; }
        private int BaseCriticalProbability { get; set; }
        private int BaseRegistFire { get; set; }
        private int BaseRegistCold { get; set; }
        private int BaseRegistLightning { get; set; }

        private readonly Dictionary<string, int> _flatModifiers = new();
        private readonly Dictionary<string, float> _percentModifiers = new();

        private long _totalAtk,
            _totalDef,
            _totalHp,
            _totalMp,
            _totalStamina,
            _totalMoveSpeed,
            _totalAttackSpeed,
            _totalCriticalDamage,
            _totalCriticalProbability,
            _totalRegistFire,
            _totalRegistCold,
            _totalRegistLightning;
        // 최종 적용된 스탯 (캐싱)
        public readonly BehaviorSubject<long> TotalAtk = new(1);
        public readonly BehaviorSubject<long> TotalDef = new(1);
        public readonly BehaviorSubject<long> TotalHp = new(100);
        public readonly BehaviorSubject<long> TotalMp = new(100);
        public readonly BehaviorSubject<long> TotalStamina = new(100);
        public readonly BehaviorSubject<long> TotalMoveSpeed = new(100);
        public readonly BehaviorSubject<long> TotalAttackSpeed = new(100);
        public readonly BehaviorSubject<long> TotalCriticalDamage = new(100);
        public readonly BehaviorSubject<long> TotalCriticalProbability = new(100);
        public readonly BehaviorSubject<long> TotalRegistFire = new(100);
        public readonly BehaviorSubject<long> TotalRegistCold = new(100);
        public readonly BehaviorSubject<long> TotalRegistLightning = new(100);

        // 장비에서 부여된 Affect(착용 지속) 추적
        private readonly HashSet<int> _equipAppliedAffects = new();

        protected virtual void Awake() { }
        protected virtual void Start() { }

        /// <summary>
        /// 스크립터블 오브젝트에 설정된 base 값 셋팅
        /// </summary>
        /// <param name="statAtk"></param>
        /// <param name="statDef"></param>
        /// <param name="statHp"></param>
        /// <param name="statMp"></param>
        /// <param name="statStamina"></param>
        /// <param name="statMoveSpeed"></param>
        /// <param name="statAttackSpeed"></param>
        /// <param name="statRegistFire"></param>
        /// <param name="statRegistCold"></param>
        /// <param name="statRegistLightning"></param>
        protected void SetBaseInfos(int statAtk, int statDef, int statHp, int statMp, int statStamina, int statMoveSpeed,
            int statAttackSpeed, int statRegistFire, int statRegistCold, int statRegistLightning)
        {
            BaseAtk = statAtk;
            BaseDef = statDef;
            BaseHp = statHp;
            BaseMp = statMp;
            BaseStamina = statStamina;
            BaseMoveSpeed = statMoveSpeed;
            BaseAttackSpeed = statAttackSpeed;
            BaseRegistFire = statRegistFire;
            BaseRegistCold = statRegistCold;
            BaseRegistLightning = statRegistLightning;
            RecalculateStats();
        }

        /// <summary>
        /// 값 업데이트
        /// - 장착 아이템(정의/인스턴스)을 기준으로 최종 옵션을 계산하여 Stat/Affect를 반영한다.
        /// </summary>
        /// <param name="characterBase"></param>
        /// <param name="equippedItems"></param>
        public void UpdateStatCache(CharacterBase characterBase, Dictionary<int, EquippedItemRef> equippedItems)
        {
            _flatModifiers.Clear();
            _percentModifiers.Clear();

            var statModifiers = new List<ConfigCommon.StruckStatus>(32);
            var desiredEquipAffects = new HashSet<int>();

            // 옵션 리졸버(테이블 기반)
            var tables = TableLoaderManager.Instance;
            var resolver = new ItemOptionResolver(tables);

            var instanceStore = SceneGame.Instance?.saveDataManager?.ItemInstances;

            foreach (var kv in equippedItems)
            {
                var equipRef = kv.Value;
                if (equipRef == null || equipRef.ItemUid <= 0) continue;

                // 1) 인스턴스 기반이면 Base + Rolled 옵션을 사용
                if (equipRef.InstanceId <= 0 || instanceStore == null ||
                    !instanceStore.TryGet(equipRef.InstanceId, out var inst) || inst == null) continue;
                
                var options = resolver.ResolveFinalOptions(inst);
                ApplyOptionsFromEntries(options, statModifiers, desiredEquipAffects);
            }

            ApplyStatModifiers(statModifiers);
            SyncEquipAffects(desiredEquipAffects);
            RecalculateStats();
        }

        private void ApplyOptionsFromEntries(List<ItemOptionEntry> options, List<ConfigCommon.StruckStatus> outStatModifiers, HashSet<int> outEquipAffects)
        {
            if (options == null || options.Count <= 0) return;

            for (int i = 0; i < options.Count; i++)
            {
                var op = options[i];
                if (!op.IsValid) continue;

                switch (op.Kind)
                {
                    case ItemOptionKind.Stat:
                        // CharacterStat은 기존처럼 STAT_* key를 받아서 처리
                        outStatModifiers.Add(new ConfigCommon.StruckStatus(op.TargetId, op.Op, op.Value));
                        break;

                    case ItemOptionKind.Affect:
                    {
                        if (TryParseIntId(op.TargetId, out var affectUid) && affectUid > 0)
                        {
                            // 착용 지속 효과는 장비 변경 시점에 apply/remove를 동기화한다.
                            outEquipAffects.Add(affectUid);
                        }
                        break;
                    }

                    case ItemOptionKind.State:
                        // State는 Affect 패키지 쪽 정책(STATE -> Affect 매핑)에 따라 처리될 수 있다.
                        // Core에서는 브리지만 제공하고, 실제 매핑은 Affect 런타임에서 처리하도록 한다.
                        AffectRuntimeBridge.ApplyState(gameObject, op.TargetId, op.Duration);
                        break;

                    case ItemOptionKind.DamageType:
                        // DamageType 기반 옵션(예: 속성 추가/전환)은 전투 파이프라인에 맞춰 확장 필요.
                        // Core 기본안에서는 저항/증가를 Stat으로 처리하는 것을 권장한다.
                        break;
                }
            }
        }

        private void SyncEquipAffects(HashSet<int> desired)
        {
            // remove
            var toRemove = _equipAppliedAffects.Where(x => !desired.Contains(x)).ToArray();
            for (int i = 0; i < toRemove.Length; i++)
            {
                RemoveAffect(toRemove[i]);
                _equipAppliedAffects.Remove(toRemove[i]);
            }

            // apply
            foreach (var uid in desired)
            {
                if (_equipAppliedAffects.Contains(uid)) continue;
                ApplyAffect(uid, 0);
                _equipAppliedAffects.Add(uid);
            }
        }

        private static bool TryParseIntId(string v, out int id)
        {
            id = 0;
            if (string.IsNullOrEmpty(v)) return false;
            return int.TryParse(v, out id);
        }

        /// <summary>
        /// 버프 적용하기
        /// </summary>
        /// <param name="affectUid"></param>
        /// <param name="duration"></param>
        protected void ApplyAffect(int affectUid, float duration)
        {
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, duration);
        }

        /// <summary>
        /// 버프 해제하기
        /// </summary>
        /// <param name="affectUid"></param>
        public void RemoveAffect(int affectUid)
        {
            AffectRuntimeBridge.RemoveAffect(gameObject, affectUid);
        }

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
                    _flatModifiers[baseStat] = _flatModifiers.GetValueOrDefault(baseStat, 0) + (isAdding ? (int)value : -(int)value);
                    if (_flatModifiers[baseStat] == 0) _flatModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Minus:
                {
                    _flatModifiers[baseStat] = _flatModifiers.GetValueOrDefault(baseStat, 0) - (isAdding ? (int)value : -(int)value);
                    if (_flatModifiers[baseStat] == 0) _flatModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Increase:
                {
                    _percentModifiers[baseStat] = _percentModifiers.GetValueOrDefault(baseStat, 0) + (isAdding ? value : -value);
                    if (Mathf.Approximately(_percentModifiers[baseStat], 0)) _percentModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.Decrease:
                {
                    _percentModifiers[baseStat] = _percentModifiers.GetValueOrDefault(baseStat, 0) - (isAdding ? value : -value);
                    if (Mathf.Approximately(_percentModifiers[baseStat], 0)) _percentModifiers.Remove(baseStat);
                    break;
                }
                case ConfigCommon.SuffixType.None:
                default:
                {
                    // legacy/간편 표기: None이면 Plus로 간주(기존 OptionType* 호환)
                    _flatModifiers[baseStat] = _flatModifiers.GetValueOrDefault(baseStat, 0) + (isAdding ? (int)value : -(int)value);
                    if (_flatModifiers[baseStat] == 0) _flatModifiers.Remove(baseStat);
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
            int flatBonus = _flatModifiers.GetValueOrDefault(statKey, 0);
            float percentBonus = _percentModifiers.GetValueOrDefault(statKey, 0);

            float finalMultiplier = 1 + (percentBonus / 100f);
            if (finalMultiplier < 0) finalMultiplier = 0; // 최소 0으로 제한

            return (long)((baseValue + flatBonus) * finalMultiplier);
        }
        /// <summary>
        /// 최종 계산하기
        /// </summary>
        public void RecalculateStats()
        {
            _totalAtk = CalculateFinalStat(ConfigCommon.StatusStatAtk, BaseAtk);
            _totalDef = CalculateFinalStat(ConfigCommon.StatusStatDef, BaseDef);
            _totalHp = CalculateFinalStat(ConfigCommon.StatusStatHp, BaseHp);
            _totalMp = CalculateFinalStat(ConfigCommon.StatusStatMp, BaseMp);
            _totalStamina = CalculateFinalStat(ConfigCommon.StatusStatStamina, BaseStamina);
            _totalMoveSpeed = CalculateFinalStat(ConfigCommon.StatusStatMoveSpeed, BaseMoveSpeed);
            _totalAttackSpeed = CalculateFinalStat(ConfigCommon.StatusStatAttackSpeed, BaseAttackSpeed);
            _totalCriticalDamage = CalculateFinalStat(ConfigCommon.StatusStatCriticalDamage, BaseCriticalDamage);
            _totalCriticalProbability = CalculateFinalStat(ConfigCommon.StatusStatCriticalProbability, BaseCriticalProbability);
            _totalRegistFire = CalculateFinalStat(ConfigCommon.StatusStatResistanceFire, BaseRegistFire);
            _totalRegistCold = CalculateFinalStat(ConfigCommon.StatusStatResistanceCold, BaseRegistCold);
            _totalRegistLightning = CalculateFinalStat(ConfigCommon.StatusStatResistanceLightning, BaseRegistLightning);

            TotalAtk.OnNext(_totalAtk);
            TotalDef.OnNext(_totalDef);
            TotalHp.OnNext(_totalHp);
            TotalMp.OnNext(_totalMp);
            TotalStamina.OnNext(_totalStamina);
            TotalMoveSpeed.OnNext(_totalMoveSpeed);
            TotalAttackSpeed.OnNext(_totalAttackSpeed);
            TotalCriticalDamage.OnNext(_totalCriticalDamage);
            TotalCriticalProbability.OnNext(_totalCriticalProbability);
            TotalRegistFire.OnNext(_totalRegistFire);
            TotalRegistCold.OnNext(_totalRegistCold);
            TotalRegistLightning.OnNext(_totalRegistLightning);
        }

        public float GetCurrentMoveSpeed(bool isPercent = true) => isPercent ? TotalMoveSpeed.Value / 100f : TotalMoveSpeed.Value;
        public float GetCurrentAttackSpeed() => TotalAttackSpeed.Value / 100f;

        public void SetCurrentMoveSpeed(int value)
        {
            if (value <= 0) return;
            BaseMoveSpeed = value;
            RecalculateStats();
        }
        /// <summary>
        /// 최종 공격력 계산
        /// </summary>
        /// <returns>계산된 최종 공격력</returns>
        protected long CalculateFinalAttack()
        {
            long baseAttack = TotalAtk.Value;
            if (baseAttack <= 0) return 0;

            float finalDamage = baseAttack;
            float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);
            
            if (!(Random.value < criticalChance)) return Mathf.RoundToInt(finalDamage);
            
            float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);
            finalDamage *= critMultiplier;

            return Mathf.RoundToInt(finalDamage);
        }

        /// <summary>
        /// 예상 평균 공격력 (크리티컬 기대값 포함)
        /// </summary>
        public float CalculateExpectedAttack()
        {
            long baseAttack = TotalAtk.Value;
            if (baseAttack <= 0) return 0;

            float criticalChance = Mathf.Clamp01(TotalCriticalProbability.Value / 100f);
            float critMultiplier = Mathf.Max(1f, TotalCriticalDamage.Value / 100f);

            // 기대값 = 일반 공격 * (1 - 크리확) + 크리티컬 공격 * (크리확)
            float expectedDamage = baseAttack * (1 - criticalChance) + (baseAttack * critMultiplier * criticalChance);
            return expectedDamage;
        }
    }
}
