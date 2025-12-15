using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// GridLayoutGroup의 설정 값만으로 특정 index 셀의 위치를 계산하는 유틸리티 클래스입니다.
    ///
    /// [특징]
    /// - 실제 자식 GameObject / RectTransform에 접근하지 않습니다.
    /// - CellSize / Spacing / Padding / Constraint / StartAxis / StartCorner / ChildAlignment 를
    ///   Unity GridLayoutGroup 내부 규칙과 동일한 방식으로 수식 계산합니다.
    /// - 최종 결과는 "실제 자식 RectTransform.transform.position" 과 동일한 값을 반환합니다.
    ///
    /// [주요 사용처]
    /// - 카드/아이콘 이동 연출
    /// - 드래그 미리보기
    /// - 셔플/프리뷰 툴
    /// - 오브젝트 생성 전 위치 예측
    /// </summary>
    public static class GridLayoutPositionUtility
    {
        /// <summary>
        /// GridLayoutGroup에서 특정 index에 해당하는 셀의 위치를 계산하여
        /// 해당 셀 중심점의 transform.position 값을 반환합니다.
        ///
        /// 이 메서드는 다음과 같은 흐름으로 동작합니다.
        /// 1) Grid 설정값을 기반으로 전체 그리드의 행/열 구조를 계산
        /// 2) index → (cellX, cellY) 변환
        /// 3) StartCorner / StartAxis / ChildAlignment 반영
        /// 4) 부모 RectTransform pivot 기준 local 좌표 계산
        /// 5) TransformPoint 를 통해 월드 좌표(transform.position) 변환
        ///
        /// 주의
        /// - childCount는 실제 grid.transform.childCount 와 동일해야
        ///   실제 배치 결과와 정확히 일치합니다.
        /// - 미리보기 용도라면 "가상 childCount"를 전달해도 됩니다.
        /// </summary>
        /// <param name="grid">
        /// 위치를 계산할 대상 GridLayoutGroup.
        /// CellSize, Spacing, Padding, Constraint, Alignment 등의 기준이 됩니다.
        /// </param>
        /// <param name="index">
        /// 계산할 셀의 인덱스.
        /// GridLayoutGroup.transform.GetChild(index) 와 동일한 순서를 기준으로 합니다.
        /// (0부터 시작)
        /// </param>
        /// <param name="childCount">
        /// 그리드에 배치되는 전체 셀 개수.
        /// 실제 자식 개수 또는 미리보기용 가상 개수를 전달할 수 있습니다.
        /// </param>
        /// <param name="position">
        /// 계산된 결과 transform.position (월드 좌표).
        /// 실제 자식 RectTransform.transform.position 과 동일한 좌표계입니다.
        /// </param>
        /// <returns>
        /// 계산 성공 시 true, 입력 값 오류 또는 계산 불가 시 false.
        /// </returns>
        public static bool TryGetCellTransformPosition(
            GridLayoutGroup grid,
            int index,
            int childCount,
            out Vector3 position)
        {
            position = default;

            // -------------------------
            // 0) 입력 값 검증
            // -------------------------
            if (grid == null) return false;
            if (childCount <= 0) return false;
            if (index < 0 || index >= childCount) return false;

            // GridLayoutGroup이 붙어있는 컨테이너 RectTransform
            // 모든 좌표 계산의 기준 좌표계(pivot 기준 local space)가 됩니다.
            RectTransform parent = grid.GetComponent<RectTransform>();
            if (parent == null) return false;

            // -------------------------
            // 1) 전체 그리드의 열/행 개수 계산
            // -------------------------
            GetGridSize(grid, childCount, parent.rect.size, out int cols, out int rows);
            if (cols <= 0 || rows <= 0) return false;

            // -------------------------
            // 2) index → (cellX, cellY)
            // -------------------------
            GetCellXY(grid, index, cols, rows, out int cellX, out int cellY);

            // -------------------------
            // 3) StartCorner 반영
            // -------------------------
            ApplyStartCorner(grid, cols, rows, ref cellX, ref cellY);

            // -------------------------
            // 4) 셀들이 실제로 차지하는 전체 영역(required space)
            // -------------------------
            Vector2 cellSize = grid.cellSize;
            Vector2 spacing = grid.spacing;
            RectOffset padding = grid.padding;

            float requiredW = cols * cellSize.x + (cols - 1) * spacing.x;
            float requiredH = rows * cellSize.y + (rows - 1) * spacing.y;

            float parentW = parent.rect.width;
            float parentH = parent.rect.height;

            // -------------------------
            // 5) 컨테이너 내부에서 남는 공간(free space)
            // -------------------------
            float freeW = parentW - padding.horizontal - requiredW;
            float freeH = parentH - padding.vertical - requiredH;

            // -------------------------
            // 6) ChildAlignment → 정렬 계수 변환
            // -------------------------
            GetAlignmentFactor(grid.childAlignment, out float ax, out float ay);

            // -------------------------
            // 7) 부모 pivot 기준 local 좌표계에서의 모서리 위치
            // -------------------------
            float leftEdge = -parent.pivot.x * parentW;
            float topEdge = (1f - parent.pivot.y) * parentH;

            // -------------------------
            // 8) 그리드 시작 위치(startX, startY)
            // -------------------------
            float startX = leftEdge + padding.left + freeW * ax;
            float startY = topEdge - padding.top - freeH * (1f - ay);

            // -------------------------
            // 9) 셀 중심 local 좌표 계산
            // -------------------------
            float localX = startX + cellX * (cellSize.x + spacing.x) + cellSize.x * 0.5f;
            float localY = startY - cellY * (cellSize.y + spacing.y) - cellSize.y * 0.5f;

            // -------------------------
            // 10) local → transform.position 변환
            // -------------------------
            position = parent.TransformPoint(new Vector3(localX, localY, 0f));
            return true;
        }

        /// <summary>
        /// GridLayoutGroup의 Constraint 설정과 childCount를 기준으로
        /// 전체 그리드의 열(cols)과 행(rows) 개수를 계산합니다.
        /// </summary>
        /// <param name="grid">
        /// GridLayoutGroup 설정 값 제공용.
        /// Constraint, ConstraintCount, StartAxis, Padding, CellSize, Spacing 값을 참조합니다.
        /// </param>
        /// <param name="childCount">
        /// 그리드에 배치될 전체 셀 개수.
        /// </param>
        /// <param name="rectSize">
        /// GridLayoutGroup이 붙어있는 RectTransform의 크기.
        /// Flexible 모드에서 가용 영역 계산에 사용됩니다.
        /// </param>
        /// <param name="cols">
        /// 계산된 열(column) 개수 (출력).
        /// </param>
        /// <param name="rows">
        /// 계산된 행(row) 개수 (출력).
        /// </param>
        private static void GetGridSize(
            GridLayoutGroup grid,
            int childCount,
            Vector2 rectSize,
            out int cols,
            out int rows)
        {
            cols = 1;
            rows = 1;

            switch (grid.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    cols = Mathf.Max(1, grid.constraintCount);
                    rows = Mathf.CeilToInt(childCount / (float)cols);
                    break;

                case GridLayoutGroup.Constraint.FixedRowCount:
                    rows = Mathf.Max(1, grid.constraintCount);
                    cols = Mathf.CeilToInt(childCount / (float)rows);
                    break;

                case GridLayoutGroup.Constraint.Flexible:
                default:
                {
                    float availW = rectSize.x - grid.padding.horizontal;
                    float availH = rectSize.y - grid.padding.vertical;

                    float stepX = grid.cellSize.x + grid.spacing.x;
                    float stepY = grid.cellSize.y + grid.spacing.y;

                    cols = stepX <= 0f ? 1 : Mathf.Max(1, Mathf.FloorToInt((availW + grid.spacing.x) / stepX));
                    rows = stepY <= 0f ? 1 : Mathf.Max(1, Mathf.FloorToInt((availH + grid.spacing.y) / stepY));

                    if (grid.startAxis == GridLayoutGroup.Axis.Horizontal)
                        rows = Mathf.Max(1, Mathf.CeilToInt(childCount / (float)cols));
                    else
                        cols = Mathf.Max(1, Mathf.CeilToInt(childCount / (float)rows));
                    break;
                }
            }
        }

        /// <summary>
        /// index 값을 그리드 상의 좌표(cellX, cellY)로 변환합니다.
        /// </summary>
        /// <param name="grid">
        /// StartAxis 설정 확인용 GridLayoutGroup.
        /// </param>
        /// <param name="index">
        /// 변환할 셀 인덱스 (0부터 시작).
        /// </param>
        /// <param name="cols">
        /// 전체 열 개수.
        /// </param>
        /// <param name="rows">
        /// 전체 행 개수.
        /// </param>
        /// <param name="x">
        /// 계산된 열 위치(cellX).
        /// </param>
        /// <param name="y">
        /// 계산된 행 위치(cellY).
        /// </param>
        private static void GetCellXY(
            GridLayoutGroup grid,
            int index,
            int cols,
            int rows,
            out int x,
            out int y)
        {
            if (grid.startAxis == GridLayoutGroup.Axis.Horizontal)
            {
                x = index % cols;
                y = index / cols;
            }
            else
            {
                x = index / rows;
                y = index % rows;
            }
        }

        /// <summary>
        /// GridLayoutGroup.StartCorner 설정에 따라
        /// (cellX, cellY) 좌표를 뒤집어 보정합니다.
        /// </summary>
        /// <param name="grid">
        /// StartCorner 설정 확인용 GridLayoutGroup.
        /// </param>
        /// <param name="cols">
        /// 전체 열 개수.
        /// </param>
        /// <param name="rows">
        /// 전체 행 개수.
        /// </param>
        /// <param name="x">
        /// 보정될 열 위치(cellX).
        /// </param>
        /// <param name="y">
        /// 보정될 행 위치(cellY).
        /// </param>
        private static void ApplyStartCorner(
            GridLayoutGroup grid,
            int cols,
            int rows,
            ref int x,
            ref int y)
        {
            bool flipX =
                grid.startCorner == GridLayoutGroup.Corner.UpperRight ||
                grid.startCorner == GridLayoutGroup.Corner.LowerRight;

            bool flipY =
                grid.startCorner == GridLayoutGroup.Corner.LowerLeft ||
                grid.startCorner == GridLayoutGroup.Corner.LowerRight;

            if (flipX) x = (cols - 1) - x;
            if (flipY) y = (rows - 1) - y;
        }

        /// <summary>
        /// ChildAlignment(TextAnchor)을 0~1 범위의 정렬 계수로 변환합니다.
        /// </summary>
        /// <param name="anchor">
        /// GridLayoutGroup.childAlignment 값.
        /// </param>
        /// <param name="x">
        /// 가로 정렬 계수 (0=Left, 0.5=Center, 1=Right).
        /// </param>
        /// <param name="y">
        /// 세로 정렬 계수 (0=Lower, 0.5=Middle, 1=Upper).
        /// </param>
        private static void GetAlignmentFactor(
            TextAnchor anchor,
            out float x,
            out float y)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:    x = 0f;   y = 1f;   break;
                case TextAnchor.UpperCenter:  x = 0.5f; y = 1f;   break;
                case TextAnchor.UpperRight:   x = 1f;   y = 1f;   break;

                case TextAnchor.MiddleLeft:   x = 0f;   y = 0.5f; break;
                case TextAnchor.MiddleCenter: x = 0.5f; y = 0.5f; break;
                case TextAnchor.MiddleRight:  x = 1f;   y = 0.5f; break;

                case TextAnchor.LowerLeft:    x = 0f;   y = 0f;   break;
                case TextAnchor.LowerCenter:  x = 0.5f; y = 0f;   break;
                case TextAnchor.LowerRight:   x = 1f;   y = 0f;   break;

                default:                      x = 0f;   y = 1f;   break;
            }
        }
    }
}
