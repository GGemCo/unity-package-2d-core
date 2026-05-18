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

        // 충돌 체크할 플레이어 수  
        private const int CountCollider = 10;
        private Collider2D[] _collider2Ds;
        
        private MonsterUIController _monsterUIController;
        private readonly List<IMonsterPoolLifecycle> _poolLifecycles = new(8);
        private bool _isPoolManaged;
        private Coroutine _returnToPoolRoutine;
        private GGemCoMonsterSettings _monsterSettings;
        private CutsceneManager _cutsceneManager;
        private readonly List<IMonsterBrainRuntimeResettable> _brainRuntimeResetters = new(4);
        private bool _pendingBrainResetOnNextFadeIn;
        
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

        public void PrepareForPoolRent(int monsterUid, CharacterRegenData regenData)
        {
            CancelPendingPoolReturn();
            ClearPendingDeathState();
            CharacterRegenData = regenData;
            uid = monsterUid;
            SetPoolManaged(true);

            if (regenData != null)
            {
                transform.position = new Vector3(regenData.x, regenData.y, transform.position.z);
            }

            SetAggro(false);
            SetBattleStatusNone();
            SetStatusNone();
            ClearSubStatus();
            SetAttackerTarget(null);
            _pendingBrainResetOnNextFadeIn = false;
            canMoveX = true;
            canMoveY = true;

            var crowdControl = GetComponent<CharacterCrowdControlController>();
            crowdControl?.ResetForPoolReturn();

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

        public void PrepareForPoolReturn()
        {
            CancelPendingPoolReturn();
            ClearPendingDeathState();
            _controllerMonster?.StopAttackCoroutine();
            _controllerMonster?.StopAllCoroutines();

            var crowdControl = GetComponent<CharacterCrowdControlController>();
            crowdControl?.ResetForPoolReturn();

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
            characterName = info.Name;
            SetBaseInfos(info.StatAtk, info.StatDef, info.StatHp, 0, 0, info.StatSuperArmor, info.StatMoveSpeed, info.StatAttackSpeed,
                info.RegistFire, info.RegistCold, info.RegistLightning, info.RegistPoison);
            CurrentHp.OnNext(info.StatHp);
            CurrentSuperArmor.OnNext(info.StatSuperArmor);
            SetScale(info.Scale);
            SetAttackType(info.AttackType);
            _deathSkillController?.SetDeathSkillMonsterUid(info.DeathSkillMonsterUid);
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
        /// 데미지 받으면 어그로 on. 공격자 등록하기
        /// </summary>
        /// <param name="attacker"></param>
        public override void OnDamage(GameObject attacker)
        {
            base.OnDamage(attacker);
            if (IsAggro() == false)
            {
                SetAggro(true);
            }
            SetAttackerTarget(attacker.transform);
            _controllerMonster?.StopAttackCoroutine();
        }
        /// <summary>
        /// 몬스터가 죽었을때 처리 
        /// </summary>
        protected override void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            base.OnDead(dieReasonType, attacker);

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
            long totalDamage = TotalAtk.Value;
        
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


