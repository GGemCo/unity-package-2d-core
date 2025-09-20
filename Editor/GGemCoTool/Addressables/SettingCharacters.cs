using System.Collections.Generic;
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
            // Common.OnGUITitle(Title);

            if (_addressableEditor.TableMonster == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Monster} 테이블이 없습니다.", MessageType.Info);
            }
            else {
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
            AddressableAssetGroup group = GetOrCreateGroup(settings, _targetGroupNameMonster);

            // 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);
            
            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryMonsters)
                {
                    var info = _addressableEditor.TableMonster.GetDataByUid(outerPair.Key);
                    if (info == null) continue;
                    var infoAnimation = _addressableEditor.TableAnimation.GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                
                    string key = $"{ConfigAddressableKey.PrefabMonster}_{infoAnimation.Uid}";
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                    string label = "";
                    
                    Add(settings, group, key, assetPath, label);
                
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterThumbnailMonster}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.Thumbnails.Monster}/{info.ImageThumbnailFileName}.png";
                        Add(settings, group, key, assetPath);
                    }
                }
            }
            
            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupNpc = GetOrCreateGroup(settings, _targetGroupNameNpc);

            ClearGroupEntries(settings, groupNpc);
            if (groupNpc)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionaryNpcs)
                {
                    var info = _addressableEditor.TableNpc.GetDataByUid(outerPair.Key);
                    if (info == null) continue;
                    var infoAnimation = _addressableEditor.TableAnimation.GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                
                    string key = $"{ConfigAddressableKey.PrefabNpc}_{infoAnimation.Uid}";
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                
                    Add(settings, groupNpc, key, assetPath);
                    
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterThumbnailNpc}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.Thumbnails.Npc}/{info.ImageThumbnailFileName}.png";
                        Add(settings, groupNpc, key, assetPath);
                    }
                }
            }
            
            AddressableAssetGroup groupPlayer = GetOrCreateGroup(settings, _targetGroupNamePlayer);
            ClearGroupEntries(settings, groupPlayer);
            if (groupPlayer)
            {
                string key = ConfigAddressableKey.PrefabPlayer;
                string assetPath = $"{ConfigAddressablePath.Characters.Player}/Player.prefab";
                
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