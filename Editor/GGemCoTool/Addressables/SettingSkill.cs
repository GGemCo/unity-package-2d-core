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
        private readonly EditorAddressable _editorAddressable;
        
        public SettingSkill(EditorAddressable editorWindow)
        {
            _editorAddressable = editorWindow;
            TargetGroupName = ConfigAddressableGroupName.SkillIconImage;
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
            Dictionary<int, StruckTableSkill> dictionary = _editorAddressable.TableSkill.GetSkills();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, TargetGroupName);
            
            string atlasFolderPath = ConfigAddressables.PathSpriteAtlas;
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
                
                    string key = $"{ConfigAddressables.KeyImageIconSkill}_{info.Uid}";
                    string assetPath = $"{ConfigAddressables.PathImageIconSkill}/{info.IconFileName}.png";
                
                    Add(settings, group, key, assetPath);
                    AddToListIfExists(assets, assetPath);
                }
            }
            ClearAndAddToAtlas(atlas, assets);
            
            Add(settings, group, ConfigAddressables.KeyImageIconSkill, AssetDatabase.GetAssetPath(atlas), ConfigAddressableLabel.ImageSkillIcon);
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}