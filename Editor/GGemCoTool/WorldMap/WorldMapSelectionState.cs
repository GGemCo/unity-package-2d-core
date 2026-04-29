using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프 에디터의 현재 선택과 캔버스 보기 상태를 보관합니다.
    /// </summary>
    internal sealed class WorldMapSelectionState
    {
        private const float MinZoom = 0.25f;
        private const float MaxZoom = 3f;

        /// <summary>현재 선택된 노드 ID입니다.</summary>
        public string SelectedNodeId { get; private set; }

        /// <summary>현재 선택된 연결선 ID입니다.</summary>
        public string SelectedEdgeId { get; private set; }

        /// <summary>연결 생성 모드의 시작 노드 ID입니다.</summary>
        public string LinkingFromNodeId { get; private set; }

        /// <summary>캔버스 팬 오프셋입니다.</summary>
        public Vector2 PanOffset { get; set; }

        /// <summary>캔버스 확대 배율입니다.</summary>
        public float Zoom { get; private set; } = 1f;

        /// <summary>연결 생성 모드인지 여부입니다.</summary>
        public bool IsLinking => !string.IsNullOrEmpty(LinkingFromNodeId);

        /// <summary>
        /// 지정한 노드를 선택하고 연결선 선택을 해제합니다.
        /// </summary>
        /// <param name="nodeId">선택할 노드 ID입니다.</param>
        public void SelectNode(string nodeId)
        {
            SelectedNodeId = nodeId;
            SelectedEdgeId = null;
        }

        /// <summary>
        /// 지정한 연결선을 선택하고 노드 선택을 해제합니다.
        /// </summary>
        /// <param name="edgeId">선택할 연결선 ID입니다.</param>
        public void SelectEdge(string edgeId)
        {
            SelectedNodeId = null;
            SelectedEdgeId = edgeId;
        }

        /// <summary>
        /// 현재 선택 상태를 모두 해제합니다.
        /// </summary>
        public void ClearSelection()
        {
            SelectedNodeId = null;
            SelectedEdgeId = null;
        }

        /// <summary>
        /// 지정한 노드에서 시작하는 연결 생성 모드로 진입합니다.
        /// </summary>
        /// <param name="nodeId">연결 시작 노드 ID입니다.</param>
        public void StartLinking(string nodeId)
        {
            LinkingFromNodeId = nodeId;
            SelectNode(nodeId);
        }

        /// <summary>
        /// 연결 생성 모드를 취소합니다.
        /// </summary>
        public void CancelLinking()
        {
            LinkingFromNodeId = null;
        }

        /// <summary>
        /// 캔버스 확대 배율을 안전 범위로 제한하여 설정합니다.
        /// </summary>
        /// <param name="zoom">설정할 확대 배율입니다.</param>
        public void SetZoom(float zoom)
        {
            Zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        /// <summary>
        /// 캔버스 보기 상태를 기본값으로 되돌립니다.
        /// </summary>
        public void ResetView()
        {
            PanOffset = Vector2.zero;
            Zoom = 1f;
        }
    }
}
