using System;
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
    public partial class CharacterStat : MonoBehaviour
    {
        // =========================
        // Batch Update (이벤트/발행 묶음)
        // =========================

        // 여러 값이 연쇄적으로 갱신되는 구간(로드/리빌드/장착 변경 등)에서
        // Recalculate는 허용하되 Publish를 지연하여 이벤트 폭발을 방지합니다.
        private int _batchUpdateCount;
        private bool _batchPublishPending;
        // 스탯 계산/발행 로직 모듈(군별 분리)
        private readonly List<ICharacterStatModule> _statModules = new(4);
        private bool _statModulesInitialized;

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
        public int BaseAtk { get; set; }
        public int BaseDef { get; set; }

        /// <summary>
        /// 기본 HP(베이스 값)입니다.
        /// </summary>
        public int BaseHp { get; set; }

        public int BaseMp { get; set; }
        public int BaseStamina { get; set; }
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
        private ItemBonusModifierProvider _itemBonusProvider;
        private RuntimeTempHpModifierProvider _runtimeTempHpProvider;

        /// <summary>
        /// 최종 계산에 포함되는 전체 Provider 목록입니다.
        /// </summary>
        private readonly List<IStatModifierProvider> _allProviders = new(4);

        /// <summary>
        /// 영구 Provider를 제외한 Provider 목록입니다(영구 modifier 가정 계산에 사용).
        /// </summary>
        private readonly List<IStatModifierProvider> _providersWithoutPersistent = new(3);
        // 내부 캐시(마지막으로 계산된 최종값)
        private long _totalAtk,
            _totalDef,
            _totalHp,
            _totalHpTemp,
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
        /// Provider 인스턴스를 생성하고 변경 이벤트를 연결합니다.
        /// </summary>
        protected virtual void Awake()
        {
            // Provider 초기화
            _equipmentProvider = new EquipmentOptionModifierProvider(gameObject);
            _persistentProvider = new PersistentModifierProvider();
            _passiveProvider = new PassiveSkillModifierProvider();
            _itemBonusProvider = new ItemBonusModifierProvider();
            _runtimeTempHpProvider = new RuntimeTempHpModifierProvider();

            _equipmentProvider.Changed += OnProviderChanged;
            _persistentProvider.Changed += OnProviderChanged;
            _passiveProvider.Changed += OnProviderChanged;
            _itemBonusProvider.Changed += OnProviderChanged;
            _runtimeTempHpProvider.Changed += OnProviderChanged;

            _allProviders.Clear();
            _allProviders.Add(_equipmentProvider);
            _allProviders.Add(_persistentProvider);
            _allProviders.Add(_passiveProvider);
            _allProviders.Add(_itemBonusProvider);
            _allProviders.Add(_runtimeTempHpProvider);

            _providersWithoutPersistent.Clear();
            _providersWithoutPersistent.Add(_equipmentProvider);
            _providersWithoutPersistent.Add(_passiveProvider);
            _providersWithoutPersistent.Add(_itemBonusProvider);
            _providersWithoutPersistent.Add(_runtimeTempHpProvider);
            
            EnsureStatModules();
        }

        private void EnsureStatModules()
        {
            if (_statModulesInitialized) return;
            _statModulesInitialized = true;

            _statModules.Clear();
            _statModules.Add(new ResourceStatModule(this));
            _statModules.Add(new CombatStatModule(this));
            _statModules.Add(new MovementStatModule(this));
            _statModules.Add(new ResistanceStatModule(this));
        }

        /// <summary>
        /// Provider의 이벤트 구독을 해제합니다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_equipmentProvider != null) _equipmentProvider.Changed -= OnProviderChanged;
            if (_persistentProvider != null) _persistentProvider.Changed -= OnProviderChanged;
            if (_passiveProvider != null) _passiveProvider.Changed -= OnProviderChanged;
            if (_itemBonusProvider != null) _itemBonusProvider.Changed -= OnProviderChanged;
            if (_runtimeTempHpProvider != null) _runtimeTempHpProvider.Changed -= OnProviderChanged;
        }

        /// <summary>
        /// Unity Start 훅입니다(확장 지점).
        /// </summary>
        protected virtual void Start()
        {
        }

        
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
        public void SetStatPointModifiers(Dictionary<string, int> flatByStatKey,
            Dictionary<string, float> percentByStatKey)
        {
            // 기존 CharacterStat은 SetStatPointModifiers만으로 재계산을 수행하지 않았습니다.
            // (호출 측에서 RecalculateStats()를 호출하는 패턴 유지)
            _persistentProvider.SetModifiers(flatByStatKey, percentByStatKey, raiseEvent: false);
        }

        #region 패시브 스킬

        public void SyncPassiveBonusHpTempMaxFromProvider()
        {
            SetPassiveBonusHpTempMax(_passiveProvider?.GetHpBonusTemp() ?? 0);
        }

        /// <summary>
        /// 패시브 스킬(장착형) Modifier 값을 갱신합니다.
        /// - 장비/스탯포인트와 별도 버킷으로 관리됩니다.
        /// - 레벨 업/장착 변경 등에서 전체를 재구성하는 방식으로 호출하는 것을 권장합니다.
        /// </summary>
        /// <param name="flatByStatKey">스탯 키별 고정(Flat) 증가량입니다.</param>
        /// <param name="percentByStatKey">스탯 키별 퍼센트(Percent) 증가율입니다.</param>
        /// <param name="recalculate">true이면 변경 이벤트를 발생시켜 즉시 재계산합니다.</param>
        public void SetPassiveSkillModifiers(Dictionary<string, int> flatByStatKey,
            Dictionary<string, float> percentByStatKey, bool recalculate = true)
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
        #endregion

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
            int superArmor = (int)StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatSuperArmor,
                BaseSuperArmor,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long moveSpeed = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatMoveSpeed, BaseMoveSpeed,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long attackSpeed = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatAttackSpeed,
                BaseAttackSpeed,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long criticalDamage = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatCriticalDamage,
                BaseCriticalDamage,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long criticalProbability = StatCalculator.CalculateFinalProjected(
                ConfigCommon.StatusStatCriticalProbability, BaseCriticalProbability,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);

            long registFire = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceFire,
                BaseRegistFire,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registCold = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceCold,
                BaseRegistCold,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registLightning = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistanceLightning,
                BaseRegistLightning,
                flatPersistentProjected, percentPersistentProjected, _providersWithoutPersistent);
            long registPoison = StatCalculator.CalculateFinalProjected(ConfigCommon.StatusStatResistancePoison,
                BaseRegistPoison,
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
            EnsureStatModules();
            // 1) 계산
            for (int i = 0; i < _statModules.Count; i++)
                _statModules[i].Recalculate();

            // 배치 업데이트 중에는 발행을 지연합니다.
            if (_batchUpdateCount > 0)
            {
                _batchPublishPending = true;
                return;
            }

            // 2) 발행(스트림 업데이트)
            for (int i = 0; i < _statModules.Count; i++)
                _statModules[i].Publish();
        }

        /// <summary>
        /// 여러 스탯 변경이 연쇄적으로 일어나는 구간에서, Publish를 End 시점으로 지연하기 위한 스코프를 시작합니다.
        /// - 스코프 내에서 <see cref="RecalculateStats"/>가 호출되더라도 실제 발행은 지연됩니다.
        /// - 스코프 종료 시, 지연된 발행이 있으면 1회만 Publish 합니다.
        /// </summary>
        public IDisposable BeginBatchUpdate()
        {
            _batchUpdateCount++;
            return new BatchUpdateScope(this);
        }

        private void EndBatchUpdate()
        {
            _batchUpdateCount = Mathf.Max(0, _batchUpdateCount - 1);
            if (_batchUpdateCount > 0) return;

            if (!_batchPublishPending) return;
            _batchPublishPending = false;

            EnsureStatModules();
            for (int i = 0; i < _statModules.Count; i++)
                _statModules[i].Publish();
        }

        private readonly struct BatchUpdateScope : IDisposable
        {
            private readonly CharacterStat _owner;
            public BatchUpdateScope(CharacterStat owner) => _owner = owner;
            public void Dispose()
            {
                if (_owner == null) return;
                _owner.EndBatchUpdate();
            }
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
        
        protected void SubscribeResourceMaxChange(BehaviorSubject<long> totalMax, BehaviorSubject<long> current,
            CharacterConstants.ResourceMaxChangePolicy policy)
        {
            if (totalMax == null || current == null) return;

            long lastMax = totalMax.Value;
            bool isFirst = true;

            totalMax.Subscribe(newMax =>
                {
                    // BehaviorSubject는 Subscribe 즉시 현재값을 내보내므로, 최초 1회는 무시합니다.
                    if (isFirst)
                    {
                        isFirst = false;
                        lastMax = newMax;
                        return;
                    }

                    long oldMax = lastMax;
                    lastMax = newMax;

                    if (IsAutoResourceSyncSuppressed) return;
                    if (newMax == oldMax) return;

                    long newCur = EvaluateCurrentOnMaxChanged(current.Value, oldMax, newMax, policy);
                    if (newCur == current.Value) return;

                    current.OnNext(newCur);
                })
                .AddTo(this);
        }
        
        private static long EvaluateCurrentOnMaxChanged(long current, long oldMax, long newMax,
            CharacterConstants.ResourceMaxChangePolicy policy)
        {
            if (newMax < 0) newMax = 0;

            // 감소 시에는 어떤 정책이든 clamp가 최우선입니다.
            if (newMax < oldMax)
            {
                return Math.Clamp(current, 0, newMax);
            }

            // 증가 또는 초기화(동일 포함)
            switch (policy)
            {
                case CharacterConstants.ResourceMaxChangePolicy.AddDelta:
                {
                    long delta = newMax - oldMax;
                    long v = current + delta;
                    return Math.Clamp(v, 0, newMax);
                }

                case CharacterConstants.ResourceMaxChangePolicy.PreserveRatio:
                {
                    if (oldMax <= 0)
                    {
                        return Math.Clamp(current, 0, newMax);
                    }

                    float ratio = Mathf.Clamp01((float)current / oldMax);
                    long v = Mathf.RoundToInt(ratio * newMax);
                    return Math.Clamp(v, 0, newMax);
                }

                case CharacterConstants.ResourceMaxChangePolicy.KeepCurrent:
                default:
                    return Math.Clamp(current, 0, newMax);
            }
        }
    }
}