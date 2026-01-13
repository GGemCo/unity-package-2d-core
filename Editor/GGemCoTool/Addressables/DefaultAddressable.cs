using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace GGemCo2DCoreEditor
{
    public class DefaultAddressable
    {
        protected string targetGroupName = ""; // 그룹 이름
        protected const string TextDisplayDialogTitle = "추가하기";
        protected const string TextDisplayDialogMessage = "기존에 등록된 내용은 삭제됩니다.\n진행하시겠습니까?";

        /// <summary>
        /// Addressable 설정이 없을 경우 새로 생성
        /// </summary>
        protected AddressableAssetSettings CreateAddressableSettings()
        {
            var settings = AddressableAssetSettings.Create(
                "Assets/AddressableAssetsData", 
                "AddressableAssetSettings", 
                true, 
                true
            );

            AddressableAssetSettingsDefaultObject.Settings = settings;
            AssetDatabase.SaveAssets();
            // Debug.Log("새로운 Addressable 설정을 생성했습니다.");
            return settings;
        }

        /// <summary>
        /// 기본 Addressable 그룹이 없을 경우 생성
        /// </summary>
        private AddressableAssetGroup CreateDefaultGroup(AddressableAssetSettings settings)
        {
            var defaultGroup = settings.CreateGroup(
                targetGroupName, 
                false, 
                false, 
                true, 
                settings.DefaultGroup.Schemas
            );

            settings.DefaultGroup = defaultGroup;
            // Debug.Log("새로운 기본 Addressable 그룹을 생성했습니다.");
            return defaultGroup;
        }
        /// <summary>
        /// 지정한 이름의 그룹이 없으면 새로 생성
        /// </summary>
        protected AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
        {
            foreach (var group in settings.groups)
            {
                if (group != null && group.Name == groupName)
                    return group;
            }

            var newGroup = settings.CreateGroup(
                groupName,
                false,
                false,
                true,
                settings.DefaultGroup.Schemas // 기존 기본 그룹의 스키마 복사
            );

            Debug.Log($"새로운 Addressable 그룹을 생성했습니다: {groupName}");
            return newGroup;
        }
        
        protected AddressableAssetEntry Add(AddressableAssetSettings settings, AddressableAssetGroup group, string keyName, string assetPath, string labelName = "")
        {
            // 대상 파일 가져오기
            // var asset = AssetDatabaseLoaderManager.LoadScriptableObject(assetPath);
            // if (!asset)
            // {
            //     Debug.LogError($"파일을 찾을 수 없습니다: {assetPath}");
            //     return null;
            // }

            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            
            // 1) 경로 정규화
            assetPath = assetPath?.Replace('\\', '/');
            
            // 2) 에셋 GUID 확인 (에셋 DB에 없으면 빈 값일 수 있음)
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[Addressables] AssetPathToGUID 실패. 에셋이 Import 되었는지 확인하세요. path={assetPath}");
                return null;
            }
            
            // 3) 엔트리 생성/이동 (Addressables 공식 API)
            // CreateOrMoveEntry: 다른 그룹에 있으면 대상 그룹으로 이동시킴 :contentReference[oaicite:2]{index=2}
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group);
            if (entry == null)
            {
                Debug.LogError($"[Addressables] CreateOrMoveEntry 실패. guid={guid}, path={assetPath}");
                return null;
            }

            // 키 값 설정
            entry.address = keyName;
            
            // 라벨 값 설정
            if (!string.IsNullOrEmpty(labelName))
            {
                entry.SetLabel(labelName, true, true);
            }

            return entry;
            // Debug.Log($"Addressable 키 값 설정: {keyName}");
        }
        protected SpriteAtlas GetOrCreateSpriteAtlas(string path)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                SpriteAtlasPackingSettings packing = atlas.GetPackingSettings();
                packing.enableRotation = false;
                packing.enableTightPacking = false;
                atlas.SetPackingSettings(packing);

                AssetDatabase.CreateAsset(atlas, path);
                AssetDatabase.SaveAssets();
            }
            return atlas;
        }

        protected void AddToListIfExists(List<Object> list, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                list.Add(asset);
            }
        }
        protected void ClearAndAddToAtlas(SpriteAtlas atlas, List<Object> assets)
        {
            atlas.Remove(atlas.GetPackables()); // 기존 등록된 에셋 제거
            if (assets.Count <= 0) return;
            atlas.Add(assets.ToArray());        // 새로 추가
        }

        /// <summary>
        /// 타겟 그룹의 모든 엔트리를 제거합니다. (그룹/스키마는 유지)
        /// </summary>
        protected static void ClearGroupEntries(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            if (settings == null || group == null) return;
            // Undo 지원
            Undo.RecordObject(group, "[SettingAffect] ClearGroupEntries");
            // entries 컬렉션은 수정 중 열거 금지 → 사본 생성
            var entries = group.entries.ToList();
            int removed = entries.Select(entry => settings.RemoveAssetEntry(entry.guid)).Count();

            Debug.Log($"[SettingAffect] 그룹 '{group.Name}' 엔트리 제거: {removed}개");
            EditorUtility.SetDirty(group);
        }

        /// <summary>
        /// 지정한 Addressables 그룹을 삭제합니다. (그룹 자체 제거)
        /// </summary>
        /// <param name="settings">Addressables 설정</param>
        /// <param name="groupName">삭제할 그룹 이름</param>
        /// <param name="showDialog">삭제 전 확인 다이얼로그 표시 여부</param>
        /// <param name="removeEntriesFirst">
        /// true면 그룹 엔트리를 먼저 제거하고 그룹을 삭제합니다.
        /// (엔트리 정리 로그/안전성 목적)
        /// </param>
        /// <returns>삭제 성공 여부</returns>
        protected bool DeleteGroup(
            AddressableAssetSettings settings,
            string groupName,
            bool showDialog = true,
            bool removeEntriesFirst = true)
        {
            if (settings == null)
            {
                Debug.LogError("[SettingAffect] Addressable settings가 null 입니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(groupName))
            {
                Debug.LogWarning("[SettingAffect] groupName이 비어있습니다.");
                return false;
            }

            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                Debug.LogWarning($"[SettingAffect] 삭제할 그룹을 찾을 수 없습니다: {groupName}");
                return false;
            }

            // DefaultGroup은 삭제 시 프로젝트에 영향을 줄 수 있으므로 방어
            if (settings.DefaultGroup == group)
            {
                Debug.LogError(
                    $"[SettingAffect] DefaultGroup('{groupName}')은 삭제할 수 없습니다. DefaultGroup을 다른 그룹으로 변경 후 시도하세요.");
                return false;
            }

            if (showDialog)
            {
                bool ok = EditorUtility.DisplayDialog(
                    TextDisplayDialogTitle,
                    TextDisplayDialogMessage + $"\n\n대상 그룹: {groupName}",
                    "진행",
                    "취소"
                );

                if (!ok)
                    return false;
            }

            Undo.RecordObject(settings, "[SettingAffect] DeleteGroup");

            if (removeEntriesFirst)
            {
                // 그룹 엔트리(Entry)만 먼저 정리 (그룹/스키마는 유지) 후 그룹 삭제
                ClearGroupEntries(settings, group);
            }

            // 그룹 삭제 (true: 그룹 폴더/에셋 파일도 함께 삭제 시도)
            // 필요에 따라 false로 변경 가능
            settings.RemoveGroup(group);

            Debug.Log($"[SettingAffect] 그룹 삭제 완료: {groupName}");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return false;
        }
    }
}