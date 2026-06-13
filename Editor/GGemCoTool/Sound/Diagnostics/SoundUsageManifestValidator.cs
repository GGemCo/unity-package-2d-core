using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 사운드 매니페스트 및 실제 AudioClip/Addressables 연결의 검증 심각도입니다.
    /// </summary>
    public enum SoundUsageValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    /// <summary>
    /// 사운드 매니페스트 검증 메시지 한 건입니다.
    /// </summary>
    public sealed class SoundUsageValidationMessage
    {
        public SoundUsageValidationSeverity Severity;
        public string Message;
    }

    /// <summary>
    /// 사운드 매니페스트 검증 결과입니다.
    /// </summary>
    public sealed class SoundUsageManifestValidationResult
    {
        private readonly List<SoundUsageValidationMessage> _messages =
            new List<SoundUsageValidationMessage>();

        public IReadOnlyList<SoundUsageValidationMessage> Messages => _messages;
        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int ResourceCount { get; internal set; }
        public int ManifestRowCount { get; internal set; }
        public bool IsValid => ErrorCount == 0;

        /// <summary>
        /// 검증 결과에 메시지를 추가하고 심각도별 개수를 갱신합니다.
        /// </summary>
        public void Add(SoundUsageValidationSeverity severity, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messages.Add(new SoundUsageValidationMessage
            {
                Severity = severity,
                Message = message,
            });

            if (severity == SoundUsageValidationSeverity.Error)
                ErrorCount++;
            else if (severity == SoundUsageValidationSeverity.Warning)
                WarningCount++;
        }
    }

    /// <summary>
    /// sound_usage_manifest, 대표 sound, 실제 리소스 및 Addressables 등록 상태를 검사합니다.
    /// </summary>
    public static class SoundUsageManifestValidator
    {
        /// <summary>
        /// 현재 프로젝트의 사운드 매니페스트와 리소스 연결을 전체 검사합니다.
        /// </summary>
        /// <param name="checkStaleness">원본 에셋이 매니페스트보다 최신인지 검사할지 여부입니다.</param>
        /// <returns>오류와 경고를 포함한 검증 결과입니다.</returns>
        public static SoundUsageManifestValidationResult Validate(bool checkStaleness = true)
        {
            SoundUsageManifestValidationResult result = new SoundUsageManifestValidationResult();
            string manifestPath = ConfigAddressableTable.TableSoundUsageManifest.Path;
            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                result.Add(
                    SoundUsageValidationSeverity.Error,
                    $"사운드 사용 매니페스트가 없습니다. 생성 도구를 실행해주세요. path={manifestPath}");
                return result;
            }

            TableSound tableSound = TableLoaderManager.LoadSoundTable(true);
            TableSoundVariant tableVariant = TableLoaderManager.LoadSoundVariantTable(true);
            TableSoundUsageManifest tableManifest =
                TableLoaderManager.LoadSoundUsageManifestTable(true);
            TableMap tableMap = TableLoaderManager.LoadMapTable();
            TableWindow tableWindow = TableLoaderManager.LoadWindowTable();

            if (tableSound == null)
                result.Add(SoundUsageValidationSeverity.Error, "sound 테이블을 로드하지 못했습니다.");
            if (tableManifest == null)
                result.Add(SoundUsageValidationSeverity.Error, "sound_usage_manifest 테이블을 로드하지 못했습니다.");
            if (tableMap == null)
                result.Add(SoundUsageValidationSeverity.Error, "map 테이블을 로드하지 못했습니다.");
            if (tableWindow == null)
                result.Add(SoundUsageValidationSeverity.Error, "window 테이블을 로드하지 못했습니다.");
            if (result.ErrorCount > 0)
                return result;

            IReadOnlyList<StruckTableSoundResource> resources =
                SoundEditorAssetUtility.CollectResourceRows(true);
            result.ResourceCount = resources.Count;
            ValidateResources(resources, result);
            ValidateRepresentativeSounds(tableSound, tableVariant, resources, result);
            ValidateManifestRows(tableManifest, tableSound, tableMap, tableWindow, result);

            if (checkStaleness)
            {
                ValidateSourceFingerprint(result);
                ValidateStaleness(manifestPath, tableManifest, tableMap, tableWindow, result);
            }

            result.Add(
                SoundUsageValidationSeverity.Info,
                $"검증 완료: manifestRows={result.ManifestRowCount}, resources={result.ResourceCount}, errors={result.ErrorCount}, warnings={result.WarningCount}");
            return result;
        }

        /// <summary>
        /// 실제 AudioClip 에셋 존재 여부와 Addressables address 연결을 검사합니다.
        /// </summary>
        private static void ValidateResources(
            IReadOnlyList<StruckTableSoundResource> resources,
            SoundUsageManifestValidationResult result)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                result.Add(SoundUsageValidationSeverity.Error, "Addressables 설정 에셋을 찾지 못했습니다.");
                return;
            }

            Dictionary<string, string> assetPathByAddress =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < resources.Count; i++)
            {
                StruckTableSoundResource row = resources[i];
                string assetPath = SoundEditorAssetUtility.ResolveAssetPath(row);
                string addressKey = row.BuildAddressKey();

                if (string.IsNullOrWhiteSpace(row.FileName) || string.IsNullOrWhiteSpace(assetPath))
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"실제 사운드 리소스의 FileName이 비어 있습니다. type={row.Type}, resourceUid={row.Uid}");
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                if (clip == null)
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"AudioClip 에셋을 찾지 못했습니다. type={row.Type}, resourceUid={row.Uid}, path={assetPath}");
                    continue;
                }

                if (assetPathByAddress.TryGetValue(addressKey, out string registeredAssetPath))
                {
                    if (!string.Equals(registeredAssetPath, assetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(
                            SoundUsageValidationSeverity.Error,
                            $"같은 사운드 address가 서로 다른 에셋을 가리킵니다. address={addressKey}, first={registeredAssetPath}, current={assetPath}");
                    }
                }
                else
                {
                    assetPathByAddress.Add(addressKey, assetPath);
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                AddressableAssetEntry entry = string.IsNullOrWhiteSpace(guid)
                    ? null
                    : settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"AudioClip이 Addressables에 등록되지 않았습니다. resourceUid={row.Uid}, path={assetPath}");
                    continue;
                }

                if (!string.Equals(entry.address, addressKey, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"사운드 Addressables address가 테이블 규칙과 다릅니다. resourceUid={row.Uid}, expected={addressKey}, actual={entry.address}");
                }

                if (entry.labels == null || !entry.labels.Contains(ConfigAddressableLabel.Sound))
                {
                    result.Add(
                        SoundUsageValidationSeverity.Warning,
                        $"사운드 Addressables 엔트리에 공용 Sound 라벨이 없습니다. resourceUid={row.Uid}, address={entry.address}");
                }
            }
        }

        /// <summary>
        /// 대표 sound의 Direct/Variant 실제 리소스 연결을 검사합니다.
        /// </summary>
        private static void ValidateRepresentativeSounds(
            TableSound tableSound,
            TableSoundVariant tableVariant,
            IReadOnlyList<StruckTableSoundResource> resources,
            SoundUsageManifestValidationResult result)
        {
            Dictionary<SoundConstants.Type, Dictionary<int, StruckTableSoundResource>> resourcesByTypeAndUid =
                resources.GroupBy(row => row.Type)
                    .ToDictionary(
                        group => group.Key,
                        group => group.GroupBy(row => row.Uid)
                            .ToDictionary(item => item.Key, item => item.First()));
            Dictionary<SoundConstants.Type, HashSet<int>> soundUidsByType = resources
                .GroupBy(row => row.Type)
                .ToDictionary(group => group.Key, group => new HashSet<int>(group.Select(row => row.SoundUid)));

            foreach (KeyValuePair<int, StruckTableSound> pair in tableSound.GetDatas())
            {
                StruckTableSound sound = pair.Value;
                if (sound == null)
                    continue;

                if (sound.Type == SoundConstants.Type.None)
                {
                    result.Add(SoundUsageValidationSeverity.Error, $"대표 sound의 Type이 None입니다. soundUid={sound.Uid}");
                    continue;
                }

                if (sound.ResolveMode != SoundConstants.ResolveMode.Variant)
                {
                    if (!soundUidsByType.TryGetValue(sound.Type, out HashSet<int> directUids) ||
                        !directUids.Contains(sound.Uid))
                    {
                        result.Add(
                            SoundUsageValidationSeverity.Error,
                            $"Direct 대표 sound에 연결된 실제 리소스가 없습니다. soundUid={sound.Uid}, type={sound.Type}");
                    }
                    continue;
                }

                IReadOnlyList<StruckTableSoundVariant> variants =
                    tableVariant?.GetVariants(sound.Uid) ?? Array.Empty<StruckTableSoundVariant>();
                bool hasEnabledVariant = false;
                bool hasPlayableOrSilentCandidate = false;
                for (int i = 0; i < variants.Count; i++)
                {
                    StruckTableSoundVariant variant = variants[i];
                    if (variant == null || !variant.Enabled)
                        continue;

                    hasEnabledVariant = true;
                    if (variant.CandidateResourceUid <= 0)
                    {
                        hasPlayableOrSilentCandidate = true;
                        continue;
                    }

                    if (!resourcesByTypeAndUid.TryGetValue(sound.Type, out Dictionary<int, StruckTableSoundResource> typedResources) ||
                        !typedResources.ContainsKey(variant.CandidateResourceUid))
                    {
                        result.Add(
                            SoundUsageValidationSeverity.Error,
                            $"Variant 후보가 대표 sound Type의 실제 리소스 테이블에 없습니다. soundUid={sound.Uid}, variantUid={variant.Uid}, resourceUid={variant.CandidateResourceUid}, type={sound.Type}");
                    }
                    else
                    {
                        hasPlayableOrSilentCandidate = true;
                    }
                }

                if (!hasEnabledVariant)
                {
                    bool hasFallback = sound.FallbackResourceUid > 0 &&
                                       resourcesByTypeAndUid.TryGetValue(sound.Type, out Dictionary<int, StruckTableSoundResource> typedResources) &&
                                       typedResources.ContainsKey(sound.FallbackResourceUid);
                    bool hasDirect = soundUidsByType.TryGetValue(sound.Type, out HashSet<int> directUids) &&
                                     directUids.Contains(sound.Uid);
                    if (!hasFallback && !hasDirect)
                    {
                        result.Add(
                            SoundUsageValidationSeverity.Error,
                            $"Variant 대표 sound에 활성 후보, 유효한 폴백 또는 직접 리소스가 없습니다. soundUid={sound.Uid}");
                    }
                }
                else if (!hasPlayableOrSilentCandidate)
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"Variant 대표 sound의 활성 후보를 재생할 수 없습니다. soundUid={sound.Uid}");
                }
            }
        }

        /// <summary>
        /// 매니페스트 범위 UID와 대표 SoundUid가 현재 테이블에 존재하는지 검사합니다.
        /// </summary>
        private static void ValidateManifestRows(
            TableSoundUsageManifest tableManifest,
            TableSound tableSound,
            TableMap tableMap,
            TableWindow tableWindow,
            SoundUsageManifestValidationResult result)
        {
            IReadOnlyDictionary<int, StruckTableSoundUsageManifest> rows = tableManifest.GetDatas();
            result.ManifestRowCount = rows.Count;
            foreach (KeyValuePair<int, StruckTableSoundUsageManifest> pair in rows)
            {
                StruckTableSoundUsageManifest row = pair.Value;
                if (row == null || !row.Enabled)
                    continue;

                if (!tableSound.TryGetDataByUid(row.SoundUid, out StruckTableSound sound) || sound == null)
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"매니페스트가 존재하지 않는 sound UID를 참조합니다. rowUid={row.Uid}, soundUid={row.SoundUid}");
                }

                bool validScope = row.ScopeType switch
                {
                    SoundUsageManifestScopeType.Map => tableMap.TryGetDataByUid(row.ScopeUid, out StruckTableMap map) && map != null,
                    SoundUsageManifestScopeType.UiWindow => tableWindow.TryGetDataByUid(row.ScopeUid, out StruckTableWindow window) && window != null,
                    _ => false,
                };
                if (!validScope)
                {
                    result.Add(
                        SoundUsageValidationSeverity.Error,
                        $"매니페스트 범위 UID가 유효하지 않습니다. rowUid={row.Uid}, scope={row.ScopeType}:{row.ScopeUid}");
                }
            }
        }

        /// <summary>
        /// 생성 시 저장한 원본 에셋 지문과 현재 지문을 비교하여 누락된 재생성 작업을 검출합니다.
        /// </summary>
        private static void ValidateSourceFingerprint(SoundUsageManifestValidationResult result)
        {
            SoundUsageManifestBuildMetadata metadata =
                SoundUsageManifestSourceFingerprint.ReadMetadata();
            if (metadata == null || string.IsNullOrWhiteSpace(metadata.SourceFingerprint))
            {
                result.Add(
                    SoundUsageValidationSeverity.Error,
                    $"사운드 매니페스트 생성 메타데이터가 없습니다. 매니페스트를 다시 생성해주세요. path={SoundUsageManifestSourceFingerprint.MetadataPath}");
                return;
            }

            string currentFingerprint =
                SoundUsageManifestSourceFingerprint.ComputeCurrentFingerprint();
            if (!string.Equals(
                    metadata.SourceFingerprint,
                    currentFingerprint,
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Add(
                    SoundUsageValidationSeverity.Error,
                    $"사운드 분석 원본이 생성 이후 변경되었습니다. 매니페스트를 다시 생성해주세요. generated={metadata.SourceFingerprint}, current={currentFingerprint}");
            }
        }

        /// <summary>
        /// 매니페스트가 원본 테이블 또는 매니페스트에 기록된 원본 에셋보다 오래되었는지 검사합니다.
        /// </summary>
        private static void ValidateStaleness(
            string manifestPath,
            TableSoundUsageManifest tableManifest,
            TableMap tableMap,
            TableWindow tableWindow,
            SoundUsageManifestValidationResult result)
        {
            DateTime manifestTime = File.GetLastWriteTimeUtc(manifestPath);
            HashSet<string> sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ConfigAddressableTable.TableMap.Path,
                ConfigAddressableTable.TableMapSound.Path,
                ConfigAddressableTable.TableMonster.Path,
                ConfigAddressableTable.TableNpc.Path,
                ConfigAddressableTable.TableAnimation.Path,
                ConfigAddressableTable.TableWindow.Path,
                ConfigAddressableTable.TableSound.Path,
                ConfigAddressableTable.TableSoundBgm.Path,
                ConfigAddressableTable.TableSoundAmbient.Path,
                ConfigAddressableTable.TableSoundSfx.Path,
                ConfigAddressableTable.TableSoundVariant.Path,
            };

            IReadOnlyDictionary<int, StruckTableMap> maps = tableMap?.GetAll();
            if (maps != null)
            {
                foreach (KeyValuePair<int, StruckTableMap> pair in maps)
                {
                    StruckTableMap map = pair.Value;
                    if (map == null || string.IsNullOrWhiteSpace(map.FolderName))
                        continue;

                    sourcePaths.Add(ConfigAddressableMap.GetAssetPathRegenMonster(map.FolderName));
                    sourcePaths.Add(ConfigAddressableMap.GetAssetPathRegenNpc(map.FolderName));
                }
            }

            IReadOnlyDictionary<int, StruckTableWindow> windows = tableWindow?.GetAll();
            if (windows != null)
            {
                foreach (KeyValuePair<int, StruckTableWindow> pair in windows)
                {
                    StruckTableWindow window = pair.Value;
                    if (window == null || !window.UseInGame || string.IsNullOrWhiteSpace(window.PrefabName))
                        continue;

                    string prefabName = Path.GetFileNameWithoutExtension(window.PrefabName);
                    string folderName = prefabName.Replace("UIWindow", string.Empty);
                    sourcePaths.Add($"{ConfigEditor.PathUIWindow}/{folderName}/{prefabName}.prefab");
                }
            }

            foreach (KeyValuePair<int, StruckTableSoundUsageManifest> pair in tableManifest.GetDatas())
            {
                string sourcePath = ExtractAssetPath(pair.Value?.SourcePath);
                if (!string.IsNullOrWhiteSpace(sourcePath))
                    sourcePaths.Add(sourcePath);
            }

            List<string> newerSources = new List<string>();
            foreach (string sourcePath in sourcePaths)
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                    continue;

                if (File.GetLastWriteTimeUtc(sourcePath) > manifestTime)
                    newerSources.Add(sourcePath);
            }

            if (newerSources.Count > 0)
            {
                string preview = string.Join(", ", newerSources.Take(5));
                result.Add(
                    SoundUsageValidationSeverity.Error,
                    $"매니페스트보다 최신인 원본이 있습니다. 매니페스트를 다시 생성해주세요. count={newerSources.Count}, sources={preview}");
            }
        }

        /// <summary>
        /// 매니페스트 SourcePath의 # 이하 세부 위치를 제거하여 실제 에셋 경로만 반환합니다.
        /// </summary>
        private static string ExtractAssetPath(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return string.Empty;

            string normalized = sourcePath.Replace(@"\n", string.Empty).Trim();
            int fragmentIndex = normalized.IndexOf('#');
            if (fragmentIndex >= 0)
                normalized = normalized.Substring(0, fragmentIndex);

            return normalized;
        }
    }
}
