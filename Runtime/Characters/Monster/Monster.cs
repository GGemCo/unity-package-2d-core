using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 기본 클레스
    /// </summary>
    public class Monster : CharacterBase, IMonsterPoolLifecycle
    {
        [Tooltip("X좌표 움직임 여부")]
        public bool canMoveX = true;
        [Tooltip("Y좌표 움직임 여부")]
        public bool canMoveY = true;
        [Tooltip("패트롤 오브젝트")]
        public GameObject patrolObject;
        public void SetPatrolObject(GameObject value) => patrolObject = value;
        
        // 몬스터 행동 처리
        private ControllerMonster _controllerMonster;
        private MonsterDeathSkillController _deathSkillController;
        private float _delayDestroyMonster;
        private CharacterConstants.Grade _grade;
        public CharacterConstants.Grade Grade => _grade;

        // 공격 범위 안에 트리거/연출/보조 Collider가 함께 들어와도 HitArea가 잘리지 않도록 여유 있게 확보한다.
        private const int CountCollider = 32;
        private Collider2D[] _collider2Ds;
        
        private MonsterUIController _monsterUIController;
        private readonly List<IMonsterPoolLifecycle> _poolLifecycles = new(8);
        private bool _isPoolManaged;
        private Coroutine _returnToPoolRoutine;
        private GGemCoMonsterSettings _monsterSettings;
        private CutsceneManager _cutsceneManager;
        private bool _suppressNextDeadCutscene;
        private readonly List<IMonsterBrainRuntimeResettable> _brainRuntimeResetters = new(4);
        private bool _pendingBrainResetOnNextFadeIn;
        private MonsterThreatController _threatController;
        private MonsterDetectionSensor2D _detectionSensor;
        private MonsterCombatRangeProfile _combatRangeProfile;
        private MonsterThreatProfile _threatProfile;
        private MonsterHomeLeashController _homeLeashController;
        private MonsterLeashProfile _leashProfile;
        private MonsterEncounterMember _encounterMember;
        private MonsterEncounterProfile _encounterProfile;
        private MonsterAttackSlotController _attackSlotController;
        private MonsterAttackSlotProfile _attackSlotProfile;
        private int _currentLevel = 1;

        /// <summary>
        /// 현재 몬스터에 적용된 감지, 기본 공격 시작, 선호 거리, 추적 한계 프로필입니다.
        /// </summary>
        public MonsterCombatRangeProfile CombatRangeProfile => _combatRangeProfile;

        /// <summary>현재 몬스터에 적용된 Threat 누적 및 타겟 전환 프로필입니다.</summary>
        public MonsterThreatProfile ThreatProfile => _threatProfile;

        /// <summary>현재 Threat 순위로 선택된 전투 타겟입니다.</summary>
        public CharacterBase CurrentCombatTarget => _threatController != null ? _threatController.CurrentTarget : null;

        /// <summary>현재 기억 중인 유효 Threat 대상 수입니다.</summary>
        public int ThreatTargetCount => _threatController != null ? _threatController.TargetCount : 0;

        /// <summary>현재 몬스터에 적용된 홈 및 Leash 정책입니다.</summary>
        public MonsterLeashProfile LeashProfile => _leashProfile;

        /// <summary>현재 몬스터의 Encounter 그룹 멤버 컴포넌트입니다.</summary>
        public MonsterEncounterMember EncounterMember => _encounterMember;

        /// <summary>현재 몬스터에 적용된 Encounter 활성화 및 지원 어그로 정책입니다.</summary>
        public MonsterEncounterProfile EncounterProfile => _encounterProfile;

        /// <summary>현재 몬스터에 적용된 다수 공격 슬롯 정책입니다.</summary>
        public MonsterAttackSlotProfile AttackSlotProfile => _attackSlotProfile;

        /// <summary>현재 유효한 공격 슬롯을 예약했는지 여부입니다.</summary>
        public bool HasAttackSlotReservation => _attackSlotController != null && _attackSlotController.HasReservation;

        /// <summary>현재 예약된 공격 슬롯 인덱스입니다. 예약이 없으면 -1입니다.</summary>
        public int ReservedAttackSlotIndex => _attackSlotController != null ? _attackSlotController.ReservedSlotIndex : -1;

        /// <summary>현재 Leash 런타임 상태입니다.</summary>
        public MonsterLeashState LeashState =>
            _homeLeashController != null ? _homeLeashController.State : MonsterLeashState.Disabled;

        /// <summary>홈 복귀 또는 재활성 대기 중인지 여부입니다.</summary>
        public bool IsLeashReturnLocked => _homeLeashController != null && _homeLeashController.IsReturnLocked;

        /// <summary>현재 홈 복귀 정책으로 피해를 무시해야 하는지 여부입니다.</summary>
        public bool IsLeashDamageImmune => _homeLeashController != null && _homeLeashController.IsDamageImmune;

        /// <summary>현재 몬스터와 홈 위치 사이의 2D 거리입니다.</summary>
        public float DistanceFromHome =>
            _homeLeashController != null ? _homeLeashController.GetOwnerDistanceFromHome() : 0f;

        /// <summary>현재 전투 타겟과 홈 위치 사이의 2D 거리입니다.</summary>
        public float TargetDistanceFromHome =>
            _homeLeashController != null ? _homeLeashController.GetCurrentTargetDistanceFromHome() : 0f;

        /// <summary>
        /// 현재 스폰된 몬스터 인스턴스에 적용된 레벨입니다.
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// 다음 1회 공용 사망 컷신(CutsceneUidDie) 재생을 건너뜁니다.
        /// </summary>
        /// <remarks>
        /// 마지막 페이즈 종료 전용 전환 컷신을 우선 재생할 때 사용합니다.
        /// </remarks>
        public void SuppressNextDeadCutsceneOnce()
        {
            _suppressNextDeadCutscene = true;
        }
        
        public void SetPoolManaged(bool value)
        {
            _isPoolManaged = value;
        }

        public void CancelPendingPoolReturn()
        {
            if (_returnToPoolRoutine != null)
            {
                StopCoroutine(_returnToPoolRoutine);
                _returnToPoolRoutine = null;
            }
        }

        /// <summary>
        /// 풀에서 다시 대여된 몬스터를 새 스폰 정보 기준으로 재초기화합니다.
        /// </summary>
        /// <param name="monsterUid">대여된 몬스터에 적용할 몬스터 테이블 UID입니다.</param>
        /// <param name="regenData">몬스터를 배치하고 초기화할 리젠 데이터입니다.</param>
        public void PrepareForPoolRent(int monsterUid, CharacterRegenData regenData)
        {
            CancelPendingPoolReturn();
            ClearPendingDeathState();
            _suppressNextDeadCutscene = false;
            CharacterRegenData = regenData;
            uid = monsterUid;
            SetPoolManaged(true);
            SetHitAreaColliderEnabled(true);
            _attackSlotController?.ReleaseReservation();

            if (regenData != null)
            {
                transform.position = new Vector3(regenData.x, regenData.y, transform.position.z);
            }

            _threatController?.ClearAllThreats();
            SetAggro(false);
            SetBattleStatusNone();
            SetStatusNone();
            ClearSubStatus();
            SetAttackerTarget(null);
            _pendingBrainResetOnNextFadeIn = false;
            canMoveX = true;
            canMoveY = true;

            var motion = GetComponent<ICharacterMotionController>();
            motion?.CancelMotion(MotionChannel.Skill, reason: 9901);
            motion?.CancelMotion(MotionChannel.CrowdControl, reason: 9902);

            var physicsOverride = GetComponent<CharacterPhysicsOverrideController>();
            physicsOverride?.ForceRestoreBaseGravity();

            NotifyPoolRentLifecycles();

            StopAllCoroutines();
            _controllerMonster?.StopAttackCoroutine();

            AffectRuntimeBridge.RemoveAll(gameObject);
            InitializeByTable();
            InitializeByAnimationTable();
            InitializeByRegenData();

            _monsterUIController ??= new MonsterUIController();
            _monsterUIController.Initialize(this);
            _monsterUIController.RebuildRuntimeUi();
            EnableSuperArmor(CurrentSuperArmor.Value > 0);
        }

        /// <summary>
        /// 몬스터를 풀로 반환하기 전에 런타임 상태와 전투 잔여 효과를 정리합니다.
        /// </summary>
        public void PrepareForPoolReturn()
        {
            CancelPendingPoolReturn();
            ClearPendingDeathState();
            _suppressNextDeadCutscene = false;
            _attackSlotController?.ReleaseReservation();
            _threatController?.ClearAllThreats();
            _controllerMonster?.StopAttackCoroutine();
            _controllerMonster?.StopAllCoroutines();

            var motion = GetComponent<ICharacterMotionController>();
            motion?.CancelMotion(MotionChannel.Skill, reason: 9911);
            motion?.CancelMotion(MotionChannel.CrowdControl, reason: 9912);

            var physicsOverride = GetComponent<CharacterPhysicsOverrideController>();
            physicsOverride?.ForceRestoreBaseGravity();

            NotifyPoolReturnLifecycles();

            AffectRuntimeBridge.RemoveAll(gameObject);
            SetAggro(false);
            SetBattleStatusNone();
            SetStatusNone();
            ClearSubStatus();
            SetAttackerTarget(null);
            _pendingBrainResetOnNextFadeIn = false;
            if (characterRigidbody2D != null)
            {
                characterRigidbody2D.linearVelocity = Vector2.zero;
                characterRigidbody2D.angularVelocity = 0f;
            }

            if (patrolObject != null)
            {
                Destroy(patrolObject);
                patrolObject = null;
            }

            _monsterUIController?.Dispose();
        }

        /// <summary>
        /// 풀 대여 생명주기를 구독한 컴포넌트에 대여 시점 초기화를 알립니다.
        /// </summary>
        /// <remarks>
        /// Crowd Control 같은 하위 컴포넌트 초기화도 이 생명주기 포트에서 처리하여
        /// 직접 호출과 생명주기 호출이 중복되지 않도록 합니다.
        /// </remarks>
        private void NotifyPoolRentLifecycles()
        {
            CollectPoolLifecycles();
            for (int i = 0; i < _poolLifecycles.Count; i++)
            {
                var lifecycle = _poolLifecycles[i];
                if (lifecycle == null || ReferenceEquals(lifecycle, this))
                    continue;

                lifecycle.OnPoolRent(this);
            }
        }

        /// <summary>
        /// 풀 반납 생명주기를 구독한 컴포넌트에 반납 시점 정리를 알립니다.
        /// </summary>
        private void NotifyPoolReturnLifecycles()
        {
            CollectPoolLifecycles();
            for (int i = 0; i < _poolLifecycles.Count; i++)
            {
                var lifecycle = _poolLifecycles[i];
                if (lifecycle == null || ReferenceEquals(lifecycle, this))
                    continue;

                lifecycle.OnPoolReturn(this);
            }
        }

        /// <summary>
        /// 현재 몬스터 오브젝트에 연결된 풀 생명주기 컴포넌트를 수집합니다.
        /// </summary>
        private void CollectPoolLifecycles()
        {
            _poolLifecycles.Clear();
            GetComponents(_poolLifecycles);
        }

        protected override void Awake()
        {
            // 먼저 선언한다.
            IsUseSkill = true;
            _collider2Ds = new Collider2D[CountCollider];
            base.Awake();
            SetAttackType(CharacterConstants.AttackType.PassiveDefense);

            if (AddressableLoaderSettings.Instance)
            {
                _delayDestroyMonster = AddressableLoaderSettings.Instance.settings.delayDestroyMonster;
                _monsterSettings = AddressableLoaderSettings.Instance.monsterSettings;
            }

            _monsterUIController = new MonsterUIController();
            _monsterUIController.Initialize(this);
        }

        protected override void Start()
        {
            base.Start();
            
            _monsterUIController.InitSubscribe();
            
            EnableSuperArmor(CurrentSuperArmor.Value > 0);
            
            _cutsceneManager = SceneGame.Instance.CutsceneManager;
        }

        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public override void InitTagSortingLayer()
        {
            base.InitTagSortingLayer();
            tag = ConfigTags.GetValue(ConfigTags.Keys.Monster);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected override void InitComponents()
        {
            // AddComponent 순서 중요
            base.InitComponents();
            if (colliderHitArea)
                colliderHitArea.gameObject.layer = LayerMask.NameToLayer(ConfigLayer.GetValue(ConfigLayer.Keys.HitAreaMonster));
            
            _threatController = gameObject.GetComponent<MonsterThreatController>();
            if (_threatController == null)
            {
                _threatController = gameObject.AddComponent<MonsterThreatController>();
            }
            _threatController.Initialize(this);
            _threatController.ThreatTargetRegistered += OnThreatTargetRegistered;
            _threatController.ThreatTargetUnregistered += OnThreatTargetUnregistered;
            _threatController.CurrentTargetChanged += OnCurrentThreatTargetChanged;

            // 순서 중요. ControllerMonster 에서 콜라이더와 Threat 타겟을 사용합니다.
            _controllerMonster = gameObject.AddComponent<ControllerMonster>();
            _controllerMonster.Initialize(_collider2Ds);

            _homeLeashController = gameObject.GetComponent<MonsterHomeLeashController>();
            if (_homeLeashController == null)
            {
                _homeLeashController = gameObject.AddComponent<MonsterHomeLeashController>();
            }
            _homeLeashController.Initialize(this, _controllerMonster);

            _encounterMember = gameObject.GetComponent<MonsterEncounterMember>();
            if (_encounterMember == null)
            {
                _encounterMember = gameObject.AddComponent<MonsterEncounterMember>();
            }
            _encounterMember.Initialize(this);

            _attackSlotController = gameObject.GetComponent<MonsterAttackSlotController>();
            if (_attackSlotController == null)
            {
                _attackSlotController = gameObject.AddComponent<MonsterAttackSlotController>();
            }
            _attackSlotController.Initialize(this);

            _detectionSensor = gameObject.GetComponent<MonsterDetectionSensor2D>();
            if (_detectionSensor == null)
            {
                _detectionSensor = gameObject.AddComponent<MonsterDetectionSensor2D>();
            }
            _detectionSensor.Initialize(this);

            _deathSkillController = gameObject.GetComponent<MonsterDeathSkillController>();
            if (_deathSkillController == null)
            {
                _deathSkillController = gameObject.AddComponent<MonsterDeathSkillController>();
            }
            _deathSkillController.Initialize(this);
        }
        /// <summary>
        /// 리젠 데이터에 저장된 맵 배치 정보를 몬스터 런타임 상태에 반영합니다.
        /// </summary>
        protected override void InitializeByRegenData()
        {
            // 맵 배치툴로 저장한 정보가 있을 경우 
            if (CharacterRegenData == null) return;

            // 맵 컬링 정책은 풀 재사용 시 이전 몬스터의 정책이 남지 않도록 리젠 데이터 기준으로 매번 갱신합니다.
            SetMapVisibilityPolicy(CharacterRegenData.MapVisibilityPolicy);
            // UpdateDirection() 에서 초기 방향 처리를 위해 추가
            directionNormalize = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            SetFlip(CharacterRegenData.IsFlip);
            canMoveX = CharacterRegenData.CanMoveX;
            canMoveY = CharacterRegenData.CanMoveY;

            // Override 값이 없는 기존 배치 데이터는 InitializeByTable에서 적용한 테이블 기본값을 유지합니다.
            if (CharacterRegenData.HasAttackTypeOverride)
            {
                SetAttackType(CharacterRegenData.AttackTypeOverride);
            }

            _homeLeashController?.CaptureHome(CharacterRegenData);
        }

        /// <summary>
        /// 맵 배치 AttackType Override를 리젠 데이터와 현재 몬스터 상태에 함께 적용합니다.
        /// </summary>
        /// <param name="attackTypeOverride">배치별 Override 값입니다. null이면 테이블 기본값을 적용합니다.</param>
        /// <param name="tableAttackType">Override가 없을 때 사용할 monster 테이블의 기본 공격 성향입니다.</param>
        public void ApplyAttackTypeOverride(
            CharacterConstants.AttackType? attackTypeOverride,
            CharacterConstants.AttackType tableAttackType)
        {
            if (CharacterRegenData != null)
            {
                CharacterRegenData.HasAttackTypeOverride = attackTypeOverride.HasValue;
                CharacterRegenData.AttackTypeOverride = attackTypeOverride.GetValueOrDefault();
            }

            SetAttackType(attackTypeOverride ?? tableAttackType);
        }

        /// <summary>
        /// 현재 맵의 Parallax 사용 여부에 따라 몬스터 이동 경계 제한 정책을 적용합니다.
        /// </summary>
        /// <param name="mapData">현재 적용할 맵 테이블 데이터입니다.</param>
        public void ApplyMapBoundaryOverrides(StruckTableMap mapData)
        {
            if (_controllerMonster == null)
            {
                _controllerMonster = GetComponent<ControllerMonster>();
            }

            // 실제 X/Y 좌표 Clamp는 ControllerMonster가 담당하므로 컨트롤러 정책만 갱신합니다.
            _controllerMonster?.ApplyMapBoundaryOverrides(mapData);
        }
        /// <summary>
        /// 테이블에서 가져온 몬스터 정보 셋팅
        /// </summary>
        protected override void InitializeByTable()
        {
            base.InitializeByTable();
            if (TableLoaderManager.Instance == null) return;
            if (uid <= 0) return;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            // monster 테이블 정보 셋팅
            var info = tableLoaderManager.GetMonsterData(uid);
            // GcLogger.Log("InitializationStat uid: "+uid+" / info.uid: "+info.uid+" / BaseMoveSpeed: "+info.statMoveSpeed);
            if (info.Uid <= 0) return;
            _grade = info.Grade;
            _currentLevel = ResolveSpawnLevel(info);
            characterName = info.Name;
            var baseAttributes = new CharacterBaseAttributeValues
            {
                atk = info.BaseAtk,
                def = info.BaseDef,
                hp = info.BaseHp,
                mp = info.BaseMp,
                stamina = info.BaseStamina,
                superArmor = info.BaseSuperArmor,
                moveSpeed = info.BaseMoveSpeed,
                attackSpeed = info.BaseAttackSpeed,
                criticalDamage = info.BaseCriticalDamage,
                criticalProbability = info.BaseCriticalProbability,
                registFire = info.BaseRegistFire,
                registCold = info.BaseRegistCold,
                registLightning = info.BaseRegistLightning,
                registPoison = info.BaseRegistPoison,
                damageFire = info.BaseDamageFire,
                damageCold = info.BaseDamageCold,
                damageLightning = info.BaseDamageLightning,
                damagePoison = info.BaseDamagePoison,
                moveStep = ResolveMonsterMoveStep(info, tableLoaderManager),
            };

            var growthStats = new CharacterGrowthStatValues
            {
                atk = info.StatAtk,
                def = info.StatDef,
                hp = info.StatHp,
                mp = info.StatMp,
                stamina = info.StatStamina,
            };

            SetBaseAndGrowthStatInfos(baseAttributes, growthStats);
            CurrentHp.OnNext(MaxHp.Value);
            CurrentMp.OnNext(MaxMp.Value);
            CurrentStamina.OnNext(MaxStamina.Value);
            CurrentSuperArmor.OnNext(TotalSuperArmor.Value);
            if (baseAttributes.moveStep > 0)
            {
                currentMoveStep = baseAttributes.moveStep;
            }
            SetScale(info.Scale);
            SetAttackType(info.AttackType);

            StruckTableMonsterCombatProfile combatProfileData = null;
            int combatProfileUid = ResolveCombatProfileUid(info);
            if (combatProfileUid > 0)
            {
                tableLoaderManager.TryGetMonsterCombatProfileData(
                    combatProfileUid,
                    out combatProfileData,
                    logIfMissing: true);
            }

            _combatRangeProfile = MonsterCombatRangeProfile.Create(combatProfileData, colliderAttackRange);
            _threatProfile = MonsterThreatProfile.Create(combatProfileData);
            _leashProfile = MonsterLeashProfile.Create(combatProfileData);
            _encounterProfile = MonsterEncounterProfile.Create(combatProfileData);
            _attackSlotProfile = MonsterAttackSlotProfile.Create(combatProfileData);
            _threatController?.Configure(_threatProfile);
            _homeLeashController?.Configure(_leashProfile);
            _encounterMember?.Configure(CharacterRegenData?.patrolData, _encounterProfile);
            _attackSlotController?.Configure(_attackSlotProfile);
            _deathSkillController?.SetDeathSkillMonsterUid(info.DeathSkillMonsterUid);
        }

        /// <summary>
        /// 맵 배치 Override가 있으면 해당 Combat Profile UID를 사용하고, 없으면 monster 테이블 기본값을 사용합니다.
        /// </summary>
        /// <param name="info">현재 몬스터 테이블 데이터입니다.</param>
        /// <returns>이번 몬스터 인스턴스에 적용할 Combat Profile UID입니다.</returns>
        private int ResolveCombatProfileUid(StruckTableMonster info)
        {
            if (CharacterRegenData?.HasCombatProfileUidOverride == true)
            {
                return Mathf.Max(0, CharacterRegenData.CombatProfileUidOverride);
            }

            return Mathf.Max(0, info?.CombatProfileUid ?? 0);
        }


        /// <summary>
        /// 몬스터의 이동 스텝을 animation 테이블의 MoveStep 컬럼에서 조회합니다.
        /// </summary>
        /// <param name="info">몬스터 테이블 row 데이터입니다.</param>
        /// <param name="tableLoaderManager">테이블 데이터 접근 관리자입니다.</param>
        /// <returns>animation 테이블에 설정된 이동 스텝입니다. 유효하지 않으면 0을 반환합니다.</returns>
        private static int ResolveMonsterMoveStep(StruckTableMonster info, TableLoaderManager tableLoaderManager)
        {
            if (info == null || tableLoaderManager == null)
            {
                return 0;
            }

            if (info.AnimationUid <= 0)
            {
                return 0;
            }

            StruckTableAnimation animationInfo = tableLoaderManager.GetAnimationData(info.AnimationUid);
            if (animationInfo is not { Uid: > 0 })
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.RoundToInt(animationInfo.MoveStep));
        }

        /// <summary>
        /// 몬스터 테이블의 레벨 범위에서 이번 스폰에 사용할 레벨을 결정합니다.
        /// </summary>
        /// <param name="info">몬스터 테이블 row 데이터입니다.</param>
        /// <returns>이번 스폰에 적용할 몬스터 레벨입니다.</returns>
        private static int ResolveSpawnLevel(StruckTableMonster info)
        {
            if (info == null)
                return 1;

            int minLevel = Mathf.Max(1, info.MinLevel);
            int maxLevel = Mathf.Max(minLevel, info.MaxLevel);
            if (minLevel == maxLevel)
                return minLevel;

            // UnityEngine.Random.Range(int, int)는 최대값이 exclusive 이므로 +1 하여 테이블 범위를 포함합니다.
            return Random.Range(minLevel, maxLevel + 1);
        }

        protected override bool InitializeByAnimationTable()
        {
            if (!base.InitializeByAnimationTable()) return false;
            
            int animationUid = 0;
            if (type == CharacterConstants.Type.Npc)
            {
                var info = TableLoaderManager.Instance.GetNpcData(uid);
                if (info == null) return false;
                animationUid = info.AnimationUid;
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                var info = TableLoaderManager.Instance.GetMonsterData(uid);
                if (info == null) return false;
                animationUid = info.AnimationUid;
            }
            if (animationUid <= 0) return false;
            StruckTableAnimation struckTableAnimation = TableLoaderManager.Instance.GetAnimationData(animationUid);
            if (struckTableAnimation is not { Uid: > 0 }) return false;
            
            return true;
        }


        /// <summary>
        /// monster_combat_profile의 논리 감지 범위에서 플레이어를 발견했을 때 감지 Threat를 등록합니다.
        /// </summary>
        /// <param name="player">감지 범위에서 발견한 플레이어입니다.</param>
        public void OnDetectedPlayerByDetectionRange(Player player)
        {
            if (!CanTrackThreatTarget(player)) return;
            if (GetAttackType() != CharacterConstants.AttackType.AggroFirst) return;

            _threatController?.SetDetectionThreatState(
                player,
                isDetected: true,
                retainWhenLost: false,
                threatValue: _threatProfile.DetectionThreat);
        }

        /// <summary>
        /// 플레이어가 감지 이탈 범위 또는 추적 한계를 벗어났을 때 프로필 정책에 따라 감지 Threat를 전환합니다.
        /// </summary>
        /// <param name="player">감지 및 추적 유지 범위를 벗어난 플레이어입니다.</param>
        /// <remarks>
        /// DistanceBased 정책은 감지 계열 Threat를 제거하고,
        /// UntilCombatReleased 정책은 현재 전투 타겟을 감지 유지 Threat로 전환하여
        /// 명시적인 전투 종료까지 타겟을 유지합니다.
        /// </remarks>
        public void OnLostPlayerByDetectionRange(Player player)
        {
            _threatController?.SetDetectionThreatState(
                player,
                isDetected: false,
                retainWhenLost:
                    _threatProfile.RetainDetectedTargetUntilCombatReleased &&
                    CurrentCombatTarget == player,
                threatValue: _threatProfile.DetectionThreat);
        }

        /// <summary>
        /// 구형 공격 범위 Trigger 호출을 논리 감지 범위 진입 처리로 전달합니다.
        /// </summary>
        /// <param name="player">구형 공격 범위 Trigger에서 전달된 플레이어입니다.</param>
        /// <remarks>
        /// 신규 전투 진입은 <see cref="MonsterDetectionSensor2D"/>가 담당합니다.
        /// 기존 프리팹과 외부 호출의 호환성을 위해 이 진입점만 유지합니다.
        /// </remarks>
        public void OnDetectedPlayerByAttackRange(Player player)
        {
            OnDetectedPlayerByDetectionRange(player);
        }

        /// <summary>
        /// 기존 공격자 기반 피격 확장점으로 호출된 경우 최소 피해 Threat를 등록합니다.
        /// </summary>
        /// <param name="attacker">피격을 발생시킨 공격자 오브젝트입니다.</param>
        public override void OnDamage(GameObject attacker)
        {
            if (!TryResolveCharacterFromAttacker(attacker, out CharacterBase target) ||
                !CanTrackThreatTarget(target))
            {
                return;
            }

            _threatController?.AddDamageThreat(target, confirmedDamage: 1L);
        }

        /// <summary>
        /// 확정된 피해량을 공격자 Threat로 변환하여 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">방어력과 가드 판정 이후 확정된 데미지 정보입니다.</param>
        public override void OnDamageResolved(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null ||
                !TryResolveCharacterFromAttacker(metadataDamage.attacker, out CharacterBase target) ||
                !CanTrackThreatTarget(target))
            {
                return;
            }

            _threatController?.AddDamageThreat(target, metadataDamage.damage);
        }

        /// <summary>
        /// Leash 귀환 중 무적 정책을 포함하여 현재 몬스터가 피해를 받을 수 있는지 확인합니다.
        /// </summary>
        /// <param name="metadataDamage">적용 예정인 데미지 메타데이터입니다.</param>
        /// <returns>피해 처리를 계속할 수 있으면 <see langword="true"/>입니다.</returns>
        public override bool CanReceiveDamage(MetadataDamage metadataDamage)
        {
            return !IsLeashDamageImmune && base.CanReceiveDamage(metadataDamage);
        }

        /// <summary>
        /// 현재 선택된 전투 타겟을 반환합니다.
        /// </summary>
        /// <param name="target">현재 Threat 순위로 선택된 캐릭터입니다.</param>
        /// <returns>유효한 타겟이 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetCurrentCombatTarget(out CharacterBase target)
        {
            if (_threatController != null && _threatController.TryGetCurrentTarget(out target))
            {
                return true;
            }

            target = null;
            return false;
        }

        /// <summary>
        /// 지정한 대상에게 특정 원인의 Threat가 남아 있는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 캐릭터입니다.</param>
        /// <param name="source">확인할 Threat 원인입니다.</param>
        /// <returns>해당 원인의 Threat가 남아 있으면 <see langword="true"/>입니다.</returns>
        public bool HasThreatSource(CharacterBase target, MonsterThreatSource source)
        {
            return _threatController != null && _threatController.HasThreatSource(target, source);
        }

        /// <summary>
        /// 지정한 Transform에 대응하는 대상의 현재 총 Threat를 조회합니다.
        /// </summary>
        /// <param name="targetTransform">조회할 캐릭터 Transform 또는 하위 Transform입니다.</param>
        /// <param name="threat">누적된 총 Threat입니다.</param>
        /// <returns>대상이 Threat 목록에 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetThreat(Transform targetTransform, out float threat)
        {
            threat = 0f;
            CharacterBase target = ResolveCharacterFromTarget(targetTransform);
            return target != null && _threatController != null && _threatController.TryGetThreat(target, out threat);
        }

        /// <summary>
        /// 현재 Threat 목록을 다시 평가하여 최종 전투 타겟을 갱신합니다.
        /// </summary>
        /// <returns>유효한 전투 타겟이 선택되었으면 <see langword="true"/>입니다.</returns>
        public bool RefreshCombatTarget()
        {
            return _threatController != null && _threatController.RefreshCurrentTarget();
        }

        /// <summary>
        /// 도발, 보스 패턴, 지원 어그로 등 외부 시스템에서 지정한 Threat를 추가합니다.
        /// </summary>
        /// <param name="target">Threat를 추가할 캐릭터입니다.</param>
        /// <param name="amount">추가할 0보다 큰 Threat 값입니다.</param>
        /// <returns>Threat가 추가되었으면 <see langword="true"/>입니다.</returns>
        public bool AddExternalThreat(CharacterBase target, float amount)
        {
            return CanTrackThreatTarget(target) &&
                   _threatController != null &&
                   _threatController.AddThreat(target, amount, MonsterThreatSource.External);
        }

        /// <summary>
        /// 도발과 같은 외부 효과로 지정한 대상을 일정 시간 동안 최우선 타겟으로 고정합니다.
        /// </summary>
        /// <param name="target">강제로 선택할 캐릭터입니다.</param>
        /// <param name="durationSeconds">고정 시간입니다. 0 이하면 명시적으로 해제할 때까지 유지합니다.</param>
        /// <returns>강제 타겟을 적용했으면 <see langword="true"/>입니다.</returns>
        public bool ForceCombatTarget(CharacterBase target, float durationSeconds)
        {
            return CanTrackThreatTarget(target) &&
                   _threatController != null &&
                   _threatController.ForceTarget(target, durationSeconds);
        }

        /// <summary>
        /// 외부 시스템에서 적용한 강제 타겟을 해제합니다.
        /// </summary>
        public void ClearForcedCombatTarget()
        {
            _threatController?.ClearForcedTarget();
        }

        /// <summary>
        /// 맵 패트롤 데이터에 설정된 Encounter 그룹을 현재 몬스터에 연결합니다.
        /// </summary>
        /// <param name="patrolData">Encounter ID를 포함할 수 있는 맵 배치 데이터입니다.</param>
        public void ConfigureEncounter(PatrolData patrolData)
        {
            _encounterMember?.Configure(patrolData, _encounterProfile);
        }

        /// <summary>
        /// Encounter 그룹 활성화 또는 지원 어그로 대상을 등록합니다.
        /// </summary>
        /// <param name="target">그룹이 함께 교전할 대상입니다.</param>
        /// <param name="threatValue">등록할 Encounter Threat입니다.</param>
        /// <returns>Threat가 새로 등록되거나 변경되었으면 <see langword="true"/>입니다.</returns>
        public bool OnDetectedTargetByEncounter(CharacterBase target, float threatValue)
        {
            if (!CanTrackThreatTarget(target) || _threatController == null)
            {
                return false;
            }

            return _threatController.SetPresenceThreat(
                target,
                MonsterThreatSource.Encounter,
                isActive: true,
                threatValue);
        }

        /// <summary>
        /// Encounter 그룹 이탈 정책에 따라 지정 대상의 Encounter Threat만 제거합니다.
        /// </summary>
        /// <param name="target">Encounter Threat를 제거할 전투 대상입니다.</param>
        /// <returns>해당 원인의 Threat가 실제로 제거되었으면 <see langword="true"/>입니다.</returns>
        public bool OnLostTargetByEncounter(CharacterBase target)
        {
            return _threatController != null &&
                   _threatController.SetPresenceThreat(
                       target,
                       MonsterThreatSource.Encounter,
                       isActive: false,
                       threatValue: 0f);
        }

        /// <summary>현재 대상의 공격 슬롯을 예약할 수 있는지 확인합니다.</summary>
        public bool CanReserveAttackSlot()
        {
            return _attackSlotController == null || _attackSlotController.CanReserveCurrentTarget();
        }

        /// <summary>현재 대상의 공격 슬롯을 예약합니다.</summary>
        public bool TryReserveAttackSlot()
        {
            return _attackSlotController == null || _attackSlotController.TryReserveCurrentTarget();
        }

        /// <summary>공격 또는 스킬 행동 시작을 공격 슬롯 컨트롤러에 알립니다.</summary>
        /// <param name="waitForExplicitCompletion">명시적 완료 이벤트가 올 때까지 예약을 유지할지 여부입니다.</param>
        public void NotifyAttackSlotActionStarted(bool waitForExplicitCompletion = false)
        {
            _attackSlotController?.NotifyCombatActionStarted(waitForExplicitCompletion);
        }

        /// <summary>공격 또는 스킬 행동 완료를 공격 슬롯 컨트롤러에 알립니다.</summary>
        public void NotifyAttackSlotActionCompleted()
        {
            _attackSlotController?.NotifyCombatActionCompleted();
        }

        /// <summary>현재 보유한 공격 슬롯을 즉시 반환합니다.</summary>
        public void ReleaseAttackSlot()
        {
            _attackSlotController?.ReleaseReservation();
        }

        /// <summary>
        /// 현재 몬스터의 홈 위치를 조회합니다.
        /// </summary>
        /// <param name="homePosition">설정된 홈 월드 좌표입니다.</param>
        /// <returns>유효한 홈 정보가 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetHomePosition(out Vector3 homePosition)
        {
            if (_homeLeashController != null && _homeLeashController.Home.IsValid)
            {
                homePosition = _homeLeashController.Home.Position;
                return true;
            }

            homePosition = default;
            return false;
        }

        /// <summary>
        /// 외부 전투 규칙에서 몬스터의 Leash Evade와 홈 복귀를 시작합니다.
        /// </summary>
        /// <param name="trigger">홈 복귀를 시작한 원인입니다.</param>
        /// <returns>새로운 Evade가 시작되었으면 <see langword="true"/>입니다.</returns>
        public bool BeginLeashEvade(MonsterLeashTrigger trigger = MonsterLeashTrigger.Manual)
        {
            return _homeLeashController != null && _homeLeashController.BeginEvade(trigger);
        }

        /// <summary>
        /// Leash Evade 정책에 따라 몬스터의 전투 자원을 현재 최대값으로 회복합니다.
        /// </summary>
        internal void RestoreResourcesForLeash()
        {
            if (IsStatusDead())
            {
                return;
            }

            CurrentHp.OnNext(MaxHp.Value);
            CurrentMp.OnNext(MaxMp.Value);
            CurrentStamina.OnNext(MaxStamina.Value);
            CurrentSuperArmor.OnNext(TotalSuperArmor.Value);
            EnableSuperArmor(CurrentSuperArmor.Value > 0);
        }

        /// <summary>
        /// 현재 몬스터가 기억하는 모든 Threat와 전투 참여 관계를 제거합니다.
        /// </summary>
        public void ClearAllThreats()
        {
            _attackSlotController?.ReleaseReservation();
            _threatController?.ClearAllThreats();
            if (_threatController == null)
            {
                SetAggro(false);
            }
        }

        /// <summary>
        /// 어그로가 외부 경로에서 해제되면 남아 있는 Threat 목록도 함께 정리합니다.
        /// </summary>
        /// <param name="isAggro">변경된 어그로 활성 여부입니다.</param>
        protected override void OnAggroStateChanged(bool isAggro)
        {
            base.OnAggroStateChanged(isAggro);
            if (!isAggro && _threatController != null && _threatController.HasTargets)
            {
                _threatController.ClearAllThreats();
            }
        }

        /// <summary>
        /// Threat 목록에 플레이어가 처음 등록되면 플레이어의 전투 참여 목록에도 이 몬스터를 등록합니다.
        /// </summary>
        private void OnThreatTargetRegistered(CharacterBase target)
        {
            if (target is Player player)
            {
                player.RegisterCombatEngagement(this);
            }

            _encounterMember?.NotifyOwnerEngaged(target);
        }

        /// <summary>
        /// 플레이어의 모든 Threat 원인이 제거되면 플레이어 전투 참여 목록에서 이 몬스터를 해제합니다.
        /// </summary>
        private void OnThreatTargetUnregistered(CharacterBase target)
        {
            if (target is Player player)
            {
                player.UnregisterCombatEngagement(this);
            }
        }

        /// <summary>
        /// Threat 선택 결과를 기존 공격 대상 필드와 어그로 상태에 동기화합니다.
        /// </summary>
        private void OnCurrentThreatTargetChanged(CharacterBase previousTarget, CharacterBase currentTarget)
        {
            _controllerMonster?.StopAttackCoroutine();
            _attackSlotController?.OnCombatTargetChanged(previousTarget, currentTarget);
            SetAttackerTarget(currentTarget != null ? currentTarget.transform : null);

            if (currentTarget != null)
            {
                if (!IsAggro())
                {
                    SetAggro(true);
                }
                return;
            }

            if (IsAggro())
            {
                SetAggro(false);
            }
        }

        /// <summary>
        /// 지정한 캐릭터를 Threat 대상으로 등록할 수 있는지 확인합니다.
        /// </summary>
        private bool CanTrackThreatTarget(CharacterBase target)
        {
            return target != null &&
                   target != this &&
                   !IsStatusDead() &&
                   !target.IsStatusDead() &&
                   !IsLeashReturnLocked;
        }

        /// <summary>
        /// 공격자 오브젝트 또는 부모 계층에서 실제 캐릭터를 찾습니다.
        /// </summary>
        private static bool TryResolveCharacterFromAttacker(GameObject attacker, out CharacterBase target)
        {
            target = attacker != null
                ? attacker.GetComponent<CharacterBase>() ?? attacker.GetComponentInParent<CharacterBase>()
                : null;
            return target != null;
        }

        /// <summary>
        /// 지정한 Transform 또는 부모 계층에서 실제 캐릭터를 찾습니다.
        /// </summary>
        private static CharacterBase ResolveCharacterFromTarget(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            return target.GetComponent<CharacterBase>() ?? target.GetComponentInParent<CharacterBase>();
        }

        /// <summary>
        /// 몬스터 사망 시 모든 Threat, 전투 상태, UI와 후속 이벤트를 정리합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            base.OnDead(dieReasonType, attacker);
            SetHitAreaColliderEnabled(false);
            _attackSlotController?.ReleaseReservation();
            ClearAllThreats();
            SetAggro(false);

            if (_monsterUIController != null)
            {
                _monsterUIController.Dispose();
            }

            _controllerMonster?.StopAllCoroutines();
            
            var isPlayer = attacker && attacker.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            var data = new MonsterKilledEventData(
                dieReasonType,
                mapUid: CharacterRegenData.MapUid,
                monsterUid: uid,
                monsterVid: vid,
                monster: gameObject,
                attacker: attacker,
                isPlayerKiller: isPlayer,
                killerUid: null
            );
            GameEventManager.MonsterKilled(data);
            
            PlayDeadCutscene(attacker);
        }

        /// <summary>
        /// 몬스터 HitArea Collider 활성 상태를 변경합니다.
        /// 사망 직후에는 공격 범위/피격 판정에 남아 자동 이동 정지 상태를 유지하지 않도록 비활성화하고, 풀에서 재사용될 때 다시 활성화합니다.
        /// </summary>
        /// <param name="enabled">HitArea Collider 활성화 여부입니다.</param>
        private void SetHitAreaColliderEnabled(bool enabled)
        {
            if (colliderHitArea == null) return;
            colliderHitArea.enabled = enabled;
        }

        /// <summary>
        /// 몬스터가 실제 사망 상태로 전환되기 전에 사망 스킬 실행을 시도합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">스킬 종료 후 기본 사망 애니메이션을 재생할지 여부입니다.</param>
        /// <param name="deathPresentation">스킬 종료 후 적용할 사망 연출 요청입니다.</param>
        /// <returns>사망 처리를 보류하고 사망 스킬을 실행 중이면 <see langword="true"/>입니다.</returns>
        protected override bool TryBeginPreDeathAction(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            return _deathSkillController != null &&
                   _deathSkillController.TryBeginDeathSkill(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 사망 스킬 컨트롤러가 스킬 종료 후 기존 사망 처리를 이어가기 위해 호출합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        /// <param name="deathPresentation">사망 원인별 전용 연출 요청입니다.</param>
        internal void CompleteDeathSkillAction(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            CompleteDeferredDeath(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 사망 연출
        /// </summary>
        /// <param name="attacker"></param>
        private void PlayDeadCutscene(GameObject attacker)
        {
            if (_suppressNextDeadCutscene)
            {
                _suppressNextDeadCutscene = false;
                return;
            }

            if (!_monsterSettings.UseCutsceneDie) return;
            bool useCutscene = _monsterSettings.IsUseCutsceneDieEnabledFor(Grade);
            if (!useCutscene || !attacker) return;
            var player = attacker.GetComponent<Player>();
            if (player == null) return;
            _cutsceneManager.SetCharacterTargetOverride(CutsceneKeyCharacterTarget.Player, player);
            _cutsceneManager.SetCharacterTargetOverride(CutsceneKeyCharacterTarget.Monster, this);
            _cutsceneManager.SetOverlayTextOverride(CutsceneKeyTextOverlay.MonsterName, characterName);
            _cutsceneManager.PlayCutscene(_monsterSettings.CutsceneUidDie);
        }

        protected override void OnDestroy()
        {
            // 비풀링 제거 경로에서도 모든 대상의 Threat와 전투 참여 관계가 남지 않도록 정리합니다.
            ClearAllThreats();
            SetAggro(false);
            if (_threatController != null)
            {
                _threatController.ThreatTargetRegistered -= OnThreatTargetRegistered;
                _threatController.ThreatTargetUnregistered -= OnThreatTargetUnregistered;
                _threatController.CurrentTargetChanged -= OnCurrentThreatTargetChanged;
            }
            base.OnDestroy();
            if (_monsterUIController != null)
            {
                _monsterUIController.Dispose();
            }

            if (patrolObject != null)
            {
                Destroy(patrolObject);
            }
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public override void OnEventAttack(StruckAnimationEventAttack struckAnimationEventAttack)
        {
            if (IsStatusDead()) return;
            
            // GcLogger.Log(@event);
            long totalDamage = CalculateFinalAttack();
        
            // 캡슐 콜라이더 2D와 충돌 중인 모든 콜라이더를 검색
            Vector2 size = new Vector2(colliderAttackRange.size.x * Mathf.Abs(transform.localScale.x), colliderAttackRange.size.y * transform.localScale.y);
            Vector2 point = (Vector2)transform.position + colliderAttackRange.offset * transform.localScale;
            
            // ContactFilter2D.noFilter 사용 (필요하면 레이어/트리거 정책을 별도 생성해서 전달)
            int hitCount = CompatPhysics2D.OverlapCapsuleNonAlloc(
                point, size, colliderAttackRange.direction, 0f,
                _collider2Ds);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
                if (!hit || !hit.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) continue;
                CharacterHitArea characterHitArea = hit.GetComponent<CharacterHitArea>();
                if (characterHitArea == null) continue;
                
                CharacterBase player = characterHitArea.target;
                
                MetadataDamage metadataDamage = new MetadataDamage
                {
                    damage = totalDamage,
                    attacker = gameObject,
                    damageType = ConfigCommon.DamageType.Physic,
                    IncludeAttackerElementDamageParts = true,
                    affectUid = struckAnimationEventAttack.TargetAffectUid
                };
                player.TakeDamage(metadataDamage);
                break;
            }
        }

        /// <summary>
        /// 몬스터 페이드 인 시작 시점 처리.
        /// </summary>
        /// <remarks>
        /// 컬링 복귀 정책이 초기화인 경우 Idle/Wait로 정렬한 뒤 Brain 런타임을 초기화합니다.
        /// </remarks>
        protected override void OnStartFadeIn()
        {
            ApplyBrainResumePolicyOnFadeInIfNeeded();

            if (_monsterUIController != null)
            {
                _monsterUIController.StartFadeIn();
            }
        }

        /// <summary>
        /// 몬스터 페이드 아웃 시작 시점 처리.
        /// </summary>
        /// <remarks>
        /// 설정된 정책이 "다음 Fade In에서 초기화"인 경우 플래그를 기록합니다.
        /// </remarks>
        protected override void OnStartFadeOut()
        {
            MarkBrainResetPendingOnFadeOutIfNeeded();

            if (_monsterUIController != null)
            {
                _monsterUIController.StartFadeOut();
            }
        }

        /// <summary>
        /// Fade Out 이후 정책에 따라 다음 Fade In에서 Brain 초기화가 필요한지 기록합니다.
        /// </summary>
        private void MarkBrainResetPendingOnFadeOutIfNeeded()
        {
            if (_monsterSettings == null)
            {
                _pendingBrainResetOnNextFadeIn = false;
                return;
            }

            _pendingBrainResetOnNextFadeIn =
                _monsterSettings.CullingBrainResumePolicy == MonsterCullingBrainResumePolicy.ResetOnNextFadeIn;
        }

        /// <summary>
        /// Fade In 시점에 Brain 복귀 정책을 적용합니다.
        /// </summary>
        /// <remarks>
        /// 초기화 정책이면 <see cref="CharacterBase.Stop"/>으로 Idle/Wait를 보장하고,
        /// 설정값에 따라 어그로 판정을 초기화한 뒤 BT 런타임 리셋 인터페이스를 호출합니다.
        /// </remarks>
        private void ApplyBrainResumePolicyOnFadeInIfNeeded()
        {
            if (!_pendingBrainResetOnNextFadeIn)
            {
                return;
            }

            _pendingBrainResetOnNextFadeIn = false;
            Stop(isForce: true);
            _controllerMonster?.RequestWait();

            if (ShouldResetAggroOnCullingBrainReset())
            {
                _controllerMonster?.RequestClearAggro();
            }

            ResetToRegenPositionOnCullingBrainResetIfNeeded();
            ResetBrainRuntimeForCulling();
        }

        /// <summary>
        /// 컬링 복귀 시 Brain 초기화와 함께 어그로 판정을 초기화할지 여부를 반환합니다.
        /// </summary>
        /// <returns>어그로 판정도 초기화해야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldResetAggroOnCullingBrainReset()
        {
            return _monsterSettings != null && _monsterSettings.ResetAggroOnCullingBrainReset;
        }

        /// <summary>
        /// 컬링 복귀 시 Brain 초기화와 함께 리젠 좌표로 위치를 되돌릴지 여부를 반환합니다.
        /// </summary>
        /// <returns>리젠 좌표 리셋을 수행해야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldResetToRegenPositionOnCullingBrainReset()
        {
            return _monsterSettings != null && _monsterSettings.ResetToRegenPositionOnCullingBrainReset;
        }

        /// <summary>
        /// 설정에 따라 몬스터를 원래 리젠 좌표로 되돌립니다.
        /// </summary>
        /// <remarks>
        /// 리젠 데이터가 없으면 위치를 변경하지 않으며, 위치를 되돌린 경우 물리 속도도 함께 정리합니다.
        /// </remarks>
        private void ResetToRegenPositionOnCullingBrainResetIfNeeded()
        {
            if (!ShouldResetToRegenPositionOnCullingBrainReset())
            {
                return;
            }

            if (CharacterRegenData == null)
            {
                return;
            }

            transform.position = new Vector3(CharacterRegenData.x, CharacterRegenData.y, transform.position.z);

            if (characterRigidbody2D != null)
            {
                characterRigidbody2D.linearVelocity = Vector2.zero;
                characterRigidbody2D.angularVelocity = 0f;
            }
        }

        /// <summary>
        /// 몬스터에 부착된 Brain 런타임 리셋 가능 컴포넌트를 찾아 초기화를 요청합니다.
        /// </summary>
        private void ResetBrainRuntimeForCulling()
        {
            _brainRuntimeResetters.Clear();
            GetComponents(_brainRuntimeResetters);

            for (int i = 0; i < _brainRuntimeResetters.Count; i++)
            {
                var resetter = _brainRuntimeResetters[i];
                if (resetter == null)
                {
                    continue;
                }

                resetter.ResetRuntimeForCulling();
            }
        }

        public override void OnAnimationCompleteDead()
        {
            base.OnAnimationCompleteDead();

            if (_isPoolManaged && SceneGame.Instance != null && SceneGame.Instance.CharacterManager != null)
            {
                CancelPendingPoolReturn();
                _returnToPoolRoutine = StartCoroutine(ReturnToPoolAfterDelay(_delayDestroyMonster));
                return;
            }

            Destroy(gameObject, _delayDestroyMonster);
        }

        private System.Collections.IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            _returnToPoolRoutine = null;
            if (SceneGame.Instance != null && SceneGame.Instance.CharacterManager != null &&
                SceneGame.Instance.CharacterManager.ReturnMonsterToPool(this))
            {
                yield break;
            }

            Destroy(gameObject);
        }

        public void OnPoolRent(Monster owner)
        {
        }

        public void OnPoolReturn(Monster owner)
        {
        }
    }
}
