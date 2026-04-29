using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 그래프 에디터가 수정하는 원본 ScriptableObject입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldMapGraph", menuName = ConfigDefine.NameSDK + "/WorldMap/WorldMapGraphAsset", order = 100)]
    public sealed class WorldMapGraphAsset : ScriptableObject
    {
        /// <summary>JSON과 Addressables 키 생성에 사용할 그래프 ID입니다.</summary>
        public string graphId = ConfigAddressableWorldMap.DefaultGraphId;

        /// <summary>에디터에서 미리 볼 배경 Sprite입니다.</summary>
        public Sprite backgroundSprite;

        /// <summary>JSON에 저장할 배경 Addressables 키 또는 경로입니다.</summary>
        public string backgroundAddress;

        /// <summary>편집 기준 해상도입니다.</summary>
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        /// <summary>런타임에서 첫 위치로 사용할 시작 노드 ID입니다.</summary>
        public string startNodeId;

        /// <summary>월드맵 노드 원본 목록입니다.</summary>
        public List<WorldMapNodeData> nodes = new List<WorldMapNodeData>();

        /// <summary>월드맵 연결선 원본 목록입니다.</summary>
        public List<WorldMapEdgeData> edges = new List<WorldMapEdgeData>();

        /// <summary>
        /// 에셋 필드가 null 또는 잘못된 기본값일 때 안전한 기본값으로 보정합니다.
        /// </summary>
        public void EnsureDefaults()
        {
            if (string.IsNullOrWhiteSpace(graphId))
            {
                graphId = ConfigAddressableWorldMap.DefaultGraphId;
            }

            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
            {
                referenceResolution = new Vector2(1920f, 1080f);
            }

            if (nodes == null)
            {
                nodes = new List<WorldMapNodeData>();
            }

            if (edges == null)
            {
                edges = new List<WorldMapEdgeData>();
            }
        }

        /// <summary>
        /// 지정한 ID의 노드를 찾습니다.
        /// </summary>
        /// <param name="nodeId">조회할 노드 ID입니다.</param>
        /// <returns>노드를 찾으면 해당 데이터, 없으면 null입니다.</returns>
        public WorldMapNodeData FindNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || nodes == null)
            {
                return null;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                WorldMapNodeData node = nodes[i];
                if (node != null && node.nodeId == nodeId)
                {
                    return node;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정한 ID의 연결선을 찾습니다.
        /// </summary>
        /// <param name="edgeId">조회할 연결선 ID입니다.</param>
        /// <returns>연결선을 찾으면 해당 데이터, 없으면 null입니다.</returns>
        public WorldMapEdgeData FindEdge(string edgeId)
        {
            if (string.IsNullOrEmpty(edgeId) || edges == null)
            {
                return null;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                WorldMapEdgeData edge = edges[i];
                if (edge != null && edge.edgeId == edgeId)
                {
                    return edge;
                }
            }

            return null;
        }

        /// <summary>
        /// 맵 UID 기반으로 그래프 내에서 중복되지 않는 노드 ID를 만듭니다.
        /// </summary>
        /// <param name="mapUid">노드가 참조할 TableMap UID입니다.</param>
        /// <returns>중복되지 않는 노드 ID입니다.</returns>
        public string CreateUniqueNodeId(int mapUid)
        {
            string baseId = mapUid > 0 ? "map_" + mapUid : "node";
            string nodeId = baseId;
            int suffix = 1;

            while (FindNode(nodeId) != null)
            {
                suffix++;
                nodeId = baseId + "_" + suffix;
            }

            return nodeId;
        }

        /// <summary>
        /// 출발/도착 노드 ID 기반으로 그래프 내에서 중복되지 않는 연결선 ID를 만듭니다.
        /// </summary>
        /// <param name="fromNodeId">출발 노드 ID입니다.</param>
        /// <param name="toNodeId">도착 노드 ID입니다.</param>
        /// <returns>중복되지 않는 연결선 ID입니다.</returns>
        public string CreateUniqueEdgeId(string fromNodeId, string toNodeId)
        {
            string safeFrom = string.IsNullOrWhiteSpace(fromNodeId) ? "from" : fromNodeId;
            string safeTo = string.IsNullOrWhiteSpace(toNodeId) ? "to" : toNodeId;
            string baseId = safeFrom + "_to_" + safeTo;
            string edgeId = baseId;
            int suffix = 1;

            while (FindEdge(edgeId) != null)
            {
                suffix++;
                edgeId = baseId + "_" + suffix;
            }

            return edgeId;
        }
    }
}
