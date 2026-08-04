using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라와 UI가 동일한 기준으로 정규화 Viewport를 계산하도록 공통 로직을 제공합니다.
    /// </summary>
    internal static class AspectViewportUtility
    {
        /// <summary>
        /// 화면 크기, 기준 종횡비, 확장 정책을 사용하여 정규화 Viewport를 계산합니다.
        /// </summary>
        /// <param name="screenWidth">현재 출력 화면의 픽셀 너비입니다.</param>
        /// <param name="screenHeight">현재 출력 화면의 픽셀 높이입니다.</param>
        /// <param name="targetAspect">기준으로 사용할 가로/세로 화면 비율입니다.</param>
        /// <param name="mode">기준보다 넓은 화면의 처리 방식입니다.</param>
        /// <returns>0~1 범위로 정규화된 Viewport입니다.</returns>
        internal static Rect CalculateViewportRect(
            int screenWidth,
            int screenHeight,
            float targetAspect,
            CameraAspectMode mode)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || targetAspect <= 0f ||
                float.IsNaN(targetAspect) || float.IsInfinity(targetAspect))
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            float screenAspect = screenWidth / (float)screenHeight;
            if (screenAspect > targetAspect)
            {
                if (mode == CameraAspectMode.ExpandHorizontal)
                {
                    // 기준보다 넓은 화면에서는 전체 폭을 사용하고 세로 범위는 그대로 유지합니다.
                    return new Rect(0f, 0f, 1f, 1f);
                }

                // 고정 비율 모드에서는 좌우에 동일한 여백을 두어 기준 폭을 유지합니다.
                float normalizedWidth = targetAspect / screenAspect;
                return new Rect((1f - normalizedWidth) * 0.5f, 0f, normalizedWidth, 1f);
            }

            // 기준보다 좁은 화면에서는 콘텐츠가 잘리지 않도록 상하에 동일한 여백을 둡니다.
            float normalizedHeight = screenAspect / targetAspect;
            return new Rect(0f, (1f - normalizedHeight) * 0.5f, 1f, normalizedHeight);
        }
    }
}
