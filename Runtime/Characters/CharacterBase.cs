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
    public class CharacterBase : CharacterStat, ICharacterActionController
    {

        /// <summary>
        /// CharacterBase 초기화 완료 여부.
        /// - <see cref="Start"/>에서 테이블/리젠/리소스 동기화 초기화가 끝난 뒤 true로 전환된다.
        /// - 패시브/장비/세이브 로드 등 외부 시스템은 이 값이 true가 된 이후에 적용하는 것을 권장한다.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// CharacterBase 초기화 완료 이벤트.
        /// - <see cref="IsInitialized"/>가 true로 전환되는 시점에 1회 호출된다.
        /// </summary>
        public event Action Initialized;
        
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

        [Header("전투")] 
        public readonly BehaviorSubject<CharacterConstants.BattleStatus> CurrentBattleStatus = new(CharacterConstants.BattleStatus.None);
        
        [Header("스킬")] 
        protected bool IsUseSkill = false;
        private ProjectileController _projectileController;
        
        [Header("스폰 데이터")] 
        public CharacterRegenData CharacterRegenData;
        
        // 현재 상태
        private CharacterConstants.CharacterStatus _currentStatus;
        private CharacterConstants.CharacterSubStatus _currentSubStatus;
        // 전투 상태(동작 상태와 분리)
        private CharacterConstants.BattleStatus _battleStatus = CharacterConstants.BattleStatus.None;

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
        private CharacterCrowdControlController _crowdControlController;
        private ICharacterMotionController _motionController;
        
        // 공격 애니메이션 종료 후 
        public event EventHandlerAnimationCompleteAttack AnimationCompleteAttack;
        // 공격 end 애니메이션 종료 후 
        public event EventHandlerAnimationCompleteAttackEnd AnimationCompleteAttackEnd;
        public event EventHandlerOnStop OnStop;
        public event EventHandlerOnAnimationEventJump OnAnimationEventJump;
        public event EventHandlerOnAnimationEventDash OnAnimationEventDash;
        public event EventHandlerOnAnimationEventMotion OnAnimationEventMotion;
        public event EventHandlerOnAnimationEventCrowdControl OnAnimationEventCrowdControl;
        // 방어 end 애니메이션 종료 후
        public event EventHandlerOnAnimationEventGuardEnd OnAnimationEventGuardEnd;
        
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
            // todo. 정리 필요
            if (IsUseSkill)
            {
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
            // CC 처리
            _crowdControlController = gameObject.AddComponent<CharacterCrowdControlController>();
            // 움직임 처리
            _motionController = gameObject.GetComponent<ICharacterMotionController>();
            if (_motionController == null)
            {
                _motionController = gameObject.AddComponent<CharacterMotionController2D>();
            }
        }
        protected override void Start()
        {
            base.Start();
            originalScaleX = transform.localScale.x;
            
            InitializeByTable();
            InitializeByAnimationTable();
            InitializeByRegenData();

            SetupResourceMaxChangeSync();
            TotalMoveSpeed
                .Subscribe(UpdateAnimationMoveTimeScale)
                .AddTo(this);

            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
            Stop(true);
            MarkInitialized();
        }

        private void MarkInitialized()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            Initialized?.Invoke();
        }


        /// <summary>
        /// 최대치(TotalHp/TotalMp/TotalStamina) 변경 시 현재값(Current*)을 정책에 따라 보정합니다.
        /// - 정책 값은 <see cref="GGemCoPlayerSettings"/>에서 설정할 수 있습니다.
        /// - Player는 자신의 settings를 우선 사용하고, 그 외 캐릭터는 AddressableLoaderSettings의 playerSettings를 fallback으로 사용합니다.
        /// </summary>
        private void SetupResourceMaxChangeSync()
        {
            var settings = GetPlayerSettingsForResourcePolicy();
            if (settings == null)
            {
                // settings가 없으면 최소한의 안전 동작(감소 시 clamp)만 수행하도록 KeepCurrent로 구독합니다.
                SubscribeResourceMaxChange(TotalHp, CurrentHp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                SubscribeResourceMaxChange(TotalMp, CurrentMp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                SubscribeResourceMaxChange(TotalStamina, CurrentStamina, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                // 임시 최대 HP는 기본적으로 자동 충전하지 않고(증가 시 Keep), 감소 시 clamp만 수행합니다.
                SubscribeResourceMaxChange(TotalHpTemp, CurrentHpTemp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                return;
            }

            SubscribeResourceMaxChange(TotalHp, CurrentHp, settings.hpMaxChangePolicy);
            SubscribeResourceMaxChange(TotalMp, CurrentMp, settings.mpMaxChangePolicy);
            SubscribeResourceMaxChange(TotalStamina, CurrentStamina, settings.staminaMaxChangePolicy);
            // 임시 최대 HP는 기본적으로 자동 충전하지 않고(증가 시 Keep), 감소 시 clamp만 수행합니다.
            SubscribeResourceMaxChange(TotalHpTemp, CurrentHpTemp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
        }

        /// <summary>
        /// 리소스 보정 정책을 적용할 Settings를 반환합니다.
        /// </summary>
        protected virtual GGemCoPlayerSettings GetPlayerSettingsForResourcePolicy()
        {
            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerSettings : null;
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
        /// <param name="target"></param>
        /// <returns></returns>
        public bool AreFacingEachOther(CharacterBase target)
        {
            float attackerDir = GetFacingDirection();
            float targetDir = target.GetFacingDirection();

            float directionToMonster = Mathf.Sign(target.transform.position.x - transform.position.x);

            return Mathf.Approximately(attackerDir, directionToMonster) && Mathf.Approximately(targetDir, -directionToMonster);
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
        public bool IsStatusDontControl() => _currentStatus == CharacterConstants.CharacterStatus.DontControl;
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
        public bool IsStatusCastingSkill() => _currentStatus == CharacterConstants.CharacterStatus.CastingSkill;
        public bool IsStatusUseSkill() => _currentStatus == CharacterConstants.CharacterStatus.UseSkill;
        public CharacterConstants.CharacterStatus GetCurrentStatus() => _currentStatus;

        public CharacterConstants.BattleStatus GetBattleStatus() => _battleStatus;
        public bool IsInBattle() => _battleStatus == CharacterConstants.BattleStatus.InBattle;

        
        private void SetStatus(CharacterConstants.CharacterStatus value) => _currentStatus = value;

        private void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            if (_battleStatus == value) return;
            _battleStatus = value;
            CurrentBattleStatus.OnNext(_battleStatus);
        }

        public void SetBattleStatusNone() => SetBattleStatus(CharacterConstants.BattleStatus.None);
        public void SetBattleStatusInBattle() => SetBattleStatus(CharacterConstants.BattleStatus.InBattle);

        public void SetStatusNone() => SetStatus(CharacterConstants.CharacterStatus.None);
        public void SetStatusDead() => SetStatus(CharacterConstants.CharacterStatus.Dead);
        public void SetStatusIdle() => SetStatus(CharacterConstants.CharacterStatus.Idle);
        public void SetStatusRun() => SetStatus(CharacterConstants.CharacterStatus.Run);
        public void SetStatusAttack() => SetStatus(CharacterConstants.CharacterStatus.Attack);
        public void SetStatusAttackComboWait() => SetStatus(CharacterConstants.CharacterStatus.AttackComboWait);
        public void SetStatusDontMove() => SetStatus(CharacterConstants.CharacterStatus.DontMove);
        public void SetStatusDontControl() => SetStatus(CharacterConstants.CharacterStatus.DontControl);
        public void SetStatusMoveForce() => SetStatus(CharacterConstants.CharacterStatus.MoveForce);
        public void SetStatusCastingSkill() => SetStatus(CharacterConstants.CharacterStatus.CastingSkill);
        public void SetStatusUseSkill() => SetStatus(CharacterConstants.CharacterStatus.UseSkill);

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
            SetBattleStatusNone();
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
            // 어그로는 전투 진입/종료의 대표 신호로 사용한다.
            // (동작 상태와 분리된 축에서 UI/BGM 등이 반응할 수 있도록 BattleStatus를 갱신)
            if (set)
            {
                SetBattleStatusInBattle();
            }
            else
            {
                SetBattleStatusNone();
                SetAttackerTarget(null);
            }
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
        /// 원인(Source)을 포함하여 Affect를 적용합니다.
        /// CrowdControl 등 방향성 계산이 필요한 기능을 위해 사용합니다.
        /// </summary>
        public void AddAffect(int affectUid, GameObject source, float duration = 0)
        {
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, source, duration);
        }
        
        /// <summary>
        /// total move speed 가 변경되었을때 wait 애니메이션의 time scale 도 변경해주기 위해서
        /// track index = 0 의 time scale 을 변경해준다.
        /// </summary>
        /// <param name="value"></param>
        private void UpdateAnimationMoveTimeScale(long value)
        {
            CharacterAnimationController.UpdateTimeScaleMove(value/100f);
            if (value == 0)
                GcLogger.LogError($"이동속도가 0으로 업데이트 되었습니다. {gameObject.name}");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // todo 지워야하는 어펙트가 있고 유지해야하는 어펙트가 있다
            // AffectController?.RemoveAllAffects();

            if (_characterDamageController != null)
            {
                _characterDamageController.Dispose();
                _characterDamageController = null;
            }
        }

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
        // todo. 정리 필요
        public virtual void UseSkill(int skillUid, int skillLevel)
        {
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

        /// <summary>
        /// 범용 모션 애니메이션 event 발생시 처리
        /// </summary>
        /// <param name="motion"></param>
        public void AnimationEventMotion(StruckAnimationEventMotion motion)
        {
            motion ??= new StruckAnimationEventMotion();

            var e = new EventArgsOnAnimationEventMotion
            {
                Handled = false,
                Motion = motion
            };

            OnAnimationEventMotion?.Invoke(this, e);

            if (!e.Handled)
            {
                TryHandleMotionEventLegacy(e.Motion);
            }
        }

        /// <summary>
        /// CrowdControl 애니메이션 event 발생시 처리
        /// </summary>
        /// <param name="crowdControl"></param>
        public void AnimationEventCrowdControl(StruckAnimationEventCrowdControl crowdControl)
        {
            crowdControl ??= new StruckAnimationEventCrowdControl();

            var e = new EventArgsOnAnimationEventCrowdControl
            {
                Handled = false,
                CrowdControl = crowdControl
            };

            OnAnimationEventCrowdControl?.Invoke(this, e);

            if (!e.Handled)
            {
                TryHandleCrowdControlEventLegacy(e.CrowdControl);
            }
        }

        private void TryHandleCrowdControlEventLegacy(StruckAnimationEventCrowdControl crowdControl)
        {
            if (crowdControl == null || crowdControl.CrowdControlUid <= 0)
                return;

            if (_crowdControlController == null)
            {
                _crowdControlController = GetComponent<CharacterCrowdControlController>();
                if (_crowdControlController == null)
                    return;
            }

            GameObject source = crowdControl.UseSelfAsSource ? gameObject : null;
            _crowdControlController.ApplyCrowdControlByUid(crowdControl.CrowdControlUid, source);
        }

        private void TryHandleMotionEventLegacy(StruckAnimationEventMotion motion)
        {
            if (motion == null)
                return;

            if (_motionController == null)
            {
                _motionController = GetComponent<ICharacterMotionController>();
                if (_motionController == null)
                    return;
            }

            switch (motion.Action)
            {
                case AnimationMotionEventAction.Trigger:
                    return;
                case AnimationMotionEventAction.Start:
                {
                    MotionRequest request = BuildMotionRequest(motion);
                    _motionController.TryStartMotion(in request);
                    return;
                }
                case AnimationMotionEventAction.Cancel:
                    _motionController.CancelMotion(motion.Channel);
                    return;
                default:
                    return;
            }
        }

        private MotionRequest BuildMotionRequest(StruckAnimationEventMotion motion)
        {
            Vector2 direction = ResolveMotionEventDirection(motion);

            return new MotionRequest(
                motion.Channel,
                motion.Kind,
                direction,
                motion.Duration,
                motion.Distance,
                motion.EaseType,
                motion.StopAtEnd,
                motion.UseMovePosition,
                motion.AllowReplace,
                motion.HoldSecondsAfter,
                motion.Height,
                motion.ArcMode,
                motion.RiseEaseType,
                motion.FallEaseType,
                motion.ApexHoldNormalized,
                motion.RiseRatioNormalized,
                motion.FallRatioNormalized);
        }

        private Vector2 ResolveMotionEventDirection(StruckAnimationEventMotion motion)
        {
            if (motion.UseFacingDirection)
            {
                return new Vector2(GetFacingDirection(), 0f);
            }

            Vector2 direction = new Vector2(motion.DirectionX, motion.DirectionY);
            if (direction.sqrMagnitude <= 1e-6f)
            {
                return new Vector2(GetFacingDirection(), 0f);
            }

            return direction.normalized;
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
        /// <summary>
        /// 외부 시스템(스킬, AI 등)에서 캐릭터의 상태 전환을 요청합니다.
        /// Skill 패키지는 직접 <see cref="CharacterBase"/>를 조작하지 않고 이 API를 사용해야 합니다.
        /// </summary>
        public bool RequestAction(in CharacterActionRequest request)
        {
            // 사망 상태에서는 새로운 액션을 받지 않습니다.
            if (_currentStatus == CharacterConstants.CharacterStatus.Dead)
                return false;

            if (request.StopMove)
            {
                // 이동 입력/방향을 즉시 중단합니다.
                directionNormalize = Vector3.zero;
            }

            SetStatus(request.Status);
            return true;
        }

        /// <summary>
        /// 특정 상태를 종료합니다(현재 상태가 일치할 때만 해제).
        /// </summary>
        public void ClearAction(CharacterConstants.CharacterStatus status)
        {
            if (_currentStatus != status) return;

            // 기본 복귀 정책: Idle. (필요 시 호출부/상태머신 정책에 맞춰 확장)
            // todo. 정리 필요. casting, skill 사용 애니메이션 마지막 프레임에 GGemCoAniEventComplete 호출로 stop 처리를 하고 있다.
            // SetStatus(CharacterConstants.CharacterStatus.Idle);
        }

        public void AnimationEventGuardEnd()
        {
            var e = new EventArgsOnAnimationEventGuardEnd { Handled = false };

            // 모든 구독자에게 알림
            OnAnimationEventGuardEnd?.Invoke(this, e);

            // 아무도 처리하지 않았으면 레거시 실행
            if (!e.Handled)
            {
                
            }
        }

        #region 슈퍼아머

        protected void EnableSuperArmor(bool enable)
        {
            _characterDamageController.EnableSuperArmor(enable);
        }

        #endregion

        public void ApplyCrowdControl(int crowdControlUid, GameObject metadataDamageAttacker)
        {
            if (GcLogger.IsNull(_crowdControlController, nameof(CharacterCrowdControlController))) return;
            _crowdControlController.ApplyCrowdControlByUid(crowdControlUid, metadataDamageAttacker);
        }
    }
}
