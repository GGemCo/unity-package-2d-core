using System;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 JSON을 편집용 GraphAsset으로 가져옵니다.
    /// </summary>
    internal static class WorldMapJsonImporter
    {
        /// <summary>
        /// JSON 파일을 읽어 지정한 그래프 에셋에 덮어씁니다.
        /// </summary>
        /// <param name="asset">가져온 데이터를 적용할 그래프 에셋입니다.</param>
        /// <param name="jsonPath">읽어올 JSON 파일 경로입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>가져오기에 성공하면 true입니다.</returns>
        public static bool TryImportIntoAsset(WorldMapGraphAsset asset, string jsonPath, out string error)
        {
            error = null;

            if (asset == null)
            {
                error = "월드맵 그래프 에셋이 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(jsonPath) || !File.Exists(jsonPath))
            {
                error = "월드맵 JSON 파일을 찾을 수 없습니다.";
                return false;
            }

            try
            {
                string content = File.ReadAllText(jsonPath);
                WorldMapGraphJson json = JsonConvert.DeserializeObject<WorldMapGraphJson>(content);
                if (json == null)
                {
                    error = "월드맵 JSON 파싱 결과가 비어 있습니다.";
                    return false;
                }

                ApplyJson(asset, json);
                return true;
            }
            catch (Exception e)
            {
                error = "월드맵 JSON 가져오기에 실패했습니다. " + e.Message;
                return false;
            }
        }

        /// <summary>
        /// JSON DTO 내용을 편집용 그래프 에셋에 적용합니다.
        /// </summary>
        /// <param name="asset">데이터를 적용할 그래프 에셋입니다.</param>
        /// <param name="json">가져온 JSON DTO입니다.</param>
        private static void ApplyJson(WorldMapGraphAsset asset, WorldMapGraphJson json)
        {
            Undo.RecordObject(asset, "월드맵 JSON 가져오기");

            asset.graphId = ConfigAddressableWorldMap.NormalizeGraphId(json.graphId);
            asset.startNodeId = json.startNodeId;
            asset.backgroundAddress = json.background != null ? json.background.address : string.Empty;
            asset.referenceResolution = json.background != null && json.background.referenceResolution != null
                ? json.background.referenceResolution.ToVector2()
                : new Vector2(1920f, 1080f);
            asset.backgroundSprite = LoadSpriteFromAddress(asset.backgroundAddress);

            asset.nodes.Clear();
            if (json.nodes != null)
            {
                for (int i = 0; i < json.nodes.Count; i++)
                {
                    WorldMapNodeJson nodeJson = json.nodes[i];
                    if (nodeJson == null)
                    {
                        continue;
                    }

                    asset.nodes.Add(new WorldMapNodeData
                    {
                        nodeId = nodeJson.nodeId,
                        mapUid = nodeJson.mapUid,
                        normalizedPosition = nodeJson.position != null ? nodeJson.position.ToVector2() : new Vector2(0.5f, 0.5f),
                        titleOverride = nodeJson.titleOverride,
                        iconAddress = nodeJson.iconAddress,
                        iconSprite = LoadSpriteFromAddress(nodeJson.iconAddress),
                        nodeType = ParseEnum(nodeJson.nodeType, WorldMapNodeType.Normal),
                        visibleByDefault = nodeJson.visibleByDefault,
                        unlockConditionKey = nodeJson.unlockConditionKey,
                    });
                }
            }

            asset.edges.Clear();
            if (json.edges != null)
            {
                for (int i = 0; i < json.edges.Count; i++)
                {
                    WorldMapEdgeJson edgeJson = json.edges[i];
                    if (edgeJson == null)
                    {
                        continue;
                    }

                    asset.edges.Add(new WorldMapEdgeData
                    {
                        edgeId = edgeJson.edgeId,
                        fromNodeId = edgeJson.fromNodeId,
                        toNodeId = edgeJson.toNodeId,
                        bidirectional = edgeJson.bidirectional,
                        edgeType = ParseEnum(edgeJson.edgeType, WorldMapEdgeType.Normal),
                        unlockConditionKey = edgeJson.unlockConditionKey,
                    });
                }
            }

            asset.EnsureDefaults();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 문자열을 enum 값으로 변환하고 실패 시 기본값을 반환합니다.
        /// </summary>
        /// <typeparam name="T">변환할 enum 타입입니다.</typeparam>
        /// <param name="value">원본 문자열입니다.</param>
        /// <param name="defaultValue">실패 시 사용할 기본값입니다.</param>
        /// <returns>변환된 enum 값입니다.</returns>
        private static T ParseEnum<T>(string value, T defaultValue) where T : struct
        {
            T result;
            return !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out result)
                ? result
                : defaultValue;
        }

        /// <summary>
        /// address가 프로젝트 에셋 경로이거나 Addressables 키일 경우 Sprite를 로드합니다.
        /// </summary>
        /// <param name="address">Sprite Addressables 키 또는 에셋 경로입니다.</param>
        /// <returns>로드된 Sprite입니다. 로드할 수 없으면 null입니다.</returns>
        private static Sprite LoadSpriteFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            if (address.StartsWith("Assets/"))
            {
                return AssetDatabase.LoadAssetAtPath<Sprite>(address);
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                return null;
            }

            for (int i = 0; i < settings.groups.Count; i++)
            {
                AddressableAssetGroup group = settings.groups[i];
                if (group == null)
                {
                    continue;
                }

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry != null && entry.address == address)
                    {
                        return AssetDatabase.LoadAssetAtPath<Sprite>(entry.AssetPath);
                    }
                }
            }

            return null;
        }
    }
}
