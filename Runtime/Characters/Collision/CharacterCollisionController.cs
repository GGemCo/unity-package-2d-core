using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 Body Collider의 레이어 적용과 이동 전 겹침 방지 정책을 담당합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCollisionController : MonoBehaviour
    {
        private const int CastHitCapacity = 12;

        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[CastHitCapacity];
        private CharacterBase _owner;
        private CapsuleCollider2D _bodyCollider;

        /// <summary>
        /// 현재 이동 차단에 사용할 Body Collider입니다.
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

            GGemCoPlayerSettings settings = GetSettings();
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
        /// 플레이어 설정에서 캐릭터 Body 충돌 옵션을 가져옵니다.
        /// </summary>
        /// <returns>설정 인스턴스입니다. 로딩 전이면 null입니다.</returns>
        private static GGemCoPlayerSettings GetSettings()
        {
            return AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.playerSettings
                : null;
        }

        /// <summary>
        /// 캐릭터 Body 충돌 기능 사용 여부를 반환합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정 인스턴스입니다.</param>
        /// <returns>사용 가능하면 true입니다.</returns>
        private static bool IsEnabled(GGemCoPlayerSettings settings)
        {
            return settings == null || settings.useCharacterBodyCollision;
        }

        /// <summary>
        /// 충돌 직전 정지를 위한 여유 거리를 반환합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정 인스턴스입니다.</param>
        /// <returns>0 이상으로 보정된 Skin Width 값입니다.</returns>
        private static float GetSkinWidth(GGemCoPlayerSettings settings)
        {
            return settings != null ? Mathf.Max(0f, settings.characterBodyCollisionSkinWidth) : 0.02f;
        }

        /// <summary>
        /// 이동 주체 타입 기준으로 차단해야 하는 상대 Body 레이어 마스크를 구성합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 캐릭터 타입입니다.</param>
        /// <returns>차단 대상으로 사용할 LayerMask 정수값입니다.</returns>
        private static int BuildBlockingLayerMask(GGemCoPlayerSettings settings, CharacterConstants.Type ownerType)
        {
            int mask = 0;
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Player, ref mask);
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Monster, ref mask);
            AppendIfBlocking(settings, ownerType, CharacterConstants.Type.Npc, ref mask);
            return mask;
        }

        /// <summary>
        /// 두 캐릭터 타입 관계가 차단 정책이면 대상 타입의 Body 레이어를 마스크에 추가합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정 인스턴스입니다.</param>
        /// <param name="ownerType">이동 주체 타입입니다.</param>
        /// <param name="otherType">검사할 상대 타입입니다.</param>
        /// <param name="mask">누적 레이어 마스크입니다.</param>
        private static void AppendIfBlocking(
            GGemCoPlayerSettings settings,
            CharacterConstants.Type ownerType,
            CharacterConstants.Type otherType,
            ref int mask)
        {
            if (ownerType == CharacterConstants.Type.None || otherType == CharacterConstants.Type.None)
                return;

            CharacterBodyCollisionPolicy policy = GetPolicy(settings, ownerType, otherType);
            if (policy != CharacterBodyCollisionPolicy.BlockMovement)
                return;

            mask |= CharacterCollisionLayerUtility.GetBodyLayerMask(otherType);
        }

        /// <summary>
        /// 두 캐릭터 타입 사이의 Body 충돌 정책을 반환합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정 인스턴스입니다.</param>
        /// <param name="a">첫 번째 캐릭터 타입입니다.</param>
        /// <param name="b">두 번째 캐릭터 타입입니다.</param>
        /// <returns>적용할 Body 충돌 정책입니다.</returns>
        private static CharacterBodyCollisionPolicy GetPolicy(
            GGemCoPlayerSettings settings,
            CharacterConstants.Type a,
            CharacterConstants.Type b)
        {
            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Monster))
                return settings != null ? settings.characterBodyCollisionPlayerMonster : CharacterBodyCollisionPolicy.BlockMovement;

            if (IsPair(a, b, CharacterConstants.Type.Player, CharacterConstants.Type.Npc))
                return settings != null ? settings.characterBodyCollisionPlayerNpc : CharacterBodyCollisionPolicy.BlockMovement;

            if (IsPair(a, b, CharacterConstants.Type.Monster, CharacterConstants.Type.Monster))
                return settings != null ? settings.characterBodyCollisionMonsterMonster : CharacterBodyCollisionPolicy.None;

            if (IsPair(a, b, CharacterConstants.Type.Monster, CharacterConstants.Type.Npc))
                return settings != null ? settings.characterBodyCollisionMonsterNpc : CharacterBodyCollisionPolicy.None;

            if (IsPair(a, b, CharacterConstants.Type.Npc, CharacterConstants.Type.Npc))
                return settings != null ? settings.characterBodyCollisionNpcNpc : CharacterBodyCollisionPolicy.None;

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
