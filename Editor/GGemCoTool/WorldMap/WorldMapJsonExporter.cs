using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 그래프 원본 에셋을 런타임 JSON DTO로 export합니다.
    /// </summary>
    internal static class WorldMapJsonExporter
    {
        private const int CurrentVersion = 1;

        /// <summary>
        /// 월드맵 그래프 에셋을 지정한 경로의 JSON 파일로 저장합니다.
        /// </summary>
        /// <param name="asset">export할 월드맵 그래프 에셋입니다.</param>
        /// <param name="assetPath">프로젝트 상대 또는 절대 JSON 경로입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>저장에 성공하면 true입니다.</returns>
        public static bool TryExport(WorldMapGraphAsset asset, string assetPath, out string error)
        {
            error = null;

            if (asset == null)
            {
                error = "월드맵 그래프 에셋이 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "JSON 저장 경로가 비어 있습니다.";
                return false;
            }

            try
            {
                asset.EnsureDefaults();
                if (!WorldMapAddressableRegistrar.TryRegisterSprites(asset, out error))
                {
                    return false;
                }

                WorldMapGraphJson jsonData = CreateJson(asset);
                string json = JsonConvert.SerializeObject(jsonData, Formatting.Indented);

                string fullPath = ToFullPath(assetPath);
                string directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(fullPath, json);
                AssetDatabase.Refresh();
                if (!WorldMapAddressableRegistrar.TryRegisterJson(asset.graphId, assetPath, out error))
                {
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                error = "월드맵 JSON 저장에 실패했습니다. " + e.Message;
                return false;
            }
        }

        /// <summary>
        /// 월드맵 그래프 에셋을 JSON DTO로 변환합니다.
        /// </summary>
        /// <param name="asset">변환할 월드맵 그래프 에셋입니다.</param>
        /// <returns>JSON DTO입니다.</returns>
        public static WorldMapGraphJson CreateJson(WorldMapGraphAsset asset)
        {
            WorldMapGraphJson json = new WorldMapGraphJson
            {
                version = CurrentVersion,
                graphId = ConfigAddressableWorldMap.NormalizeGraphId(asset.graphId),
                startNodeId = asset.startNodeId,
                background = new WorldMapBackgroundJson
                {
                    address = ResolveBackgroundAddress(asset),
                    referenceResolution = new WorldMapVector2Json(asset.referenceResolution),
                },
                nodes = new List<WorldMapNodeJson>(),
                edges = new List<WorldMapEdgeJson>(),
            };

            for (int i = 0; i < asset.nodes.Count; i++)
            {
                WorldMapNodeData node = asset.nodes[i];
                if (node == null)
                {
                    continue;
                }

                json.nodes.Add(new WorldMapNodeJson
                {
                    nodeId = node.nodeId,
                    mapUid = node.mapUid,
                    position = new WorldMapVector2Json(node.normalizedPosition),
                    titleOverride = node.titleOverride,
                    iconAddress = node.iconAddress,
                    inactiveSpriteAddress = ResolveNodeInactiveSpriteAddress(asset, node),
                    decorationSpriteAddress = ResolveNodeDecorationSpriteAddress(asset, node),
                    decorationAnimatorControllerAddress = ResolveNodeDecorationAnimatorControllerAddress(asset, node),
                    decorationAnimationName = node.decorationAnimationName,
                    decorationLoop = node.decorationLoop,
                    decorationOffset = new WorldMapVector2Json(node.decorationOffset),
                    decorationSize = new WorldMapVector2Json(node.decorationSize),
                    decorationScale = new WorldMapVector2Json(node.decorationScale),
                    nodeType = node.nodeType.ToString(),
                    visibleByDefault = node.visibleByDefault,
                    inactiveByDefault = node.inactiveByDefault,
                    unlockConditionKey = node.unlockConditionKey,
                });
            }

            for (int i = 0; i < asset.edges.Count; i++)
            {
                WorldMapEdgeData edge = asset.edges[i];
                if (edge == null)
                {
                    continue;
                }

                json.edges.Add(new WorldMapEdgeJson
                {
                    edgeId = edge.edgeId,
                    fromNodeId = edge.fromNodeId,
                    toNodeId = edge.toNodeId,
                    bidirectional = edge.bidirectional,
                    edgeType = edge.edgeType.ToString(),
                    edgeSpriteAddress = ResolveEdgeSpriteAddress(asset, edge),
                    unlockConditionKey = edge.unlockConditionKey,
                });
            }

            return json;
        }

        /// <summary>
        /// 배경 Sprite 또는 수동 address를 기준으로 JSON에 기록할 address를 결정합니다.
        /// </summary>
        /// <param name="asset">배경 정보를 가진 그래프 에셋입니다.</param>
        /// <returns>JSON에 기록할 배경 address입니다.</returns>
        private static string ResolveBackgroundAddress(WorldMapGraphAsset asset)
        {
            if (asset.backgroundSprite != null)
            {
                return ConfigAddressableWorldMap.GetBackgroundKey(asset.graphId);
            }

            if (!string.IsNullOrWhiteSpace(asset.backgroundAddress))
            {
                return asset.backgroundAddress;
            }

            return string.Empty;
        }

        /// <summary>
        /// 노드의 비활성 Sprite 원본 또는 수동 address를 기준으로 JSON에 기록할 address를 결정합니다.
        /// </summary>
        /// <param name="asset">노드가 포함된 월드맵 그래프 에셋입니다.</param>
        /// <param name="node">address를 결정할 월드맵 노드 데이터입니다.</param>
        /// <returns>JSON에 기록할 비활성 Sprite address입니다.</returns>
        private static string ResolveNodeInactiveSpriteAddress(WorldMapGraphAsset asset, WorldMapNodeData node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node.inactiveSprite != null)
            {
                return !string.IsNullOrWhiteSpace(node.inactiveSpriteAddress)
                    ? node.inactiveSpriteAddress
                    : ConfigAddressableWorldMap.GetNodeInactiveSpriteKey(asset.graphId, node.nodeId);
            }

            return node.inactiveSpriteAddress;
        }

        /// <summary>
        /// 노드 데코레이션 Sprite 원본 또는 수동 address를 기준으로 JSON에 기록할 address를 결정합니다.
        /// </summary>
        /// <param name="asset">노드가 포함된 월드맵 그래프 에셋입니다.</param>
        /// <param name="node">address를 결정할 월드맵 노드 데이터입니다.</param>
        /// <returns>JSON에 기록할 데코레이션 Sprite address입니다.</returns>
        private static string ResolveNodeDecorationSpriteAddress(WorldMapGraphAsset asset, WorldMapNodeData node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node.decorationSprite != null)
            {
                return !string.IsNullOrWhiteSpace(node.decorationSpriteAddress)
                    ? node.decorationSpriteAddress
                    : ConfigAddressableWorldMap.GetNodeDecorationSpriteKey(asset.graphId, node.nodeId);
            }

            return node.decorationSpriteAddress;
        }

        /// <summary>
        /// 노드 데코레이션 AnimatorController 원본 또는 수동 address를 기준으로 JSON에 기록할 address를 결정합니다.
        /// </summary>
        /// <param name="asset">노드가 포함된 월드맵 그래프 에셋입니다.</param>
        /// <param name="node">address를 결정할 월드맵 노드 데이터입니다.</param>
        /// <returns>JSON에 기록할 데코레이션 AnimatorController address입니다.</returns>
        private static string ResolveNodeDecorationAnimatorControllerAddress(WorldMapGraphAsset asset, WorldMapNodeData node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            if (node.decorationAnimatorController != null)
            {
                return !string.IsNullOrWhiteSpace(node.decorationAnimatorControllerAddress)
                    ? node.decorationAnimatorControllerAddress
                    : ConfigAddressableWorldMap.GetNodeDecorationAnimatorKey(asset.graphId, node.nodeId);
            }

            return node.decorationAnimatorControllerAddress;
        }

        /// <summary>
        /// 연결선 Sprite 원본 또는 수동 address를 기반으로 JSON에 기록할 address를 결정합니다.
        /// </summary>
        /// <param name="asset">연결선 정보를 보유한 월드맵 그래프 에셋입니다.</param>
        /// <param name="edge">address를 결정할 연결선 데이터입니다.</param>
        /// <returns>JSON에 기록할 연결선 Sprite address입니다.</returns>
        private static string ResolveEdgeSpriteAddress(WorldMapGraphAsset asset, WorldMapEdgeData edge)
        {
            if (edge == null)
            {
                return string.Empty;
            }

            if (edge.edgeSprite != null)
            {
                return !string.IsNullOrWhiteSpace(edge.edgeSpriteAddress)
                    ? edge.edgeSpriteAddress
                    : ConfigAddressableWorldMap.GetEdgeSpriteKey(asset.graphId, edge.edgeId);
            }

            return edge.edgeSpriteAddress;
        }

        /// <summary>
        /// 프로젝트 상대 경로를 실제 파일 시스템 절대 경로로 변환합니다.
        /// </summary>
        /// <param name="path">프로젝트 상대 또는 절대 경로입니다.</param>
        /// <returns>파일 시스템 절대 경로입니다.</returns>
        private static string ToFullPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }
    }
}
