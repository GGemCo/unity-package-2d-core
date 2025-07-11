﻿using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 테이블 등록하기
    /// </summary>
    public class SettingCharacters : DefaultAddressable
    {
        private const string Title = "캐릭터 추가하기";
        private readonly AddressableEditor _addressableEditor;
        private readonly string _targetGroupNameMonster;
        private readonly string _targetGroupNameNpc;
        private readonly string _targetGroupNamePlayer;

        public SettingCharacters(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            _targetGroupNameMonster = ConfigAddressableGroupName.Monster;
            _targetGroupNameNpc = ConfigAddressableGroupName.Npc;
            _targetGroupNamePlayer = ConfigAddressableGroupName.Player;
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
            Dictionary<int, Dictionary<string, string>> dictionaryMonsters = _addressableEditor.TableMonster.GetDatas();
            Dictionary<int, Dictionary<string, string>> dictionaryNpcs = _addressableEditor.TableNpc.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupMonster = GetOrCreateGroup(settings, _targetGroupNameMonster);

            if (groupMonster)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryMonsters)
                {
                    var info = _addressableEditor.TableMonster.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                    var infoAnimation = _addressableEditor.TableAnimation.GetDataByUid(info.SpineUid);
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressables.KeyPrefabMonster}_{infoAnimation.Uid}";
                    string assetPath = $"{ConfigAddressables.PathPrefabMonster}/{infoAnimation.PrefabName}.prefab";
                    string label = "";
                    
                    Add(settings, groupMonster, key, assetPath, label);
                
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressables.KeyCharacterThumbnailMonster}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressables.PathCharacterThumbnailMonster}/{info.ImageThumbnailFileName}.png";
                        Add(settings, groupMonster, key, assetPath);
                    }
                }
            }
            
            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupNpc = GetOrCreateGroup(settings, _targetGroupNameNpc);

            if (groupNpc)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryNpcs)
                {
                    var info = _addressableEditor.TableNpc.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                    var infoAnimation = _addressableEditor.TableAnimation.GetDataByUid(info.SpineUid);
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressables.KeyPrefabNpc}_{infoAnimation.Uid}";
                    string assetPath = $"{ConfigAddressables.PathPrefabNpc}/{infoAnimation.PrefabName}.prefab";
                
                    Add(settings, groupNpc, key, assetPath);
                    
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressables.KeyCharacterThumbnailNpc}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressables.PathCharacterThumbnailNpc}/{info.ImageThumbnailFileName}.png";
                        Add(settings, groupNpc, key, assetPath);
                    }
                }
            }
            
            AddressableAssetGroup groupPlayer = GetOrCreateGroup(settings, _targetGroupNamePlayer);
            if (groupPlayer)
            {
                string key = ConfigAddressables.KeyPrefabPlayer;
                string assetPath = $"{ConfigAddressables.PathPrefabPlayer}/Player.prefab";
                
                Add(settings, groupPlayer, key, assetPath);
            }


            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            // 테이블 다시 로드하기
            _addressableEditor.LoadTables();
            
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }

    }
}