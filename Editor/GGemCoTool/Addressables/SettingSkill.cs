using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingSkill : DefaultAddressable
    {
        private const string Title = "스킬 아이콘 추가하기";
        private readonly AddressableEditor _addressableEditor;
        
        public SettingSkill(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = ConfigAddressableGroupName.SkillIcon;
        }
        public void OnGUI()
        {
            // Common.OnGUITitle(Title);

            if (TableLoaderManager.LoadSkillTable() == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Skill} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth), GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "스킬 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
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
            Dictionary<int, StruckTableSkill> dictionary = TableLoaderManager.LoadSkillTable().GetSkills();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }
            
            ClearGroupEntries(settings, group);
            
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);
    
            var atlas = GetOrCreateSpriteAtlas($"{atlasFolderPath}/SkillIconAtlas.spriteatlas");
            
            List<Object> assets = new();
            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (var data in dictionary)
                {
                    var info = data.Value;
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressableKey.SkillIcon}_{info.Uid}";
                    string assetPath = $"{ConfigAddressablePath.Images.Icon.Skill}/{info.IconFileName}.png";
                
                    Add(settings, group, key, assetPath);
                    AddToListIfExists(assets, assetPath);
                }
            }
            ClearAndAddToAtlas(atlas, assets);
            
            if (assets.Count > 0)
                Add(settings, group, ConfigAddressableKey.SkillIcon, AssetDatabase.GetAssetPath(atlas), ConfigAddressableLabel.ImageSkillIcon);
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 스킬 설정 완료", ctx);
            }
            else
            {
                EditorUtility.DisplayDialog(Title, "[Addressable] 스킬 설정 완료", "OK");
            }
        }
    }
}