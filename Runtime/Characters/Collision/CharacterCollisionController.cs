using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider의 레이어 적용, 이동 전 차단, 겹침 해소 정책을 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCollisionController : MonoBehaviour
    {
        private const int CastHitCapacity = 12;
        private const int OverlapCapacity = 12;

        private static readonly HashSet<int> ConfiguredDeadBodyLayers = new();

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[CastHitCapacity];
        private readonly Collider2D[] _overlaps = new Collider2D[OverlapCapacity];

        private CharacterBase _owner;
        private CapsuleCollider2D _bodyCollider;
        private bool _bodyColliderDisabledByDeath;
        private bool _bodyColliderEnabledBeforeDeath;
        private float _strongSeparationRemainingTime;
        private float _strongSeparationMultiplier = 1f;
        private float _timedSeparationRemainingTime;
        private float _timedSeparationMultiplier = 1f;

        /// <summary>
        /// 현재 이동 차단과 겹침 해소에 사용할 Body Collider입니다.
        /// </summary>
        public CapsuleCollider2D BodyCollider => _bodyCollider;

        /// <summary>
        /// 캐릭터와 Body Collider 참조를 초기화합니다.
        /// </summary>
        /// <param name="owner">이 컨트롤러가 제어할 캐릭터입니다.</param>
        /// <param name="bodyCollider">이동 차단용 Body Collider 후보입니다.</param>
        public void Initialize(CharacterBase owner, CapsuleCollider2D bodyCollider)
        {
            _owner = owner != null ? owner : GetComponent<CharacterBase>();
            CapsuleCollider2D resolvedCollider = bodyCollider != null ? bodyCollider : CharacterCollisionLayerUtility.FindBodyCollider(_owner);
            if (!ReferenceEquals(_bodyCollider, resolvedCollider))
            {
                _bodyCollider = resolvedCollider;
                _bodyColliderDisabledByDeath = false;
                _bodyColliderEnabledBeforeDeath = _bodyCollider == null || _bodyCollider.enabled;
            }

            Refresh();
        }

        /// <summary>
        /// 캐릭터 타입과 현재 Collider 상태를 기준으로 Body Collider 레이어와 사망 상태를 다시 적용합니다.
        /// </summary>
        public void Refresh()
        {
            if (_owner == null)
            {
                _owner = GetComponent<CharacterBase>();
            }

            if (_bodyCollider == null)
            {
                _bodyCollider = CharacterCollisionLayerUtility.FindBodyCollider(_owner);
                _bodyColliderDisabledByDeath = false;
                _bodyColliderEnabledBeforeDeath = _bodyCollider == null || _bodyCollider.enabled;
            }

            ApplyBodyLayer();
            SyncDeathCollisionState();
        }

        /// <summary>
        /// 요청된 이동량을 캐릭터 Body 충돌 정책에 맞게 보정합니다.
        /// </summary>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        public bool TryResolveMove(Vector3 requestedDelta, out Vector3 resolvedDelta)
        {
            CharacterCollisionSettings settings = GetSettings();
            if (!IsEnabled(settings) || !CanParticipateInCollision(_owner, settings))
            {
                resolvedDelta = requestedDelta;
                return true;
            }

            return TryResolveMoveInternal(settings, requestedDelta, out resolvedDelta);
        }

        /// <summary>
        /// 모션 이동량을 모션 전용 Body 충돌 정책에 맞게 보정합니다.
        /// </summary>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="channel">모션 요청 채널입니다.</param>
        /// <param name="policyOverride">요청 단위에서 지정한 Body 충돌 정책입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        public bool TryResolveMotionMove(
            Vector3 requestedDelta,
            MotionChannel channel,
            MotionBodyCollisionPolicy policyOverride,
            out Vector3 resolvedDelta)
        {
            CharacterCollisionSettings settings = GetSettings();
            MotionBodyCollisionPolicy policy = ResolveMotionBodyCollisionPolicy(settings, channel, policyOverride);

            if (!CanUseMotionBodyCollision(settings) || !CanParticipateInCollision(_owner, settings) || !ShouldBlockBeforeMove(policy))
            {
                resolvedDelta = requestedDelta;
                return true;
            }

            return TryResolveMoveInternal(settings, requestedDelta, out resolvedDelta);
        }

        /// <summary>
        /// 캐릭터 Body 이동량 보정을 실제로 수행하는 공통 내부 함수입니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        private bool TryResolveMoveInternal(
            CharacterCollisionSettings settings,
            Vector3 requestedDelta,
            out Vector3 resolvedDelta)
        {
            resolvedDelta = requestedDelta;

            Refresh();

            if (_owner == null || _bodyCollider == null || !_bodyCollider.enabled)
                return true;

            int blockingLayerMask = BuildBlockingLayerMask(settings, _owner.type);
            if (blockingLayerMask == 0)
                return true;

            bool moved = CharacterMovementResolver2D.TryResolveMove(
                _owner,
                _bodyCollider,
                (Vector2)requestedDelta,
                blockingLayerMask,
                GetSkinWidth(settings),
                settings,
                _castHits,
                out Vector2 resolved2D);

            resolvedDelta = new Vector3(resolved2D.x, resolved2D.y, requestedDelta.z);
            return moved;
        }

        /// <summary>
        /// 현재 Body Collider와 겹친 상대 캐릭터를 정책에 따라 부드럽게 분리합니다.
        /// </summary>
        /// <param name="multiplier">이번 프레임에 적용할 분리 강도 배율입니다.</param>
        /// <returns>분리 이동을 적용했으면 true입니다.</returns>
        public bool TrySeparateOverlaps(float multiplier = 1f)
        {
            CharacterCollisionSettings settings = GetSettings();
            if (!CanSeparate(settings) || !CanParticipateInCollision(_owner, settings))
                return false;

            Refresh();

            if (_owner == null || _bodyCollider == null || !_bodyCollider.enabled)
                return false;

            int separationLayerMask = BuildSeparationLayerMask(settings, _owner.type);
            if (separationLayerMask == 0)
                return false;

            float finalMultiplier = Mathf.Max(0f, multiplier) * ConsumeStrongSeparationMultiplier();
            bool separated = CharacterBodySeparationResolver2D.TryResolveOverlap(
                _owner,
                _bodyCollider,
                separationLayerMask,
                GetSeparationMaxStep(settings) * finalMultiplier,
                GetSeparationPadding(settings),
                GetSeparationHorizontalBias(settings),
                GetSeparationVerticalBias(settings),
                settings,
                _overlaps,
                out Vector2 separationDelta);

            if (!separated)
                return false;

            _owner.transform.position += (Vector3)separationDelta;
            return true;
        }

        /// <summary>
        /// 일정 시간 동안 겹침 해소 강도를 높이도록 요청합니다.
        /// </summary>
        /// <param name="duration">강화 분리 보정을 유지할 시간입니다.</param>
        /// <param name="multiplier">분리 이동량에 곱할 배율입니다.</param>
        public void RequestStrongSeparation(float duration, float multiplier)
        {
            if (duration <= 0f || multiplier <= 1f)
                return;

            _strongSeparationRemainingTime = Mathf.Max(_strongSeparationRemainingTime, duration);
            _strongSeparationMultiplier = Mathf.Max(_strongSeparationMultiplier, multiplier);
        }

        /// <summary>
        /// 설정된 점프 착지용 강화 분리 보정을 요청합니다.
        /// </summary>
        public void RequestLandingSeparation()
        {
            CharacterCollisionSettings settings = GetSettings();
            if (!CanSeparate(settings) || !CanParticipateInCollision(_owner, settings))
                return;

            RequestStrongSeparation(GetLandingSeparationDuration(settings), GetLandingSeparationMultiplier(settings));
        }

        /// <summary>
        /// 일정 시간 동안 FixedUpdate에서 반복 겹침 해소를 수행하도록 요청합니다.
        /// </summary>
        /// <param name="duration">반복 분리 보정을 유지할 시간입니다.</param>
        /// <param name="multiplier">분리 이동량에 곱할 배율입니다.</param>
        public void RequestTimedSeparation(float duration, float multiplier)
        {
            if (duration <= 0f || multiplier <= 0f)
                return;

            _timedSeparationRemainingTime = Mathf.Max(_timedSeparationRemainingTime, duration);
            _timedSeparationMultiplier = Mathf.Max(_timedSeparationMultiplier, multiplier);
        }

        /// <summary>
        /// 모션 이동 후 채널별 설정과 요청 오버라이드에 맞춰 반복 겹침 해소를 요청합니다.
        /// </summary>
        /// <param name="channel">모션 요청 채널입니다.</param>
        /// <param name="policyOverride">요청 단위에서 지정한 Body 충돌 정책입니다.</param>
        /// <param name="durationOverride">반복 분리 지속 시간 오버라이드입니다. 0 미만이면 설정 기본값을 사용합니다.</param>
        /// <param name="multiplierOverride">분리 배율 오버라이드입니다. 0 이하이면 설정 기본값을 사용합니다.</param>
        public void RequestMotionSeparation(
            MotionChannel channel,
            MotionBodyCollisionPolicy policyOverride,
            float durationOverride,
            float multiplierOverride)
        {
            CharacterCollisionSettings settings = GetSettings();
            MotionBodyCollisionPolicy policy = ResolveMotionBodyCollisionPolicy(settings, channel, policyOverride);

            if (!CanUseMotionBodyCollision(settings) || !CanParticipateInCollision(_owner, settings) || !ShouldSeparateAfterMove(policy))
                return;

            float duration = GetMotionSeparationDuration(settings, durationOverride);
            float multiplier = GetMotionSeparationMultiplier(settings, channel, multiplierOverride);
            RequestTimedSeparation(duration, multiplier);
        }

        /// <summary>
        /// 사망 상태 변경 후 Body Collider의 충돌 참여 상태를 즉시 갱신합니다.
        /// </summary>
        public void ApplyDeathCollisionState()
        {
            SyncDeathCollisionState();
        }

        /// <summary>
        /// 풀 재사용이나 부활 후 Body Collider의 기존 활성 상태를 복원합니다.
        /// </summary>
        public void RestoreAliveCollisionState()
        {
            if (_bodyColliderDisabledByDeath && _bodyCollider != null)
            {
                _bodyCollider.enabled = _bodyColliderEnabledBeforeDeath;
                _bodyColliderDisabledByDeath = false;
            }

            Refresh();
        }

        /// <summary>
        /// 특정 캐릭터가 Body 충돌 검사에 참여할 수 있는 상태인지 검사합니다.
        /// </summary>
        /// <param name="character">검사할 캐릭터입니다.</param>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>이동 차단 또는 겹침 해소 대상으로 사용할 수 있으면 true입니다.</returns>
        public static bool CanParticipateInCollision(CharacterBase character, CharacterCollisionSettings settings)
        {
            if (character == null)
                return false;

            if (GetDeadCharacterBodyCollisionMode(settings) == DeadCharacterBodyCollisionMode.Keep)
                return true;

            if (character.IsStatusDead())
                return false;

            if (ShouldIgnoreDeathPendingCharacters(settings) && character.IsDeathPending)
                return false;

            return true;
        }

        /// <summary>
        /// 모션 이동 등에서 등록한 시간 기반 겹침 해소 요청을 FixedUpdate 주기로 처리합니다.
        /// </summary>
        private void FixedUpdate()
        {
            TickTimedSeparation(Time.fixedDeltaTime);
        }

        /// <summary>
        /// 남은 시간 기반 분리 요청을 진행하고, 필요 시 현재 겹침 상태를 해소합니다.
        /// </summary>
        /// <param name="deltaTime">이번 FixedUpdate의 시간 간격입니다.</param>
        private void TickTimedSeparation(float deltaTime)
        {
            if (_timedSeparationRemainingTime <= 0f)
                return;

            _timedSeparationRemainingTime = Mathf.Max(0f, _timedSeparationRemainingTime - Mathf.Max(0f, deltaTime));
            TrySeparateOverlaps(_timedSeparationMultiplier);

            if (_timedSeparationRemainingTime > 0f)
                return;

            _timedSeparationMultiplier = 1f;
        }

        /// <summary>
        /// 현재 캐릭터 타입에 맞는 Body Collider 레이어를 적용합니다.
        /// </summary>
        private void ApplyBodyLayer()
        {
            if (_owner == null || _bodyCollider == null)
                return;

            int bodyLayer = CharacterCollisionLayerUtility.GetBodyLayer(_owner.type);
            if (bodyLayer < 0)
                return;

            _bodyCollider.gameObject.layer = bodyLayer;
        }

        /// <summary>
        /// 사망 처리 정책에 따라 Body Collider 활성 상태와 레이어를 동기화합니다.
        /// </summary>
        private void SyncDeathCollisionState()
        {
            if (_bodyCollider == null)
                return;

            CharacterCollisionSettings settings = GetSettings();
            DeadCharacterBodyCollisionMode mode = GetDeadCharacterBodyCollisionMode(settings);
            bool isExcludedByDeath = IsEnabled(settings) && !CanParticipateInCollision(_owner, settings);
            bool shouldDisable = mode == DeadCharacterBodyCollisionMode.DisableBodyCollider && isExcludedByDeath;

            if (shouldDisable)
            {
                if (!_bodyColliderDisabledByDeath)
                {
                    _bodyColliderEnabledBeforeDeath = _bodyCollider.enabled;
                    _bodyColliderDisabledByDeath = true;
                }

                _bodyCollider.enabled = false;
                return;
            }

            if (_bodyColliderDisabledByDeath)
            {
                _bodyCollider.enabled = _bodyColliderEnabledBeforeDeath;
                _bodyColliderDisabledByDeath = false;
            }

            if (mode == DeadCharacterBodyCollisionMode.GroundOnlyLayer && isExcludedByDeath)
            {
                ApplyDeadBodyGroundOnlyLayer(settings);
            }
        }

        /// <summary>
        /// 사망 캐릭터 Body Collider를 지면 유지 전용 레이어로 변경합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <remarks>
        /// Collider를 비활성화하지 않고 레이어만 변경하여, 지면과는 계속 충돌하되
        /// 플레이어/몬스터/NPC Body Collider와는 충돌하지 않도록 분리합니다.
        /// </remarks>
        private void ApplyDeadBodyGroundOnlyLayer(CharacterCollisionSettings settings)
        {
            int deadBodyLayer = GetDeadCharacterBodyLayer(settings);
            if (deadBodyLayer < 0)
                return;

            EnsureDeadBodyLayerCollisionMatrix(settings, deadBodyLayer);
            _bodyCollider.gameObject.layer = deadBodyLayer;
        }

        /// <summary>
        /// 사망 Body 전용 레이어 인덱스를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>유효한 Unity 레이어이면 인덱스, 없으면 -1입니다.</returns>
        private static int GetDeadCharacterBodyLayer(CharacterCollisionSettings settings)
        {
            if (settings != null)
            {
                string layerName = settings.GetDeadCharacterBodyLayerName();
                return string.IsNullOrEmpty(layerName) ? -1 : LayerMask.NameToLayer(layerName);
            }

            return CharacterCollisionLayerUtility.GetLayer(ConfigLayer.Keys.CharacterBodyDead);
        }

        /// <summary>
        /// 사망 Body 전용 레이어의 런타임 충돌 행렬을 한 번 보정합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="deadBodyLayer">사망 Body 전용 Unity 레이어 인덱스입니다.</param>
        /// <remarks>
        /// 프로젝트 설정의 Layer Collision Matrix가 아직 정리되지 않은 경우에도
        /// 사망 몬스터가 플레이어 이동/바닥 체크 Collider를 막지 않도록 런타임에서 안전장치를 적용합니다.
        /// </remarks>
        private static void EnsureDeadBodyLayerCollisionMatrix(CharacterCollisionSettings settings, int deadBodyLayer)
        {
            if (deadBodyLayer < 0 || !ShouldConfigureDeadBodyLayerCollisionMatrix(settings))
                return;

            if (!ConfiguredDeadBodyLayers.Add(deadBodyLayer))
                return;

            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetBodyLayer(CharacterConstants.Type.Player), true);
            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetBodyLayer(CharacterConstants.Type.Monster), true);
            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetBodyLayer(CharacterConstants.Type.Npc), true);
            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetLayer(ConfigLayer.Keys.HitAreaPlayer), true);
            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetLayer(ConfigLayer.Keys.HitAreaMonster), true);

            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetLayer(ConfigLayer.Keys.TileMapGround), false);
            SetLayerCollisionIgnored(deadBodyLayer, CharacterCollisionLayerUtility.GetLayer(ConfigLayer.Keys.TileMapOneWayPlatform), false);
        }

        /// <summary>
        /// 두 레이어가 모두 유효할 때만 Physics2D 충돌 무시 상태를 적용합니다.
        /// </summary>
        /// <param name="a">첫 번째 Unity 레이어 인덱스입니다.</param>
        /// <param name="b">두 번째 Unity 레이어 인덱스입니다.</param>
        /// <param name="ignored">충돌을 무시해야 하면 true입니다.</param>
        private static void SetLayerCollisionIgnored(int a, int b, bool ignored)
        {
            if (a < 0 || b < 0 || a == b)
                return;

            Physics2D.IgnoreLayerCollision(a, b, ignored);
        }

        /// <summary>
        /// 현재 로드된 캐릭터 충돌 설정을 가져옵니다.
        /// </summary>
        /// <returns>설정 인스턴스입니다. 로딩 전이면 null입니다.</returns>
        private static CharacterCollisionSettings GetSettings()
        {
            return AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.characterCollisionSettings
                : null;
        }

        /// <summary>
        /// 캐릭터 Body 충돌 기능 사용 여부를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>사용 가능하면 true입니다.</returns>
        private static bool IsEnabled(CharacterCollisionSettings settings)
        {
            return settings == null || settings.useCharacterBodyCollision;
        }

        /// <summary>
        /// 캐릭터 Body 겹침 해소 기능 사용 여부를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>사용 가능하면 true입니다.</returns>
        private static bool CanSeparate(CharacterCollisionSettings settings)
        {
            return IsEnabled(settings) && (settings == null || settings.useCharacterBodySeparation);
        }

        /// <summary>
        /// 모션 이동용 캐릭터 Body 충돌 보정 기능을 사용할 수 있는지 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>사용 가능하면 true입니다.</returns>
        private static bool CanUseMotionBodyCollision(CharacterCollisionSettings settings)
        {
            return IsEnabled(settings) && (settings == null || settings.useMotionBodyCollision);
        }

        /// <summary>
        /// 채널과 요청 오버라이드를 기준으로 실제 적용할 모션 Body 충돌 정책을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="channel">모션 요청 채널입니다.</param>
        /// <param name="policyOverride">요청 단위 오버라이드 정책입니다.</param>
        /// <returns>실제로 적용할 모션 Body 충돌 정책입니다.</returns>
        private static MotionBodyCollisionPolicy ResolveMotionBodyCollisionPolicy(
            CharacterCollisionSettings settings,
            MotionChannel channel,
            MotionBodyCollisionPolicy policyOverride)
        {
            if (policyOverride != MotionBodyCollisionPolicy.UseCharacterDefault)
                return policyOverride;

            if (settings == null)
                return MotionBodyCollisionPolicy.SeparateAfterMove;

            return channel == MotionChannel.CrowdControl
                ? settings.crowdControlMotionBodyCollisionPolicy
                : settings.skillMotionBodyCollisionPolicy;
        }

        /// <summary>
        /// 모션 정책이 이동 전 차단을 요구하는지 검사합니다.
        /// </summary>
        /// <param name="policy">검사할 모션 Body 충돌 정책입니다.</param>
        /// <returns>이동 전 차단을 수행해야 하면 true입니다.</returns>
        private static bool ShouldBlockBeforeMove(MotionBodyCollisionPolicy policy)
        {
            return policy == MotionBodyCollisionPolicy.BlockBeforeMove ||
                   policy == MotionBodyCollisionPolicy.BlockAndSeparate;
        }

        /// <summary>
        /// 모션 정책이 이동 후 겹침 해소를 요구하는지 검사합니다.
        /// </summary>
        /// <param name="policy">검사할 모션 Body 충돌 정책입니다.</param>
        /// <returns>이동 후 겹침 해소를 수행해야 하면 true입니다.</returns>
        private static bool ShouldSeparateAfterMove(MotionBodyCollisionPolicy policy)
        {
            return policy == MotionBodyCollisionPolicy.SeparateAfterMove ||
                   policy == MotionBodyCollisionPolicy.BlockAndSeparate;
        }

        /// <summary>
        /// 사망 캐릭터 Body 충돌 처리 방식을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>설정된 사망 캐릭터 처리 방식입니다.</returns>
        private static DeadCharacterBodyCollisionMode GetDeadCharacterBodyCollisionMode(CharacterCollisionSettings settings)
        {
            return settings != null
                ? settings.deadCharacterBodyCollisionMode
                : DeadCharacterBodyCollisionMode.GroundOnlyLayer;
        }

        /// <summary>
        /// 사망 보류 상태 캐릭터를 Body 충돌 검사에서 제외할지 여부를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>제외해야 하면 true입니다.</returns>
        private static bool ShouldIgnoreDeathPendingCharacters(CharacterCollisionSettings settings)
        {
            return settings == null || settings.ignoreDeathPendingCharacters;
        }

        /// <summary>
        /// 사망 Body 전용 레이어의 런타임 충돌 행렬을 보정할지 여부를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>보정해야 하면 true입니다.</returns>
        private static bool ShouldConfigureDeadBodyLayerCollisionMatrix(CharacterCollisionSettings settings)
        {
            return settings == null || settings.configureDeadCharacterBodyLayerCollisionMatrix;
        }

        /// <summary>
        /// 충돌 직전 정지를 위한 여유 거리를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 Skin Width 값입니다.</returns>
        private static float GetSkinWidth(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.collisionSkinWidth) : 0.02f;
        }

        /// <summary>
        /// 한 프레임에 허용할 최대 분리 이동량을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 최대 이동량입니다.</returns>
        private static float GetSeparationMaxStep(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.separationMaxStep) : 0.06f;
        }

        /// <summary>
        /// 겹침 해소 후 남길 여유 거리를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 여유 거리입니다.</returns>
        private static float GetSeparationPadding(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.separationPadding) : 0.03f;
        }

        /// <summary>
        /// 수평 방향 분리 가중치를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 수평 가중치입니다.</returns>
        private static float GetSeparationHorizontalBias(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.separationHorizontalBias) : 1f;
        }

        /// <summary>
        /// 수직 방향 분리 가중치를 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 수직 가중치입니다.</returns>
        private static float GetSeparationVerticalBias(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.separationVerticalBias) : 0.2f;
        }

        /// <summary>
        /// 점프 착지 직후 강화 분리 지속 시간을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 지속 시간입니다.</returns>
        private static float GetLandingSeparationDuration(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.landingSeparationDuration) : 0.2f;
        }

        /// <summary>
        /// 점프 착지 직후 강화 분리 배율을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>1 이상으로 보정된 배율입니다.</returns>
        private static float GetLandingSeparationMultiplier(CharacterCollisionSettings settings)
        {
            return settings != null ? Mathf.Max(1f, settings.landingSeparationMultiplier) : 1.5f;
        }

        /// <summary>
        /// 모션 이동 후 겹침 해소 요청을 유지할 시간을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="overrideValue">요청 단위 오버라이드 값입니다.</param>
        /// <returns>0 이상으로 보정된 지속 시간입니다.</returns>
        private static float GetMotionSeparationDuration(CharacterCollisionSettings settings, float overrideValue)
        {
            if (overrideValue >= 0f)
                return Mathf.Max(0f, overrideValue);

            return settings != null ? Mathf.Max(0f, settings.motionSeparationDuration) : 0.18f;
        }

        /// <summary>
        /// 모션 이동 후 겹침 해소에 사용할 채널별 배율을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="channel">모션 요청 채널입니다.</param>
        /// <param name="overrideValue">요청 단위 오버라이드 값입니다.</param>
        /// <returns>1 이상으로 보정된 배율입니다.</returns>
        private static float GetMotionSeparationMultiplier(
            CharacterCollisionSettings settings,
            MotionChannel channel,
            float overrideValue)
        {
            if (overrideValue > 0f)
                return Mathf.Max(1f, overrideValue);

            if (settings == null)
                return channel == MotionChannel.CrowdControl ? 1.75f : 1.35f;

            return channel == MotionChannel.CrowdControl
                ? Mathf.Max(1f, settings.crowdControlMotionSeparationMultiplier)
                : Mathf.Max(1f, settings.skillMotionSeparationMultiplier);
        }

        /// <summary>
        /// 강화 분리 보정의 현재 배율을 반환하고 남은 시간을 갱신합니다.
        /// </summary>
        /// <returns>현재 프레임에 적용할 강화 분리 배율입니다.</returns>
        private float ConsumeStrongSeparationMultiplier()
        {
            if (_strongSeparationRemainingTime <= 0f)
            {
                _strongSeparationMultiplier = 1f;
                return 1f;
            }

            _strongSeparationRemainingTime -= Time.deltaTime;
            if (_strongSeparationRemainingTime <= 0f)
            {
                float lastMultiplier = Mathf.Max(1f, _strongSeparationMultiplier);
                _strongSeparationMultiplier = 1f;
                return lastMultiplier;
            }

            return Mathf.Max(1f, _strongSeparationMultiplier);
        }

        /// <summary>
        /// 이동 주체 타입 기준으로 차단해야 하는 상대 Body 레이어 마스크를 구성합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 캐릭터 타입입니다.</param>
        /// <returns>차단 대상으로 사용할 LayerMask 정수값입니다.</returns>
        private static int BuildBlockingLayerMask(CharacterCollisionSettings settings, CharacterConstants.Type ownerType)
        {
            int mask = 0;
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Player, ref mask);
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Monster, ref mask);
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Npc, ref mask);
            return mask;
        }

        /// <summary>
        /// 이동 주체 타입 기준으로 겹침 해소 대상 Body 레이어 마스크를 구성합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 캐릭터 타입입니다.</param>
        /// <returns>겹침 해소 대상으로 사용할 LayerMask 정수값입니다.</returns>
        private static int BuildSeparationLayerMask(CharacterCollisionSettings settings, CharacterConstants.Type ownerType)
        {
            int mask = 0;
            AppendIfSeparating(settings, ownerType, CharacterConstants.Type.Player, ref mask);
            AppendIfSeparating(settings, ownerType, CharacterConstants.Type.Monster, ref mask);
            AppendIfSeparating(settings, ownerType, CharacterConstants.Type.Npc, ref mask);
            return mask;
        }

        /// <summary>
        /// 두 캐릭터 타입 관계가 이동 차단 정책이면 대상 타입의 Body 레이어를 마스크에 추가합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 타입입니다.</param>
        /// <param name="otherType">검사할 상대 타입입니다.</param>
        /// <param name="mask">누적 레이어 마스크입니다.</param>
        private static void AppendIfBlocking(
            CharacterCollisionSettings settings,
            CharacterConstants.Type ownerType,
            CharacterConstants.Type otherType,
            ref int mask)
        {
            if (ownerType == CharacterConstants.Type.None || otherType == CharacterConstants.Type.None)
                return;

            CharacterBodyCollisionPolicy policy = GetPolicy(settings, ownerType, otherType);
            if (policy != CharacterBodyCollisionPolicy.BlockMovement && policy != CharacterBodyCollisionPolicy.BlockAndSeparate)
                return;

            mask |= CharacterCollisionLayerUtility.GetBodyLayerMask(otherType);
        }

        /// <summary>
        /// 두 캐릭터 타입 관계가 겹침 해소 정책이면 대상 타입의 Body 레이어를 마스크에 추가합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 타입입니다.</param>
        /// <param name="otherType">검사할 상대 타입입니다.</param>
        /// <param name="mask">누적 레이어 마스크입니다.</param>
        private static void AppendIfSeparating(
            CharacterCollisionSettings settings,
            CharacterConstants.Type ownerType,
            CharacterConstants.Type otherType,
            ref int mask)
        {
            if (ownerType == CharacterConstants.Type.None || otherType == CharacterConstants.Type.None)
                return;

            CharacterBodyCollisionPolicy policy = GetPolicy(settings, ownerType, otherType);
            if (policy != CharacterBodyCollisionPolicy.SeparateWhenOverlapped && policy != CharacterBodyCollisionPolicy.BlockAndSeparate)
                return;

            mask |= CharacterCollisionLayerUtility.GetBodyLayerMask(otherType);
        }

        /// <summary>
        /// 두 캐릭터 타입 사이의 Body 충돌 정책을 반환합니다.
        /// </summary>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="a">첫 번째 캐릭터 타입입니다.</param>
        /// <param name="b">두 번째 캐릭터 타입입니다.</param>
        /// <returns>적용할 Body 충돌 정책입니다.</returns>
        private static CharacterBodyCollisionPolicy GetPolicy(
            CharacterCollisionSettings settings,
            CharacterConstants.Type a,
            CharacterConstants.Type b)
        {
            if (settings != null)
                return settings.GetPolicy(a, b);

            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Monster))
                return CharacterBodyCollisionPolicy.BlockAndSeparate;

            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Npc))
                return CharacterBodyCollisionPolicy.BlockAndSeparate;

            return CharacterBodyCollisionPolicy.None;
        }

        /// <summary>
        /// 두 타입이 순서와 무관하게 동일한 관계인지 검사합니다.
        /// </summary>
        /// <param name="a">첫 번째 실제 타입입니다.</param>
        /// <param name="b">두 번째 실제 타입입니다.</param>
        /// <param name="expectedA">기대 타입 A입니다.</param>
        /// <param name="expectedB">기대 타입 B입니다.</param>
        /// <returns>동일한 관계이면 true입니다.</returns>
        private static bool IsPair(
            CharacterConstants.Type a,
            CharacterConstants.Type b,
            CharacterConstants.Type expectedA,
            CharacterConstants.Type expectedB)
        {
            return (a == expectedA && b == expectedB) ||
                   (a == expectedB && b == expectedA);
        }
    }
}
