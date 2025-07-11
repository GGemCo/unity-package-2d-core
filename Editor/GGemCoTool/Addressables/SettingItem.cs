﻿using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 아이템 아이콘, 드랍 이미지 등록하기
    /// </summary>
    public class SettingItem : DefaultAddressable
    {
        private const string Title = "아이템 아이콘, 드랍 이미지 추가하기";
        private readonly AddressableEditor _addressableEditor;
        private string _groupNameIconImage;
        private string _groupNameEquipImage;

        public SettingItem(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            TargetGroupName = ConfigAddressableGroupName.ItemDropImage;
            _groupNameIconImage = ConfigAddressableGroupName.ItemIconImage;
            _groupNameEquipImage = ConfigAddressableGroupName.ItemEquipImage;
        }
        public void OnGUI()
        {
            Common.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                Setup();
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        private void Setup()
        {
            Dictionary<int, Dictionary<string, string>> dictionary = _addressableEditor.TableItem.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupDropImage = GetOrCreateGroup(settings, TargetGroupName);
            AddressableAssetGroup groupEquipImage = GetOrCreateGroup(settings, _groupNameEquipImage);
            AddressableAssetGroup groupIconImage = GetOrCreateGroup(settings, _groupNameIconImage);

            // SpriteAtlas 생성
            string atlasFolderPath = ConfigAddressables.PathSpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);
    
            var atlasDrop = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemDropAtlas.spriteatlas");
            var atlasIcon = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemIconAtlas.spriteatlas");
            SpriteAtlas atlasEquip = GetOrCreateSpriteAtlas($"{atlasFolderPath}/ItemEquipAtlas.spriteatlas");
            // 장비 아틀라스는 spine 에서 사용하기위해 Read/Write 를 활성화 시킨다.
            SpriteAtlasTextureSettings spriteAtlasTextureSettings = new SpriteAtlasTextureSettings
            {
                anisoLevel = 1,
                readable = true,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            };
            atlasEquip.SetTextureSettings(spriteAtlasTextureSettings);
            
            List<Object> assetsDrop = new();
            List<Object> assetsIcon = new();
            List<Object> assetsEquip = new();
            
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
            {
                var info = _addressableEditor.TableItem.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;

                // Drop 이미지
                string dropPath = $"{ConfigAddressables.Path}/{info.ImageItemPath}.png";
                Add(settings, groupDropImage, $"{ConfigAddressableLabel.ImageItemDrop}_{info.Uid}", dropPath);
                AddToListIfExists(assetsDrop, dropPath);

                // Icon 이미지
                string iconPath = $"{ConfigAddressables.Path}/{info.ImagePath}.png";
                Add(settings, groupIconImage, $"{ConfigAddressableLabel.ImageItemIcon}_{info.Uid}", iconPath);
                AddToListIfExists(assetsIcon, iconPath);

                if (info.Type != ItemConstants.Type.Equip) continue;

                // Equip 이미지
                string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{info.Uid}";
                List<string> slotNames = ItemConstants.SlotNameByPartsType[info.PartsID];
                if (slotNames != null)
                {
                    foreach (string slotName in slotNames)
                    {
                        if (string.IsNullOrEmpty(slotName)) continue;
                        string equipPath = $"{ConfigAddressables.Path}/{info.PartsImagePath}_{slotName}.png";
                        Add(settings, groupEquipImage, baseKey, equipPath);
                        AddToListIfExists(assetsEquip, equipPath);
                    }
                }
            }
            // 기본 장비 이미지
            foreach (var data in ItemConstants.FolderNameByPartsType)
            {
                var partType = data.Key;
                if (partType == ItemConstants.PartsType.None) continue;
                string folderName = data.Value;
                if (string.IsNullOrEmpty(folderName)) continue;
                List<string> slotNames = ItemConstants.SlotNameByPartsType.GetValueOrDefault(partType);
                foreach (var slotName in slotNames)
                {
                    if (string.IsNullOrEmpty(slotName)) continue;
                    string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{folderName}_{slotName}";
                    string equipPath = $"{ConfigAddressables.PathItemParts}/{folderName}/{slotName}.png";
                    Add(settings, groupEquipImage, baseKey, equipPath);
                    AddToListIfExists(assetsEquip, equipPath);
                }
            }
            // blank 이미지
            string key = $"{ConfigAddressableLabel.ImageItemIcon}_blank";
            string path = $"{ConfigAddressables.Path}/Images/Icon/blank.png";
            Add(settings, groupIconImage, key, path);
            AddToListIfExists(assetsIcon, path);
            
            ClearAndAddToAtlas(atlasDrop, assetsDrop);
            ClearAndAddToAtlas(atlasIcon, assetsIcon);
            ClearAndAddToAtlas(atlasEquip, assetsEquip);

            // Atlas 를 Addressables 에 등록
            Add(settings, groupDropImage, ConfigAddressableLabel.ImageItemDrop, AssetDatabase.GetAssetPath(atlasDrop), ConfigAddressableLabel.ImageItemDrop);
            Add(settings, groupIconImage, ConfigAddressableLabel.ImageItemIcon, AssetDatabase.GetAssetPath(atlasIcon), ConfigAddressableLabel.ImageItemIcon);
            Add(settings, groupEquipImage, ConfigAddressableLabel.ImageItemEquip, AssetDatabase.GetAssetPath(atlasEquip), ConfigAddressableLabel.ImageItemEquip);
            
            // 강제로 pack 시키기
            SpriteAtlasUtility.PackAtlases(new[] { atlasDrop, atlasIcon, atlasEquip }, EditorUserBuildSettings.activeBuildTarget, false);

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            
            // 테이블 다시 로드하기
            _addressableEditor.LoadTables();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}