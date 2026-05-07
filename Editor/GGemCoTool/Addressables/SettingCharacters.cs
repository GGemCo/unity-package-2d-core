using System.Collections.Generic;
using System.IO;
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
        
        private readonly string _targetGroupNameCharacterThumbnail;
        private readonly string _targetGroupNameCharacterImageName;

        public SettingCharacters(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            _targetGroupNameMonster = ConfigAddressableGroupName.Monster;
            _targetGroupNameNpc = ConfigAddressableGroupName.Npc;
            _targetGroupNamePlayer = ConfigAddressableGroupName.Player;
            _targetGroupNameCharacterThumbnail = ConfigAddressableGroupName.CharacterThumbnail;
            _targetGroupNameCharacterImageName = ConfigAddressableGroupName.CharacterImageName;
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (!File.Exists($"{ConfigAddressableTable.TableMonster.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Monster} 테이블이 없습니다.", MessageType.Info);
            }
            else {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "캐릭터 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                    }
                }
            }
        }
        
        /// <summary>
        /// Addressable 설정하기
        /// </summary>
        public void Setup(EditorSetupContext ctx = null)
        {
            if (ctx == null)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result) return;
            }

            Dictionary<int, StruckTableMonster> dictionaryMonsters = TableLoaderManager.LoadMonsterTable().GetDatas();
            Dictionary<int, StruckTableNpc> dictionaryNpcs = TableLoaderManager.LoadNpcTable().GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("[Addressable] Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            AddressableAssetGroup groupCharacterThumbnail = GetOrCreateGroup(settings, _targetGroupNameCharacterThumbnail);
            AddressableAssetGroup groupNameCharacterImageName = GetOrCreateGroup(settings, _targetGroupNameCharacterImageName);
            
            #region 몬스터

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, _targetGroupNameMonster);
            // 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);
            
            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, StruckTableMonster> outerPair in dictionaryMonsters)
                {
                    var info = outerPair.Value;
                    if (info == null) continue;
                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                
                    string key = $"{ConfigAddressableKey.PrefabMonster}_{infoAnimation.Uid}";
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                    Add(settings, group, key, assetPath);
                
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterThumbnailMonster}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.Thumbnails.Monster}/{info.ImageThumbnailFileName}.png";
                        var label = ConfigAddressableLabel.CharacterThumbnail;
                        Add(settings, groupCharacterThumbnail, key, assetPath, label);
                    }
                    // UIWindowBattleHudMonster의 이름 이미지
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterImageNameMonster}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.ImageName.Monster}/{info.ImageThumbnailFileName}.png";
                        var label = ConfigAddressableLabel.CharacterImageName;
                        Add(settings, groupNameCharacterImageName, key, assetPath, label);
                    }
                }
            }
            #endregion

            #region NPC
            
            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup groupNpc = GetOrCreateGroup(settings, _targetGroupNameNpc);

            ClearGroupEntries(settings, groupNpc);
            if (groupNpc)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, StruckTableNpc> outerPair in dictionaryNpcs)
                {
                    var info = outerPair.Value;
                    if (info == null) continue;
                    var infoAnimation = TableLoaderManager.LoadAnimationTable().GetDataByUid(info.AnimationUid);
                    if (infoAnimation == null) continue;
                
                    string key = $"{ConfigAddressableKey.PrefabNpc}_{infoAnimation.Uid}";
                    string assetPath = ConfigAddressableMap.GetPathCharacter(infoAnimation, true);
                
                    Add(settings, groupNpc, key, assetPath);
                    
                    // 썸네일 있으면 추가
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterThumbnailNpc}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.Thumbnails.Npc}/{info.ImageThumbnailFileName}.png";
                        var label = ConfigAddressableLabel.CharacterThumbnail;
                        Add(settings, groupCharacterThumbnail, key, assetPath, label);
                    }
                    // UIWindowBattleHudMonster의 이름 이미지
                    if (!string.IsNullOrEmpty(info.ImageThumbnailFileName))
                    {
                        key = $"{ConfigAddressableKey.CharacterImageNameNpc}_{info.ImageThumbnailFileName}";
                        assetPath = $"{ConfigAddressablePath.Characters.ImageName.Npc}/{info.ImageThumbnailFileName}.png";
                        var label = ConfigAddressableLabel.CharacterImageName;
                        Add(settings, groupNameCharacterImageName, key, assetPath, label);
                    }
                }
            }
            #endregion

            #region 플레이어
            AddressableAssetGroup groupPlayer = GetOrCreateGroup(settings, _targetGroupNamePlayer);
            ClearGroupEntries(settings, groupPlayer);
            if (groupPlayer)
            {
                string key = ConfigAddressableKey.PrefabPlayer;
                string assetPath = $"{ConfigAddressablePath.Characters.Player}/Player.prefab";

                Add(settings, groupPlayer, key, assetPath);

                // 썸네일 있으면 추가
                key = $"{ConfigAddressableKey.CharacterThumbnailPlayer}";
                assetPath = $"{ConfigAddressablePath.Characters.Thumbnails.Player}/Player.png";
                var label = ConfigAddressableLabel.CharacterThumbnail;
                Add(settings, groupCharacterThumbnail, key, assetPath, label);
            }
            #endregion

            var maps = TableLoaderManager.LoadMapTable().GetDatas(); // 프로젝트의 Map 테이블 타입/로더에 맞게 사용
            foreach (var pair in maps)
            {
                var mapInfo = pair.Value;
                if (mapInfo == null) continue;

                string folderName = mapInfo.FolderName;

                AddressableCharacterLabelUtility.ApplyMapLabelFromRegen(
                    settings,
                    folderName,
                    ConfigAddressableMap.GetAssetPathRegenMonster(folderName),
                    AddressableCharacterType.Monster,
                    clearExistingLabel: true);

                AddressableCharacterLabelUtility.ApplyMapLabelFromRegen(
                    settings,
                    folderName,
                    ConfigAddressableMap.GetAssetPathRegenNpc(folderName),
                    AddressableCharacterType.Npc,
                    clearExistingLabel: true);
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 캐릭터 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] 캐릭터 설정 완료", "OK");
            }
        }

    }
}
