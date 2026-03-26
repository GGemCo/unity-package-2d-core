using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity Object 검색 API 버전 호환 래퍼.
    /// - FindObjectsOfType / FindObjectOfType obsolete 대응
    /// - Unity 버전별 API 차이를 한 군데에서 흡수
    /// - 호출자는 includeInactive / sortMode 의도만 전달
    /// </summary>
    public static class CompatObjectFind
    {
        /// <summary>
        /// 타입 T의 모든 Object를 검색합니다.
        /// 기본값은 비활성 제외, 정렬 없음입니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindAll<T>(
            bool includeInactive = false,
            FindObjectsSortMode sortMode = FindObjectsSortMode.None)
            where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                sortMode);
#else
            return Object.FindObjectsOfType<T>(includeInactive);
#endif
        }

        /// <summary>
        /// 타입 T의 첫 번째 Object를 검색합니다.
        /// "첫 번째" 의미가 필요한 경우 사용합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindFirst<T>(bool includeInactive = false)
            where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<T>(includeInactive);
#endif
        }

        /// <summary>
        /// 타입 T의 아무 Object 하나를 검색합니다.
        /// 순서가 중요하지 않고, 가장 빠른 1개 검색이 목적일 때 사용합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindAny<T>(bool includeInactive = false)
            where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            return Object.FindObjectOfType<T>(includeInactive);
#endif
        }

        /// <summary>
        /// 타입 T의 검색 결과를 List/foreach 없이 바로 순회할 때 사용할 수 있도록
        /// null/empty 보호된 배열을 반환합니다.
        /// 현재는 FindAll과 동일하지만 호출 의도를 명확히 하기 위한 별칭입니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindAllUnsorted<T>(bool includeInactive = false)
            where T : Object
        {
            return FindAll<T>(includeInactive, FindObjectsSortMode.None);
        }
    }
}