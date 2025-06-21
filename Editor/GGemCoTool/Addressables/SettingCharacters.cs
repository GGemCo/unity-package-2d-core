using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 테이블 등록하기
    /// </summary>
    public class SettingCharacters : DefaultAddressable
    {
        private const string Title = "캐릭터 추가하기";
        private readonly EditorAddressable _editorAddressable;
        private readonly string _targetGroupNameMonster;
        private readonly string _targetGroupNameNpc;

        public SettingCharacters(EditorAddressable editorWindow)
        {
            _editorAddressable = editorWindow;
            _targetGroupNameMonster = "GGemCo_Character_Monster";
            _targetGroupNameNpc = "GGemCo_Character_Npc";
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
            Dictionary<int, Dictionary<string, string>> dictionaryMonsters = _editorAddressable.TableMonster.GetDatas();
            Dictionary<int, Dictionary<string, string>> dictionaryNpcs = _editorAddressable.TableNpc.GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                GcLogger.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupMonster = GetOrCreateGroup(settings, _targetGroupNameMonster);

            if (groupMonster)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryMonsters)
                {
                    var info = _editorAddressable.TableMonster.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                    var infoAnimation = _editorAddressable.TableAnimation.GetDataByUid(info.SpineUid);
                    if (info.Uid <= 0) continue;
                
                    string key = $"GGemCo_Character_Monster_{infoAnimation.Uid}";
                    string assetPath = $"{ConfigAddressables.Path}/{infoAnimation.PrefabPath}.prefab";
                    string label = "";
                
                    Add(settings, groupMonster, key, assetPath, label);
                }
            }
            
            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupNpc = GetOrCreateGroup(settings, _targetGroupNameNpc);

            if (groupNpc)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryNpcs)
                {
                    var info = _editorAddressable.TableNpc.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                    var infoAnimation = _editorAddressable.TableAnimation.GetDataByUid(info.SpineUid);
                    if (info.Uid <= 0) continue;
                
                    string key = $"GGemCo_Character_Npc_{infoAnimation.Uid}";
                    string assetPath = $"{ConfigAddressables.Path}/{infoAnimation.PrefabPath}.prefab";
                
                    Add(settings, groupNpc, key, assetPath);
                }
            }

            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            // 테이블 다시 로드하기
            _editorAddressable.LoadTables();
            
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }

    }
}