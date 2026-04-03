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
            Stop(true);

            _monsterUIController ??= new MonsterUIController();
            _monsterUIController.Initialize(this);
            _monsterUIController.RebuildRuntimeUi();
            EnableSuperArmor(CurrentSuperArmor.Value > 0);
        }

        public void PrepareForPoolReturn()
        {
            CancelPendingPoolReturn();
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

        protected override void OnStartFadeIn()
        {
            if (_monsterUIController != null)
            {
                _monsterUIController.StartFadeIn();
            }
        }
        protected override void OnStartFadeOut()
        {
            if (_monsterUIController != null)
            {
                _monsterUIController.StartFadeOut();
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
