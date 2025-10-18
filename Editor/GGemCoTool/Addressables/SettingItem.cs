using System.Collections.Generic;
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
        private readonly string _groupNameIconImage;
        private readonly string _groupNameEquipImage;

        public SettingItem(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.ItemGroup.DropImage;
            _groupNameIconImage = ConfigAddressableGroupName.ItemGroup.IconImage;
            _groupNameEquipImage = ConfigAddressableGroupName.ItemGroup.EquipImage;
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (_addressableEditor.TableItem == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Item} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    Setup();
                }
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        private void Setup()
        {
            bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
            if (!result) return;
            
            Dictionary<int, Dictionary<string, string>> dictionary = _addressableEditor.TableItem.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupDropImage = GetOrCreateGroup(settings, targetGroupName);
            AddressableAssetGroup groupEquipImage = GetOrCreateGroup(settings, _groupNameEquipImage);
            AddressableAssetGroup groupIconImage = GetOrCreateGroup(settings, _groupNameIconImage);
            
            ClearGroupEntries(settings, groupDropImage);
            ClearGroupEntries(settings, groupEquipImage);
            ClearGroupEntries(settings, groupIconImage);
            
            // SpriteAtlas 생성
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
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
                string dropPath = $"{ConfigAddressablePath.Root}/{info.ImageItemPath}.png";
                Add(settings, groupDropImage, $"{ConfigAddressableLabel.ImageItemDrop}_{info.Uid}", dropPath);
                AddToListIfExists(assetsDrop, dropPath);

                // Icon 이미지
                string iconPath = $"{ConfigAddressablePath.Root}/{info.ImagePath}.png";
                Add(settings, groupIconImage, $"{ConfigAddressableLabel.ImageItemIcon}_{info.Uid}", iconPath);
                AddToListIfExists(assetsIcon, iconPath);

                if (info.Type != ItemConstants.Type.Equip) continue;

                // Equip 이미지
                string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{info.Uid}";
                List<string> slotNames = ItemConstants.SlotNameByPartsType.GetValueOrDefault(info.PartsID);
                if (slotNames != null)
                {
                    foreach (string slotName in slotNames)
                    {
                        if (string.IsNullOrEmpty(slotName)) continue;
                        string equipPath = $"{ConfigAddressablePath.Root}/{info.PartsImagePath}_{slotName}.png";
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
                if (slotNames == null) continue;
                foreach (var slotName in slotNames)
                {
                    if (string.IsNullOrEmpty(slotName)) continue;
                    string baseKey = $"{ConfigAddressableLabel.ImageItemEquip}_{folderName}_{slotName}";
                    string equipPath = $"{ConfigAddressablePath.Images.Parts}/{folderName}/{slotName}.png";
                    Add(settings, groupEquipImage, baseKey, equipPath);
                    AddToListIfExists(assetsEquip, equipPath);
                }
            }
            // blank 이미지
            string key = $"{ConfigAddressableLabel.ImageItemIcon}_blank";
            string path = $"{ConfigAddressablePath.Root}/Images/Icon/blank.png";
            Add(settings, groupIconImage, key, path);
            AddToListIfExists(assetsIcon, path);
            
            ClearAndAddToAtlas(atlasDrop, assetsDrop);
            ClearAndAddToAtlas(atlasIcon, assetsIcon);
            ClearAndAddToAtlas(atlasEquip, assetsEquip);

            // Atlas 를 Addressables 에 등록
            if (assetsDrop.Count > 0)
                Add(settings, groupDropImage, ConfigAddressableLabel.ImageItemDrop, AssetDatabase.GetAssetPath(atlasDrop), ConfigAddressableLabel.ImageItemDrop);
            
            if (assetsIcon.Count > 0)
                Add(settings, groupIconImage, ConfigAddressableLabel.ImageItemIcon, AssetDatabase.GetAssetPath(atlasIcon), ConfigAddressableLabel.ImageItemIcon);
            
            if (assetsEquip.Count > 0)
                Add(settings, groupEquipImage, ConfigAddressableLabel.ImageItemEquip, AssetDatabase.GetAssetPath(atlasEquip), ConfigAddressableLabel.ImageItemEquip);
            
            // 강제로 pack 시키기
            if (assetsDrop.Count > 0 || assetsIcon.Count > 0 || assetsEquip.Count > 0)
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