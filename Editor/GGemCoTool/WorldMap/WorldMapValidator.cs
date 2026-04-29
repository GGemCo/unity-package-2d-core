using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프 원본 데이터가 export 가능한지 검증합니다.
    /// </summary>
    internal static class WorldMapValidator
    {
        /// <summary>
        /// 월드맵 그래프 에셋 전체를 검증합니다.
        /// </summary>
        /// <param name="asset">검증할 월드맵 그래프 에셋입니다.</param>
        /// <param name="tableMap">TableMap 참조 검증에 사용할 맵 테이블입니다.</param>
        /// <returns>검증 결과 리포트입니다.</returns>
        public static WorldMapValidationReport Validate(WorldMapGraphAsset asset, TableMap tableMap)
        {
            WorldMapValidationReport report = new WorldMapValidationReport();

            if (asset == null)
            {
                report.Add(WorldMapValidationSeverity.Error, "월드맵 그래프 에셋이 선택되지 않았습니다.");
                return report;
            }

            asset.EnsureDefaults();
            ValidateGraphFields(asset, report);
            ValidateNodes(asset, tableMap, report);
            ValidateEdges(asset, report);
            ValidateOrphanNodes(asset, report);

            if (report.Messages.Count == 0)
            {
                report.Add(WorldMapValidationSeverity.Info, "검증을 통과했습니다.");
            }

            return report;
        }

        /// <summary>
        /// 그래프 공통 필드를 검증합니다.
        /// </summary>
        /// <param name="asset">검증할 월드맵 그래프 에셋입니다.</param>
        /// <param name="report">검증 메시지를 누적할 리포트입니다.</param>
        private static void ValidateGraphFields(WorldMapGraphAsset asset, WorldMapValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(asset.graphId))
            {
                report.Add(WorldMapValidationSeverity.Error, "graphId가 비어 있습니다.");
            }

            if (asset.referenceResolution.x <= 0f || asset.referenceResolution.y <= 0f)
            {
                report.Add(WorldMapValidationSeverity.Error, "기준 해상도는 0보다 커야 합니다.");
            }

            if (asset.backgroundSprite == null && string.IsNullOrWhiteSpace(asset.backgroundAddress))
            {
                report.Add(WorldMapValidationSeverity.Warning, "배경 Sprite 또는 배경 address가 설정되지 않았습니다.");
            }

            if (asset.nodes.Count > 0 && string.IsNullOrWhiteSpace(asset.startNodeId))
            {
                report.Add(WorldMapValidationSeverity.Error, "시작 노드가 설정되지 않았습니다.");
            }
            else if (!string.IsNullOrWhiteSpace(asset.startNodeId) && asset.FindNode(asset.startNodeId) == null)
            {
                report.Add(WorldMapValidationSeverity.Error, "시작 노드 ID가 존재하지 않는 노드를 가리킵니다.", asset.startNodeId);
            }
        }

        /// <summary>
        /// 노드 목록의 ID, TableMap 참조, 좌표, 중복 상태를 검증합니다.
        /// </summary>
        /// <param name="asset">검증할 월드맵 그래프 에셋입니다.</param>
        /// <param name="tableMap">TableMap 참조 검증에 사용할 맵 테이블입니다.</param>
        /// <param name="report">검증 메시지를 누적할 리포트입니다.</param>
        private static void ValidateNodes(WorldMapGraphAsset asset, TableMap tableMap, WorldMapValidationReport report)
        {
            HashSet<string> nodeIds = new HashSet<string>();
            Dictionary<int, int> mapUidCounts = new Dictionary<int, int>();

            for (int i = 0; i < asset.nodes.Count; i++)
            {
                WorldMapNodeData node = asset.nodes[i];
                if (node == null)
                {
                    report.Add(WorldMapValidationSeverity.Error, "노드 목록에 null 항목이 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(node.nodeId))
                {
                    report.Add(WorldMapValidationSeverity.Error, "nodeId가 비어 있는 노드가 있습니다.");
                }
                else if (!nodeIds.Add(node.nodeId))
                {
                    report.Add(WorldMapValidationSeverity.Error, "nodeId가 중복되었습니다: " + node.nodeId, node.nodeId);
                }

                if (node.mapUid <= 0)
                {
                    report.Add(WorldMapValidationSeverity.Error, "mapUid가 유효하지 않습니다.", node.nodeId);
                }
                else
                {
                    if (!mapUidCounts.ContainsKey(node.mapUid))
                    {
                        mapUidCounts[node.mapUid] = 0;
                    }

                    mapUidCounts[node.mapUid]++;
                    if (tableMap != null && tableMap.GetDataByUid(node.mapUid) == null)
                    {
                        report.Add(WorldMapValidationSeverity.Error, "TableMap에 없는 mapUid입니다: " + node.mapUid, node.nodeId);
                    }
                }

                Vector2 position = node.normalizedPosition;
                if (position.x < 0f || position.x > 1f || position.y < 0f || position.y > 1f)
                {
                    report.Add(WorldMapValidationSeverity.Error, "노드 좌표가 0~1 범위를 벗어났습니다.", node.nodeId);
                }

                if (node.visibleByDefault && string.IsNullOrWhiteSpace(node.iconAddress))
                {
                    report.Add(WorldMapValidationSeverity.Warning, "기본 표시 노드에 iconAddress가 없습니다.", node.nodeId);
                }
            }

            foreach (KeyValuePair<int, int> pair in mapUidCounts)
            {
                if (pair.Value > 1)
                {
                    report.Add(WorldMapValidationSeverity.Warning, "동일한 mapUid를 참조하는 노드가 여러 개 있습니다: " + pair.Key);
                }
            }
        }

        /// <summary>
        /// 연결선 목록의 참조 무결성과 중복 상태를 검증합니다.
        /// </summary>
        /// <param name="asset">검증할 월드맵 그래프 에셋입니다.</param>
        /// <param name="report">검증 메시지를 누적할 리포트입니다.</param>
        private static void ValidateEdges(WorldMapGraphAsset asset, WorldMapValidationReport report)
        {
            HashSet<string> edgeIds = new HashSet<string>();
            HashSet<string> directedPairs = new HashSet<string>();
            HashSet<string> bidirectionalPairs = new HashSet<string>();

            for (int i = 0; i < asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = asset.edges[i];
                if (edge == null)
                {
                    report.Add(WorldMapValidationSeverity.Error, "연결선 목록에 null 항목이 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(edge.edgeId))
                {
                    report.Add(WorldMapValidationSeverity.Error, "edgeId가 비어 있는 연결선이 있습니다.");
                }
                else if (!edgeIds.Add(edge.edgeId))
                {
                    report.Add(WorldMapValidationSeverity.Error, "edgeId가 중복되었습니다: " + edge.edgeId, edge.edgeId);
                }

                if (string.IsNullOrWhiteSpace(edge.fromNodeId) || asset.FindNode(edge.fromNodeId) == null)
                {
                    report.Add(WorldMapValidationSeverity.Error, "존재하지 않는 fromNodeId를 참조합니다.", edge.edgeId);
                }

                if (string.IsNullOrWhiteSpace(edge.toNodeId) || asset.FindNode(edge.toNodeId) == null)
                {
                    report.Add(WorldMapValidationSeverity.Error, "존재하지 않는 toNodeId를 참조합니다.", edge.edgeId);
                }

                if (!string.IsNullOrWhiteSpace(edge.fromNodeId) && edge.fromNodeId == edge.toNodeId)
                {
                    report.Add(WorldMapValidationSeverity.Error, "자기 자신으로 연결된 edge가 있습니다.", edge.edgeId);
                }

                string directedKey = edge.fromNodeId + "->" + edge.toNodeId;
                if (!directedPairs.Add(directedKey))
                {
                    report.Add(WorldMapValidationSeverity.Error, "동일 방향 연결선이 중복되었습니다.", edge.edgeId);
                }

                if (edge.bidirectional)
                {
                    string bidirectionalKey = CreateUnorderedPairKey(edge.fromNodeId, edge.toNodeId);
                    if (!bidirectionalPairs.Add(bidirectionalKey))
                    {
                        report.Add(WorldMapValidationSeverity.Error, "양방향 연결선이 중복되었습니다.", edge.edgeId);
                    }
                }
            }
        }

        /// <summary>
        /// 연결선이 하나도 없는 고아 노드를 찾아 경고합니다.
        /// </summary>
        /// <param name="asset">검증할 월드맵 그래프 에셋입니다.</param>
        /// <param name="report">검증 메시지를 누적할 리포트입니다.</param>
        private static void ValidateOrphanNodes(WorldMapGraphAsset asset, WorldMapValidationReport report)
        {
            if (asset.nodes.Count <= 1)
            {
                return;
            }

            HashSet<string> connectedNodeIds = new HashSet<string>();
            for (int i = 0; i < asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                connectedNodeIds.Add(edge.fromNodeId);
                connectedNodeIds.Add(edge.toNodeId);
            }

            for (int i = 0; i < asset.nodes.Count; i++)
            {
                WorldMapNodeData node = asset.nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                {
                    continue;
                }

                if (!connectedNodeIds.Contains(node.nodeId))
                {
                    report.Add(WorldMapValidationSeverity.Warning, "연결선이 없는 고아 노드입니다.", node.nodeId);
                }
            }
        }

        /// <summary>
        /// 노드 쌍을 방향과 무관한 중복 검사 키로 변환합니다.
        /// </summary>
        /// <param name="a">첫 번째 노드 ID입니다.</param>
        /// <param name="b">두 번째 노드 ID입니다.</param>
        /// <returns>방향과 무관한 노드 쌍 키입니다.</returns>
        private static string CreateUnorderedPairKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "<->" + b : b + "<->" + a;
        }
    }
}
