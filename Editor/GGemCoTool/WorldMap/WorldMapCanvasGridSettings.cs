using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 중앙 캔버스의 Grid/Snap 편집 옵션을 보관합니다.
    /// </summary>
    internal sealed class WorldMapCanvasGridSettings
    {
        private const int DefaultGridCellWidth = 160;
        private const int DefaultGridCellHeight = 135;
        private const int DefaultMajorLineInterval = 4;

        /// <summary>Grid 표시 여부입니다.</summary>
        public bool ShowGrid = true;

        /// <summary>노드 이동 시 Snap 적용 여부입니다.</summary>
        public bool SnapEnabled = true;

        /// <summary>기준 해상도 픽셀 단위의 Grid 셀 크기입니다.</summary>
        public Vector2Int GridCellSize = new Vector2Int(DefaultGridCellWidth, DefaultGridCellHeight);

        /// <summary>강조선을 몇 칸마다 그릴지 결정하는 간격입니다.</summary>
        public int MajorLineInterval = DefaultMajorLineInterval;

        /// <summary>
        /// 설정값이 유효 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        public void Sanitize()
        {
            GridCellSize.x = Mathf.Max(1, GridCellSize.x);
            GridCellSize.y = Mathf.Max(1, GridCellSize.y);
            MajorLineInterval = Mathf.Max(1, MajorLineInterval);
        }
    }

    /// <summary>
    /// 월드맵 캔버스의 Grid/Snap 계산을 담당하는 유틸리티입니다.
    /// </summary>
    internal static class WorldMapCanvasGridUtility
    {
        /// <summary>
        /// 정규화 좌표를 현재 Grid 설정에 맞춰 Snap 합니다.
        /// </summary>
        /// <param name="normalizedPosition">0~1 기준의 정규화 좌표입니다.</param>
        /// <param name="referenceResolution">Grid 기준이 되는 해상도입니다.</param>
        /// <param name="settings">현재 Grid/Snap 설정입니다.</param>
        /// <returns>Snap 적용 후의 정규화 좌표입니다.</returns>
        public static Vector2 ApplySnapNormalized(
            Vector2 normalizedPosition,
            Vector2 referenceResolution,
            WorldMapCanvasGridSettings settings)
        {
            if (settings == null || !settings.SnapEnabled)
            {
                return normalizedPosition;
            }

            Vector2 snapStep = GetNormalizedSnapStep(referenceResolution, settings);
            if (snapStep.x <= 0f || snapStep.y <= 0f)
            {
                return normalizedPosition;
            }

            return new Vector2(
                Mathf.Round(normalizedPosition.x / snapStep.x) * snapStep.x,
                Mathf.Round(normalizedPosition.y / snapStep.y) * snapStep.y);
        }

        /// <summary>
        /// 현재 Grid 셀 크기를 정규화 좌표 단위로 변환합니다.
        /// </summary>
        /// <param name="referenceResolution">Grid 기준 해상도입니다.</param>
        /// <param name="settings">현재 Grid/Snap 설정입니다.</param>
        /// <returns>정규화 좌표 기준 Snap 간격입니다.</returns>
        public static Vector2 GetNormalizedSnapStep(Vector2 referenceResolution, WorldMapCanvasGridSettings settings)
        {
            if (settings == null)
            {
                return Vector2.zero;
            }

            settings.Sanitize();

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                settings.GridCellSize.x / referenceResolution.x,
                settings.GridCellSize.y / referenceResolution.y);
        }
    }
}
