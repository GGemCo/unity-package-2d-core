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
    public partial class CharacterBase : CharacterStat, ICharacterActionController, ISuperArmorDamageReceiver
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
        private bool _defaultLimitBoundaryBottom;

        /// <summary>
        /// 맵 전환 등 의도적으로 플레이어를 재배치하는 동안 하단 경계 사망 처리를 일시 중지할지 여부입니다.
        /// </summary>
        private bool _suppressEndTilemapYDeath;

        /// <summary>
        /// 플레이어 설정에서 읽은 하단 맵 경계 제한 기본값을 저장합니다.
        /// 맵별 Parallax 정책이 해제될 때 원본 설정으로 되돌리기 위해 사용합니다.
        /// </summary>
        /// <param name="isEnabled">플레이어 설정에 정의된 하단 경계 제한 활성화 여부입니다.</param>
        protected void SetDefaultBoundaryBottomLimit(bool isEnabled)
        {
            _defaultLimitBoundaryBottom = isEnabled;
            _limitBoundaryBottom = isEnabled;
        }

        /// <summary>
        /// 현재 맵의 Parallax 사용 여부에 따라 하단 맵 경계 제한을 적용합니다.
        /// Parallax 맵에서는 제한을 해제하고, 일반 맵에서는 플레이어 설정의 원본 값으로 복원합니다.
        /// </summary>
        /// <param name="mapData">현재 적용할 맵 테이블 데이터입니다.</param>
        public void ApplyMapBoundaryBottomOverride(StruckTableMap mapData)
        {
            _limitBoundaryBottom = mapData != null && mapData.UseParallax
                ? false
                : _defaultLimitBoundaryBottom;
        }
        
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
        private LaserController _laserController;
        
        public CharacterRegenData CharacterRegenData;

        /// <summary>
        /// 현재 맵에서 이 캐릭터가 카메라 컬링을 따를지 여부를 나타내는 표시 정책입니다.
        /// </summary>
        private MapCharacterVisibilityPolicy _mapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;

        /// <summary>
        /// 현재 맵에서 이 캐릭터에게 적용할 표시/컬링 정책을 반환합니다.
        /// </summary>
        public MapCharacterVisibilityPolicy MapVisibilityPolicy => _mapVisibilityPolicy;
        
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
        private CharacterElementGaugeController _elementGaugeController;
        private bool _isMoveForceActive;
        private int _moveForceVersion;
        private Vector2 _moveForceTargetPosition;
        private static readonly WaitForFixedUpdate WaitForMoveForceFixedUpdate = new WaitForFixedUpdate();

        public CharacterElementGaugeController ElementGaugeController => _elementGaugeController;

        /// <summary>
        /// 현재 캐릭터가 강제 이동 보간을 진행 중인지 여부입니다.
        /// </summary>
        public bool IsMoveForceActive => _isMoveForceActive;
        
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
        /// 공중 상태를 관리하는 공용 컨트롤러입니다.
        /// 필요 시 현재 캐릭터 오브젝트에 자동으로 추가합니다.
        /// </summary>
        private CharacterAirborneStateController _airborneStateController;

        /// <summary>
        /// 공중 상태 컨트롤러를 반환합니다.
        /// Jump, Crowd Control, Skill Lunge 등 여러 시스템이 같은 기준으로 공중 상태를 등록/조회할 때 사용합니다.
        /// </summary>
        public CharacterAirborneStateController AirborneStateController
        {
            get
            {
                if (_airborneStateController == null)
                {
                    _airborneStateController = GetComponent<CharacterAirborneStateController>();
                    if (_airborneStateController == null)
                        _airborneStateController = gameObject.AddComponent<CharacterAirborneStateController>();
                }

                return _airborneStateController;
            }
        }

        /// <summary>
        /// 강제 공중 상태를 등록하고 해제용 핸들을 반환합니다.
        /// 각 시스템은 반환된 핸들을 보관했다가 자신이 등록한 상태만 해제해야 합니다.
        /// </summary>
        /// <param name="source">공중 상태를 등록한 원인입니다.</param>
        /// <param name="reason">디버그 확인용 사유 문자열입니다.</param>
        /// <returns>등록된 공중 상태 핸들입니다.</returns>
        public CharacterAirborneHandle AcquireAirborne(CharacterAirborneSource source, string reason = null)
        {
            return AirborneStateController.AcquireAirborne(source, reason);
        }

        /// <summary>
        /// 이전에 등록한 강제 공중 상태를 해제합니다.
        /// </summary>
        /// <param name="handle">해제할 공중 상태 핸들입니다.</param>
        /// <returns>실제로 해제되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool ReleaseAirborne(CharacterAirborneHandle handle)
        {
            if (_airborneStateController == null)
                return false;

            return _airborneStateController.ReleaseAirborne(handle);
        }

        /// <summary>
        /// Ground Probe와 강제 공중 토큰을 합산하여 현재 캐릭터가 공중 상태인지 반환합니다.
        /// </summary>
        /// <param name="maxGroundDistance">지면 판정에 사용할 최대 거리입니다.</param>
        /// <returns>공중 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsAirborne(float maxGroundDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance)
        {
            return TryGetAirborneInfo(out CharacterAirborneInfo info, maxGroundDistance) && info.IsAirborne;
        }

        /// <summary>
        /// 지정한 원인으로 인해 현재 캐릭터가 공중 상태인지 확인합니다.
        /// </summary>
        /// <param name="source">확인할 공중 상태 원인입니다.</param>
        /// <param name="maxGroundDistance">지면 판정에 사용할 최대 거리입니다.</param>
        /// <returns>지정한 원인이 활성화되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsAirborneBy(CharacterAirborneSource source, float maxGroundDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance)
        {
            return TryGetAirborneInfo(out CharacterAirborneInfo info, maxGroundDistance) && (info.Source & source) != 0;
        }

        /// <summary>
        /// 현재 캐릭터의 공중 상태 스냅샷을 조회합니다.
        /// </summary>
        /// <param name="info">계산된 공중 상태 정보입니다.</param>
        /// <param name="maxGroundDistance">지면 판정에 사용할 최대 거리입니다.</param>
        /// <returns>공중 상태 컨트롤러를 통해 정보를 계산했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGetAirborneInfo(out CharacterAirborneInfo info, float maxGroundDistance = CharacterGroundProbeUtility.DefaultGroundedCheckDistance)
        {
            info = AirborneStateController.GetAirborneInfo(maxGroundDistance);
            return true;
        }

        /// <summary>
        /// 맵 상주 캐릭터로 등록된 뒤 사용할 표시/컬링 정책을 설정합니다.
        /// </summary>
        /// <param name="policy">적용할 맵 표시 정책입니다.</param>
        public void SetMapVisibilityPolicy(MapCharacterVisibilityPolicy policy)
        {
            _mapVisibilityPolicy = policy;
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
        /// 하단 경계 이탈 사망 처리를 일시적으로 중지하거나 다시 활성화합니다.
        /// 맵 이동처럼 이전 맵을 제거한 뒤 새 스폰 좌표로 재배치하는 동안,
        /// 캐릭터가 아직 이전 좌표에 남아 있어 <see cref="CharacterConstants.DieReasonType.EndTilemapY"/>로 제거되는 상황을 방지합니다.
        /// </summary>
        /// <param name="isSuppressed"><see langword="true"/>이면 하단 경계 사망 처리를 중지하고, <see langword="false"/>이면 다시 활성화합니다.</param>
        public void SetEndTilemapYDeathSuppressed(bool isSuppressed)
        {
            _suppressEndTilemapYDeath = isSuppressed;
        }

        /// <summary>
        /// 현재 하단 경계 이탈 사망 처리가 일시 중지되어 있는지 반환합니다.
        /// </summary>
        /// <returns>하단 경계 사망 처리가 중지되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsEndTilemapYDeathSuppressed()
        {
            return _suppressEndTilemapYDeath;
        }

        /// <summary>
        /// 캐릭터를 지정한 좌표로 즉시 이동시킵니다.
        /// </summary>
        /// <param name="x">이동시킬 월드 X 좌표입니다.</param>
        /// <param name="y">이동시킬 월드 Y 좌표입니다.</param>
        public void MoveTeleport(float x, float y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
            RefreshCharacterBodyCollision();
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

        /// <summary>
        /// HitArea Collider 내부의 임의 월드 좌표를 반환합니다.
        /// </summary>
        /// <returns>HitArea 내부에서 선택된 임의 월드 좌표입니다. HitArea가 없으면 캐릭터 위치를 반환합니다.</returns>
        public Vector3 GetRandomWorldPositionInHitArea()
        {
            if (!colliderHitArea)
            {
                return transform.position;
            }

            return TryGetRandomWorldPositionInHitArea(colliderHitArea, out Vector3 randomPosition)
                ? randomPosition
                : ResolveHitAreaFallbackWorldPosition(colliderHitArea);
        }

        /// <summary>
        /// 지정한 HitArea Collider의 월드 Bounds 안에서 실제 Collider 내부에 포함되는 임의 좌표를 찾습니다.
        /// </summary>
        /// <param name="hitArea">임의 좌표를 선택할 HitArea Collider입니다.</param>
        /// <param name="worldPosition">선택된 월드 좌표입니다.</param>
        /// <returns>유효한 내부 좌표를 찾으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetRandomWorldPositionInHitArea(CapsuleCollider2D hitArea, out Vector3 worldPosition)
        {
            const int maxAttempts = 16;

            Bounds bounds = hitArea.bounds;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y));

                if (!hitArea.OverlapPoint(candidate))
                    continue;

                worldPosition = new Vector3(candidate.x, candidate.y, transform.position.z);
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// HitArea 내부 임의 좌표를 찾지 못했을 때 사용할 안전한 기준 위치를 반환합니다.
        /// </summary>
        /// <param name="hitArea">Fallback 기준으로 사용할 HitArea Collider입니다.</param>
        /// <returns>HitArea Bounds 중심을 캐릭터 Z 좌표와 결합한 월드 좌표입니다.</returns>
        private Vector3 ResolveHitAreaFallbackWorldPosition(CapsuleCollider2D hitArea)
        {
            Vector3 center = hitArea.bounds.center;
            center.z = transform.position.z;
            return center;
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
                int version = ++_moveForceVersion;
                StartCoroutine(MoveForceRoutine(newPosition, duration, version));
            }
            else
            {
                transform.position += new Vector3(x, y, 0);
            }
        }

        /// <summary>
        /// 캐릭터 Body 충돌 설정을 적용하면서 현재 위치에 강제 이동을 추가합니다.
        /// </summary>
        /// <param name="x">추가로 이동할 X 거리입니다.</param>
        /// <param name="y">추가로 이동할 Y 거리입니다.</param>
        /// <param name="duration">물리 틱 기준 보간에 사용할 이동 시간입니다.</param>
        /// <remarks>
        /// 기본 콤보 전진처럼 상대 캐릭터를 통과하면 안 되는 이동에 사용합니다.
        /// 관계별 차단과 분리 여부는 <see cref="GGemCoCharacterCollisionSettings"/>의 정책을 따릅니다.
        /// </remarks>
        public void AddMoveForceWithCharacterBodyCollision(float x = 0f, float y = 0f, float duration = 0f)
        {
            Vector2 moveDelta = new Vector2(x, y);
            if (moveDelta.sqrMagnitude <= 0.000001f) return;

            int version = ++_moveForceVersion;
            if (duration > 0f)
            {
                StartCoroutine(MoveForceWithCharacterBodyCollisionRoutine(moveDelta, duration, version));
                return;
            }

            TryApplyCharacterBodyCollisionMoveForceDelta(moveDelta);
            _isMoveForceActive = false;
            _moveForceTargetPosition = transform.position;
        }

        /// <summary>
        /// 진행 중인 강제 이동 보간을 취소합니다.
        /// </summary>
        /// <returns>취소할 강제 이동이 있었으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// <see cref="AddMoveForce(float, float, float)"/> 또는
        /// <see cref="AddMoveForceWithCharacterBodyCollision(float, float, float)"/>로 시작된 보간을 즉시 무효화합니다.
        /// 버전 값을 증가시켜 이전 보간 코루틴이 다음 프레임에 더 이상 위치를 갱신하지 않도록 차단합니다.
        /// </remarks>
        public bool CancelMoveForce()
        {
            if (!_isMoveForceActive)
                return false;

            _moveForceVersion++;
            _isMoveForceActive = false;
            _moveForceTargetPosition = transform.position;
            return true;
        }

        /// <summary>
        /// 현재 강제 이동 목표까지 남은 거리를 조회합니다.
        /// </summary>
        /// <param name="remainingDistance">강제 이동 목표까지 남은 월드 거리입니다.</param>
        /// <returns>진행 중인 강제 이동이 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGetRemainingMoveForceDistance(out float remainingDistance)
        {
            remainingDistance = 0f;
            if (!_isMoveForceActive)
            {
                return false;
            }

            remainingDistance = Vector2.Distance(transform.position, _moveForceTargetPosition);
            return true;
        }

        /// <summary>
        /// 지정한 위치까지 강제 이동을 시간에 따라 보간합니다.
        /// </summary>
        /// <param name="position">이동 목표 위치입니다.</param>
        /// <param name="duration">보간에 사용할 이동 시간입니다.</param>
        /// <param name="version">가장 최근 강제 이동 요청인지 판정하기 위한 버전 값입니다.</param>
        /// <returns>강제 이동이 완료될 때까지 진행되는 코루틴입니다.</returns>
        private System.Collections.IEnumerator MoveForceRoutine(Vector3 position, float duration, int version)
        {
            if (!characterRigidbody2D) yield break;
            if (duration <= 0f) yield break;
            
            Vector2 startPosition = transform.position;
            Vector2 targetPosition = position;
            float elapsed = 0f;

            if (version == _moveForceVersion)
            {
                _isMoveForceActive = true;
                _moveForceTargetPosition = targetPosition;
            }

            while (elapsed < duration)
            {
                if (version != _moveForceVersion)
                    yield break;

                float t = elapsed / duration;
                float easedT = Easing.EaseOutQuad(t);
                Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, easedT);
                characterRigidbody2D.MovePosition(newPosition);

                // HitStop 중에는 Rigidbody가 정지될 수 있으므로 남은 이동 시간이 소모되지 않도록 보존합니다.
                if (IsHitStopped)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (version != _moveForceVersion)
                yield break;

            characterRigidbody2D.MovePosition(targetPosition);
            _isMoveForceActive = false;
        }

        /// <summary>
        /// 캐릭터 Body 충돌 정책을 적용하여 지정한 이동량을 물리 틱별 증분으로 처리합니다.
        /// </summary>
        /// <param name="moveDelta">전체 강제 이동량입니다.</param>
        /// <param name="duration">이동을 진행할 시간입니다.</param>
        /// <param name="version">가장 최근 강제 이동 요청인지 판정하기 위한 버전 값입니다.</param>
        /// <returns>충돌 보정 강제 이동이 완료될 때까지 진행되는 코루틴입니다.</returns>
        private System.Collections.IEnumerator MoveForceWithCharacterBodyCollisionRoutine(
            Vector2 moveDelta,
            float duration,
            int version)
        {
            if (!characterRigidbody2D || duration <= 0f)
            {
                if (version == _moveForceVersion)
                {
                    _isMoveForceActive = false;
                    _moveForceTargetPosition = transform.position;
                }

                yield break;
            }

            float elapsed = 0f;
            float previousEasedProgress = 0f;

            if (version == _moveForceVersion)
            {
                _isMoveForceActive = true;
                _moveForceTargetPosition = characterRigidbody2D.position + moveDelta;
            }

            // 시작 시점부터 겹쳐 있는 경우에는 이동 차단 전에 우선 분리를 시도합니다.
            TrySeparateCharacterBodyOverlaps();

            while (elapsed < duration)
            {
                yield return WaitForMoveForceFixedUpdate;

                if (version != _moveForceVersion)
                    yield break;

                // HitStop 중에는 물리 이동과 진행 시간을 모두 보존합니다.
                if (IsHitStopped)
                    continue;

                float nextElapsed = Mathf.Min(duration, elapsed + Time.fixedDeltaTime);
                float normalizedTime = nextElapsed / duration;
                float easedProgress = Easing.EaseOutQuad(normalizedTime);
                float progressDelta = Mathf.Max(0f, easedProgress - previousEasedProgress);

                if (progressDelta > 0f)
                {
                    // 차단된 이동량은 다음 틱에 누적하지 않아 상대가 비켜날 때 순간 이동하지 않도록 합니다.
                    Vector2 requestedDelta = moveDelta * progressDelta;
                    TryApplyCharacterBodyCollisionMoveForceDelta(requestedDelta);
                }

                elapsed = nextElapsed;
                previousEasedProgress = easedProgress;
            }

            if (version != _moveForceVersion)
                yield break;

            // 마지막 MovePosition이 물리 월드에 반영된 다음 남은 겹침을 검사합니다.
            yield return WaitForMoveForceFixedUpdate;
            if (version != _moveForceVersion)
                yield break;

            // 목표점 강제 스냅 없이 실제 적용 위치를 기준으로 설정 정책에 따른 분리만 수행합니다.
            TrySeparateCharacterBodyOverlaps();
            _isMoveForceActive = false;
        }

        /// <summary>
        /// 단일 물리 틱의 강제 이동량을 캐릭터 Body 충돌 정책에 맞게 보정하여 적용합니다.
        /// </summary>
        /// <param name="requestedDelta">이번 물리 틱에 요청된 월드 이동량입니다.</param>
        /// <returns>보정된 이동량을 실제로 적용했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryApplyCharacterBodyCollisionMoveForceDelta(Vector2 requestedDelta)
        {
            if (requestedDelta.sqrMagnitude <= 0.000001f)
                return false;

            // 이전 물리 틱에서 이미 겹친 경우 먼저 분리한 뒤 현재 이동량의 진입 가능 여부를 검사합니다.
            TrySeparateCharacterBodyOverlaps();

            if (!TryResolveCharacterBodyMove(requestedDelta, out Vector3 resolvedDelta))
                return false;

            Vector2 resolved2D = new Vector2(resolvedDelta.x, resolvedDelta.y);
            if (resolved2D.sqrMagnitude <= 0.000001f)
                return false;

            if (characterRigidbody2D)
            {
                characterRigidbody2D.MovePosition(characterRigidbody2D.position + resolved2D);
            }
            else
            {
                transform.position += (Vector3)resolved2D;
            }

            return true;
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

        public void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete)
        {
            CharacterAnimationController.AnimationEventComplete(struckAnimationEventComplete);
        }
    }
}
