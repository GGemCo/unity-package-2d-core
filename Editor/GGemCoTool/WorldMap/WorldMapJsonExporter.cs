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
                    nodeType = node.nodeType.ToString(),
                    visibleByDefault = node.visibleByDefault,
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
                    unlockConditionKey = edge.unlockConditionKey,
                });
            }

            return json;
        }

        /// <summary>
        /// 배경 address가 비어 있을 때 Sprite의 에셋 경로를 대체값으로 사용합니다.
        /// </summary>
        /// <param name="asset">배경 정보를 가진 그래프 에셋입니다.</param>
        /// <returns>JSON에 기록할 배경 address입니다.</returns>
        private static string ResolveBackgroundAddress(WorldMapGraphAsset asset)
        {
            if (!string.IsNullOrWhiteSpace(asset.backgroundAddress))
            {
                return asset.backgroundAddress;
            }

            return asset.backgroundSprite != null ? AssetDatabase.GetAssetPath(asset.backgroundSprite) : string.Empty;
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
