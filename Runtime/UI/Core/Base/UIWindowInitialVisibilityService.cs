using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// TableWindow의 기본 활성 상태를 기준으로 UIWindow의 초기 표시 상태를 적용합니다.
    /// </summary>
    internal sealed class UIWindowInitialVisibilityService
    {
        private readonly Func<List<UIWindow>> _getManagedWindows;

        /// <summary>
        /// 초기 표시 상태 서비스를 생성합니다.
        /// </summary>
        /// <param name="getManagedWindows">현재 관리 중인 UIWindow 목록을 반환하는 함수입니다.</param>
        public UIWindowInitialVisibilityService(Func<List<UIWindow>> getManagedWindows)
        {
            _getManagedWindows = getManagedWindows;
        }

        /// <summary>
        /// 기본 비활성 윈도우를 레이아웃 갱신 전까지 시각적으로 숨긴 상태로 유지합니다.
        /// </summary>
        public void PrepareDefaultInactiveWindows()
        {
            List<UIWindow> windows = _getManagedWindows?.Invoke();
            if (windows == null)
            {
                return;
            }

            for (int i = 0; i < windows.Count; i++)
            {
                UIWindow window = windows[i];
                if (window == null || window.GetDefaultActive())
                {
                    continue;
                }

                window.PrepareDefaultInactiveBeforeInitialLayout();
            }
        }

        /// <summary>
        /// 모든 UIWindow의 Start 이후 기본 비활성 윈도우의 Transform/Layout을 한 번 갱신하고 최종 비활성화합니다.
        /// </summary>
        /// <returns>초기 레이아웃 적용을 지연하기 위한 코루틴입니다.</returns>
        public IEnumerator ApplyDefaultInactiveAfterInitialLayout()
        {
            // 각 UIWindow의 Start가 먼저 완료되어야 기본 초기화와 참조 캐시가 누락되지 않습니다.
            yield return null;

            Canvas.ForceUpdateCanvases();

            List<UIWindow> windows = _getManagedWindows?.Invoke();
            if (windows == null)
            {
                yield break;
            }

            for (int i = 0; i < windows.Count; i++)
            {
                UIWindow window = windows[i];
                if (window == null || window.GetDefaultActive())
                {
                    continue;
                }

                RebuildWindowLayout(window);
                window.ApplyDefaultInactiveAfterInitialLayout();
            }
        }

        /// <summary>
        /// 지정한 UIWindow의 RectTransform 계층 레이아웃을 즉시 갱신합니다.
        /// </summary>
        /// <param name="window">레이아웃을 갱신할 UIWindow입니다.</param>
        private static void RebuildWindowLayout(UIWindow window)
        {
            RectTransform rectTransform = window.transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
