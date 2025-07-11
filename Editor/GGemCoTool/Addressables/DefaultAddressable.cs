﻿using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace GGemCo2DCoreEditor
{
    public class DefaultAddressable
    {
        protected string TargetGroupName = ""; // 그룹 이름

        /// <summary>
        /// Addressable 설정이 없을 경우 새로 생성
        /// </summary>
        protected AddressableAssetSettings CreateAddressableSettings()
        {
            var settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData", 
                "AddressableAssetSettings", 
                true, 
                true
            );

            AddressableAssetSettingsDefaultObject.Settings = settings;
            AssetDatabase.SaveAssets();
            // Debug.Log("새로운 Addressable 설정을 생성했습니다.");
            return settings;
        }

        /// <summary>
        /// 기본 Addressable 그룹이 없을 경우 생성
        /// </summary>
        private AddressableAssetGroup CreateDefaultGroup(AddressableAssetSettings settings)
        {
            var defaultGroup = settings.CreateGroup(
                TargetGroupName, 
                false, 
                false, 
                true, 
                settings.DefaultGroup.Schemas
            );

            settings.DefaultGroup = defaultGroup;
            // Debug.Log("새로운 기본 Addressable 그룹을 생성했습니다.");
            return defaultGroup;
        }
        /// <summary>
        /// 지정한 이름의 그룹이 없으면 새로 생성
        /// </summary>
        protected AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            foreach (var group in settings.groups)
            {
                if (group != null && group.Name == groupName)
                    return group;
            }

            var newGroup = settings.CreateGroup(
                groupName,
                false,
                false,
                true,
                settings.DefaultGroup.Schemas // 기존 기본 그룹의 스키마 복사
            );

            Debug.Log($"새로운 Addressable 그룹을 생성했습니다: {groupName}");
            return newGroup;
        }
        
        protected void Add(AddressableAssetSettings settings, AddressableAssetGroup group, string keyName, string assetPath, string labelName = "")
        {
            // 대상 파일 가져오기
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (!asset)
            {
                Debug.LogError($"파일을 찾을 수 없습니다: {assetPath}");
                return;
            }

            // 기존 Addressable 항목 확인
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));

            if (entry == null)
            {
                // 신규 Addressable 항목 추가
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
                Debug.Log($"Addressable 항목을 추가했습니다: {assetPath}");
            }
            else
            {
                Debug.Log($"이미 Addressable에 등록된 항목입니다: {assetPath}");
            }

            // 키 값 설정
            entry.address = keyName;
            // 라벨 값 설정
            if (!string.IsNullOrEmpty(labelName))
            {
                entry.SetLabel(labelName, true, true);
            }

            // Debug.Log($"Addressable 키 값 설정: {keyName}");
        }
        protected SpriteAtlas GetOrCreateSpriteAtlas(string path)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
                packing.enableRotation = false;
                packing.enableTightPacking = false;
                atlas.SetPackingSettings(packing);

                AssetDatabase.CreateAsset(atlas, path);
                AssetDatabase.SaveAssets();
            }
            return atlas;
        }

        protected void AddToListIfExists(List<Object> list, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                list.Add(asset);
            }
        }
        protected void ClearAndAddToAtlas(SpriteAtlas atlas, List<Object> assets)
        {
            atlas.Remove(atlas.GetPackables()); // 기존 등록된 에셋 제거
            atlas.Add(assets.ToArray());        // 새로 추가
        }
    }
}