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
    /// <summary>
    /// Unity Addressables 그룹/엔트리/스프라이트 아틀라스 등록을 위한 공통 유틸리티(기본 클래스)입니다.
    /// </summary>
    /// <remarks>
    /// 에디터 전용 코드이며(Addressables/AssetDatabase/Undo/EditorUtility 사용),
    /// 파생 클래스에서 <see cref="targetGroupName"/>을 지정해 사용하도록 의도되었습니다.
    /// </remarks>
    public class DefaultAddressable
    {
        /// <summary>
        /// 대상 Addressables 그룹 이름입니다. 파생 클래스에서 값을 설정합니다.
        /// </summary>
        protected string targetGroupName = ""; // 그룹 이름

        /// <summary>
        /// 확인 다이얼로그 제목 텍스트입니다.
        /// </summary>
        protected const string TextDisplayDialogTitle = "추가하기";

        /// <summary>
        /// 확인 다이얼로그 메시지 텍스트입니다.
        /// </summary>
        protected const string TextDisplayDialogMessage = "기존에 등록된 내용은 삭제됩니다.\n진행하시겠습니까?";

        /// <summary>
        /// Addressable 설정(<see cref="AddressableAssetSettings"/>)이 없을 경우 새로 생성하고 기본 설정 객체에 연결합니다.
        /// </summary>
        /// <returns>생성된 Addressables 설정 객체입니다.</returns>
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
        /// 기본 Addressable 그룹이 없을 경우 생성하고, 설정의 DefaultGroup으로 지정합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <returns>생성된 기본 그룹입니다.</returns>
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
        /// 지정한 이름의 Addressables 그룹을 가져오며, 존재하지 않으면 기본 그룹의 스키마를 복사해 새로 생성합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="groupName">가져오거나 생성할 그룹 이름입니다.</param>
        /// <returns>기존 또는 새로 생성된 그룹입니다.</returns>
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

        /// <summary>
        /// 지정한 에셋 경로를 Addressables에 등록(또는 다른 그룹에서 이동)하고, address/label을 설정합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">등록 대상 그룹입니다.</param>
        /// <param name="keyName">Addressables address(키)로 사용할 문자열입니다.</param>
        /// <param name="assetPath">프로젝트 내 에셋 경로(예: Assets/...)입니다.</param>
        /// <param name="labelName">설정할 라벨 이름(선택)입니다. 비어있으면 라벨을 설정하지 않습니다.</param>
        /// <returns>등록된 엔트리이며, 실패 시 null을 반환합니다.</returns>
        protected AddressableAssetEntry Add(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string keyName,
            string assetPath,
            string labelName = "")
        {
            // 대상 파일 가져오기
            // var asset = AssetDatabaseLoaderManager.LoadScriptableObject(assetPath);
            // if (!asset)
            // {
            //     Debug.LogError($"파일을 찾을 수 없습니다: {assetPath}");
            //     return null;
            // }

            // 1) 경로 정규화
            assetPath = assetPath?.Replace('\\', '/');
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            // 2) 에셋 GUID 확인 (에셋 DB에 없으면 빈 값일 수 있음)
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[Addressables] AssetPathToGUID 실패. 에셋이 Import 되었는지 확인하세요. path={assetPath}");
                return null;
            }

            // 3) 엔트리 생성/이동
            // - 기존에 다른 그룹에 있으면 대상 그룹으로 이동합니다.
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

        /// <summary>
        /// 지정 경로의 <see cref="SpriteAtlas"/>를 로드하고, 없으면 생성하여 기본 패킹 옵션을 설정한 뒤 에셋으로 저장합니다.
        /// </summary>
        /// <param name="path">아틀라스 에셋 경로(예: Assets/.../Atlas.spriteatlas)입니다.</param>
        /// <returns>기존 또는 새로 생성된 스프라이트 아틀라스입니다.</returns>
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

        /// <summary>
        /// 지정 경로의 에셋이 존재하면 로드하여 리스트에 추가합니다.
        /// </summary>
        /// <param name="list">에셋을 누적할 대상 리스트입니다.</param>
        /// <param name="assetPath">프로젝트 내 에셋 경로(예: Assets/...)입니다.</param>
        protected void AddToListIfExists(List<Object> list, string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset != null)
            {
                list.Add(asset);
            }
        }

        /// <summary>
        /// 스프라이트 아틀라스의 기존 Packables를 제거한 뒤, 전달된 에셋 목록으로 다시 등록합니다.
        /// </summary>
        /// <param name="atlas">대상 스프라이트 아틀라스입니다.</param>
        /// <param name="assets">등록할 에셋 목록입니다.</param>
        protected void ClearAndAddToAtlas(SpriteAtlas atlas, List<Object> assets)
        {
            atlas.Remove(atlas.GetPackables()); // 기존 등록된 에셋 제거
            if (assets.Count <= 0) return;
            atlas.Add(assets.ToArray());        // 새로 추가
        }

        /// <summary>
        /// 타겟 그룹의 모든 엔트리를 제거합니다. (그룹/스키마는 유지)
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">엔트리를 비울 대상 그룹입니다.</param>
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
        /// <exception cref="System.InvalidOperationException">
        /// TODO: 호출 정책에 따라 예외로 처리할지(현재는 로그+false 반환) 결정이 필요합니다.
        /// </exception>
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

            // NOTE: 현재 구현은 성공 시에도 false를 반환하고 있습니다(원본 코드 유지).
            //       호출 측이 반환값을 신뢰한다면 true 반환으로 수정이 필요합니다.
            return false;
        }

        /// <summary>
        /// <see cref="targetGroupName"/> 그룹의 모든 엔트리를 제거합니다. (그룹/스키마는 유지)
        /// </summary>
        /// <param name="ctx">에디터 셋업 로그/컨텍스트 객체입니다.</param>
        public void ClearGroup(EditorSetupContext ctx)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);

            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }

            // 그룹 엔트리 전체 초기화 (스키마/설정은 유지)
            ClearGroupEntries(settings, group);
        }

        /// <summary>
        /// <see cref="targetGroupName"/> 그룹을 삭제합니다.
        /// </summary>
        /// <param name="ctx">에디터 셋업 로그/컨텍스트 객체입니다.</param>
        public void RemoveGroup(EditorSetupContext ctx)
        {
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // 그룹 삭제
            DeleteGroup(settings, targetGroupName);
        }
    }
}
