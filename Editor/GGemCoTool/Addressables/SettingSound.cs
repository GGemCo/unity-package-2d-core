using System;
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
    /// 사운드 Addressables 동기화 실행 옵션입니다.
    /// - 수동 실행 시에는 확인/완료 다이얼로그를 켜고,
    /// - 자동 실행(예: 테이블 저장 후) 시에는 다이얼로그 없이 동작하도록 제어합니다.
    /// </summary>
    public sealed class SettingSoundOptions
    {
        public bool ShowConfirmDialog = true;
        public bool ShowCompletedDialog = true;
        public bool SaveAssets = true;
        public EditorSetupContext Context;
    }

    public class SettingSound : DefaultAddressable
    {
        private sealed class SoundAddressableRow
        {
            public int Uid;
            public SoundConstants.Type Type;
            public SoundConstants.SubType SubType;
            public string FileName;
            public bool UseIntroScene;
        }

        private const string Title = "사운드 추가하기";
        private readonly AddressableEditor _addressableEditor;

        public SettingSound(AddressableEditor addressableEditorWindow)
        {
            _addressableEditor = addressableEditorWindow;
            targetGroupName = $"{ConfigAddressableGroupName.Sound}";
        }

        public void OnGUI()
        {
            if (!File.Exists($"{ConfigAddressableTable.TableSound.Path}"))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableTable.Sound} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(_addressableEditor.buttonWidth),
                        GUILayout.Height(_addressableEditor.buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "사운드 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.",
                            "OK");
                    }
                }
            }
        }

        /// <summary>
        /// 수동 버튼 실행 진입점입니다.
        /// 내부 동작은 <see cref="SyncFromTable"/>를 호출해 공통 처리합니다.
        /// </summary>
        /// <param name="ctx">프로젝트 셋업 컨텍스트입니다. null이면 일반 에디터 다이얼로그 모드로 실행됩니다.</param>
        public void Setup(EditorSetupContext ctx = null)
        {
            SyncFromTable(new SettingSoundOptions
            {
                Context = ctx,
                ShowConfirmDialog = ctx == null,
                ShowCompletedDialog = ctx == null,
                SaveAssets = true,
            });
        }

        /// <summary>
        /// sound_bgm/sound_ambient/sound_sfx 실제 리소스 테이블을 기준으로 Addressables 사운드 그룹을 재구성합니다.
        /// 실제 리소스 테이블이 아직 없으면 레거시 sound.FileName 행을 사용합니다.
        /// </summary>
        /// <param name="options">동기화 옵션입니다. null이면 기본 옵션으로 실행됩니다.</param>
        public static void SyncFromTable(SettingSoundOptions options = null)
        {
            options ??= new SettingSoundOptions();

            if (options.ShowConfirmDialog)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result)
                    return;
            }

            if (!TryPrepareSyncEnvironment(options, out EditorSetupContext ctx, out SettingSound helper, out AddressableAssetSettings settings, out AddressableAssetGroup group))
                return;

            List<SoundAddressableRow> rows = CollectAllSoundAddressableRows(true);
            ClearGroupEntries(settings, group);

            int upsertCount = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                AddressableAssetEntry entry = UpsertSoundEntry(helper, settings, group, rows[i]);
                if (entry != null)
                    upsertCount++;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (options.SaveAssets)
                AssetDatabase.SaveAssets();

            string completedMessage = $"[Addressable] 사운드 설정 완료 (등록/갱신: {upsertCount})";
            if (ctx != null)
            {
                HelperLog.Info(completedMessage, ctx);
            }
            else if (options.ShowCompletedDialog)
            {
                EditorUtility.DisplayDialog(Title, completedMessage, "OK");
            }
        }

        /// <summary>
        /// 레거시 sound 테이블의 변경분만 Addressables에 증분 반영합니다.
        /// 신규 실제 리소스 테이블을 사용하지 않는 프로젝트의 호환 경로입니다.
        /// </summary>
        /// <param name="rowsToUpsert">등록/갱신 대상 사운드 행입니다.</param>
        /// <param name="rowsToRemove">삭제 대상 사운드 행입니다.</param>
        /// <param name="options">동기화 옵션입니다. null이면 기본 옵션으로 실행됩니다.</param>
        public static void SyncFromTableDelta(
            IReadOnlyList<StruckTableSound> rowsToUpsert,
            IReadOnlyList<StruckTableSound> rowsToRemove,
            SettingSoundOptions options = null)
        {
            SyncRowsDelta(ConvertLegacyRows(rowsToUpsert), ConvertLegacyRows(rowsToRemove), options);
        }

        /// <summary>
        /// sound_bgm/sound_ambient/sound_sfx 리소스 테이블의 변경분만 Addressables에 증분 반영합니다.
        /// </summary>
        /// <param name="rowsToUpsert">등록/갱신 대상 실제 리소스 행입니다.</param>
        /// <param name="rowsToRemove">삭제 대상 실제 리소스 행입니다.</param>
        /// <param name="options">동기화 옵션입니다. null이면 기본 옵션으로 실행됩니다.</param>
        public static void SyncResourceDelta(
            IReadOnlyList<StruckTableSoundResource> rowsToUpsert,
            IReadOnlyList<StruckTableSoundResource> rowsToRemove,
            SettingSoundOptions options = null)
        {
            SyncRowsDelta(ConvertResourceRows(rowsToUpsert), ConvertResourceRows(rowsToRemove), options);
        }

        /// <summary>
        /// Addressables 사운드 엔트리 변경분을 공통 DTO 기준으로 반영합니다.
        /// </summary>
        /// <param name="rowsToUpsert">등록/갱신 대상 행입니다.</param>
        /// <param name="rowsToRemove">삭제 대상 행입니다.</param>
        /// <param name="options">동기화 옵션입니다.</param>
        private static void SyncRowsDelta(
            IReadOnlyList<SoundAddressableRow> rowsToUpsert,
            IReadOnlyList<SoundAddressableRow> rowsToRemove,
            SettingSoundOptions options = null)
        {
            options ??= new SettingSoundOptions();

            bool hasUpsert = rowsToUpsert != null && rowsToUpsert.Count > 0;
            bool hasRemove = rowsToRemove != null && rowsToRemove.Count > 0;
            if (!hasUpsert && !hasRemove)
                return;

            if (options.ShowConfirmDialog)
            {
                bool result = EditorUtility.DisplayDialog(
                    TextDisplayDialogTitle,
                    "사운드 Addressables 변경분을 동기화합니다.\n진행하시겠습니까?",
                    "네",
                    "아니요");
                if (!result)
                    return;
            }

            if (!TryPrepareSyncEnvironment(options, out EditorSetupContext ctx, out SettingSound helper, out AddressableAssetSettings settings, out AddressableAssetGroup group))
                return;

            int removedCount = 0;
            if (hasRemove)
            {
                for (int i = 0; i < rowsToRemove.Count; i++)
                {
                    if (TryRemoveSoundEntry(settings, group, rowsToRemove[i]))
                        removedCount++;
                }
            }

            int upsertCount = 0;
            if (hasUpsert)
            {
                for (int i = 0; i < rowsToUpsert.Count; i++)
                {
                    AddressableAssetEntry entry = UpsertSoundEntry(helper, settings, group, rowsToUpsert[i]);
                    if (entry != null)
                        upsertCount++;
                }
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (options.SaveAssets)
                AssetDatabase.SaveAssets();

            string completedMessage = $"[Addressable] 사운드 변경분 동기화 완료 (등록/갱신: {upsertCount}, 삭제: {removedCount})";
            if (ctx != null)
            {
                HelperLog.Info(completedMessage, ctx);
            }
            else if (options.ShowCompletedDialog)
            {
                EditorUtility.DisplayDialog(Title, completedMessage, "OK");
            }
        }

        /// <summary>
        /// 신규 실제 리소스 테이블을 우선 수집하고, 비어 있으면 레거시 sound.FileName 행을 수집합니다.
        /// </summary>
        /// <param name="forceReload">테이블을 강제로 다시 읽을지 여부입니다.</param>
        /// <returns>Addressables에 등록할 사운드 리소스 목록입니다.</returns>
        private static List<SoundAddressableRow> CollectAllSoundAddressableRows(bool forceReload)
        {
            List<SoundAddressableRow> rows = new List<SoundAddressableRow>();

            TableSoundBgm bgmTable = TableLoaderManager.LoadSoundBgmTable(forceReload);
            TableSoundAmbient ambientTable = TableLoaderManager.LoadSoundAmbientTable(forceReload);
            TableSoundSfx sfxTable = TableLoaderManager.LoadSoundSfxTable(forceReload);

            AppendResourceRows(rows, bgmTable?.GetDatas());
            AppendResourceRows(rows, ambientTable?.GetDatas());
            AppendResourceRows(rows, sfxTable?.GetDatas());

            bool hasBgmResources = bgmTable != null && bgmTable.GetCount() > 0;
            bool hasAmbientResources = ambientTable != null && ambientTable.GetCount() > 0;
            bool hasSfxResources = sfxTable != null && sfxTable.GetCount() > 0;

            TableSound legacyTable = TableLoaderManager.LoadSoundTable(forceReload);
            if (legacyTable == null)
                return rows;

            foreach (KeyValuePair<int, StruckTableSound> pair in legacyTable.GetDatas())
            {
                StruckTableSound legacy = pair.Value;
                if (legacy == null)
                    continue;

                if ((legacy.Type == SoundConstants.Type.Bgm && hasBgmResources)
                    || (legacy.Type == SoundConstants.Type.Ambient && hasAmbientResources)
                    || (legacy.Type == SoundConstants.Type.Sfx && hasSfxResources))
                    continue;

                SoundAddressableRow row = ConvertLegacyRow(legacy);
                if (row != null)
                    rows.Add(row);
            }

            return rows;
        }

        /// <summary>
        /// 실제 리소스 테이블 행 사전을 Addressables 등록 목록에 추가합니다.
        /// </summary>
        /// <typeparam name="TResource">사운드 리소스 행 타입입니다.</typeparam>
        /// <param name="target">결과 목록입니다.</param>
        /// <param name="source">테이블 행 사전입니다.</param>
        private static void AppendResourceRows<TResource>(List<SoundAddressableRow> target, IReadOnlyDictionary<int, TResource> source)
            where TResource : StruckTableSoundResource
        {
            if (target == null || source == null)
                return;

            foreach (KeyValuePair<int, TResource> pair in source)
            {
                SoundAddressableRow row = ConvertResourceRow(pair.Value);
                if (row != null)
                    target.Add(row);
            }
        }

        /// <summary>
        /// 증분/전체 동기화에서 공통으로 사용하는 Addressables 설정/그룹을 준비합니다.
        /// 설정이 없으면 생성하고, 사운드 그룹이 없으면 생성합니다.
        /// </summary>
        /// <param name="options">동기화 옵션입니다.</param>
        /// <param name="ctx">실행 컨텍스트입니다.</param>
        /// <param name="helper">사운드 Addressables 헬퍼 인스턴스입니다.</param>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">사운드 대상 그룹입니다.</param>
        /// <returns>동기화 준비에 성공하면 true를 반환합니다.</returns>
        private static bool TryPrepareSyncEnvironment(
            SettingSoundOptions options,
            out EditorSetupContext ctx,
            out SettingSound helper,
            out AddressableAssetSettings settings,
            out AddressableAssetGroup group)
        {
            ctx = options?.Context;
            helper = new SettingSound(null);
            settings = AddressableAssetSettingsDefaultObject.Settings;
            group = null;

            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = helper.CreateAddressableSettings();
            }

            group = helper.GetOrCreateGroup(settings, helper.targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{helper.targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 사운드 리소스 행 1건을 Addressables에 등록/갱신합니다.
        /// 기존 엔트리가 다른 그룹에 있어도 대상 그룹으로 이동되며, 라벨도 함께 갱신됩니다.
        /// </summary>
        /// <param name="helper">사운드 Addressables 헬퍼 인스턴스입니다.</param>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">등록 대상 그룹입니다.</param>
        /// <param name="info">등록할 사운드 리소스 행입니다.</param>
        /// <returns>등록된 엔트리이며, 실패 시 null을 반환합니다.</returns>
        private static AddressableAssetEntry UpsertSoundEntry(
            SettingSound helper,
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            SoundAddressableRow info)
        {
            if (helper == null || settings == null || group == null || info == null || info.Uid <= 0 || string.IsNullOrWhiteSpace(info.FileName))
                return null;

            string key = BuildAddressKey(info.FileName);
            string assetPath = ResolveAssetPath(info);
            AddressableAssetEntry entry = helper.Add(settings, group, key, assetPath, ConfigAddressableLabel.Sound);
            entry?.SetLabel(ConfigAddressableLabel.SoundIntro, info.UseIntroScene, true);
            return entry;
        }

        /// <summary>
        /// 사운드 행 1건에 대응하는 Addressables 엔트리를 제거합니다.
        /// 우선 address 키로 제거를 시도하고, 실패하면 GUID 기반 제거를 시도합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">삭제 대상 그룹입니다.</param>
        /// <param name="info">삭제할 사운드 리소스 행입니다.</param>
        /// <returns>삭제에 성공하면 true를 반환합니다.</returns>
        private static bool TryRemoveSoundEntry(AddressableAssetSettings settings, AddressableAssetGroup group, SoundAddressableRow info)
        {
            if (settings == null || group == null || info == null || info.Uid <= 0 || string.IsNullOrWhiteSpace(info.FileName))
                return false;

            string key = BuildAddressKey(info.FileName);
            if (TryRemoveSoundEntryByAddress(settings, group, key))
                return true;

            string assetPath = ResolveAssetPath(info);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrWhiteSpace(guid))
                return false;

            if (!ContainsGroupEntryGuid(group, guid))
                return false;

            return settings.RemoveAssetEntry(guid);
        }

        /// <summary>
        /// 주소 키가 일치하는 엔트리를 그룹에서 찾아 제거합니다.
        /// </summary>
        /// <param name="settings">Addressables 설정 객체입니다.</param>
        /// <param name="group">검색 대상 그룹입니다.</param>
        /// <param name="address">제거할 address 키입니다.</param>
        /// <returns>삭제에 성공하면 true를 반환합니다.</returns>
        private static bool TryRemoveSoundEntryByAddress(AddressableAssetSettings settings, AddressableAssetGroup group, string address)
        {
            if (settings == null || group == null || string.IsNullOrWhiteSpace(address))
                return false;

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry == null)
                    continue;

                if (!string.Equals(entry.address, address, StringComparison.OrdinalIgnoreCase))
                    continue;

                return settings.RemoveAssetEntry(entry.guid);
            }

            return false;
        }

        /// <summary>
        /// 지정한 GUID 엔트리가 현재 그룹에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="group">검색 대상 그룹입니다.</param>
        /// <param name="guid">검사할 에셋 GUID입니다.</param>
        /// <returns>그룹에 엔트리가 있으면 true를 반환합니다.</returns>
        private static bool ContainsGroupEntryGuid(AddressableAssetGroup group, string guid)
        {
            if (group == null || string.IsNullOrWhiteSpace(guid))
                return false;

            foreach (AddressableAssetEntry entry in group.entries)
            {
                if (entry != null && string.Equals(entry.guid, guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 사운드 행에서 사용하는 Addressables address 키를 생성합니다.
        /// 기존 런타임 조회 규칙(`{SDK}_Sound_{FileName}`)과 동일한 포맷을 유지합니다.
        /// </summary>
        /// <param name="fileName">사운드 파일명(또는 상대 경로)입니다.</param>
        /// <returns>Addressables address 키 문자열입니다.</returns>
        private static string BuildAddressKey(string fileName)
        {
            return $"{ConfigAddressableGroupName.Sound}_{fileName}";
        }

        /// <summary>
        /// 사운드 리소스 행의 FileName 값을 Addressables가 읽을 수 있는 실제 에셋 경로로 해석합니다.
        /// </summary>
        /// <param name="info">사운드 리소스 행 데이터입니다.</param>
        /// <returns>Assets 기준 정규화된 에셋 경로를 반환합니다.</returns>
        private static string ResolveAssetPath(SoundAddressableRow info)
        {
            string rawFileName = info?.FileName ?? string.Empty;
            string normalizedFileName = NormalizePath(rawFileName);

            // 이미 Assets 절대(프로젝트 기준) 경로로 입력된 경우 그대로 사용
            if (Path.IsPathRooted(normalizedFileName) ||
                normalizedFileName.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return normalizedFileName;

            // DataAddressable 루트 기준 상대 경로(Sounds/...)로 입력된 경우
            if (normalizedFileName.StartsWith("Sounds/", StringComparison.OrdinalIgnoreCase))
                return ConfigAddressablePath.Combine(ConfigAddressablePath.Root, normalizedFileName);

            // 기본 규칙: Type/SubType 기반 폴더 + FileName
            string basePath = ConfigAddressablePath.BuildSoundPath(info.Type, info.SubType);
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = ConfigAddressablePath.Sounds;

            return ConfigAddressablePath.Combine(basePath, normalizedFileName);
        }

        /// <summary>
        /// 실제 리소스 행 목록을 Addressables 처리용 DTO 목록으로 변환합니다.
        /// </summary>
        /// <param name="rows">실제 리소스 행 목록입니다.</param>
        /// <returns>Addressables 처리용 DTO 목록입니다.</returns>
        private static List<SoundAddressableRow> ConvertResourceRows(IReadOnlyList<StruckTableSoundResource> rows)
        {
            List<SoundAddressableRow> result = new List<SoundAddressableRow>();
            if (rows == null)
                return result;

            for (int i = 0; i < rows.Count; i++)
            {
                SoundAddressableRow row = ConvertResourceRow(rows[i]);
                if (row != null)
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 실제 리소스 행 1건을 Addressables 처리용 DTO로 변환합니다.
        /// </summary>
        /// <param name="row">실제 리소스 행입니다.</param>
        /// <returns>Addressables 처리용 DTO입니다.</returns>
        private static SoundAddressableRow ConvertResourceRow(StruckTableSoundResource row)
        {
            if (row == null || row.Uid <= 0 || string.IsNullOrWhiteSpace(row.FileName))
                return null;

            return new SoundAddressableRow
            {
                Uid = row.Uid,
                Type = row.Type,
                SubType = row.SubType,
                FileName = row.FileName,
                UseIntroScene = row.UseIntroScene,
            };
        }

        /// <summary>
        /// 레거시 sound 행 목록을 Addressables 처리용 DTO 목록으로 변환합니다.
        /// </summary>
        /// <param name="rows">레거시 sound 행 목록입니다.</param>
        /// <returns>Addressables 처리용 DTO 목록입니다.</returns>
        private static List<SoundAddressableRow> ConvertLegacyRows(IReadOnlyList<StruckTableSound> rows)
        {
            List<SoundAddressableRow> result = new List<SoundAddressableRow>();
            if (rows == null)
                return result;

            for (int i = 0; i < rows.Count; i++)
            {
                SoundAddressableRow row = ConvertLegacyRow(rows[i]);
                if (row != null)
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 레거시 sound 행 1건을 Addressables 처리용 DTO로 변환합니다.
        /// </summary>
        /// <param name="row">레거시 sound 행입니다.</param>
        /// <returns>Addressables 처리용 DTO입니다.</returns>
        private static SoundAddressableRow ConvertLegacyRow(StruckTableSound row)
        {
            if (row == null || row.Uid <= 0 || string.IsNullOrWhiteSpace(row.FileName))
                return null;

            return new SoundAddressableRow
            {
                Uid = row.Uid,
                Type = row.Type,
                SubType = row.SubType,
                FileName = row.FileName,
                UseIntroScene = row.UseIntroScene,
            };
        }

        /// <summary>
        /// 경로 문자열을 슬래시 기준으로 정규화하고 양끝 공백/따옴표를 제거합니다.
        /// </summary>
        /// <param name="path">원본 경로 문자열입니다.</param>
        /// <returns>정규화된 경로 문자열입니다.</returns>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            string trimmed = path.Trim().Trim('"');
            return ConfigAddressablePath.EnsureForwardSlashes(trimmed);
        }
    }
}
