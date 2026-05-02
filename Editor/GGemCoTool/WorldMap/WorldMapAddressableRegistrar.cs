using GGemCo2DCore;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 월드맵 export 과정에서 JSON, 배경 Sprite, 노드 아이콘 Sprite를 Addressables에 등록합니다.
    /// </summary>
    internal static class WorldMapAddressableRegistrar
    {
        /// <summary>
        /// 월드맵 그래프 에셋이 참조하는 Sprite들을 Addressables에 등록하고 JSON에 기록할 address를 확정합니다.
        /// </summary>
        /// <param name="asset">등록할 월드맵 그래프 에셋입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>등록에 성공하면 true입니다.</returns>
        public static bool TryRegisterSprites(WorldMapGraphAsset asset, out string error)
        {
            error = null;
            if (asset == null)
            {
                error = "월드맵 그래프 에셋이 없습니다.";
                return false;
            }

            AddressableAssetSettings settings = GetOrCreateSettings();
            AddressableAssetGroup group = GetOrCreateGroup(settings, ConfigAddressableGroupName.WorldMap);
            if (group == null)
            {
                error = "월드맵 Addressables 그룹을 생성하지 못했습니다.";
                return false;
            }

            bool changed = false;
            Dictionary<string, string> registeredKeyByAssetPath = new Dictionary<string, string>();
            if (asset.backgroundSprite != null)
            {
                string backgroundKey = ConfigAddressableWorldMap.GetBackgroundKey(asset.graphId);
                if (!TryRegisterAsset(settings, group, backgroundKey, asset.backgroundSprite, out error))
                {
                    return false;
                }

                asset.backgroundAddress = backgroundKey;
                registeredKeyByAssetPath[AssetDatabase.GetAssetPath(asset.backgroundSprite)] = backgroundKey;
                changed = true;
            }

            if (asset.nodes != null)
            {
                for (int i = 0; i < asset.nodes.Count; i++)
                {
                    WorldMapNodeData node = asset.nodes[i];
                    if (node == null)
                    {
                        continue;
                    }

                    if (node.iconSprite != null)
                    {
                        string iconKey = ConfigAddressableWorldMap.GetNodeIconKey(asset.graphId, node.nodeId);
                        string iconAssetPath = AssetDatabase.GetAssetPath(node.iconSprite);
                        if (registeredKeyByAssetPath.TryGetValue(iconAssetPath, out string sharedKey))
                        {
                            node.iconAddress = sharedKey;
                            changed = true;
                        }
                        else
                        {
                            if (!TryRegisterAsset(settings, group, iconKey, node.iconSprite, out error))
                            {
                                return false;
                            }

                            node.iconAddress = iconKey;
                            registeredKeyByAssetPath[iconAssetPath] = iconKey;
                            changed = true;
                        }
                    }

                    if (node.inactiveSprite == null)
                    {
                        continue;
                    }

                    string inactiveSpriteKey = ConfigAddressableWorldMap.GetNodeInactiveSpriteKey(asset.graphId, node.nodeId);
                    string inactiveSpriteAssetPath = AssetDatabase.GetAssetPath(node.inactiveSprite);
                    if (registeredKeyByAssetPath.TryGetValue(inactiveSpriteAssetPath, out string sharedInactiveSpriteKey))
                    {
                        node.inactiveSpriteAddress = sharedInactiveSpriteKey;
                        changed = true;
                        continue;
                    }

                    if (!TryRegisterAsset(settings, group, inactiveSpriteKey, node.inactiveSprite, out error))
                    {
                        return false;
                    }

                    node.inactiveSpriteAddress = inactiveSpriteKey;
                    registeredKeyByAssetPath[inactiveSpriteAssetPath] = inactiveSpriteKey;
                    changed = true;
                }
            }

            if (asset.edges != null)
            {
                for (int i = 0; i < asset.edges.Count; i++)
                {
                    WorldMapEdgeData edge = asset.edges[i];
                    if (edge == null || edge.edgeSprite == null)
                    {
                        continue;
                    }

                    string edgeSpriteKey = ConfigAddressableWorldMap.GetEdgeSpriteKey(asset.graphId, edge.edgeId);
                    string edgeSpriteAssetPath = AssetDatabase.GetAssetPath(edge.edgeSprite);
                    if (registeredKeyByAssetPath.TryGetValue(edgeSpriteAssetPath, out string sharedKey))
                    {
                        edge.edgeSpriteAddress = sharedKey;
                        changed = true;
                        continue;
                    }

                    if (!TryRegisterAsset(settings, group, edgeSpriteKey, edge.edgeSprite, out error))
                    {
                        return false;
                    }

                    edge.edgeSpriteAddress = edgeSpriteKey;
                    registeredKeyByAssetPath[edgeSpriteAssetPath] = edgeSpriteKey;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(asset);
            }

            SaveAddressableSettings(settings);
            return true;
        }

        /// <summary>
        /// export된 월드맵 JSON 파일을 Addressables에 등록합니다.
        /// </summary>
        /// <param name="graphId">월드맵 그래프 ID입니다.</param>
        /// <param name="jsonAssetPath">프로젝트 상대 JSON 에셋 경로입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>등록에 성공하면 true입니다.</returns>
        public static bool TryRegisterJson(string graphId, string jsonAssetPath, out string error)
        {
            error = null;
            AddressableAssetSettings settings = GetOrCreateSettings();
            AddressableAssetGroup group = GetOrCreateGroup(settings, ConfigAddressableGroupName.WorldMap);
            if (group == null)
            {
                error = "월드맵 Addressables 그룹을 생성하지 못했습니다.";
                return false;
            }

            string key = ConfigAddressableWorldMap.GetKey(graphId);
            if (!TryRegisterAssetPath(settings, group, key, jsonAssetPath, out error))
            {
                return false;
            }

            SaveAddressableSettings(settings);
            return true;
        }

        /// <summary>
        /// Addressables 설정을 가져오며, 없으면 새로 생성합니다.
        /// </summary>
        /// <returns>Addressables 설정 객체입니다.</returns>
        private static AddressableAssetSettings GetOrCreateSettings()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                return settings;
            }

            settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData",
                "AddressableAssetSettings",
                true,
                true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            return settings;
        }

        /// <summary>
        /// 지정한 Addressables 그룹을 가져오거나 새로 생성합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="groupName">그룹 이름입니다.</param>
        /// <returns>Addressables 그룹입니다.</returns>
        private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            if (settings == null)
            {
                return null;
            }

            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group != null)
            {
                return group;
            }

            return settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
        }

        /// <summary>
        /// Unity Object 에셋을 Addressables에 등록합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">등록할 그룹입니다.</param>
        /// <param name="key">Addressables 키입니다.</param>
        /// <param name="asset">등록할 에셋입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>등록에 성공하면 true입니다.</returns>
        private static bool TryRegisterAsset(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string key,
            Object asset,
            out string error)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            return TryRegisterAssetPath(settings, group, key, assetPath, out error);
        }

        /// <summary>
        /// 프로젝트 상대 에셋 경로를 Addressables에 등록합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">등록할 그룹입니다.</param>
        /// <param name="key">Addressables 키입니다.</param>
        /// <param name="assetPath">프로젝트 상대 에셋 경로입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>등록에 성공하면 true입니다.</returns>
        private static bool TryRegisterAssetPath(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string key,
            string assetPath,
            out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "Addressables에 등록할 에셋 경로가 비어 있습니다.";
                return false;
            }

            assetPath = assetPath.Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
            {
                error = "Addressables에 등록할 에셋 GUID를 찾지 못했습니다. path=" + assetPath;
                return false;
            }

            AddressableAssetEntry entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group);
            if (entry == null)
            {
                error = "Addressables 엔트리를 생성하지 못했습니다. path=" + assetPath;
                return false;
            }

            entry.address = key;
            entry.SetLabel(ConfigAddressableLabel.WorldMap, true, true);
            return true;
        }

        /// <summary>
        /// Addressables 설정 변경 사항을 저장합니다.
        /// </summary>
        /// <param name="settings">저장할 Addressables 설정 객체입니다.</param>
        private static void SaveAddressableSettings(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
        }
    }
}
