using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 초기화와 런타임 부트스트랩 절차를 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        /// <summary>
        /// 캐릭터 런타임 초기화를 시작하고 필수 의존성을 준비합니다.
        /// </summary>
        protected override void Awake()
        {
            if (AddressableLoaderSettings.Instance == null) return;

            base.Awake();
            InitializeCharacterLifecycle();
        }

        /// <summary>
        /// 테이블 연동과 상태 동기화를 마무리하고 초기화 완료를 알립니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            FinalizeCharacterLifecycle();
        }

        /// <summary>
        /// Awake 단계에서 필요한 기본 초기화 순서를 실행합니다.
        /// </summary>
        private void InitializeCharacterLifecycle()
        {
            EnsureAffectSystem();
            ResetCharacterRuntimeState();
            InitTagSortingLayer();
            InitComponents();
            InitializeCombatSupport();
            InitializeRuntimeDefaults();
        }

        /// <summary>
        /// Start 단계에서 데이터 기반 초기화와 후처리를 수행합니다.
        /// </summary>
        private void FinalizeCharacterLifecycle()
        {
            _physicsOverrideController?.CaptureBaseGravityScale(force: true);
            originalScaleX = transform.localScale.x;

            InitializeByTable();
            InitializeByAnimationTable();
            InitializeByRegenData();

            BindResourcePolicies();
            CacheSceneMetrics();
            Stop(true);
            MarkInitialized();
        }

        /// <summary>
        /// 런타임 시작 전 캐릭터의 내부 상태를 기본값으로 되돌립니다.
        /// </summary>
        private void ResetCharacterRuntimeState()
        {
            ClearPendingDeathState();
            CharacterRegenData = null;
            SetAttackType(CharacterConstants.AttackType.None);
            SetAggro(false);
            SetSubStatus(CharacterConstants.CharacterSubStatus.None);
        }

        /// <summary>
        /// 전투 관련 보조 컨트롤러와 참조를 초기화합니다.
        /// </summary>
        private void InitializeCombatSupport()
        {
            _projectileController = new ProjectileController();
            _projectileController.Initialize(this);

            _laserController = new LaserController();
            _laserController.Initialize(this);

            _characterDamageController = new CharacterDamageController();
            _characterDamageController.Initialize(this);

            characterPickUpPosition = GetComponentInChildren<CharacterPickUpPosition>();
        }

        /// <summary>
        /// 설정 파일 기반의 기본 런타임 옵션을 반영합니다.
        /// </summary>
        private void InitializeRuntimeDefaults()
        {
            // todo. 스킬 시스템 분리 시 전용 초기화 지점으로 이동 검토
            if (IsUseSkill)
            {
            }

            defaultFacingDirection8 = AddressableLoaderSettings.Instance.playerSettings.facingDirection8;
            _limitBoundaryBottom = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryBottom;
        }

        /// <summary>
        /// 리소스 정책과 Reactive 구독을 연결합니다.
        /// </summary>
        private void BindResourcePolicies()
        {
            SetupResourceMaxChangeSync();
            TotalMoveSpeed
                .Subscribe(UpdateAnimationMoveTimeScale)
                .AddTo(this);
        }

        /// <summary>
        /// 씬 기반 계산에 필요한 캐시 값을 갱신합니다.
        /// </summary>
        private void CacheSceneMetrics()
        {
            Vector2 size = SceneGame.Instance.mapManager.GetCurrentMapSize();
            _mapSizeHeight = size.y;
        }

        /// <summary>
        /// 캐릭터에 필요한 런타임 컴포넌트를 탐색하거나 추가합니다.
        /// </summary>
        protected virtual void InitComponents()
        {
            characterRigidbody2D = gameObject.GetComponent<Rigidbody2D>();
            _physicsOverrideController = gameObject.GetComponent<CharacterPhysicsOverrideController>();
            if (_physicsOverrideController == null)
            {
                _physicsOverrideController = gameObject.AddComponent<CharacterPhysicsOverrideController>();
            }

            colliderMapObject = gameObject.GetComponentInChildren<CapsuleCollider2D>();

            CharacterAttackRange characterAttackRange = gameObject.GetComponentInChildren<CharacterAttackRange>();
            if (characterAttackRange)
            {
                characterAttackRange.Initialize(this);
                colliderAttackRange = characterAttackRange.gameObject.GetComponent<CapsuleCollider2D>();
            }

            CharacterHitArea characterHitArea = gameObject.GetComponentInChildren<CharacterHitArea>();
            if (characterHitArea)
            {
                characterHitArea.Initialize(this);
                colliderHitArea = characterHitArea.gameObject.GetComponent<CapsuleCollider2D>();
            }

            _crowdControlController = gameObject.AddComponent<CharacterCrowdControlController>();
            _elementGaugeController = null;
            if (this is Player)
            {
                _elementGaugeController = gameObject.GetComponent<CharacterElementGaugeController>();
                if (_elementGaugeController == null)
                {
                    _elementGaugeController = gameObject.AddComponent<CharacterElementGaugeController>();
                }
            }
            _hitStopController = gameObject.GetComponent<CharacterHitStopController>();
            if (_hitStopController == null)
            {
                _hitStopController = gameObject.AddComponent<CharacterHitStopController>();
            }

            _motionController = gameObject.GetComponent<ICharacterMotionController>();
            if (_motionController == null)
            {
                _motionController = gameObject.AddComponent<CharacterMotionController2D>();
            }
        }

        /// <summary>
        /// 초기화 완료 플래그를 설정하고 완료 이벤트를 한 번만 발행합니다.
        /// </summary>
        private void MarkInitialized()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            Initialized?.Invoke();
        }

        /// <summary>
        /// 최대 리소스 변경 시 현재값을 보정하는 정책 구독을 설정합니다.
        /// </summary>
        private void SetupResourceMaxChangeSync()
        {
            var settings = GetPlayerSettingsForResourcePolicy();
            if (settings == null)
            {
                SubscribeResourceMaxChange(TotalHp, CurrentHp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                SubscribeResourceMaxChange(TotalMp, CurrentMp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                SubscribeResourceMaxChange(TotalStamina, CurrentStamina, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                SubscribeResourceMaxChange(TotalHpTemp, CurrentHpTemp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
                return;
            }

            SubscribeResourceMaxChange(TotalHp, CurrentHp, settings.hpMaxChangePolicy);
            SubscribeResourceMaxChange(TotalMp, CurrentMp, settings.mpMaxChangePolicy);
            SubscribeResourceMaxChange(TotalStamina, CurrentStamina, settings.staminaMaxChangePolicy);
            SubscribeResourceMaxChange(TotalHpTemp, CurrentHpTemp, CharacterConstants.ResourceMaxChangePolicy.KeepCurrent);
        }

        /// <summary>
        /// 리소스 보정 정책에 사용할 플레이어 설정을 반환합니다.
        /// </summary>
        /// <returns>적용할 설정 인스턴스입니다. 설정이 없으면 <see langword="null"/>입니다.</returns>
        protected virtual GGemCoPlayerSettings GetPlayerSettingsForResourcePolicy()
        {
            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerSettings : null;
        }

        /// <summary>
        /// 테이블 기반 캐릭터 데이터를 현재 인스턴스에 반영합니다.
        /// </summary>
        protected virtual void InitializeByTable()
        {
        }

        /// <summary>
        /// 리젠 데이터 기반의 초기 상태를 현재 인스턴스에 반영합니다.
        /// </summary>
        protected virtual void InitializeByRegenData()
        {
        }

        /// <summary>
        /// 애니메이션 테이블 정보를 읽어 이동 스텝과 기본 방향을 초기화합니다.
        /// </summary>
        /// <returns>애니메이션 정보를 성공적으로 반영했으면 <see langword="true"/>를 반환합니다.</returns>
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
        /// 이동 속도 변경에 맞춰 대기 애니메이션의 재생 속도를 갱신합니다.
        /// </summary>
        /// <param name="value">적용할 총 이동 속도 값입니다.</param>
        private void UpdateAnimationMoveTimeScale(long value)
        {
            CharacterAnimationController.UpdateTimeScaleMove(value / 100f);
            if (value == 0)
                GcLogger.LogError($"이동속도가 0으로 업데이트 되었습니다. {gameObject.name}");
        }

        /// <summary>
        /// Affect 패키지가 설치된 경우 런타임에 필요한 보조 컴포넌트를 준비합니다.
        /// </summary>
        private void EnsureAffectSystem()
        {
            AffectRuntimeBridge.EnsureAffectSystem(gameObject);
        }
    }
}
