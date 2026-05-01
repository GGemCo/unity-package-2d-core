using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프 에디터의 상단 툴바/본문/좌우 패널/중앙 캔버스 배치 정보를 보관합니다.
    /// </summary>
    internal readonly struct WorldMapEditorLayout
    {
        /// <summary>전체 창 Rect입니다.</summary>
        public readonly Rect WindowRect;

        /// <summary>상단 툴바 Rect입니다.</summary>
        public readonly Rect ToolbarRect;

        /// <summary>툴바를 제외한 본문 Rect입니다.</summary>
        public readonly Rect BodyRect;

        /// <summary>좌측 패널 Rect입니다.</summary>
        public readonly Rect LeftPanelRect;

        /// <summary>우측 패널 Rect입니다.</summary>
        public readonly Rect RightPanelRect;

        /// <summary>중앙 캔버스의 기본 배치 Rect입니다.</summary>
        public readonly Rect CanvasHostRect;

        /// <summary>
        /// 레이아웃 정보를 초기화합니다.
        /// </summary>
        /// <param name="windowRect">전체 창 Rect입니다.</param>
        /// <param name="toolbarRect">상단 툴바 Rect입니다.</param>
        /// <param name="bodyRect">툴바를 제외한 본문 Rect입니다.</param>
        /// <param name="leftPanelRect">좌측 패널 Rect입니다.</param>
        /// <param name="rightPanelRect">우측 패널 Rect입니다.</param>
        /// <param name="canvasHostRect">중앙 캔버스의 기본 배치 Rect입니다.</param>
        public WorldMapEditorLayout(
            Rect windowRect,
            Rect toolbarRect,
            Rect bodyRect,
            Rect leftPanelRect,
            Rect rightPanelRect,
            Rect canvasHostRect)
        {
            WindowRect = windowRect;
            ToolbarRect = toolbarRect;
            BodyRect = bodyRect;
            LeftPanelRect = leftPanelRect;
            RightPanelRect = rightPanelRect;
            CanvasHostRect = canvasHostRect;
        }
    }

    /// <summary>
    /// 월드맵 그래프 에디터의 고정 Rect 레이아웃을 계산하는 유틸리티입니다.
    /// </summary>
    internal static class WorldMapEditorLayoutUtility
    {
        /// <summary>
        /// 전체 창 Rect와 패널 폭 정보를 바탕으로 에디터 레이아웃을 계산합니다.
        /// </summary>
        /// <param name="windowRect">전체 창 Rect입니다.</param>
        /// <param name="leftPanelWidth">좌측 패널 폭입니다.</param>
        /// <param name="rightPanelWidth">우측 패널 폭입니다.</param>
        /// <param name="toolbarHeight">상단 툴바 높이입니다.</param>
        /// <returns>계산된 월드맵 에디터 레이아웃입니다.</returns>
        public static WorldMapEditorLayout Build(
            Rect windowRect,
            float leftPanelWidth,
            float rightPanelWidth,
            float toolbarHeight)
        {
            float safeToolbarHeight = Mathf.Max(0f, toolbarHeight);
            float safeWindowWidth = Mathf.Max(0f, windowRect.width);
            float safeWindowHeight = Mathf.Max(0f, windowRect.height);

            Rect toolbarRect = new Rect(windowRect.xMin, windowRect.yMin, safeWindowWidth, safeToolbarHeight);
            Rect bodyRect = new Rect(
                windowRect.xMin,
                toolbarRect.yMax,
                safeWindowWidth,
                Mathf.Max(0f, safeWindowHeight - safeToolbarHeight));

            float clampedLeftWidth = Mathf.Min(Mathf.Max(0f, leftPanelWidth), bodyRect.width);
            float remainingWidthAfterLeft = Mathf.Max(0f, bodyRect.width - clampedLeftWidth);
            float clampedRightWidth = Mathf.Min(Mathf.Max(0f, rightPanelWidth), remainingWidthAfterLeft);

            Rect leftPanelRect = new Rect(bodyRect.xMin, bodyRect.yMin, clampedLeftWidth, bodyRect.height);
            Rect rightPanelRect = new Rect(bodyRect.xMax - clampedRightWidth, bodyRect.yMin, clampedRightWidth, bodyRect.height);
            Rect canvasHostRect = new Rect(
                leftPanelRect.xMax,
                bodyRect.yMin,
                Mathf.Max(0f, rightPanelRect.xMin - leftPanelRect.xMax),
                bodyRect.height);

            return new WorldMapEditorLayout(
                windowRect,
                toolbarRect,
                bodyRect,
                leftPanelRect,
                rightPanelRect,
                canvasHostRect);
        }
    }
}
