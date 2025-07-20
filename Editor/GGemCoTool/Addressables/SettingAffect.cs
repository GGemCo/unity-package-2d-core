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
    /// 아이템 아이콘, 드랍 이미지 등록하기
    /// </summary>
    public class SettingAffect : DefaultAddressable
    {
        private const string Title = "어펙트 아이콘 이미지 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingAffect(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            TargetGroupName = ConfigAddressableGroupName.AffectIconImage;
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
            Dictionary<int, Dictionary<string, string>> dictionary = _addressableEditor.TableAffect.GetDatas();
            
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
    
            var atlas = GetOrCreateSpriteAtlas($"{atlasFolderPath}/AffectIconAtlas.spriteatlas");
            
            List<Object> assets = new();
            if (group)
            {
                // foreach 문을 사용하여 딕셔너리 내용을 출력
                foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
                {
                    var info = _addressableEditor.TableAffect.GetDataByUid(outerPair.Key);
                    if (info.Uid <= 0) continue;
                
                    string key = $"{ConfigAddressables.KeyImageIconAffect}_{info.Uid}";
                    string assetPath = $"{ConfigAddressables.PathImageIconAffect}";
                    if (info.Type == AffectConstants.Type.Buff)
                    {
                        assetPath = $"{assetPath}/Buff";
                    }
                    else
                    {
                        assetPath = $"{assetPath}/DeBuff";
                    }
                    assetPath = $"{assetPath}/{info.IconFileName}.png";
                
                    Add(settings, group, key, assetPath);
                    AddToListIfExists(assets, assetPath);
                }
            }
            ClearAndAddToAtlas(atlas, assets);
            
            Add(settings, group, ConfigAddressables.KeyImageIconAffect, AssetDatabase.GetAssetPath(atlas), ConfigAddressableLabel.ImageAffectIcon);
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료", "OK");
        }
    }
}