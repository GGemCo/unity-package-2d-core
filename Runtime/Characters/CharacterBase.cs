using System;
using System.Collections;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 공용 
    /// </summary>
    public class CharacterBase : CharacterStat
    {
        [Header("캐릭터 정보")]
        // 캐릭터 타입
        [HideInInspector] public CharacterConstants.Type type;
        // 캐릭터 테이블 Uid
        [HideInInspector] public int uid;
        // 스폰될때 부여되는 가상번호 vid
        [HideInInspector] public int vid;
        // 현재 이동 스텝
        [HideInInspector] public float currentMoveStep;
        // 어그로
        private CharacterConstants.AttackType _attackType;
        private bool _isAggro;
        [HideInInspector] public string characterName;
        
        [Header("캐릭터 방향 관련")]
        // 원본 방향
        [HideInInspector] public CharacterConstants.FacingDirection8 defaultFacingDirection8 = CharacterConstants.FacingDirection8.Left;
        // 현재 방향
        private CharacterConstants.FacingDirection8 _currentFacing = CharacterConstants.FacingDirection8.Right;
        public CharacterConstants.FacingDirection8 CurrentFacing => _currentFacing;
        private bool _limitBoundaryBottom;
        
        // 방향은 CurrentFacing 으로 판단한다. 
        [Header("리젠 정보 설정")]
        [Tooltip("좌우 플립 여부")]
        public bool isFlip; // 좌우 flip 여부. 맵에 배치할 때, 연출 캐릭터 배치할 때 사용. 스크립트는 IsFlipped() 사용
        // 방향
        [HideInInspector] public Vector3 directionNormalize;
        // 좌우 flip 가능 여부
        private bool _isPossibleFlip = true;
        // 초기 scale x 값
        [HideInInspector] public float originalScaleX;
        
        [Header("애니메이션 및 렌더링 관련")]
        // 애니메이션 컨트롤러
        public ICharacterAnimationController CharacterAnimationController;
        private Renderer _characterRenderer;
        private CharacterConstants.CharacterSortingOrder _sortingOrder;
        
        [Header("상태 및 스탯")] public readonly BehaviorSubject<long> CurrentHp = new(0);
        public readonly BehaviorSubject<long> CurrentMp = new(0);

        [Header("스킬")] 
        protected bool IsUseSkill = false;
        private SkillController _skillController;
        private ProjectileController _projectileController;
        
        [Header("스폰 데이터")] 
        public CharacterRegenData CharacterRegenData;
        
        // 현재 상태
        private CharacterConstants.CharacterStatus _currentStatus;
        private CharacterConstants.CharacterSubStatus _currentSubStatus;
        // fade in, out 효과 시작 여부. 맵에서 컬링 될때 사용
        private bool _isStartFade;
        private float _characterHeight;
        private float _characterWidth;
        // 공격한 GameObject 의 Transform
        [HideInInspector] public Transform attackerTransform;
        // 캐릭터 간의 충돌 체크용
        [HideInInspector] public CapsuleCollider2D colliderAttackRange;
        // 캐릭터 hit area 체크용
        [HideInInspector] public CapsuleCollider2D colliderHitArea;
        // 맵 height 값, sorting order 계산에 사용
        private float _mapSizeHeight;
        [HideInInspector] public Rigidbody2D characterRigidbody2D;
        // 맵 object, ground 체크
        [HideInInspector] public CapsuleCollider2D colliderMapObject;
        // 데미지 처리 컨트롤러
        private CharacterDamageController _characterDamageController;
        // pick up 액션시 위치, 스프라이트 처리
        protected CharacterPickUpPosition characterPickUpPosition;
        
        // 공격 애니메이션 종료 후 
        public event EventHandlerAnimationCompleteAttack AnimationCompleteAttack;
        // 공격 end 애니메이션 종료 후 
        public event EventHandlerAnimationCompleteAttackEnd AnimationCompleteAttackEnd;
        public event EventHandlerOnStop OnStop;
        public event EventHandlerOnAnimationEventJump OnAnimationEventJump;
        public event EventHandlerOnAnimationEventDash OnAnimationEventDash;
        
        public static event Action<CharacterBase> OnCharacterUseTool; // Destroy 직전 1회
        public static event Action<CharacterBase> OnCharacterUseSeed; // Destroy 직전 1회
        
        protected override void Awake()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            base.Awake();
            EnsureAffectSystem();
            CharacterRegenData = null;
            SetAttackType(CharacterConstants.AttackType.None);
            SetAggro(false);
            SetSubStatus(CharacterConstants.CharacterSubStatus.None);
            // 태그 먼저 처리
            InitTagSortingLayer();
            InitComponents();
            if (IsUseSkill)
            {
                _skillController = new SkillController();
                _skillController.Initialize(this);
            }
            _projectileController = new ProjectileController();
            _projectileController.Initialize(this);
            defaultFacingDirection8 = AddressableLoaderSettings.Instance.playerSettings.facingDirection8;
            _limitBoundaryBottom  = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryBottom;
            
            // 데미지 컨트롤러 초기화
            _characterDamageController = new CharacterDamageController();
            _characterDamageController.Initialize(this);
            
            characterPickUpPosition = GetComponentInChildren<CharacterPickUpPosition>();
        }

        /// <summary>
        /// tag, sorting layer, layer 셋팅하기
        /// </summary>
        public virtual void InitTagSortingLayer()
        {
            if (_characterRenderer == null)
            {
                _characterRenderer = GetComponent<Renderer>();
            }
            _characterRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.Character);
        }
        /// <summary>
        /// 캐릭터에 필요한 컴포넌트 추가하기
        /// </summary>
        protected virtual void InitComponents()
        {
            characterRigidbody2D = gameObject.GetComponent<Rigidbody2D>();
            // 맵 object 충돌 체크용
            colliderMapObject = gameObject.GetComponentInChildren<CapsuleCollider2D>();
            // attack range
            CharacterAttackRange characterAttackRange = gameObject.GetComponentInChildren<CharacterAttackRange>();
            if (characterAttackRange)
            {
                characterAttackRange.Initialize(this);
                colliderAttackRange = characterAttackRange.gameObject.GetComponent<CapsuleCollider2D>();
            }
            // hit area
            CharacterHitArea characterHitArea = gameObject.GetComponentInChildren<CharacterHitArea>();
            if (characterHitArea)
            {
                characterHitArea.Initialize(this);
                colliderHitArea = characterHitArea.gameObject.GetComponent<CapsuleCollider2D>();
            }
        }
        protected override void Start()
        {
            base.Start();
            originalScaleX = transform.localScale.x;
            
            InitializeByTable();
            InitializeByAnimationTable();
            InitializeByRegenData();
            
            TotalMoveSpeed
                .Subscribe(UpdateAnimationMoveTimeScale)
                .AddTo(this);

            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
            Stop(true);
        }
        /// <summary>
        /// 테이블에서 가져온 몬스터 정보 셋팅
        /// </summary>
        protected virtual void InitializeByTable()
        {
        }
        /// <summary>
        /// regen_data 의 정보 셋팅
        /// </summary>
        protected virtual void InitializeByRegenData()
        {
            
        }
        /// <summary>
        /// animation 테이블 정보 셋팅
        /// </summary>
        protected virtual bool InitializeByAnimationTable()
        {
            if (uid <= 0) return false;
            int animationUid = 0;
            if (type == CharacterConstants.Type.Npc)
            {
                if (!TableLoaderManager.Instance.TryGetNpcData(uid, out var info)) return false;
                animationUid = info.AnimationUid;
            }
            else if (type == CharacterConstants.Type.Monster)
            {
                if (!TableLoaderManager.Instance.TryGetMonsterData(uid, out var info)) return false;
                animationUid = info.AnimationUid;
            }
            if (animationUid <= 0) return false;
            
            if (!TableLoaderManager.Instance.TryGetAnimationData(animationUid, out var struckTableAnimation)) return false;
            currentMoveStep = struckTableAnimation.MoveStep;

            SetHeight(struckTableAnimation.Height);
            defaultFacingDirection8 = struckTableAnimation.DefaultFacingDirection8;
            return true;
        }
        /// <summary>
        /// 캐릭터가 flip 되었는지 체크
        /// </summary>
        /// <returns></returns>
        public bool IsFlipped() {
            switch (defaultFacingDirection8)
            {
                case CharacterConstants.FacingDirection8.Left:
                    return _currentFacing == CharacterConstants.FacingDirection8.Right;
                case CharacterConstants.FacingDirection8.Right:
                    return _currentFacing == CharacterConstants.FacingDirection8.Left;
                default:
                    return false;
            }
        }
        public void SetIsPossibleFlip(bool set) => _isPossibleFlip = set;

        private bool IsPossibleFlip() => _isPossibleFlip;
        /// <summary>
        /// 캐릭터 방향 셋팅하기
        /// </summary>
        /// <param name="value"></param>
        public void SetFlip(bool value)
        {
            if (IsPossibleFlip() != true) return;
            isFlip = value;
            switch (defaultFacingDirection8)
            {
                case CharacterConstants.FacingDirection8.Left:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Right : CharacterConstants.FacingDirection8.Left);
                    break;
                case CharacterConstants.FacingDirection8.Right:
                    SetFacing(value ? CharacterConstants.FacingDirection8.Left : CharacterConstants.FacingDirection8.Right);
                    break;
                case CharacterConstants.FacingDirection8.None:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.Up:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                case CharacterConstants.FacingDirection8.Down:
                case CharacterConstants.FacingDirection8.DownRight:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetFacing(Vector2 dir)
        {
            SetFacing(CharacterConstants.ToFacingDirection8(dir));
        }

        public void SetFacing(CharacterConstants.FacingDirection8 dir)
        {
            if (IsPossibleFlip() != true || dir == CharacterConstants.FacingDirection8.None) return;
            // GcLogger.Log($"set facing: {dir}");
            _currentFacing = dir;

            float sign = 1;
            if ((defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right &&
                 dir is CharacterConstants.FacingDirection8.Left or CharacterConstants.FacingDirection8.DownLeft
                     or CharacterConstants.FacingDirection8.UpLeft) ||
                (defaultFacingDirection8 == CharacterConstants.FacingDirection8.Left &&
                 dir is CharacterConstants.FacingDirection8.Right or CharacterConstants.FacingDirection8.DownRight
                     or CharacterConstants.FacingDirection8.UpRight))
            {
                sign = -1;
            }

            transform.localScale = new Vector3(originalScaleX * sign, transform.localScale.y, transform.localScale.z);
        }
        /// <summary>
        /// 타겟 오브젝트가 있을경우 방향 셋팅하기
        /// </summary>
        /// <param name="targetTransform"></param>
        protected void SetFlipToTarget(Transform targetTransform)
        {
            SetFlip(transform.position.x <= targetTransform.position.x);
        }
        /// <summary>
        /// 실제 보는 방향: 디폴트 방향이 오른쪽이면 localScale.x가 양수면 오른쪽, 음수면 왼쪽
        /// </summary>
        /// <returns></returns>
        private float GetFacingDirection()
        {
            float sign = Mathf.Sign(transform.localScale.x);
            return defaultFacingDirection8 == CharacterConstants.FacingDirection8.Right ? sign : -sign;
        }
        /// <summary>
        /// 플레이어와 몬스터가 마주보고 있는지 체크 
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        protected bool AreFacingEachOther(Transform monster)
        {
            CharacterBase player = SceneGame.Instance.player.GetComponent<CharacterBase>();
            CharacterBase monsterChar = monster.GetComponent<CharacterBase>();

            float playerDir = player.GetFacingDirection();
            float monsterDir = monsterChar.GetFacingDirection();

            float directionToMonster = Mathf.Sign(monster.position.x - transform.position.x);

            return Mathf.Approximately(playerDir, directionToMonster) && Mathf.Approximately(monsterDir, -directionToMonster);
        }
        /// <summary>
        /// 캐릭터 순서. sorting order 처리 
        /// </summary>
        private void UpdatePosition()
        {
            if (_sortingOrder == CharacterConstants.CharacterSortingOrder.Fixed) return;

            int baseSortingOrder = MathHelper.GetSortingOrder(_mapSizeHeight, transform.position.y);
            
            baseSortingOrder = _sortingOrder switch
            {
                CharacterConstants.CharacterSortingOrder.AlwaysOnTop => CharacterConstants.SortingOrderTop,
                CharacterConstants.CharacterSortingOrder.AlwaysOnBottom => CharacterConstants.SortingOrderBottom,
                _ => baseSortingOrder
            };

            _characterRenderer.sortingOrder = baseSortingOrder;
        }
        protected virtual void Update()
        {
            if (IsStatusDead()) return;
            if (CheckEndGround()) return;
            
            UpdatePosition();
        }
        /// <summary>
        /// 땅 바닥 체크. y 좌표가 0 보다 작으면 사망 처리
        /// </summary>
        /// <returns></returns>
        private bool CheckEndGround()
        {
            if (transform.position.y > 0) return false;
            if (_limitBoundaryBottom) return false;
            
            Dead(CharacterConstants.DieReasonType.EndTilemapY);
            
            return true;
        }

        /// <summary>
        /// 강제로 이동시키기
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MoveTeleport(float x, float y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
        }
        public bool IsStatusDead() => _currentStatus == CharacterConstants.CharacterStatus.Dead;
        public bool IsStatusAttack() => _currentStatus == CharacterConstants.CharacterStatus.Attack;
        public bool IsStatusAttackComboWait() => _currentStatus == CharacterConstants.CharacterStatus.AttackComboWait;
        public bool IsStatusDontMove() => _currentStatus == CharacterConstants.CharacterStatus.DontMove;
        public bool IsStatusRun() => _currentStatus == CharacterConstants.CharacterStatus.Run;
        public bool IsStatusIdle() => _currentStatus == CharacterConstants.CharacterStatus.Idle;
        public bool IsStatusNone() => _currentStatus == CharacterConstants.CharacterStatus.None;
        public bool IsStatusMoveForce() => _currentStatus == CharacterConstants.CharacterStatus.MoveForce;
        public bool IsStatusDamage() => _currentStatus == CharacterConstants.CharacterStatus.Damage;
        public bool IsStatusJump() => _currentStatus == CharacterConstants.CharacterStatus.Jump;
        public bool IsStatusKnockback() => _currentStatus == CharacterConstants.CharacterStatus.Knockback;
        public bool IsStatusDash() => _currentStatus == CharacterConstants.CharacterStatus.Dash;
        public bool IsStatusClimb() => _currentStatus == CharacterConstants.CharacterStatus.Climb;
        public bool IsStatusPush() => _currentStatus == CharacterConstants.CharacterStatus.Push;
        public bool IsStatusSimulationTool() => _currentStatus == CharacterConstants.CharacterStatus.SimulationTool;
        public CharacterConstants.CharacterStatus GetCurrentStatus() => _currentStatus;
        
        private void SetStatus(CharacterConstants.CharacterStatus value) => _currentStatus = value;
        public void SetStatusNone() => SetStatus(CharacterConstants.CharacterStatus.None);
        public void SetStatusDead() => SetStatus(CharacterConstants.CharacterStatus.Dead);
        public void SetStatusIdle() => SetStatus(CharacterConstants.CharacterStatus.Idle);
        public void SetStatusRun() => SetStatus(CharacterConstants.CharacterStatus.Run);
        public void SetStatusAttack() => SetStatus(CharacterConstants.CharacterStatus.Attack);
        public void SetStatusAttackComboWait() => SetStatus(CharacterConstants.CharacterStatus.AttackComboWait);
        public void SetStatusDontMove() => SetStatus(CharacterConstants.CharacterStatus.DontMove);
        public void SetStatusMoveForce() => SetStatus(CharacterConstants.CharacterStatus.MoveForce);
        public void SetStatusDamage() => SetStatus(CharacterConstants.CharacterStatus.Damage);
        public void SetStatusJump() => SetStatus(CharacterConstants.CharacterStatus.Jump);
        public void SetStatusKnockback() => SetStatus(CharacterConstants.CharacterStatus.Knockback);
        public void SetStatusDash() => SetStatus(CharacterConstants.CharacterStatus.Dash);
        public void SetStatusClimb() => SetStatus(CharacterConstants.CharacterStatus.Climb);
        public void SetStatusPush() => SetStatus(CharacterConstants.CharacterStatus.Push);
        public void SetStatusSimulationTool() => SetStatus(CharacterConstants.CharacterStatus.SimulationTool);

        public void SetScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, 0);
            originalScaleX = scale;
        }
        /// <summary>
        /// fade in 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeIn()
        {
            if (_isStartFade) return;
            _isStartFade = true;
            gameObject.SetActive(true);
            StartCoroutine(FadeIn(ConfigCommon.CharacterFadeSec));
            OnStartFadeIn();
        }

        protected virtual void OnStartFadeIn()
        {
        }

        /// <summary>
        /// fade out 효과 시작. 맵 컬링시 사용
        /// </summary>
        public void StartFadeOut()
        {
            if (_isStartFade) return;
            _isStartFade = true;
            StartCoroutine(FadeOut(ConfigCommon.CharacterFadeSec));
            OnStartFadeOut();
        }

        protected virtual void OnStartFadeOut()
        {
        }

        private IEnumerator FadeIn(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, true);
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return CharacterAnimationController.FadeEffect(duration, false);
            gameObject.SetActive(false);
        }
        public void SetIsStartFade(bool value)
        {
            _isStartFade = value;
        }

        public float GetHeight()
        {
            return _characterHeight;
        }

        protected void SetHeight(float value)
        {
            _characterHeight = value;
        }
        /// <summary>
        /// localScale 이 적용된 캐릭터 크기 가져오기
        /// </summary>
        /// <returns></returns>
        public float GetHeightByScale()
        {
            return GetHeight() * Math.Abs(transform.localScale.x);
        }
        public float GetWidth()
        {
            return _characterWidth;
        }

        protected void SetWidth(float value)
        {
            _characterWidth = value;
        }
        public virtual float GetCurrentMoveStep()
        {
            return currentMoveStep;
        }
        /// <summary>
        /// attack 이벤트 처리 
        /// </summary>
        public virtual void OnEventAttack(StruckAnimationEventAttack struckAnimationEventAttack)
        {
        }
        /// <summary>
        /// 캐릭터 사망 처리
        /// </summary>
        /// <param name="dieReasonType"></param>
        /// <param name="attacker"></param>
        public void Dead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            SetStatusDead();
            if (dieReasonType != CharacterConstants.DieReasonType.EndTilemapY)
                CharacterAnimationController.PlayDeadAnimation();
            
            // 어펙트 모두 지우기
            AffectRuntimeBridge.RemoveAll(gameObject);

            OnDead(dieReasonType, attacker);
        }

        /// <summary>
        /// 캐릭터가 죽었을때 처리 
        /// </summary>
        protected virtual void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            if (dieReasonType == CharacterConstants.DieReasonType.EndTilemapY)
            {
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// 내가 데미지 받았을때 처리 
        /// </summary>
        /// <param name="metadataDamage">속성 데미지 타입</param>
        public void TakeDamage(MetadataDamage metadataDamage)
        {
            _characterDamageController.TakeDamage(metadataDamage);
        }
        public virtual void OnDamage(GameObject attacker)
        {
        }

        /// <summary>
        /// 공격한 오브젝트 설정하기 
        /// </summary>
        /// <param name="attacker"></param>
        public void SetAttackerTarget(Transform attacker)
        {
            attackerTransform = attacker;
        }

        public bool IsAttackerStatusDead()
        {
            if (attackerTransform == null || attackerTransform.GetComponent<CharacterBase>() == null) return false;
            return attackerTransform.GetComponent<CharacterBase>().IsStatusDead();
        }

        public void SetAggro(bool set)
        {
            _isAggro = set;
        }
        public bool IsAggro()
        {
            return _isAggro;
        }
        public CharacterConstants.AttackType GetAttackType()
        {
            return _attackType;
        }

        protected void SetAttackType(CharacterConstants.AttackType pattackType)
        {
            _attackType = pattackType;
        }
        /// <summary>
        /// 어펙트 추가하기
        /// </summary>
        /// <param name="affectUid"></param>
        /// <param name="duration"></param>
        public void AddAffect(int affectUid, float duration = 0)
        {
            // Affect 패키지가 설치되어 있으면 실제 적용.
            // 미설치 시에는 아무 일도 하지 않는다.
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, duration);
        }

        /// <summary>
        /// total move speed 가 변경되었을때 wait 애니메이션의 time scale 도 변경해주기 위해서
        /// track index = 0 의 time scale 을 변경해준다.
        /// </summary>
        /// <param name="value"></param>
        private void UpdateAnimationMoveTimeScale(long value)
        {
            CharacterAnimationController.UpdateTimeScaleMove(value/100f);
        }
        /// <summary>
        /// 현재 마력 더하기
        /// </summary>
        /// <param name="value"></param>
        public void AddMp(int value)
        {
            long newVale = CurrentMp.Value + value;
            if (newVale > TotalMp.Value)
            {
                newVale = TotalMp.Value;
            }
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
            CurrentMp.OnNext(newVale);
        }
        // /// <summary>
        // /// disable 되었을때 어펙트 효과 모두 지워주기
        // /// todo 지워야하는 어펙트가 있고 유지해야하는 어펙트가 있다
        // /// </summary>
        // private void OnDestroy()
        // {
        //     AffectController?.RemoveAllAffects();
        // }

        public void Stop(bool isForce = false)
        {
            if (!isForce && IsStatusDead()) return;
            if (!isForce && IsStatusIdle()) return;
            if (!isForce && IsStatusKnockback())
            {
                return;
            }
            
            SetStatusIdle();
            CharacterAnimationController?.PlayWaitAnimation();
            
            var e = new EventArgsOnStop { Handled = false };

            // 모든 구독자에게 알림
            OnStop?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
            {
                
            }
        }
        public float GetRandomPositionYInHitArea()
        {
            if (!colliderHitArea)
            {
                return transform.position.y;
            }
            // 캡슐의 로컬 공간 기준 Y 범위 계산
            float halfHeight = colliderHitArea.size.y / 2f;
            float minLocalY = colliderHitArea.offset.y - halfHeight;
            float maxLocalY = colliderHitArea.offset.y + halfHeight;

            // 로컬 Y 기준 무작위 값
            float randomLocalY = Random.Range(minLocalY, maxLocalY);

            // 로컬 좌표 → 월드 좌표 변환
            Vector2 localPoint = new Vector2(0f, randomLocalY);
            Vector2 worldPoint = transform.TransformPoint(localPoint);
            return worldPoint.y;
        }
        public void LaunchProjectile(MetadataProjectile metadataProjectile)
        {
            if (_projectileController == null) return;
            _projectileController.Launch(metadataProjectile);
        }
        public virtual void UseSkill(int skillUid, int skillLevel)
        {
            _skillController?.MakeSkill(skillUid, skillLevel);
        }

        public bool IsPlayer()
        {
            return type == CharacterConstants.Type.Player;
        }
        public bool IsNPC()
        {
            return type == CharacterConstants.Type.Npc;
        }

        public bool IsMonster()
        {
            return type == CharacterConstants.Type.Monster;
        }
        /// <summary>
        /// 공격 애니메이션 종료 후 처리 
        /// </summary>
        public void OnAnimationCompleteAttack()
        {
            RequestAnimationCompleteAttackWithFallback(LegacyAnimationCompleteAttack);
        }
        private void LegacyAnimationCompleteAttack()
        {
            Stop();
        }

        /// <summary>
        /// 공격 애니메이션 종료 알림. 외부가 처리하지 않으면 레거시 폴백을 수행.
        /// </summary>
        private void RequestAnimationCompleteAttackWithFallback(Action legacyFallback)
        {
            var e = new EventArgsAnimationAttack { Handled = false };

            // 모든 구독자에게 알림
            AnimationCompleteAttack?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
                legacyFallback?.Invoke();
        }

        public void OnAnimationCompleteAttackEnd()
        {
            RequestAnimationCompleteAttackEndWithFallback(LegacyAnimationCompleteAttackEnd);
        }
        /// <summary>
        /// 공격 End 애니메이션 종료 알림. 외부가 처리하지 않으면 레거시 폴백을 수행.
        /// </summary>
        private void RequestAnimationCompleteAttackEndWithFallback(Action legacyFallback)
        {
            var e = new EventArgsAnimationAttackEnd { Handled = false };

            // 모든 구독자에게 알림
            AnimationCompleteAttackEnd?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
                legacyFallback?.Invoke();
        }
        private void LegacyAnimationCompleteAttackEnd()
        {
        }

        /// <summary>
        /// 현재 위치에서 추가로 강제 이동 시키기
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="duration"></param>
        public void AddMoveForce(float x = 0, float y = 0, float duration = 0)
        {
            if (x == 0 && y == 0) return;
            if (duration > 0)
            {
                Vector3 newPosition = transform.position + new Vector3(x, y, 0);
                StartCoroutine(MoveForceRoutine(newPosition, duration));
            }
            else
            {
                transform.position += new Vector3(x, y, 0);
            }
        }

        private IEnumerator MoveForceRoutine(Vector3 position, float duration = 0)
        {
            if (!characterRigidbody2D) yield break;
            if (duration == 0) yield break;
            
            Vector2 startPosition = transform.position;
            Vector2 targetPosition = position;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = Easing.EaseOutQuad(t); // easing 적용
                Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
                characterRigidbody2D.MovePosition(newPosition);

                elapsed += Time.deltaTime;
                yield return null;
            }
            characterRigidbody2D.MovePosition(targetPosition); // 마지막 위치 보정
        }
        /// <summary>
        /// 점프 애니메이션 관련 event 발생시 처리
        /// </summary>
        /// <param name="eventName"></param>
        public void AnimationEventJump(string eventName)
        {
            var e = new EventArgsOnAnimationEventJump { Handled = false, EventName = eventName };

            // 모든 구독자에게 알림
            OnAnimationEventJump?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
            {
                
            }
        }
        /// <summary>
        /// 대시 애니메이션 관련 event 발생시 처리
        /// </summary>
        /// <param name="eventName"></param>
        public void AnimationEventDash(string eventName)
        {
            var e = new EventArgsOnAnimationEventDash { Handled = false, EventName = eventName };

            // 모든 구독자에게 알림
            OnAnimationEventDash?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
            {
                
            }
        }

        public virtual void OnTriggerEnterByAttackRange(Collider2D collision)
        {
        }

        public virtual bool OnTriggerExitByAttackRange(Collider2D collision)
        {
            // 에디터에서 플레이 종료시 OnTriggerExit2D 함수를 호출하지 않도록 처리 
#if UNITY_EDITOR
            if (UnityEditorHelper.IsExitingPlayMode)
                return false;
#endif
            return true;
        }

        public virtual void OnAnimationCompleteDead()
        {
        }
        /// <summary>
        /// RigidBody2D의 Sleep 모드 설정
        /// </summary>
        /// <param name="mode"></param>
        public void SetRigidBody2DSleepMode(RigidbodySleepMode2D mode)
        {
            if (!characterRigidbody2D)
            {
                GcLogger.LogError($"RigidBody2D 컴포넌트가 없습니다.");
                return;
            }
            characterRigidbody2D.sleepMode = mode;
        }

        public void UseTool()
        {
            OnCharacterUseTool?.Invoke(this);
        }
        public void UseSeed()
        {
            OnCharacterUseSeed?.Invoke(this);
        }

        public virtual bool IsEquipSimulationTool()
        {
            return false;
        }

        public virtual bool IsEquipAxe()
        {
            return false;
        }

        public virtual bool IsEquipPickAxe()
        {
            return false;
        }

        public virtual bool IsEquipSickle()
        {
            return false;
        }

        public virtual bool IsEquipHoe()
        {
            return false;
        }

        public virtual bool IsEquipWatering()
        {
            return false;
        }
        public virtual bool IsEquipSeed()
        {
            return false;
        }

        public void SetSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.ClearFlags();
            _currentSubStatus = value;
        }
        public void AddSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.AddFlag(value);
        }
        public void RemoveSubStatus(CharacterConstants.CharacterSubStatus value)
        {
            _currentSubStatus = _currentSubStatus.RemoveFlag(value);
        }
        public void ClearSubStatus()
        {
            _currentSubStatus = _currentSubStatus.ClearFlags();
        }

        public virtual void ChangePickUpSprite()
        {
        }

        /// <summary>
        /// 어펙트 시스템을 별도 패키지(com.ggemco.2d.affect)로 분리한 흐름에 맞춰,
        /// 런타임에 필요한 컴포넌트를 자동으로 준비한다.
        /// </summary>
        private void EnsureAffectSystem()
        {
            // Affect 패키지가 설치되어 있으면 필요한 컴포넌트를 자동 부착한다.
            // (Core는 Affect를 직접 참조하지 않는다.)
            AffectRuntimeBridge.EnsureAffectSystem(gameObject);
        }
    }
}
