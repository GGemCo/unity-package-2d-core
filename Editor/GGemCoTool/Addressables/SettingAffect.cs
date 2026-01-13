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
            targetGroupName = ConfigAddressableGroupName.AffectIcon;
        }

        public void OnGUI()
        {
            if (TableLoaderManager.LoadAffectTable() == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Affect} 테이블이 없습니다.", MessageType.Info);
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
                        EditorUtility.DisplayDialog(Title, "어펙트 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
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
            
            Dictionary<int, StruckTableAffect> dictionary = TableLoaderManager.LoadAffectTable().GetDatas();

            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // 타겟 그룹 가져오기/생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            // 1) 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);

            // 스프라이트 아틀라스 준비
            string atlasFolderPath = ConfigAddressablePath.SpriteAtlas;
            Directory.CreateDirectory(atlasFolderPath);
            var atlas = GetOrCreateSpriteAtlas($"{atlasFolderPath}/AffectIconAtlas.spriteatlas");

            // 2) 테이블 기반으로 엔트리 재구성
            List<Object> assets = new();
            foreach (KeyValuePair<int, StruckTableAffect> outerPair in dictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;

                string key = $"{ConfigAddressableKey.AffectIcon}_{info.Uid}";
                string assetPath = $"{ConfigAddressablePath.Images.Icon.Affect}";
                assetPath = info.Type == AffectConstants.Type.Buff
                    ? $"{assetPath}/Buff"
                    : $"{assetPath}/DeBuff";
                assetPath = $"{assetPath}/{info.IconFileName}.png";

                Add(settings, group, key, assetPath);
                AddToListIfExists(assets, assetPath);
            }

            // 아틀라스 재구성
            ClearAndAddToAtlas(atlas, assets);

            // 아틀라스 자체도 Addressable 로 등록(공용 키/라벨)
            if (assets.Count > 0)
            {
                Add(settings, group, ConfigAddressableKey.AffectIcon, AssetDatabase.GetAssetPath(atlas),
                    ConfigAddressableLabel.ImageAffectIcon);
            }

            // 적용/저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 어펙트 설정 완료", ctx);
            }
            else
            {
                EditorUtility.DisplayDialog(Title, "[Addressable] 어펙트 설정 완료", "OK");
            }
        }
    }
}
