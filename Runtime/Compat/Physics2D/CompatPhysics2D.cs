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
        /// 필터 없이 전체를 검색합니다.
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
            return OverlapCapsuleNonAlloc(point, size, direction, angle, filter, results);
        }

        /// <summary>
        /// Capsule Overlap 결과를 results 배열에 채우고, 채워진 개수를 반환합니다.
        /// ContactFilter2D를 통해 Layer / Depth / Trigger 정책을 전달할 수 있습니다.
        /// Unity 6+: OverlapCapsule(ContactFilter2D, Collider2D[]) 경로 사용
        /// 이전: OverlapCapsuleNonAlloc(layerMask/minDepth/maxDepth) 경로 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OverlapCapsuleNonAlloc(
            Vector2 point,
            Vector2 size,
            CapsuleDirection2D direction,
            float angle,
            ContactFilter2D contactFilter,
            Collider2D[] results)
        {
            if (results == null || results.Length == 0)
                return 0;

#if UNITY_6000_0_OR_NEWER
            return Physics2D.OverlapCapsule(point, size, direction, angle, contactFilter, results);
#else
            int layerMask = contactFilter.useLayerMask ? contactFilter.layerMask : Physics2D.AllLayers;

            float minDepth = contactFilter.useDepth ? contactFilter.minDepth : float.NegativeInfinity;
            float maxDepth = contactFilter.useDepth ? contactFilter.maxDepth : float.PositiveInfinity;

            // NOTE:
            // 구버전 OverlapCapsuleNonAlloc는 ContactFilter2D의 모든 세부 옵션을 직접 반영하지 못할 수 있습니다.
            // 현재는 layerMask / depth 중심으로 degrade 합니다.
            return Physics2D.OverlapCapsuleNonAlloc(
                point,
                size,
                direction,
                angle,
                results,
                layerMask,
                minDepth,
                maxDepth);
#endif
        }

        /// <summary>
        /// LayerMask 기반 ContactFilter2D 생성 헬퍼.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContactFilter2D CreateLayerFilter(
            LayerMask layerMask,
            bool useTriggers = true)
        {
            var filter = CompatContactFilter2D.CreateNoFilter();
            filter.SetLayerMask(layerMask);
            filter.useTriggers = useTriggers;
            return filter;
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
            return Physics2D.BoxCast(origin, size, angle, direction, contactFilter, results, distance);
#else
            int layerMask = contactFilter.useLayerMask ? contactFilter.layerMask : Physics2D.AllLayers;

            float minDepth = contactFilter.useDepth ? contactFilter.minDepth : float.NegativeInfinity;
            float maxDepth = contactFilter.useDepth ? contactFilter.maxDepth : float.PositiveInfinity;

            return Physics2D.BoxCastNonAlloc(origin, size, angle, direction, results, distance, layerMask, minDepth, maxDepth);
#endif
        }

        /// <summary>
        /// Collider2D 오브젝트 기준 오버랩을 results 배열에 채우고, 채워진 개수를 반환합니다.
        /// Unity 6+: Collider2D.Overlap(ContactFilter2D, Collider2D[]) 사용
        /// 이전: Collider2D.OverlapCollider(ContactFilter2D, Collider2D[]) 사용
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OverlapColliderNonAlloc(
            Collider2D collider,
            ContactFilter2D contactFilter,
            Collider2D[] results)
        {
            if (!collider || results == null || results.Length == 0)
                return 0;

#if UNITY_6000_0_OR_NEWER
            return collider.Overlap(contactFilter, results);
#else
            return collider.OverlapCollider(contactFilter, results);
#endif
        }

        /// <summary>
        /// 필터 없이(=no filter) 오버랩.
        /// 내부적으로 "필터 없음"을 생성해 전달합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OverlapColliderNonAlloc(
            Collider2D collider,
            Collider2D[] results)
        {
            var filter = CompatContactFilter2D.CreateNoFilter();
            return OverlapColliderNonAlloc(collider, filter, results);
        }
    }
}