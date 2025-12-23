using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// ContactFilter2D 버전 호환 유틸리티
    /// - Unity 6 이전/이후의 NoFilter 생성 방식 차이를 흡수합니다.
    /// </summary>
    public static class CompatContactFilter2D
    {
        /// <summary>
        /// 모든 레이어/뎁스를 허용하는 NoFilter 반환
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ContactFilter2D CreateNoFilter()
        {
#if UNITY_6000_0_OR_NEWER
            return ContactFilter2D.noFilter;
#else
            var filter = new ContactFilter2D();
            filter.NoFilter();
            return filter;
#endif
        }
    }
}