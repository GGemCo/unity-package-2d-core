using System.Runtime.CompilerServices;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// RectTransform anchoredPosition의 기준 위치를 캐싱하는 유틸리티입니다.
    /// </summary>
    public static class UIEffectMoveUtility
    {
        private sealed class State
        {
            public Vector2 BasePosition;
            public bool HasBasePosition;
        }

        private static readonly ConditionalWeakTable<RectTransform, State> States = new();

        /// <summary>
        /// 기준 위치가 아직 없을 때 현재 anchoredPosition을 기준 위치로 캐싱합니다.
        /// </summary>
        public static void CacheBasePosition(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            var state = States.GetOrCreateValue(target);
            if (state.HasBasePosition)
            {
                return;
            }

            state.BasePosition = target.anchoredPosition;
            state.HasBasePosition = true;
        }

        /// <summary>
        /// 현재 anchoredPosition을 새로운 기준 위치로 갱신합니다.
        /// </summary>
        public static void RefreshBasePosition(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            var state = States.GetOrCreateValue(target);
            state.BasePosition = target.anchoredPosition;
            state.HasBasePosition = true;
        }

        /// <summary>
        /// 캐시된 기준 위치를 반환하고, 없으면 현재 anchoredPosition을 기준으로 생성합니다.
        /// </summary>
        public static Vector2 GetOrCacheBasePosition(RectTransform target)
        {
            if (target == null)
            {
                return Vector2.zero;
            }

            var state = States.GetOrCreateValue(target);
            if (!state.HasBasePosition)
            {
                state.BasePosition = target.anchoredPosition;
                state.HasBasePosition = true;
            }

            return state.BasePosition;
        }
    }
}
