using System.Collections.Generic;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 스탯을 관리하는 Facade 컴포넌트입니다.
    /// - 스탯 변경 요인을 Provider(장비/영구/패시브)로 분리하여 유지보수성과 확장성을 높입니다.
    /// - 합산/계산 규칙은 <see cref="StatCalculator"/>에서 담당합니다.
    /// </summary>
    /// <remarks>
    /// Provider 목록:
    /// - 장비/옵션: <see cref="EquipmentOptionModifierProvider"/>
    /// - 영구(스탯 포인트 등): <see cref="PersistentModifierProvider"/>
    /// - 패시브 스킬: <see cref="PassiveSkillModifierProvider"/>
    /// </remarks>
    public class CharacterStat : MonoBehaviour
    {
        /// <summary>
        /// 캐릭터의 계산된 스탯 총합 스냅샷(읽기 전용)입니다.
        /// - UI 미리보기/시뮬레이션 등에서 특정 시점의 값을 전달하기 위해 사용합니다.
        /// </summary>
        public readonly struct CharacterTotals
        {
            public readonly long Atk;
            public readonly long Def;
            public readonly long Hp;
            public readonly long Mp;
            public readonly long Stamina;
            public readonly int SuperArmor;
            public readonly long MoveSpeed;
            public readonly long AttackSpeed;
            public readonly long CriticalDamage;
            public readonly long CriticalProbability;
            public readonly long RegistFire;
            public readonly long RegistCold;
            public readonly long RegistLightning;
            public readonly long RegistPoison;

            /// <summary>
            /// 모든 스탯 총합 값을 받아 스냅샷을 생성합니다.
            /// </summary>
            public CharacterTotals(
                long atk, long def, long hp, long mp, long stamina,
                int superArmor,
                long moveSpeed, long attackSpeed,
                long criticalDamage, long criticalProbability,
                long registFire, long registCold, long registLightning, long registPoison)
            {
                Atk = atk;
                Def = def;
                Hp = hp;
                Mp = mp;
                Stamina = stamina;
                SuperArmor = superArmor;
                MoveSpeed = moveSpeed;
                AttackSpeed = attackSpeed;
                CriticalDamage = criticalDamage;
                CriticalProbability = criticalProbability;
                RegistFire = registFire;
                RegistCold = registCold;
                RegistLightning = registLightning;
                RegistPoison = registPoison;
            }
        }

        // 기본 스탯(베이스 값). Provider의 modifier들이 이 값에 누적되어 최종값이 계산됩니다.
        private int BaseAtk { get; set; }
        private int BaseDef { get; set; }

        /// <summary>
        /// 기본 HP(베이스 값)입니다.
        /// </summary>
        public int BaseHp { get; set; }

        private int BaseMp { get; set; }
        private int BaseStamina { get; set; }
        private int BaseSuperArmor { get; set; }
        private int BaseMoveSpeed { get; set; }
        private int BaseAttackSpeed { get; set; }
        private int BaseCriticalDamage { get; set; }
        private int BaseCriticalProbability { get; set; }
        private int BaseRegistFire { get; set; }
        private int BaseRegistCold { get; set; }
        private int BaseRegistLightning { get; set; }
        private int BaseRegistPoison { get; set; }

        // Provider 분리(장비/영구/패시브)
        private EquipmentOptionModifierProvider _equipmentProvider;
        private PersistentModifierProvider _persistentProvider;
        private PassiveSkillModifierProvider _passiveProvider;

        /// <summary>
        /// 최종 계산에 포함되는 전체 Provider 목록입니다.
        /// </summary>
        private readonly List<IStatModifierProvider> _allProviders = new(3);

        /// <summary>
        /// 영구 Provider를 제외한 Provider 목록입니다(영구 modifier 가정 계산에 사용).
        /// </summary>
        private readonly List<IStatModifierProvider> _providersWithoutPersistent = new(2);

        // 내부 캐시(마지막으로 계산된 최종값)
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
            _totalRegistLightning,
            _totalRegistPoison;

        private int _totalSuperArmor;

        /// <summary>
        /// 최종 공격력(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalAtk = new(1);

        /// <summary>
        /// 최종 방어력(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalDef = new(1);

        /// <summary>
        /// 최종 HP(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalHp = new(100);

        /// <summary>
        /// 최종 MP(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalMp = new(100);

        /// <summary>
        /// 최종 스태미나(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalStamina = new(100);

        /// <summary>
        /// 최종 슈퍼아머(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<int> TotalSuperArmor = new(100);

        /// <summary>
        /// 최종 이동속도(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalMoveSpeed = new(100);

        /// <summary>
        /// 최종 공격속도(계산 결과)를 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalAttackSpeed = new(100);

        /// <summary>
        /// 최종 크리티컬 피해량(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalCriticalDamage = new(100);

        /// <summary>
        /// 최종 크리티컬 확률(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalCriticalProbability = new(100);

        /// <summary>
        /// 최종 화염 저항(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalRegistFire = new(100);

        /// <summary>
        /// 최종 냉기 저항(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalRegistCold = new(100);

        /// <summary>
        /// 최종 번개 저항(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalRegistLightning = new(100);

        /// <summary>
        /// 최종 독 저항(계산 결과)을 스트림으로 제공합니다.
        /// </summary>
        public readonly BehaviorSubject<long> TotalRegistPoison = new(100);

        /// <summary>
        /// Provider 인스턴스를 생성하고 변경 이벤트를 연결합니다.
        /// </summary>
        protected virtual void Awake()
        {
            // Provider 초기화
            _equipmentProvider = new EquipmentOptionModifierProvider(gameObject);
            _persistentProvider = new PersistentModifierProvider();
            _passiveProvider = new PassiveSkillModifierProvider();

            _equipmentProvider.Changed += OnProviderChanged;
            _persistentProvider.Changed += OnProviderChanged;
            _passiveProvider.Changed += OnProviderChanged;

            _allProviders.Clear();
            _allProviders.Add(_equipmentProvider);
            _allProviders.Add(_persistentProvider);
            _allProviders.Add(_passiveProvider);

            _providersWithoutPersistent.Clear();
            _providersWithoutPersistent.Add(_equipmentProvider);
            _providersWithoutPersistent.Add(_passiveProvider);
        }

        /// <summary>
        /// Provider의 이벤트 구독을 해제합니다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_equipmentProvider != null) _equipmentProvider.Changed -= OnProviderChanged;
            if (_persistentProvider != null) _persistentProvider.Changed -= OnProviderChanged;
            if (_passiveProvider != null) _passiveProvider.Changed -= OnProviderChanged;
        }

        /// <summary>
        /// Unity Start 훅입니다(확장 지점).
        /// </summary>
        protected virtual void Start() { }

        /// <summary>
        /// Provider 변경 이벤트를 받아 전체 스탯을 재계산합니다.
        /// </summary>
        private void OnProviderChanged()
        {
            RecalculateStats();
        }

        /// <summary>
        /// 스크립터블 오브젝트 등에 정의된 기본 스탯 값을 설정하고 즉시 재계산합니다.
        /// </summary>
        /// <param name="statAtk">기본 공격력입니다.</param>
        /// <param name="statDef">기본 방어력입니다.</param>
        /// <param name="statHp">기본 HP입니다.</param>
        /// <param name="statMp">기본 MP입니다.</param>
        /// <param name="statStamina">기본 스태미나입니다.</param>
        /// <param name="statSuperArmor">기본 슈퍼아머입니다.</param>
        /// <param name="statMoveSpeed">기본 이동속도입니다.</param>
        /// <param name="statAttackSpeed">기본 공격속도입니다.</param>
        /// <param name="statRegistFire">기본 화염 저항입니다.</param>
        /// <param name="statRegistCold">기본 냉기 저항입니다.</param>
        /// <param name="statRegistLightning">기본 번개 저항입니다.</param>
        /// <param name="statRegistPoison">기본 독 저항입니다.</param>
        protected void SetBaseInfos(int statAtk, int statDef, int statHp, int statMp, int statStamina,
            int statSuperArmor, int statMoveSpeed,
            int statAttackSpeed, int statRegistFire, int statRegistCold, int statRegistLightning, int statRegistPoison)
        {
            BaseAtk = statAtk;
            BaseDef = statDef;
            BaseHp = statHp;
            BaseMp = statMp;
            BaseStamina = statStamina;
            BaseSuperArmor = statSuperArmor;
            BaseMoveSpeed = statMoveSpeed;
            BaseAttackSpeed = statAttackSpeed;
            BaseRegistFire = statRegistFire;
            BaseRegistCold = statRegistCold;
            BaseRegistLightning = statRegistLightning;
            BaseRegistPoison = statRegistPoison;

            RecalculateStats();
        }

        /// <summary>
        /// 장착 아이템 정보를 기반으로 장비 Provider의 캐시를 갱신합니다.
        /// - 실제 재계산은 Provider의 Changed 이벤트에 의해 트리거됩니다.
        /// </summary>
        /// <param name="characterBase">캐릭터의 기본 정의(직업/성장 등) 정보입니다.</param>
        /// <param name="equippedItems">슬롯/부위 기준의 장착 아이템 참조 목록입니다.</param>
        public void UpdateStatCache(CharacterBase characterBase, Dictionary<int, EquippedItemRef> equippedItems)
        {
            _equipmentProvider.UpdateFromEquippedItems(characterBase, equippedItems);
            // Provider의 Changed 이벤트가 재계산을 트리거합니다.
        }

        /// <summary>
        /// 장비/옵션 버킷(Equipment Provider)에 스탯 변경값을 누적 적용합니다.
        /// </summary>
        /// <param name="modifiers">적용할 스탯 변경 목록입니다.</param>
        public void ApplyStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            _equipmentProvider.ApplyStatModifiers(modifiers);
        }

        /// <summary>
        /// 장비/옵션 버킷(Equipment Provider)에서 스탯 변경값을 제거합니다.
        /// </summary>
        /// <param name="modifiers">제거할 스탯 변경 목록입니다.</param>
        public void RemoveStatModifiers(List<ConfigCommon.StruckStatus> modifiers)
        {
            _equipmentProvider.RemoveStatModifiers(modifiers);
        }

        /// <summary>
        /// 스탯 포인트 등 영구 Modifier 값을 갱신합니다.
        /// - 장비 갱신(<see cref="UpdateStatCache"/>)과 무관하게 유지됩니다.
        /// - 이 메서드는 기본적으로 재계산을 트리거하지 않습니다(호출 측에서 <see cref="RecalculateStats"/> 호출).
        /// </summary>
        /// <param name="flatByStatKey">스탯 키별 고정(Flat) 증가량입니다.</param>
        /// <param name="percentByStatKey">스탯 키별 퍼센트(Percent) 증가율입니다.</param>
        public void SetStatPointModifiers(Dictionary<string, int> flatByStatKey, Dictionary<string, float> percentByStatKey)
        {
            // 기존 CharacterStat은 SetStatPointModifiers만으로 재계산을 수행하지 않았습니다.
            // (호출 측에서 RecalculateStats()를 호출하는 패턴 유지)
            _persistentProvider.SetModifiers(flatByStatKey, percentByStatKey, raiseEvent: false);
        }

        /// <summary>
        /// 패시브 스킬(장착형) Modifier 값을 갱신합니다.
        /// - 장비/스탯포인트와 별도 버킷으로 관리됩니다.
        /// - 레벨 업/장착 변경 등에서 전체를 재구성하는 방식으로 호출하는 것을 권장합니다.
        /// </summary>
        /// <param name="flatByStatKey">스탯 키별 고정(Flat) 증가량입니다.</param>
        /// <param name="percentByStatKey">스탯 키별 퍼센트(Percent) 증가율입니다.</param>
        /// <param name="recalculate">true이면 변경 이벤트를 발생시켜 즉시 재계산합니다.</param>
        public void SetPassiveSkillModifiers(Dictionary<string, int> flatByStatKey, Dictionary<string, float> percentByStatKey, bool recalculate = true)
        {
            _passiveProvider.SetModifiers(flatByStatKey, percentByStatKey, raiseEvent: recalculate);
            if (recalculate)
            {
                // Changed 이벤트로 인해 RecalculateStats()가 호출됩니다.
            }
        }

        /// <summary>
        /// 패시브 스킬 Modifier를 모두 제거합니다.
        /// </summary>
        /// <param name="recalculate">true이면 변경 이벤트를 발생시켜 즉시 재계산합니다.</param>
        public void ClearPassiveSkillModifiers(bool recalculate = true)
        {
            _passiveProvider.Clear(raiseEvent: recalculate);
        }

        /// <summary>
        /// (부작용 없음) 현재 장비/패시브 modifier는 유지한 채,
        /// 영구 modifier(스탯 포인트 등)만 특정 값으로 가정했을 때의 총합을 계산합니다.
        /// </summary>
        /// <param name="flatPersistentProjected">가정할 영구 Flat 증가량(스탯 키 기준)입니다.</param>
        /// <param name="percentPersistentProjected">가정할 영구 Percent 증가율(스탯 키 기준)입니다.</param>
        /// <returns>가정값을 반영하여 계산된 스탯 총합 스냅샷입니다.</returns>
        public CharacterTotals CalculateTotalsWithPersistentModifiers(
            Dictionary<string, int> flatPersistentProjected,
            Dictionary<string, float> percentPersistentProjected)
        {
            long atk = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatAtk, BaseAtk,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long def = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatDef, BaseDef,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long hp = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatHp, BaseHp,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long mp = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatMp, BaseMp,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long stamina = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatStamina, BaseStamina,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            int superArmor = (int)StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatSuperArmor, BaseSuperArmor,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long moveSpeed = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatMoveSpeed, BaseMoveSpeed,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long attackSpeed = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatAttackSpeed, BaseAttackSpeed,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long criticalDamage = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatCriticalDamage, BaseCriticalDamage,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long criticalProbability = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatCriticalProbability, BaseCriticalProbability,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long registFire = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceFire, BaseRegistFire,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registCold = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceCold, BaseRegistCold,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registLightning = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceLightning, BaseRegistLightning,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registPoison = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistancePoison, BaseRegistPoison,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            return new CharacterTotals(
                atk, def, hp, mp, stamina,
                superArmor,
                moveSpeed, attackSpeed,
                criticalDamage, criticalProbability,
                registFire, registCold, registLightning, registPoison);
        }

        /// <summary>
        /// 모든 Provider(장비/영구/패시브)를 반영하여 최종 스탯을 재계산하고,
        /// 각 <see cref="BehaviorSubject{T}"/>에 계산 결과를 발행합니다.
        /// </summary>
        public void RecalculateStats()
        {
            _totalAtk = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAtk, BaseAtk, _allProviders);
            _totalDef = StatCalculator.CalculateFinal(ConfigCommon.StatusStatDef, BaseDef, _allProviders);
            _totalHp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatHp, BaseHp, _allProviders);
            _totalMp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMp, BaseMp, _allProviders);
            _totalStamina = StatCalculator.CalculateFinal(ConfigCommon.StatusStatStamina, BaseStamina, _allProviders);
            _totalSuperArmor = (int)StatCalculator.CalculateFinal(ConfigCommon.StatusStatSuperArmor, BaseSuperArmor, _allProviders);

            _totalMoveSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMoveSpeed, BaseMoveSpeed, _allProviders);
            _totalAttackSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAttackSpeed, BaseAttackSpeed, _allProviders);

            _totalCriticalDamage = StatCalculator.CalculateFinal(ConfigCommon.StatusStatCriticalDamage, BaseCriticalDamage, _allProviders);
            _totalCriticalProbability = StatCalculator.CalculateFinal(ConfigCommon.StatusStatCriticalProbability, BaseCriticalProbability, _allProviders);

            _totalRegistFire = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceFire, BaseRegistFire, _allProviders);
            _totalRegistCold = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceCold, BaseRegistCold, _allProviders);
            _totalRegistLightning = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceLightning, BaseRegistLightning, _allProviders);
            _totalRegistPoison = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistancePoison, BaseRegistPoison, _allProviders);

            TotalAtk.OnNext(_totalAtk);
            TotalDef.OnNext(_totalDef);
            TotalHp.OnNext(_totalHp);
            TotalMp.OnNext(_totalMp);
            TotalStamina.OnNext(_totalStamina);
            TotalSuperArmor.OnNext(_totalSuperArmor);
            TotalMoveSpeed.OnNext(_totalMoveSpeed);
            TotalAttackSpeed.OnNext(_totalAttackSpeed);
            TotalCriticalDamage.OnNext(_totalCriticalDamage);
            TotalCriticalProbability.OnNext(_totalCriticalProbability);
            TotalRegistFire.OnNext(_totalRegistFire);
            TotalRegistCold.OnNext(_totalRegistCold);
            TotalRegistLightning.OnNext(_totalRegistLightning);
            TotalRegistPoison.OnNext(_totalRegistPoison);
        }

        /// <summary>
        /// 현재 이동속도를 반환합니다.
        /// </summary>
        /// <param name="isPercent">true이면 100 기준 퍼센트 값(예: 120 → 1.2)으로 변환합니다.</param>
        /// <returns>이동속도 값(퍼센트 변환 여부에 따라 스케일이 달라집니다).</returns>
        public float GetCurrentMoveSpeed(bool isPercent = true)
            => isPercent ? TotalMoveSpeed.Value / 100f : TotalMoveSpeed.Value;

        /// <summary>
        /// 현재 공격속도를 100 기준 퍼센트 값으로 반환합니다.
        /// </summary>
        /// <returns>공격속도(예: 120 → 1.2)입니다.</returns>
        public float GetCurrentAttackSpeed() => TotalAttackSpeed.Value / 100f;

        /// <summary>
        /// 기본 이동속도를 변경하고 전체 스탯을 재계산합니다.
        /// </summary>
        /// <param name="value">설정할 기본 이동속도 값입니다(0 이하는 무시).</param>
        public void SetCurrentMoveSpeed(int value)
        {
            if (value <= 0) return;
            BaseMoveSpeed = value;
            RecalculateStats();
        }

        /// <summary>
        /// 현재 스탯(크리티컬 확률/피해량 포함)을 반영하여 1회 공격의 최종 피해를 계산합니다.
        /// </summary>
        /// <remarks>
        /// - 크리티컬 확률에 따라 난수(<see cref="Random.value"/>)로 크리티컬 여부를 결정합니다.
        /// - 크리티컬 피해 배율은 최소 1.0으로 보정합니다.
        /// </remarks>
        /// <returns>난수 결과를 반영한 1회 공격 피해량입니다.</returns>
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
        /// 크리티컬 기대값을 포함한 예상 평균 공격력을 계산합니다.
        /// </summary>
        /// <remarks>
        /// 기대값 = 일반 공격 * (1 - 크리확) + (일반 공격 * 크리배율) * 크리확
        /// </remarks>
        /// <returns>크리티컬 확률/배율을 반영한 평균 피해 기대값입니다.</returns>
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

        /// <summary>
        /// 지정한 Affect를 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="affectUid">적용할 Affect의 UID입니다.</param>
        /// <param name="duration">지속 시간(초)입니다.</param>
        public void ApplyAffect(int affectUid, float duration)
        {
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, duration);
        }

        /// <summary>
        /// 지정한 Affect를 현재 캐릭터에서 제거합니다.
        /// </summary>
        /// <param name="affectUid">제거할 Affect의 UID입니다.</param>
        public void RemoveAffect(int affectUid)
        {
            AffectRuntimeBridge.RemoveAffect(gameObject, affectUid);
        }
    }
}