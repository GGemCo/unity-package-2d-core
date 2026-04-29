using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어별 월드맵 진행 상태를 저장하는 세이브 데이터입니다.
    /// </summary>
    [Serializable]
    public sealed class WorldMapProgressData
    {
        /// <summary>현재 플레이어 위치 노드 ID입니다.</summary>
        public string currentNodeId;

        /// <summary>방문한 노드 ID 목록입니다.</summary>
        public List<string> visitedNodeIds = new List<string>();

        /// <summary>해금된 노드 ID 목록입니다.</summary>
        public List<string> unlockedNodeIds = new List<string>();

        /// <summary>개방된 연결선 ID 목록입니다.</summary>
        public List<string> openedEdgeIds = new List<string>();

        /// <summary>
        /// 현재 노드를 변경하고 방문/해금 목록에 함께 반영합니다.
        /// </summary>
        /// <param name="nodeId">현재 위치로 설정할 노드 ID입니다.</param>
        public void SetCurrentNode(string nodeId)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                return;
            }

            currentNodeId = nodeId;
            AddUnique(ref visitedNodeIds, nodeId);
            AddUnique(ref unlockedNodeIds, nodeId);
        }

        /// <summary>
        /// 지정한 노드를 해금 목록에 추가합니다.
        /// </summary>
        /// <param name="nodeId">해금할 노드 ID입니다.</param>
        public void UnlockNode(string nodeId)
        {
            AddUnique(ref unlockedNodeIds, nodeId);
        }

        /// <summary>
        /// 지정한 연결선을 개방 목록에 추가합니다.
        /// </summary>
        /// <param name="edgeId">개방할 연결선 ID입니다.</param>
        public void OpenEdge(string edgeId)
        {
            AddUnique(ref openedEdgeIds, edgeId);
        }

        /// <summary>
        /// 지정한 노드를 방문했는지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 노드 ID입니다.</param>
        /// <returns>방문한 노드이면 true입니다.</returns>
        public bool IsVisitedNode(string nodeId)
        {
            return Contains(visitedNodeIds, nodeId);
        }

        /// <summary>
        /// 지정한 노드가 해금되었는지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 노드 ID입니다.</param>
        /// <returns>해금된 노드이면 true입니다.</returns>
        public bool IsUnlockedNode(string nodeId)
        {
            return Contains(unlockedNodeIds, nodeId);
        }

        /// <summary>
        /// 지정한 연결선이 개방되었는지 확인합니다.
        /// </summary>
        /// <param name="edgeId">확인할 연결선 ID입니다.</param>
        /// <returns>개방된 연결선이면 true입니다.</returns>
        public bool IsOpenedEdge(string edgeId)
        {
            return Contains(openedEdgeIds, edgeId);
        }

        /// <summary>
        /// 목록에 같은 값이 없을 때만 값을 추가합니다.
        /// </summary>
        /// <param name="list">대상 문자열 목록입니다.</param>
        /// <param name="value">추가할 값입니다.</param>
        private static void AddUnique(ref List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (list == null)
            {
                list = new List<string>();
            }

            if (list.Contains(value))
            {
                return;
            }

            list.Add(value);
        }

        /// <summary>
        /// 문자열 목록에 값이 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="list">대상 문자열 목록입니다.</param>
        /// <param name="value">확인할 값입니다.</param>
        /// <returns>값이 포함되어 있으면 true입니다.</returns>
        private static bool Contains(List<string> list, string value)
        {
            return list != null && !string.IsNullOrWhiteSpace(value) && list.Contains(value);
        }
    }
}
