using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 현재 지면 상태를 공용 규칙으로 판정하기 위한 Ground Probe 유틸리티입니다.
    /// Skill, Crowd Control 등 여러 시스템이 동일한 지면 판정 기준을 공유하도록 돕습니다.
    /// </summary>
    public static class CharacterGroundProbeUtility
    {
        /// <summary>
        /// Ground probe 시작 위치를 캐릭터 하단보다 약간 위로 올리기 위한 오프셋입니다.
        /// 지면과 거의 붙어있는 상태에서 Raycast 시작점이 즉시 히트되는 문제를 줄이기 위해 사용합니다.
        /// </summary>
        public const float ProbeUpOffset = 0.1f;

        /// <summary>
        /// 별도 값이 전달되지 않았을 때 사용할 기본 지상 판정 거리입니다.
        /// 캐릭터 하단에서 이 값 이하로 지면이 탐지되면 지상 상태로 간주합니다.
        /// </summary>
        public const float DefaultGroundedCheckDistance = 1.0f;

        /// <summary>
        /// 프로젝트 공용 Ground probe 레이어 마스크를 반환합니다.
        /// </summary>
        public static int GetDefaultGroundProbeMask()
        {
            int mask = 0;
            mask |= LayerMask.GetMask(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapGround));
            mask |= LayerMask.GetMask(ConfigLayer.GetValue(ConfigLayer.Keys.TileMapOneWayPlatform));
            return mask;
        }

        /// <summary>
        /// 캐릭터의 실제 Collider Bounds를 수집합니다.
        /// Trigger Collider는 제외하고, 자신의 Rigidbody2D에 연결된 Collider만 포함합니다.
        /// </summary>
        public static bool TryGetCharacterWorldBounds(Component owner, Rigidbody2D rigidbody2D, out Bounds bounds)
        {
            bounds = default;

            if (owner == null)
                return false;

            Collider2D[] colliders = owner.GetComponentsInChildren<Collider2D>();
            bool hasBounds = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider == null || !collider.enabled || collider.isTrigger)
                    continue;

                if (rigidbody2D != null && collider.attachedRigidbody != null && collider.attachedRigidbody != rigidbody2D)
                    continue;

                if (!hasBounds)
                {
                    bounds = collider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            if (!hasBounds)
            {
                Vector3 position = rigidbody2D != null ? (Vector3)rigidbody2D.position : owner.transform.position;
                bounds = new Bounds(position, Vector3.zero);
            }

            return true;
        }

        /// <summary>
        /// 캐릭터 하단에서 아래 방향으로 지면을 탐색합니다.
        /// </summary>
        public static bool TryProbeGroundBelow(Component owner, Rigidbody2D rigidbody2D, float maxGroundDistance, out float groundY, out float bottomY)
        {
            return TryProbeGroundBelow(owner, rigidbody2D, maxGroundDistance, GetDefaultGroundProbeMask(), out groundY, out bottomY);
        }

        /// <summary>
        /// 지정된 레이어 마스크를 사용해 캐릭터 하단에서 아래 방향으로 지면을 탐색합니다.
        /// </summary>
        public static bool TryProbeGroundBelow(Component owner, Rigidbody2D rigidbody2D, float maxGroundDistance, int groundMask, out float groundY, out float bottomY)
        {
            groundY = 0f;
            bottomY = 0f;

            if (owner == null || groundMask == 0)
                return false;

            if (!TryGetCharacterWorldBounds(owner, rigidbody2D, out Bounds bounds))
                return false;

            bottomY = bounds.min.y;
            Vector2 origin = new Vector2(bounds.center.x, bottomY + ProbeUpOffset);
            float distance = Mathf.Max(0f, maxGroundDistance) + ProbeUpOffset;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, groundMask);
            if (hit.collider == null)
                return false;

            groundY = hit.point.y;
            return true;
        }

        /// <summary>
        /// 캐릭터가 현재 지면 위에 있는지 여부를 반환합니다.
        /// </summary>
        public static bool IsCurrentlyGrounded(Component owner, Rigidbody2D rigidbody2D, float maxGroundDistance = DefaultGroundedCheckDistance)
        {
            return IsCurrentlyGrounded(owner, rigidbody2D, GetDefaultGroundProbeMask(), maxGroundDistance);
        }

        /// <summary>
        /// 지정된 레이어 마스크 기준으로 캐릭터가 현재 지면 위에 있는지 여부를 반환합니다.
        /// </summary>
        public static bool IsCurrentlyGrounded(Component owner, Rigidbody2D rigidbody2D, int groundMask, float maxGroundDistance = DefaultGroundedCheckDistance)
        {
            if (maxGroundDistance < 0f)
                maxGroundDistance = 0f;

            if (!TryProbeGroundBelow(owner, rigidbody2D, maxGroundDistance, groundMask, out float groundY, out float bottomY))
                return false;

            float distanceToGround = bottomY - groundY;
            return distanceToGround >= -ProbeUpOffset && distanceToGround <= maxGroundDistance;
        }
    }
}
