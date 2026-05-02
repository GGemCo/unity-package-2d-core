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

        /// <summary>
        /// TableMap UID를 기준으로 월드맵 노드 정의를 찾습니다.
        /// </summary>
        /// <param name="mapUid">찾을 TableMap UID입니다.</param>
        /// <param name="node">찾은 월드맵 노드 정의입니다.</param>
        /// <returns>노드를 찾으면 true, 찾지 못하면 false를 반환합니다.</returns>
        public bool TryGetNodeByMapUid(int mapUid, out WorldMapNodeDefinition node)
        {
            if (mapUid <= 0)
            {
                node = null;
                return false;
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                WorldMapNodeDefinition candidate = _nodes[i];
                if (candidate != null && candidate.MapUid == mapUid)
                {
                    node = candidate;
                    return true;
                }
            }

            node = null;
            return false;
        }

        /// <summary>
        /// 두 노드가 월드맵 edge로 바로 연결되어 있는지 확인합니다.
        /// </summary>
        /// <param name="fromNodeId">출발 노드 ID입니다.</param>
        /// <param name="toNodeId">도착 노드 ID입니다.</param>
        /// <returns>출발 노드에서 도착 노드로 바로 이동할 수 있는 edge가 있으면 true를 반환합니다.</returns>
        public bool IsAdjacentNode(string fromNodeId, string toNodeId)
        {
            if (string.IsNullOrWhiteSpace(fromNodeId) || string.IsNullOrWhiteSpace(toNodeId))
            {
                return false;
            }

            for (int i = 0; i < _edges.Count; i++)
            {
                WorldMapEdgeDefinition edge = _edges[i];
                if (edge == null)
                {
                    continue;
                }

                if (edge.FromNodeId == fromNodeId && edge.ToNodeId == toNodeId)
                {
                    return true;
                }

                if (edge.Bidirectional && edge.FromNodeId == toNodeId && edge.ToNodeId == fromNodeId)
                {
                    return true;
                }
            }

            return false;
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

        /// <summary>비활성 상태에서 아이콘을 대체할 Sprite Addressables 키입니다.</summary>
        public string InactiveSpriteAddress { get; private set; }

        /// <summary>노드 데코레이션을 대체할 Sprite Addressables 키입니다.</summary>
        public string DecorationSpriteAddress { get; private set; }

        /// <summary>노드 데코레이션 애니메이션에 사용할 AnimatorController Addressables 키입니다.</summary>
        public string DecorationAnimatorControllerAddress { get; private set; }

        /// <summary>데코레이션 AnimatorController에서 재생할 상태 이름입니다.</summary>
        public string DecorationAnimationName { get; private set; }

        /// <summary>데코레이션 애니메이션을 반복 재생할지 여부입니다.</summary>
        public bool DecorationLoop { get; private set; }

        /// <summary>월드맵 아이콘 중앙을 기준으로 적용할 데코레이션 위치 오프셋입니다.</summary>
        public Vector2 DecorationOffset { get; private set; }

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
                InactiveSpriteAddress = json.inactiveSpriteAddress,
                DecorationSpriteAddress = json.decorationSpriteAddress,
                DecorationAnimatorControllerAddress = json.decorationAnimatorControllerAddress,
                DecorationAnimationName = json.decorationAnimationName,
                DecorationLoop = json.decorationLoop,
                DecorationOffset = json.decorationOffset != null ? json.decorationOffset.ToVector2() : Vector2.zero,
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
    /// <summary>
    /// 월드맵 노드 데코레이션을 런타임 UI에 전달하기 위한 값 묶음입니다.
    /// </summary>
    public struct WorldMapNodeDecorationRuntimeData
    {
        /// <summary>비어 있는 데코레이션 override 값입니다.</summary>
        public static WorldMapNodeDecorationRuntimeData Empty => new WorldMapNodeDecorationRuntimeData(
            null,
            null,
            string.Empty,
            true,
            Vector2.zero);

        /// <summary>정적 데코레이션으로 표시할 Sprite입니다.</summary>
        public Sprite Sprite { get; private set; }

        /// <summary>애니메이션 데코레이션에 사용할 AnimatorController입니다.</summary>
        public RuntimeAnimatorController AnimatorController { get; private set; }

        /// <summary>AnimatorController에서 재생할 상태 이름입니다.</summary>
        public string AnimationName { get; private set; }

        /// <summary>애니메이션을 반복 재생할지 여부입니다.</summary>
        public bool Loop { get; private set; }

        /// <summary>월드맵 아이콘 중앙 기준 데코레이션 위치 오프셋입니다.</summary>
        public Vector2 Offset { get; private set; }

        /// <summary>
        /// 런타임 데코레이션 전달 값을 생성합니다.
        /// </summary>
        /// <param name="sprite">정적 데코레이션 Sprite입니다.</param>
        /// <param name="animatorController">애니메이션 데코레이션 AnimatorController입니다.</param>
        /// <param name="animationName">재생할 Animator 상태 이름입니다.</param>
        /// <param name="loop">애니메이션 반복 여부입니다.</param>
        /// <param name="offset">아이콘 중앙 기준 위치 오프셋입니다.</param>
        public WorldMapNodeDecorationRuntimeData(
            Sprite sprite,
            RuntimeAnimatorController animatorController,
            string animationName,
            bool loop,
            Vector2 offset)
        {
            Sprite = sprite;
            AnimatorController = animatorController;
            AnimationName = animationName;
            Loop = loop;
            Offset = offset;
        }
    }

    /// <summary>
    /// 런타임에 사용하는 월드맵 연결선 정의입니다.
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
