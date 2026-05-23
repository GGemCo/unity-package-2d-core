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

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[CastHitCapacity];
        private readonly Collider2D[] _overlaps = new Collider2D[OverlapCapacity];

        private CharacterBase _owner;
        private CapsuleCollider2D _bodyCollider;
        private float _strongSeparationRemainingTime;
        private float _strongSeparationMultiplier = 1f;

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
            _bodyCollider = bodyCollider != null ? bodyCollider : CharacterCollisionLayerUtility.FindBodyCollider(_owner);
            Refresh();
        }

        /// <summary>
        /// 캐릭터 타입과 현재 Collider 상태를 기준으로 Body Collider 레이어를 다시 적용합니다.
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
            }

            ApplyBodyLayer();
        }

        /// <summary>
        /// 요청된 이동량을 캐릭터 Body 충돌 정책에 맞게 보정합니다.
        /// </summary>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        public bool TryResolveMove(Vector3 requestedDelta, out Vector3 resolvedDelta)
        {
            resolvedDelta = requestedDelta;

            CharacterCollisionSettings settings = GetSettings();
            if (!IsEnabled(settings))
                return true;

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
            if (!CanSeparate(settings))
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
            if (!CanSeparate(settings))
                return;

            RequestStrongSeparation(GetLandingSeparationDuration(settings), GetLandingSeparationMultiplier(settings));
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
