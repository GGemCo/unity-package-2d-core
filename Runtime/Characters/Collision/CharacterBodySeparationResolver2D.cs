using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 이미 겹친 캐릭터 Body Collider를 감지하고 자연스럽게 분리할 이동량을 계산합니다.
    /// </summary>
    public static class CharacterBodySeparationResolver2D
    {
        private const float DirectionEpsilon = 0.0001f;
        private const float MoveEpsilon = 0.000001f;

        /// <summary>
        /// 현재 Body Collider와 겹친 상대 Body Collider를 기준으로 분리 이동량을 계산합니다.
        /// </summary>
        /// <param name="owner">분리 이동을 적용할 캐릭터입니다.</param>
        /// <param name="bodyCollider">분리 기준 Body Collider입니다.</param>
        /// <param name="separationLayerMask">겹침 해소 대상으로 사용할 Body 레이어 마스크입니다.</param>
        /// <param name="maxStep">한 프레임에 적용할 최대 분리 거리입니다.</param>
        /// <param name="padding">겹침 해소 후 남길 추가 여유 거리입니다.</param>
        /// <param name="horizontalBias">수평 방향 분리 가중치입니다.</param>
        /// <param name="verticalBias">수직 방향 분리 가중치입니다.</param>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="overlaps">Overlap 결과를 담을 재사용 배열입니다.</param>
        /// <param name="separationDelta">계산된 월드 기준 분리 이동량입니다.</param>
        /// <returns>분리 이동량이 계산되었으면 true입니다.</returns>
        public static bool TryResolveOverlap(
            CharacterBase owner,
            CapsuleCollider2D bodyCollider,
            int separationLayerMask,
            float maxStep,
            float padding,
            float horizontalBias,
            float verticalBias,
            GGemCoCharacterCollisionSettings settings,
            Collider2D[] overlaps,
            out Vector2 separationDelta)
        {
            separationDelta = Vector2.zero;

            if (owner == null || bodyCollider == null || !bodyCollider.enabled)
                return false;

            if (separationLayerMask == 0 || overlaps == null || overlaps.Length == 0)
                return false;

            ContactFilter2D filter = CompatPhysics2D.CreateLayerFilter(separationLayerMask, useTriggers: true);
            int overlapCount = CompatPhysics2D.OverlapColliderNonAlloc(bodyCollider, filter, overlaps);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D otherCollider = overlaps[i];
                if (ShouldIgnore(owner, otherCollider, settings))
                    continue;

                ColliderDistance2D distance = bodyCollider.Distance(otherCollider);
                if (!distance.isValid || !distance.isOverlapped)
                    continue;

                CharacterBase other = otherCollider.GetComponentInParent<CharacterBase>();
                Vector2 direction = ResolvePreferredDirection(owner, other, distance.normal);
                if (direction.sqrMagnitude <= MoveEpsilon)
                    continue;

                float depth = Mathf.Abs(distance.distance) + Mathf.Max(0f, padding);
                Vector2 push = direction.normalized * depth;
                push.x *= Mathf.Max(0f, horizontalBias);
                push.y *= Mathf.Max(0f, verticalBias);

                separationDelta += push;
            }

            if (separationDelta.sqrMagnitude <= MoveEpsilon)
                return false;

            float safeMaxStep = Mathf.Max(0f, maxStep);
            if (safeMaxStep > 0f)
            {
                separationDelta = Vector2.ClampMagnitude(separationDelta, safeMaxStep);
            }

            return separationDelta.sqrMagnitude > MoveEpsilon;
        }

        /// <summary>
        /// 겹침 해소에서 제외해야 하는 Collider인지 검사합니다.
        /// </summary>
        /// <param name="owner">분리 이동을 적용할 캐릭터입니다.</param>
        /// <param name="otherCollider">검사할 상대 Collider입니다.</param>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>겹침 해소 대상에서 제외해야 하면 true입니다.</returns>
        private static bool ShouldIgnore(CharacterBase owner, Collider2D otherCollider, GGemCoCharacterCollisionSettings settings)
        {
            if (owner == null || otherCollider == null)
                return true;

            if (otherCollider.transform.IsChildOf(owner.transform))
                return true;

            if (CharacterCollisionLayerUtility.IsSensorCollider(otherCollider))
                return true;

            CharacterBase other = otherCollider.GetComponentInParent<CharacterBase>();
            if (other == null || ReferenceEquals(owner, other))
                return true;

            if (!CharacterCollisionController.CanParticipateInCollision(other, settings))
                return true;

            return other.type == CharacterConstants.Type.None;
        }

        /// <summary>
        /// 플레이어가 NPC/몬스터와 겹친 경우 수평 방향을 우선하여 자연스러운 분리 방향을 계산합니다.
        /// </summary>
        /// <param name="owner">분리 이동을 적용할 캐릭터입니다.</param>
        /// <param name="other">겹친 상대 캐릭터입니다.</param>
        /// <param name="distanceNormal">Unity ColliderDistance2D가 제공한 기본 분리 법선입니다.</param>
        /// <returns>분리에 사용할 방향입니다.</returns>
        private static Vector2 ResolvePreferredDirection(
            CharacterBase owner,
            CharacterBase other,
            Vector2 distanceNormal)
        {
            if (owner != null && owner.type == CharacterConstants.Type.Player && other != null)
            {
                float deltaX = owner.transform.position.x - other.transform.position.x;
                if (Mathf.Abs(deltaX) > DirectionEpsilon)
                    return new Vector2(Mathf.Sign(deltaX), 0f);

                if (Mathf.Abs(distanceNormal.x) > DirectionEpsilon)
                    return new Vector2(Mathf.Sign(distanceNormal.x), 0f);

                if (owner.directionNormalize.sqrMagnitude > DirectionEpsilon && Mathf.Abs(owner.directionNormalize.x) > DirectionEpsilon)
                    return new Vector2(-Mathf.Sign(owner.directionNormalize.x), 0f);

                return Vector2.right;
            }

            if (distanceNormal.sqrMagnitude > MoveEpsilon)
                return distanceNormal;

            return Vector2.right;
        }
    }
}
