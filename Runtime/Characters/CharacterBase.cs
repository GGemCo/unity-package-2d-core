using System;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어, 몬스터, NPC가 공유하는 공용 캐릭터 기반 클래스입니다.
    /// 2D 코어 시스템의 초기화, 전투, 표현, 애니메이션 이벤트를 partial 구현으로 나누어 제공합니다.
    /// </summary>
    public partial class CharacterBase : CharacterStat, ICharacterActionController
    {
        /// <summary>
        /// CharacterBase 초기화 완료 여부입니다.
        /// 외부 시스템은 이 값이 <see langword="true"/>가 된 뒤 캐릭터 상태를 적용하는 것이 안전합니다.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// CharacterBase 초기화 완료 시점에 한 번 호출되는 이벤트입니다.
        /// </summary>
        public event Action Initialized;
        
        // 캐릭터 정보
        [HideInInspector] public CharacterConstants.Type type;
        [HideInInspector] public int uid;
        [HideInInspector] public int vid;
        [HideInInspector] public float currentMoveStep;
        [HideInInspector] public string characterName;
        
        // 캐릭터 방향 관련
        [HideInInspector] public CharacterConstants.FacingDirection8 defaultFacingDirection8 = CharacterConstants.FacingDirection8.Left;
        private CharacterConstants.FacingDirection8 _currentFacing = CharacterConstants.FacingDirection8.Right;

        /// <summary>
        /// 현재 바라보는 8방향 값을 반환합니다.
        /// </summary>
        public CharacterConstants.FacingDirection8 CurrentFacing => _currentFacing;

        private bool _limitBoundaryBottom;
        
        // 좌우 플립 여부
        public bool isFlip;
        [HideInInspector] public Vector3 directionNormalize;
        private bool _isPossibleFlip = true;
        [HideInInspector] public float originalScaleX;
        
        // 애니메이션 및 렌더링 관련
        public ICharacterAnimationController CharacterAnimationController;
        private Renderer _characterRenderer;
        private CharacterConstants.CharacterSortingOrder _sortingOrder;

        /// <summary>
        /// 전투 상태 변경을 구독할 Reactive 스트림입니다.
        /// </summary>
        public readonly BehaviorSubject<CharacterConstants.BattleStatus> CurrentBattleStatus = new(CharacterConstants.BattleStatus.None);
        
        protected bool IsUseSkill = false;
        private ProjectileController _projectileController;
        
        public CharacterRegenData CharacterRegenData;
        
        private CharacterConstants.CharacterSubStatus _currentSubStatus;
        private bool _isStartFade;
        private float _characterHeight;
        private float _characterWidth;

        [HideInInspector] public Transform attackerTransform;
        [HideInInspector] public CapsuleCollider2D colliderAttackRange;
        [HideInInspector] public CapsuleCollider2D colliderHitArea;
        private float _mapSizeHeight;
        [HideInInspector] public Rigidbody2D characterRigidbody2D;
        [HideInInspector] public CapsuleCollider2D colliderMapObject;

        private CharacterDamageController _characterDamageController;
        protected CharacterPickUpPosition characterPickUpPosition;
        private CharacterCrowdControlController _crowdControlController;
        private ICharacterMotionController _motionController;
        private CharacterPhysicsOverrideController _physicsOverrideController;
        private SpriteWhiteOverlayController _spriteWhiteOverlayController;
        private CharacterHitStopController _hitStopController;
        
        public event EventHandlerAnimationCompleteAttack AnimationCompleteAttack;
        public event EventHandlerAnimationCompleteAttackEnd AnimationCompleteAttackEnd;
        public event EventHandlerOnStop OnStop;
        public event EventHandlerOnAnimationEventJump OnAnimationEventJump;
        public event EventHandlerOnAnimationEventDash OnAnimationEventDash;
        public event EventHandlerOnAnimationEventMotion OnAnimationEventMotion;
        public event EventHandlerOnAnimationEventCrowdControl OnAnimationEventCrowdControl;
        public event EventHandlerOnAnimationEventGuardEnd OnAnimationEventGuardEnd;
        
        public static event Action<CharacterBase> OnCharacterUseTool;
        public static event Action<CharacterBase> OnCharacterUseSeed;
        
        /// <summary>
        /// 스프라이트 화이트 오버레이 연출 설정을 표현하는 값 타입입니다.
        /// </summary>
        public readonly struct HitStopConfig
        {
            /// <summary>
            /// 경직을 사용하지 않는 기본 설정을 반환합니다.
            /// </summary>
            public static HitStopConfig Disabled => new HitStopConfig(false, 0f, 0f, true, true, true, true);

            public readonly bool Enabled;
            public readonly float DefaultSelfSeconds;
            public readonly float DefaultReceiveSeconds;
            public readonly bool PauseAnimation;
            public readonly bool FreezePhysics;
            public readonly bool LockControl;
            public readonly bool LockMovement;

            public HitStopConfig(
                bool enabled,
                float defaultSelfSeconds,
                float defaultReceiveSeconds,
                bool pauseAnimation,
                bool freezePhysics,
                bool lockControl,
                bool lockMovement)
            {
                Enabled = enabled;
                DefaultSelfSeconds = Mathf.Max(0f, defaultSelfSeconds);
                DefaultReceiveSeconds = Mathf.Max(0f, defaultReceiveSeconds);
                PauseAnimation = pauseAnimation;
                FreezePhysics = freezePhysics;
                LockControl = lockControl;
                LockMovement = lockMovement;
            }
        }

        public readonly struct SpriteWhiteOverlayConfig
        {
            /// <summary>
            /// 오버레이를 사용하지 않는 기본 설정을 반환합니다.
            /// </summary>
            public static SpriteWhiteOverlayConfig Disabled => new SpriteWhiteOverlayConfig(false, Color.white, 0f);

            public readonly bool Enabled;
            public readonly Color Color;
            public readonly float FlashDuration;

            /// <summary>
            /// 화이트 오버레이 설정을 생성합니다.
            /// </summary>
            /// <param name="enabled">오버레이 활성화 여부입니다.</param>
            /// <param name="color">플래시에 사용할 색상입니다.</param>
            /// <param name="flashDuration">플래시 지속 시간입니다.</param>
            public SpriteWhiteOverlayConfig(bool enabled, Color color, float flashDuration)
            {
                Enabled = enabled;
                Color = color;
                FlashDuration = flashDuration;
            }
        }

        /// <summary>
        /// 외부에서 준비한 스프라이트 오버레이 컨트롤러를 바인딩합니다.
        /// </summary>
        /// <param name="controller">바인딩할 오버레이 컨트롤러입니다.</param>
        public void BindSpriteWhiteOverlayController(SpriteWhiteOverlayController controller)
        {
            _spriteWhiteOverlayController = controller;
        }

        /// <summary>
        /// 설정에 따라 스프라이트 오버레이 컨트롤러를 준비합니다.
        /// </summary>
        /// <returns>오버레이가 활성화되어 컨트롤러 준비에 성공했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryEnsureSpriteWhiteOverlayController()
        {
            var config = GetSpriteWhiteOverlayConfig();
            if (!config.Enabled)
            {
                return false;
            }

            var controller = _spriteWhiteOverlayController != null
                ? _spriteWhiteOverlayController
                : GetComponent<SpriteWhiteOverlayController>();

            if (controller == null)
            {
                controller = gameObject.AddComponent<SpriteWhiteOverlayController>();
            }

            controller.Configure(config.Color, refreshTargets: true);
            BindSpriteWhiteOverlayController(controller);
            return true;
        }

        /// <summary>
        /// 피격 시 스프라이트 화이트 오버레이 플래시를 재생합니다.
        /// </summary>
        public void TryPlaySpriteWhiteOverlayOnHit()
        {
            var config = GetSpriteWhiteOverlayConfig();
            if (!config.Enabled)
            {
                return;
            }

            var controller = _spriteWhiteOverlayController != null
                ? _spriteWhiteOverlayController
                : GetComponent<SpriteWhiteOverlayController>();

            if (controller == null)
            {
                return;
            }

            controller.Configure(config.Color);
            controller.Flash(Mathf.Max(0.01f, config.FlashDuration));
            BindSpriteWhiteOverlayController(controller);
        }

        /// <summary>
        /// 현재 캐릭터에 바인딩된 경직 컨트롤러를 반환합니다. 필요하면 자동으로 추가합니다.
        /// </summary>
        public CharacterHitStopController HitStopController
        {
            get
            {
                if (_hitStopController == null)
                {
                    _hitStopController = GetComponent<CharacterHitStopController>();
                    if (_hitStopController == null)
                    {
                        _hitStopController = gameObject.AddComponent<CharacterHitStopController>();
                    }
                }

                return _hitStopController;
            }
        }

        /// <summary>
        /// 현재 경직이 활성화되어 있는지 여부입니다.
        /// </summary>
        public bool IsHitStopped => _hitStopController != null && _hitStopController.IsActive;

        /// <summary>
        /// 캐릭터 타입에 맞는 기본 경직 설정을 계산합니다.
        /// </summary>
        protected virtual HitStopConfig GetHitStopConfig()
        {
            if (this is Player)
            {
                var playerSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.playerSettings
                    : null;

                if (playerSettings == null || !playerSettings.useHitStop)
                {
                    return HitStopConfig.Disabled;
                }

                return new HitStopConfig(
                    true,
                    playerSettings.defaultSelfHitStopSeconds,
                    playerSettings.defaultReceiveHitStopSeconds,
                    playerSettings.hitStopPauseAnimation,
                    playerSettings.hitStopFreezePhysics,
                    playerSettings.hitStopLockControl,
                    playerSettings.hitStopLockMovement);
            }

            if (this is Monster)
            {
                var monsterSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.monsterSettings
                    : null;

                if (monsterSettings == null || !monsterSettings.useHitStop)
                {
                    return HitStopConfig.Disabled;
                }

                return new HitStopConfig(
                    true,
                    monsterSettings.defaultSelfHitStopSeconds,
                    monsterSettings.defaultReceiveHitStopSeconds,
                    monsterSettings.hitStopPauseAnimation,
                    monsterSettings.hitStopFreezePhysics,
                    monsterSettings.hitStopLockControl,
                    monsterSettings.hitStopLockMovement);
            }

            return HitStopConfig.Disabled;
        }

        /// <summary>
        /// 현재 캐릭터에 해석된 기본 경직 설정을 반환합니다.
        /// </summary>
        public HitStopConfig GetResolvedHitStopConfig() => GetHitStopConfig();

        /// <summary>
        /// 기본 설정을 사용해 자신에게 경직을 적용합니다.
        /// </summary>
        /// <param name="seconds">적용할 경직 시간(초)입니다.</param>
        /// <param name="sourceSkillUid">원인 스킬 UID입니다.</param>
        public void ApplyHitStop(float seconds, int sourceSkillUid = 0)
        {
            var config = GetHitStopConfig();
            if (!config.Enabled || seconds <= 0f)
            {
                return;
            }

            ApplyHitStop(new HitStopRequest(
                seconds,
                lockControl: config.LockControl,
                lockMovement: config.LockMovement,
                pauseAnimation: config.PauseAnimation,
                freezePhysics: config.FreezePhysics,
                sourceSkillUid: sourceSkillUid));
        }

        /// <summary>
        /// 지정한 요청으로 자신에게 경직을 적용합니다.
        /// </summary>
        public void ApplyHitStop(in HitStopRequest request)
        {
            if (request.DurationSeconds <= 0f || IsStatusDead())
            {
                return;
            }

            HitStopController.Apply(in request);
        }

        /// <summary>
        /// 현재 캐릭터 타입에 맞는 스프라이트 화이트 오버레이 설정을 계산합니다.
        /// </summary>
        /// <returns>현재 캐릭터에 적용할 오버레이 설정입니다.</returns>
        protected virtual SpriteWhiteOverlayConfig GetSpriteWhiteOverlayConfig()
        {
            if (this is Player)
            {
                var playerSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.playerSettings
                    : null;

                if (playerSettings == null || !playerSettings.useSpriteWhiteOverlay)
                {
                    return SpriteWhiteOverlayConfig.Disabled;
                }

                return new SpriteWhiteOverlayConfig(
                    true,
                    playerSettings.spriteWhiteOverlayColor,
                    playerSettings.spriteWhiteOverlayFlashDuration);
            }

            if (this is Monster)
            {
                var monsterSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.monsterSettings
                    : null;

                if (monsterSettings == null || !monsterSettings.useSpriteWhiteOverlay)
                {
                    return SpriteWhiteOverlayConfig.Disabled;
                }

                return new SpriteWhiteOverlayConfig(
                    true,
                    monsterSettings.spriteWhiteOverlayColor,
                    monsterSettings.spriteWhiteOverlayFlashDuration);
            }

            return SpriteWhiteOverlayConfig.Disabled;
        }

        /// <summary>
        /// 공용 Ground probe 규칙으로 현재 캐릭터가 지면 위에 있는지 판정합니다.
        /// Skill, Crowd Control 등 여러 시스템이 동일한 기준을 사용할 수 있도록 제공합니다.
        /// </summary>
        /// <param name="maxGroundDistance">지면 탐색에 사용할 최대 거리입니다.</param>
        /// <returns>지면 위에 있다고 판단되면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsCurrentlyGrounded(float maxGroundDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance)
        {
            return CharacterGroundProbeUtility.IsCurrentlyGrounded(this, characterRigidbody2D, maxGroundDistance);
        }

        /// <summary>
        /// 공용 Ground probe 규칙으로 캐릭터 하단의 지면을 탐색합니다.
        /// </summary>
        /// <param name="maxGroundDistance">지면 탐색에 사용할 최대 거리입니다.</param>
        /// <param name="groundY">탐색된 지면의 Y 좌표입니다.</param>
        /// <param name="bottomY">캐릭터 하단 기준 Y 좌표입니다.</param>
        /// <returns>지면 탐색에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryProbeGroundBelow(float maxGroundDistance, out float groundY, out float bottomY)
        {
            return CharacterGroundProbeUtility.TryProbeGroundBelow(this, characterRigidbody2D, maxGroundDistance, out groundY, out bottomY);
        }

        /// <summary>
        /// Skill/Crowd Control 공용 Ground probe 레이어 마스크를 반환합니다.
        /// </summary>
        /// <returns>공용 지면 탐색에 사용할 레이어 마스크입니다.</returns>
        public static int GetDefaultGroundProbeMask()
        {
            return CharacterGroundProbeUtility.GetDefaultGroundProbeMask();
        }

        /// <summary>
        /// 캐릭터 렌더러의 태그 및 정렬 레이어 관련 기본 설정을 적용합니다.
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
        /// 물리 오버라이드 컨트롤러를 반환합니다.
        /// 필요하면 현재 게임 오브젝트에 컴포넌트를 추가합니다.
        /// </summary>
        /// <returns>사용 가능한 <see cref="CharacterPhysicsOverrideController"/> 인스턴스입니다.</returns>
        public CharacterPhysicsOverrideController PhysicsOverrideController
        {
            get
            {
                if (_physicsOverrideController == null)
                {
                    _physicsOverrideController = GetComponent<CharacterPhysicsOverrideController>();
                    if (_physicsOverrideController == null)
                    {
                        _physicsOverrideController = gameObject.AddComponent<CharacterPhysicsOverrideController>();
                    }
                }

                return _physicsOverrideController;
            }
        }

        /// <summary>
        /// 캐릭터를 지정한 좌표로 즉시 이동시킵니다.
        /// </summary>
        /// <param name="x">이동시킬 월드 X 좌표입니다.</param>
        /// <param name="y">이동시킬 월드 Y 좌표입니다.</param>
        public void MoveTeleport(float x, float y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
        }

        /// <summary>
        /// 현재 캐릭터의 이동 스텝 값을 반환합니다.
        /// </summary>
        /// <returns>현재 이동 스텝 값입니다.</returns>
        public virtual float GetCurrentMoveStep()
        {
            return currentMoveStep;
        }

        /// <summary>
        /// 공격 애니메이션 이벤트를 처리하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <param name="struckAnimationEventAttack">처리할 공격 이벤트 데이터입니다.</param>
        public virtual void OnEventAttack(StruckAnimationEventAttack struckAnimationEventAttack)
        {
        }

        /// <summary>
        /// 캐릭터 소멸 시 내부 컨트롤러 정리와 중력 복구를 수행합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            // todo. 지워야 하는 Affect와 유지해야 하는 Affect 분리 정책 검토

            _physicsOverrideController?.ForceRestoreBaseGravity();

            if (_characterDamageController != null)
            {
                _characterDamageController.Dispose();
                _characterDamageController = null;
            }
        }

        /// <summary>
        /// 히트 영역 내부의 임의 Y 좌표를 반환합니다.
        /// </summary>
        /// <returns>히트 영역 기준의 랜덤 월드 Y 좌표입니다.</returns>
        public float GetRandomPositionYInHitArea()
        {
            if (!colliderHitArea)
            {
                return transform.position.y;
            }

            float halfHeight = colliderHitArea.size.y / 2f;
            float minLocalY = colliderHitArea.offset.y - halfHeight;
            float maxLocalY = colliderHitArea.offset.y + halfHeight;
            float randomLocalY = Random.Range(minLocalY, maxLocalY);
            Vector2 localPoint = new Vector2(0f, randomLocalY);
            Vector2 worldPoint = transform.TransformPoint(localPoint);
            return worldPoint.y;
        }

        // todo. 스킬 시스템 분리 시 전용 구현체로 이동 검토
        /// <summary>
        /// 스킬 사용을 처리하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <param name="skillUid">사용할 스킬 식별자입니다.</param>
        /// <param name="skillLevel">사용할 스킬 레벨입니다.</param>
        public virtual void UseSkill(int skillUid, int skillLevel)
        {
        }

        /// <summary>
        /// 현재 캐릭터가 플레이어인지 확인합니다.
        /// </summary>
        /// <returns>캐릭터 타입이 Player이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsPlayer()
        {
            return type == CharacterConstants.Type.Player;
        }

        /// <summary>
        /// 현재 캐릭터가 NPC인지 확인합니다.
        /// </summary>
        /// <returns>캐릭터 타입이 Npc이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsNPC()
        {
            return type == CharacterConstants.Type.Npc;
        }

        /// <summary>
        /// 현재 캐릭터가 몬스터인지 확인합니다.
        /// </summary>
        /// <returns>캐릭터 타입이 Monster이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsMonster()
        {
            return type == CharacterConstants.Type.Monster;
        }

        /// <summary>
        /// 현재 위치에서 추가로 강제 이동을 적용합니다.
        /// </summary>
        /// <param name="x">추가로 이동할 X 거리입니다.</param>
        /// <param name="y">추가로 이동할 Y 거리입니다.</param>
        /// <param name="duration">보간에 사용할 이동 시간입니다.</param>
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

        /// <summary>
        /// 지정한 위치까지 강제 이동을 시간에 따라 보간합니다.
        /// </summary>
        /// <param name="position">이동 목표 위치입니다.</param>
        /// <param name="duration">보간에 사용할 이동 시간입니다.</param>
        /// <returns>강제 이동이 완료될 때까지 진행되는 코루틴입니다.</returns>
        private System.Collections.IEnumerator MoveForceRoutine(Vector3 position, float duration = 0)
        {
            if (!characterRigidbody2D) yield break;
            if (duration == 0) yield break;
            
            Vector2 startPosition = transform.position;
            Vector2 targetPosition = position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float easedT = Easing.EaseOutQuad(t);
                Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
                characterRigidbody2D.MovePosition(newPosition);

                elapsed += Time.deltaTime;
                yield return null;
            }

            characterRigidbody2D.MovePosition(targetPosition);
        }

        /// <summary>
        /// 공격 범위 트리거 진입 시 파생 클래스가 구현할 확장 지점입니다.
        /// </summary>
        /// <param name="collision">충돌한 Collider입니다.</param>
        public virtual void OnTriggerEnterByAttackRange(Collider2D collision)
        {
        }

        /// <summary>
        /// 공격 범위 트리거 이탈 시 파생 클래스가 구현할 확장 지점입니다.
        /// </summary>
        /// <param name="collision">충돌이 종료된 Collider입니다.</param>
        /// <returns>정상적인 이탈 처리로 간주하면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool OnTriggerExitByAttackRange(Collider2D collision)
        {
#if UNITY_EDITOR
            if (UnityEditorHelper.IsExitingPlayMode)
                return false;
#endif
            return true;
        }

        /// <summary>
        /// 사망 애니메이션 완료 시 파생 클래스가 구현할 확장 지점입니다.
        /// </summary>
        public virtual void OnAnimationCompleteDead()
        {
        }

        /// <summary>
        /// RigidBody2D의 Sleep 모드를 설정합니다.
        /// </summary>
        /// <param name="mode">적용할 Rigidbody2D Sleep 모드입니다.</param>
        public void SetRigidBody2DSleepMode(RigidbodySleepMode2D mode)
        {
            if (!characterRigidbody2D)
            {
                GcLogger.LogError("RigidBody2D 컴포넌트가 없습니다.");
                return;
            }
            characterRigidbody2D.sleepMode = mode;
        }

        /// <summary>
        /// 도구 사용 이벤트를 발행합니다.
        /// </summary>
        public void UseTool()
        {
            OnCharacterUseTool?.Invoke(this);
        }

        /// <summary>
        /// 씨앗 사용 이벤트를 발행합니다.
        /// </summary>
        public void UseSeed()
        {
            OnCharacterUseSeed?.Invoke(this);
        }

        /// <summary>
        /// 현재 장비가 시뮬레이션 도구인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>시뮬레이션 도구가 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipSimulationTool()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 도끼인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>도끼가 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipAxe()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 곡괭이인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>곡괭이가 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipPickAxe()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 낫인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>낫이 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipSickle()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 괭이인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>괭이가 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipHoe()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 물뿌리개인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>물뿌리개가 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipWatering()
        {
            return false;
        }

        /// <summary>
        /// 현재 장비가 씨앗인지 확인하는 파생 클래스 확장 지점입니다.
        /// </summary>
        /// <returns>씨앗이 장착되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public virtual bool IsEquipSeed()
        {
            return false;
        }

        /// <summary>
        /// 슈퍼아머 상태를 활성화하거나 비활성화합니다.
        /// </summary>
        /// <param name="enable">활성화 여부입니다.</param>
        protected void EnableSuperArmor(bool enable)
        {
            _characterDamageController.EnableSuperArmor(enable);
        }
    }
}
