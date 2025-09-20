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
            if (_addressableEditor.TableAffect == null)
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Affect} 테이블이 없습니다.", MessageType.Info);
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
            
            Dictionary<int, Dictionary<string, string>> dictionary = _addressableEditor.TableAffect.GetDatas();

            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                Debug.LogWarning("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.");
                settings = CreateAddressableSettings();
            }

            // 타겟 그룹 가져오기/생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                EditorUtility.DisplayDialog(Title, "그룹을 생성/가져오지 못했습니다.", "OK");
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
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
            {
                var info = _addressableEditor.TableAffect.GetDataByUid(outerPair.Key);
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
            EditorUtility.DisplayDialog(Title, "Addressable 설정 완료 (그룹 엔트리 초기화 후 재구성)", "OK");
        }
    }
}
