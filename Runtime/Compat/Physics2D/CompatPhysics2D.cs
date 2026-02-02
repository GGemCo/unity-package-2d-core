using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Physics2D 버전 호환 래퍼.
    /// - Unity 버전별 API 차이를 한 군데에서 흡수합니다.
    /// - 호출자는 "항상 hitCount 기반 루프"만 사용하도록 유도합니다.
    /// </summary>
    public static class CompatPhysics2D
    {
        /// <summary>
        /// Capsule Overlap 결과를 results 배열에 채우고, 채워진 개수를 반환합니다.
        /// Unity 6+: OverlapCapsule(ContactFilter2D, Collider2D[]) 경로 사용
        /// 이전: OverlapCapsuleNonAlloc(layerMask/minDepth/maxDepth) 경로 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OverlapCapsuleNonAlloc(
            Vector2 point,
            Vector2 size,
            CapsuleDirection2D direction,
            float angle,
            Collider2D[] results)
        {
            if (results == null || results.Length == 0)
                return 0;

            var filter = CompatContactFilter2D.CreateNoFilter();
#if UNITY_6000_0_OR_NEWER
            // Unity 6에서는 ContactFilter2D + 배열에 결과를 채우고 개수를 반환하는 형태를 사용
            // (사용자 코드에서 이미 이 오버로드를 사용 중)
            return Physics2D.OverlapCapsule(point, size, direction, angle, filter, results);
#else
            // 구버전: NonAlloc + layerMask/minDepth/maxDepth 기반
            // OverlapCapsuleNonAlloc는 results 배열에 채운 개수를 반환합니다. :contentReference[oaicite:0]{index=0}
            int layerMask = filter.useLayerMask ? filter.layerMask : Physics2D.AllLayers;

            // Depth 옵션을 쓰지 않으면 전체로
            float minDepth = filter.useDepth ? filter.minDepth : float.NegativeInfinity;
            float maxDepth = filter.useDepth ? filter.maxDepth : float.PositiveInfinity;

            return Physics2D.OverlapCapsuleNonAlloc(point, size, direction, angle, results, layerMask, minDepth, maxDepth);
#endif
        }

        /// <summary>
        /// BoxCast 결과를 results 배열에 채우고, 채워진 개수를 반환합니다.
        /// Unity 6+: BoxCast(ContactFilter2D, RaycastHit2D[]) 경로 사용
        /// 이전: BoxCastNonAlloc(layerMask/minDepth/maxDepth) 경로 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BoxCastNonAlloc(
            Vector2 origin,
            Vector2 size,
            float angle,
            Vector2 direction,
            ContactFilter2D contactFilter,
            RaycastHit2D[] results,
            float distance = Mathf.Infinity)
        {
            if (results == null || results.Length == 0)
                return 0;

#if UNITY_6000_0_OR_NEWER
            // Unity 6: int를 반환하며 results 배열에 채움
            return Physics2D.BoxCast(origin, size, angle, direction, contactFilter, results, distance);
#else
            // 구버전: NonAlloc + layerMask/minDepth/maxDepth 기반
            int layerMask = contactFilter.useLayerMask ? contactFilter.layerMask : Physics2D.AllLayers;

            float minDepth = contactFilter.useDepth ? contactFilter.minDepth : float.NegativeInfinity;
            float maxDepth = contactFilter.useDepth ? contactFilter.maxDepth : float.PositiveInfinity;

            // NOTE:
            // 구버전 BoxCastNonAlloc는 ContactFilter2D의 "노멀 각도" 등의 세부 필터를 직접 지원하지 않습니다.
            // (현재 사용처가 벽 감지용이라 layer/depth 필터만으로도 충분한 케이스가 대부분)
            return Physics2D.BoxCastNonAlloc(origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth);
#endif
        }
    }
}
