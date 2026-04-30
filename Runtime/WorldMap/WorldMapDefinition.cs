using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 런타임에서 사용하는 월드맵 정의 데이터입니다.
    /// </summary>
    public sealed class WorldMapDefinition
    {
        private readonly List<WorldMapNodeDefinition> _nodes;
        private readonly List<WorldMapEdgeDefinition> _edges;
        private readonly Dictionary<string, WorldMapNodeDefinition> _nodeById;

        /// <summary>월드맵 그래프 ID입니다.</summary>
        public string GraphId { get; private set; }

        /// <summary>시작 노드 ID입니다.</summary>
        public string StartNodeId { get; private set; }

        /// <summary>배경 이미지 Addressables 키 또는 경로입니다.</summary>
        public string BackgroundAddress { get; private set; }

        /// <summary>편집 기준 해상도입니다.</summary>
        public Vector2 ReferenceResolution { get; private set; }

        /// <summary>노드 목록입니다.</summary>
        public IReadOnlyList<WorldMapNodeDefinition> Nodes => _nodes;

        /// <summary>연결선 목록입니다.</summary>
        public IReadOnlyList<WorldMapEdgeDefinition> Edges => _edges;

        /// <summary>
        /// 런타임 정의 객체를 초기화합니다.
        /// </summary>
        private WorldMapDefinition()
        {
            _nodes = new List<WorldMapNodeDefinition>();
            _edges = new List<WorldMapEdgeDefinition>();
            _nodeById = new Dictionary<string, WorldMapNodeDefinition>();
            ReferenceResolution = new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// JSON DTO를 런타임 정의 객체로 변환합니다.
        /// </summary>
        /// <param name="json">변환할 JSON DTO입니다.</param>
        /// <returns>런타임 월드맵 정의입니다.</returns>
        public static WorldMapDefinition FromJson(WorldMapGraphJson json)
        {
            WorldMapDefinition definition = new WorldMapDefinition();

            if (json == null)
            {
                return definition;
            }

            definition.GraphId = json.graphId;
            definition.StartNodeId = json.startNodeId;

            if (json.background != null)
            {
                definition.BackgroundAddress = json.background.address;
                if (json.background.referenceResolution != null)
                {
                    definition.ReferenceResolution = json.background.referenceResolution.ToVector2();
                }
            }

            if (json.nodes != null)
            {
                for (int i = 0; i < json.nodes.Count; i++)
                {
                    WorldMapNodeJson nodeJson = json.nodes[i];
                    if (nodeJson == null || string.IsNullOrWhiteSpace(nodeJson.nodeId))
                    {
                        continue;
                    }

                    WorldMapNodeDefinition node = WorldMapNodeDefinition.FromJson(nodeJson);
                    definition._nodes.Add(node);
                    definition._nodeById[node.NodeId] = node;
                }
            }

            if (json.edges != null)
            {
                for (int i = 0; i < json.edges.Count; i++)
                {
                    WorldMapEdgeJson edgeJson = json.edges[i];
                    if (edgeJson == null || string.IsNullOrWhiteSpace(edgeJson.edgeId))
                    {
                        continue;
                    }

                    definition._edges.Add(WorldMapEdgeDefinition.FromJson(edgeJson));
                }
            }

            return definition;
        }

        /// <summary>
        /// 노드 ID로 런타임 노드 정의를 찾습니다.
        /// </summary>
        /// <param name="nodeId">조회할 노드 ID입니다.</param>
        /// <param name="node">조회된 노드 정의입니다.</param>
        /// <returns>노드를 찾으면 true, 없으면 false입니다.</returns>
        public bool TryGetNode(string nodeId, out WorldMapNodeDefinition node)
        {
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                node = null;
                return false;
            }

            return _nodeById.TryGetValue(nodeId, out node);
        }
    }

    /// <summary>
    /// 런타임에서 사용하는 월드맵 노드 정의입니다.
    /// </summary>
    public sealed class WorldMapNodeDefinition
    {
        /// <summary>그래프 내부 노드 ID입니다.</summary>
        public string NodeId { get; private set; }

        /// <summary>TableMap UID입니다.</summary>
        public int MapUid { get; private set; }

        /// <summary>0~1 범위의 정규화 좌표입니다.</summary>
        public Vector2 NormalizedPosition { get; private set; }

        /// <summary>표시 제목 override입니다.</summary>
        public string TitleOverride { get; private set; }

        /// <summary>아이콘 Addressables 키 또는 경로입니다.</summary>
        public string IconAddress { get; private set; }

        /// <summary>노드 타입입니다.</summary>
        public WorldMapNodeType NodeType { get; private set; }

        /// <summary>기본 표시 여부입니다.</summary>
        public bool VisibleByDefault { get; private set; }

        /// <summary>처음부터 월드맵에 보이지만 비활성 상태로 표시할지 여부입니다.</summary>
        public bool InactiveByDefault { get; private set; }

        /// <summary>해금 조건 키입니다.</summary>
        public string UnlockConditionKey { get; private set; }

        /// <summary>
        /// JSON DTO를 런타임 노드 정의로 변환합니다.
        /// </summary>
        /// <param name="json">변환할 노드 DTO입니다.</param>
        /// <returns>런타임 노드 정의입니다.</returns>
        public static WorldMapNodeDefinition FromJson(WorldMapNodeJson json)
        {
            WorldMapNodeType nodeType = WorldMapNodeType.Normal;
            if (!string.IsNullOrWhiteSpace(json.nodeType))
            {
                Enum.TryParse(json.nodeType, true, out nodeType);
            }

            return new WorldMapNodeDefinition
            {
                NodeId = json.nodeId,
                MapUid = json.mapUid,
                NormalizedPosition = json.position != null ? json.position.ToVector2() : Vector2.zero,
                TitleOverride = json.titleOverride,
                IconAddress = json.iconAddress,
                NodeType = nodeType,
                VisibleByDefault = json.visibleByDefault,
                InactiveByDefault = json.inactiveByDefault,
                UnlockConditionKey = json.unlockConditionKey,
            };
        }
    }

    /// <summary>
    /// 런타임에서 사용하는 월드맵 연결선 정의입니다.
    /// </summary>
    public sealed class WorldMapEdgeDefinition
    {
        /// <summary>그래프 내부 연결선 ID입니다.</summary>
        public string EdgeId { get; private set; }

        /// <summary>출발 노드 ID입니다.</summary>
        public string FromNodeId { get; private set; }

        /// <summary>도착 노드 ID입니다.</summary>
        public string ToNodeId { get; private set; }

        /// <summary>양방향 연결 여부입니다.</summary>
        public bool Bidirectional { get; private set; }

        /// <summary>연결선 타입입니다.</summary>
        public WorldMapEdgeType EdgeType { get; private set; }

        /// <summary>연결선 스프라이트 Addressables 키 또는 에셋 경로입니다.</summary>
        public string EdgeSpriteAddress { get; private set; }

        /// <summary>해금 조건 키입니다.</summary>
        public string UnlockConditionKey { get; private set; }

        /// <summary>
        /// JSON DTO를 런타임 연결선 정의로 변환합니다.
        /// </summary>
        /// <param name="json">변환할 연결선 DTO입니다.</param>
        /// <returns>런타임 연결선 정의입니다.</returns>
        public static WorldMapEdgeDefinition FromJson(WorldMapEdgeJson json)
        {
            WorldMapEdgeType edgeType = WorldMapEdgeType.Normal;
            if (!string.IsNullOrWhiteSpace(json.edgeType))
            {
                Enum.TryParse(json.edgeType, true, out edgeType);
            }

            return new WorldMapEdgeDefinition
            {
                EdgeId = json.edgeId,
                FromNodeId = json.fromNodeId,
                ToNodeId = json.toNodeId,
                Bidirectional = json.bidirectional,
                EdgeType = edgeType,
                EdgeSpriteAddress = json.edgeSpriteAddress,
                UnlockConditionKey = json.unlockConditionKey,
            };
        }
    }
}
