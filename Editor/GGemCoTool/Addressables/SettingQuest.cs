﻿using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingQuest : DefaultAddressable
    {
        private const string Title = "퀘스트 추가하기";
        private readonly AddressableEditor _addressableEditor;
        
        public SettingQuest(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            TargetGroupName = ConfigAddressableGroupName.Quest;
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
            Dictionary<int, Dictionary<string, string>> dictionary = _addressableEditor.TableQuest.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupMonster = GetOrCreateGroup(settings, TargetGroupName);
            
            if (groupMonster)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
                {
                    var info = _addressableEditor.TableQuest.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressables.KeyQuest}_{info.Uid}";
                    string assetPath = $"{ConfigAddressables.PathJsonQuest}/{info.FileName}.json";
                    string label = ConfigAddressableLabel.Quest;
                
                    Add(settings, groupMonster, key, assetPath, label);
                }
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}