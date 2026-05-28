using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 이동 전 Body Collider 기준으로 충돌을 예측하고 안전한 이동량을 계산합니다.
    /// </summary>
    public static class CharacterMovementResolver2D
    {
        private const float MoveEpsilon = 0.000001f;

        /// <summary>
        /// 요청된 이동량을 캐릭터 Body 충돌 정책에 맞게 보정합니다.
        /// </summary>
        /// <param name="owner">이동 주체 캐릭터입니다.</param>
        /// <param name="bodyCollider">이동 주체의 Body Capsule Collider입니다.</param>
        /// <param name="requestedDelta">월드 기준 요청 이동량입니다.</param>
        /// <param name="blockingLayerMask">차단 대상으로 사용할 Body 레이어 마스크입니다.</param>
        /// <param name="skinWidth">충돌체와 살짝 떨어져 멈추기 위한 여유 거리입니다.</param>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <param name="hits">Cast 결과를 담을 재사용 배열입니다.</param>
        /// <param name="resolvedDelta">충돌을 고려해 보정된 이동량입니다.</param>
        /// <returns>일부라도 이동 가능하면 true, 완전히 차단되면 false입니다.</returns>
        public static bool TryResolveMove(
            CharacterBase owner,
            CapsuleCollider2D bodyCollider,
            Vector2 requestedDelta,
            int blockingLayerMask,
            float skinWidth,
            GGemCoCharacterCollisionSettings settings,
            RaycastHit2D[] hits,
            out Vector2 resolvedDelta)
        {
            resolvedDelta = requestedDelta;

            if (owner == null || bodyCollider == null || !bodyCollider.enabled)
                return true;

            float distance = requestedDelta.magnitude;
            if (distance <= MoveEpsilon)
                return true;

            if (blockingLayerMask == 0 || hits == null || hits.Length == 0)
                return true;

            Vector2 direction = requestedDelta / distance;
            Vector2 point = GetWorldCapsulePoint(bodyCollider);
            Vector2 size = GetWorldCapsuleSize(bodyCollider);
            float angle = bodyCollider.transform.eulerAngles.z;
            float safeSkinWidth = Mathf.Max(0f, skinWidth);

            // Body Collider를 Trigger로 운용하는 프리팹도 지원하기 위해 Trigger 결과를 포함합니다.
            // HitArea/AttackRange 같은 감지용 Trigger는 ShouldIgnoreHit에서 컴포넌트 기준으로 제외합니다.
            ContactFilter2D filter = CompatPhysics2D.CreateLayerFilter(blockingLayerMask, useTriggers: true);
            int hitCount = CompatPhysics2D.CapsuleCastNonAlloc(
                point,
                size,
                bodyCollider.direction,
                angle,
                direction,
                filter,
                hits,
                distance + safeSkinWidth);

            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hits[i];
                Collider2D hitCollider = hit.collider;
                if (ShouldIgnoreHit(owner, hitCollider, settings))
                    continue;

                // 이미 겹친 상태에서 시작한 경우에는 이동 자체가 영구 차단되지 않도록 해당 0거리 Hit를 건너뜁니다.
                // 정상 접촉(거리 0, 겹침 아님)은 계속 차단하여 새 겹침을 만들지 않습니다.
                if (hit.distance <= MoveEpsilon && IsAlreadyOverlapped(bodyCollider, hitCollider))
                    continue;

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                }
            }

            if (float.IsPositiveInfinity(nearestDistance))
                return true;

            float allowedDistance = Mathf.Clamp(nearestDistance - safeSkinWidth, 0f, distance);
            resolvedDelta = direction * allowedDistance;
            return allowedDistance > MoveEpsilon;
        }

        /// <summary>
        /// 두 Collider가 현재 이미 겹친 상태인지 검사합니다.
        /// </summary>
        /// <param name="bodyCollider">이동 주체 Body Collider입니다.</param>
        /// <param name="hitCollider">Cast로 감지된 상대 Collider입니다.</param>
        /// <returns>현재 프레임 시작 시점부터 이미 겹친 상태이면 true입니다.</returns>
        private static bool IsAlreadyOverlapped(Collider2D bodyCollider, Collider2D hitCollider)
        {
            if (bodyCollider == null || hitCollider == null)
                return false;

            ColliderDistance2D distance = bodyCollider.Distance(hitCollider);
            return distance.isValid && distance.isOverlapped;
        }

        /// <summary>
        /// Capsule Collider의 월드 기준 중심점을 계산합니다.
        /// </summary>
        /// <param name="collider">계산할 Capsule Collider입니다.</param>
        /// <returns>월드 기준 중심점입니다.</returns>
        private static Vector2 GetWorldCapsulePoint(CapsuleCollider2D collider)
        {
            return collider.transform.TransformPoint(collider.offset);
        }

        /// <summary>
        /// Capsule Collider의 월드 기준 크기를 계산합니다.
        /// </summary>
        /// <param name="collider">계산할 Capsule Collider입니다.</param>
        /// <returns>월드 스케일이 반영된 Capsule 크기입니다.</returns>
        private static Vector2 GetWorldCapsuleSize(CapsuleCollider2D collider)
        {
            Vector3 scale = collider.transform.lossyScale;
            return new Vector2(
                Mathf.Abs(collider.size.x * scale.x),
                Mathf.Abs(collider.size.y * scale.y));
        }

        /// <summary>
        /// Cast 결과 중 자기 자신과 감지용 Collider를 제외해야 하는지 판단합니다.
        /// </summary>
        /// <param name="owner">이동 주체 캐릭터입니다.</param>
        /// <param name="hitCollider">검사할 충돌체입니다.</param>
        /// <param name="settings">캐릭터 충돌 설정 인스턴스입니다.</param>
        /// <returns>이동 차단 대상에서 제외해야 하면 true입니다.</returns>
        private static bool ShouldIgnoreHit(CharacterBase owner, Collider2D hitCollider, GGemCoCharacterCollisionSettings settings)
        {
            if (owner == null || hitCollider == null)
                return true;

            if (hitCollider.transform.IsChildOf(owner.transform))
                return true;

            if (CharacterCollisionLayerUtility.IsSensorCollider(hitCollider))
                return true;

            CharacterBase other = hitCollider.GetComponentInParent<CharacterBase>();
            if (other == null || ReferenceEquals(other, owner))
                return true;

            if (!CharacterCollisionController.CanParticipateInCollision(other, settings))
                return true;

            return other.type == CharacterConstants.Type.None;
        }
    }
}
