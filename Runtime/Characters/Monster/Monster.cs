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
        private CharacterConstants.CombatStartReason _combatStartReason = CharacterConstants.CombatStartReason.None;
        private int _currentLevel = 1;

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

            if (regenData != null)
            {
                transform.position = new Vector3(regenData.x, regenData.y, transform.position.z);
            }

            SetAggro(false);
            SetBattleStatusNone();
            SetStatusNone();
            ClearSubStatus();
            SetAttackerTarget(null);
            _combatStartReason = CharacterConstants.CombatStartReason.None;
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
            _combatStartReason = CharacterConstants.CombatStartReason.None;
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
            
            // 순서 중요. ControllerMonster 에서 콜라이더를 사용
            _controllerMonster = gameObject.AddComponent<ControllerMonster>();
            _controllerMonster.Initialize(_collider2Ds);

            _deathSkillController = gameObject.GetComponent<MonsterDeathSkillController>();
            if (_deathSkillController == null)
            {
                _deathSkillController = gameObject.AddComponent<MonsterDeathSkillController>();
            }
            _deathSkillController.Initialize(this);
        }
        /// <summary>
        /// regen_data 의 정보 셋팅
        /// </summary>
        protected override void InitializeByRegenData()
        {
            // 맵 배치툴로 저장한 정보가 있을 경우 
            if (CharacterRegenData == null) return;
            // UpdateDirection() 에서 초기 방향 처리를 위해 추가
            directionNormalize = new Vector3(CharacterRegenData.IsFlip?1:-1, 0, 0);
            SetFlip(CharacterRegenData.IsFlip);
            canMoveX = CharacterRegenData.CanMoveX;
            canMoveY = CharacterRegenData.CanMoveY;
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
            // GcLogger.Log("InitializationStat uid: "+uid+" / info.uid: "+info.uid+" / StatMoveSpeed: "+info.statMoveSpeed);
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
                moveSpeed = info.BaseMoveSpeed != 0 ? info.BaseMoveSpeed : info.StatMoveSpeed,
                attackSpeed = info.BaseAttackSpeed != 0 ? info.BaseAttackSpeed : info.StatAttackSpeed,
                criticalDamage = info.BaseCriticalDamage,
                criticalProbability = info.BaseCriticalProbability,
                resistanceFire = info.BaseRegistFire != 0 ? info.BaseRegistFire : info.RegistFire,
                resistanceCold = info.BaseRegistCold != 0 ? info.BaseRegistCold : info.RegistCold,
                resistanceLightning = info.BaseRegistLightning != 0 ? info.BaseRegistLightning : info.RegistLightning,
                resistancePoison = info.BaseRegistPoison != 0 ? info.BaseRegistPoison : info.RegistPoison,
            };

            var growthStats = new CharacterGrowthStatValues
            {
                atk = info.StatAtk,
                def = info.StatDef,
                hp = info.StatHp,
            };

            SetBaseAndGrowthStatInfos(baseAttributes, growthStats);
            CurrentHp.OnNext(TotalHp.Value);
            CurrentSuperArmor.OnNext(TotalSuperArmor.Value);
            SetScale(info.Scale);
            SetAttackType(info.AttackType);
            _deathSkillController?.SetDeathSkillMonsterUid(info.DeathSkillMonsterUid);
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
        /// 패트롤 감지 영역에서 플레이어를 발견했을 때 몬스터의 교전 정책을 적용합니다.
        /// </summary>
        /// <param name="player">패트롤 영역에 진입한 플레이어입니다.</param>
        /// <remarks>
        /// <see cref="CharacterConstants.AttackType.AggroFirst"/> 몬스터는 감지 즉시 전투를 시작하고,
        /// <see cref="CharacterConstants.AttackType.PassiveDefense"/> 몬스터는 감지만 기록하며 전투를 시작하지 않습니다.
        /// </remarks>
        public void OnDetectedPlayerByPatrol(Player player)
        {
            if (!CanBeginCombatWithPlayer(player)) return;

            if (GetAttackType() != CharacterConstants.AttackType.AggroFirst)
            {
                return;
            }

            BeginCombatWithPlayer(player, CharacterConstants.CombatStartReason.DetectedByPatrol);
        }

        /// <summary>
        /// 패트롤 감지 영역에서 플레이어가 이탈했을 때 패트롤 감지로 시작된 전투만 정리합니다.
        /// </summary>
        /// <param name="player">패트롤 영역에서 이탈한 플레이어입니다.</param>
        /// <remarks>
        /// 플레이어가 몬스터를 공격해서 전투가 시작된 상태는 패트롤 영역 이탈만으로 종료하지 않습니다.
        /// 이 처리를 통해 후공 몬스터가 피격으로 전투에 들어간 뒤 즉시 전투가 꺼지는 문제를 방지합니다.
        /// </remarks>
        public void OnLostPlayerByPatrol(Player player)
        {
            if (player == null) return;
            if (_combatStartReason != CharacterConstants.CombatStartReason.DetectedByPatrol) return;
            if (attackerTransform != player.transform) return;

            _controllerMonster?.StopAttackCoroutine();
            SetAggro(false);
            _combatStartReason = CharacterConstants.CombatStartReason.None;

            player.ClearAutoMoveTargetMonster(gameObject);
            player.SetBattleStatusNone();
        }

        /// <summary>
        /// 공격 범위 Trigger에서 플레이어를 발견했을 때 선공 몬스터의 전투를 시작합니다.
        /// </summary>
        /// <param name="player">공격 범위에 진입한 플레이어입니다.</param>
        /// <remarks>
        /// 레거시 Brain의 공격 범위 감지는 패트롤 오브젝트가 없는 몬스터의 기본 선공 진입점으로 사용됩니다.
        /// </remarks>
        public void OnDetectedPlayerByAttackRange(Player player)
        {
            if (!CanBeginCombatWithPlayer(player)) return;

            if (GetAttackType() != CharacterConstants.AttackType.AggroFirst)
            {
                return;
            }

            BeginCombatWithPlayer(player, CharacterConstants.CombatStartReason.DetectedByAttackRange);
        }

        /// <summary>
        /// 데미지를 받으면 공격자를 기준으로 전투 상태와 어그로 대상을 갱신합니다.
        /// </summary>
        /// <param name="attacker">데미지를 발생시킨 공격자 오브젝트입니다.</param>
        public override void OnDamage(GameObject attacker)
        {
            base.OnDamage(attacker);

            if (attacker == null || IsStatusDead())
            {
                return;
            }

            if (TryGetPlayerFromAttacker(attacker, out Player player))
            {
                BeginCombatWithPlayer(player, CharacterConstants.CombatStartReason.DamagedByPlayer);
                return;
            }

            BeginCombatWithAttacker(attacker.transform, CharacterConstants.CombatStartReason.DamagedByNonPlayer);
        }

        /// <summary>
        /// 플레이어를 대상으로 전투를 시작할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="player">전투 대상 플레이어입니다.</param>
        /// <returns>전투를 시작할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanBeginCombatWithPlayer(Player player)
        {
            return player != null && !IsStatusDead() && !player.IsStatusDead();
        }

        /// <summary>
        /// 공격자 오브젝트에서 플레이어 컴포넌트를 찾습니다.
        /// </summary>
        /// <param name="attacker">데미지를 발생시킨 공격자 오브젝트입니다.</param>
        /// <param name="player">찾은 플레이어 컴포넌트입니다.</param>
        /// <returns>플레이어를 찾으면 <see langword="true"/>입니다.</returns>
        private static bool TryGetPlayerFromAttacker(GameObject attacker, out Player player)
        {
            player = null;
            if (attacker == null) return false;

            player = attacker.GetComponent<Player>();
            if (player != null) return true;

            player = attacker.GetComponentInParent<Player>();
            return player != null;
        }

        /// <summary>
        /// 플레이어를 대상으로 몬스터와 플레이어의 전투 상태를 함께 시작합니다.
        /// </summary>
        /// <param name="player">전투 대상 플레이어입니다.</param>
        /// <param name="reason">전투가 시작된 원인입니다.</param>
        private void BeginCombatWithPlayer(Player player, CharacterConstants.CombatStartReason reason)
        {
            if (!CanBeginCombatWithPlayer(player)) return;

            BeginCombatWithAttacker(player.transform, reason);
            player.SetBattleStatusInBattle();
            player.SetAutoMoveTargetMonster(gameObject);
        }

        /// <summary>
        /// 지정한 공격자를 대상으로 몬스터 전투 상태와 어그로 대상을 설정합니다.
        /// </summary>
        /// <param name="attacker">전투 대상으로 기록할 공격자 Transform입니다.</param>
        /// <param name="reason">전투가 시작된 원인입니다.</param>
        private void BeginCombatWithAttacker(Transform attacker, CharacterConstants.CombatStartReason reason)
        {
            if (attacker == null || IsStatusDead()) return;

            _combatStartReason = reason;
            if (!IsAggro())
            {
                SetAggro(true);
            }

            SetAttackerTarget(attacker);
            _controllerMonster?.StopAttackCoroutine();
        }
        /// <summary>
        /// 몬스터 사망 시 전투 상태, UI, 이벤트, 컷신 후처리를 수행합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            base.OnDead(dieReasonType, attacker);
            SetHitAreaColliderEnabled(false);
            EndPlayerCombatOnDeath(attacker);
            SetAggro(false);
            _combatStartReason = CharacterConstants.CombatStartReason.None;

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
        /// 몬스터 사망 시 플레이어에게 남아 있는 현재 전투 타겟과 전투 상태를 정리합니다.
        /// </summary>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <remarks>
        /// BT 몬스터와 레거시 몬스터 모두 전투 시작은 Core의 몬스터 어그로/공격자 기록을 사용합니다.
        /// 따라서 사망 후처리도 Brain 종류와 무관하게 Core 몬스터 공통 로직에서 처리해야 합니다.
        /// </remarks>
        private void EndPlayerCombatOnDeath(GameObject attacker)
        {
            Player player = ResolvePlayerCombatTargetOnDeath(attacker);
            if (player == null)
            {
                return;
            }

            player.ClearAutoMoveTargetMonster(gameObject);
            player.SetBattleStatusNone();
        }

        /// <summary>
        /// 사망 시 전투 종료를 알려야 할 플레이어를 공격자와 현재 어그로 대상에서 해석합니다.
        /// </summary>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <returns>전투 종료 처리를 적용할 플레이어입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private Player ResolvePlayerCombatTargetOnDeath(GameObject attacker)
        {
            if (TryGetPlayerFromAttacker(attacker, out Player playerFromAttacker))
            {
                return playerFromAttacker;
            }

            if (attackerTransform == null)
            {
                return null;
            }

            Player playerFromTarget = attackerTransform.GetComponent<Player>();
            if (playerFromTarget != null)
            {
                return playerFromTarget;
            }

            return attackerTransform.GetComponentInParent<Player>();
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

