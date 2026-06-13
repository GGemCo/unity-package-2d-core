using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 사운드 매니페스트 생성 시점의 원본 에셋 지문을 저장하는 메타데이터입니다.
    /// </summary>
    [Serializable]
    internal sealed class SoundUsageManifestBuildMetadata
    {
        public string GeneratedAtUtc;
        public string SourceFingerprint;
        public int SourceCount;
        public string UnityVersion;
    }

    /// <summary>
    /// 맵 배치, 캐릭터/UI 프리팹, 스킬 RuntimeSequence 및 관련 테이블의 변경 여부를 계산합니다.
    /// </summary>
    internal static class SoundUsageManifestSourceFingerprint
    {
        /// <summary>
        /// 매니페스트 테이블 옆에 저장할 생성 메타데이터 경로입니다.
        /// </summary>
        public static string MetadataPath =>
            $"{ConfigAddressableTable.TableSoundUsageManifest.Path}.build.json";

        /// <summary>
        /// 현재 프로젝트에서 사운드 매니페스트 분석에 영향을 줄 수 있는 원본 경로와 지문을 계산합니다.
        /// </summary>
        /// <returns>원본 개수와 결합 지문을 포함한 메타데이터입니다.</returns>
        public static SoundUsageManifestBuildMetadata CreateMetadata()
        {
            IReadOnlyList<string> sourcePaths = CollectSourcePaths();
            return new SoundUsageManifestBuildMetadata
            {
                GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                SourceFingerprint = ComputeFingerprint(sourcePaths),
                SourceCount = sourcePaths.Count,
                UnityVersion = Application.unityVersion,
            };
        }

        /// <summary>
        /// 메타데이터를 UTF-8 BOM 없는 JSON으로 저장하고 Unity AssetDatabase에 반영합니다.
        /// </summary>
        /// <param name="metadata">저장할 원본 지문 메타데이터입니다.</param>
        public static void WriteMetadata(SoundUsageManifestBuildMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            string directory = Path.GetDirectoryName(MetadataPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(MetadataPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(
                MetadataPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// 저장된 생성 메타데이터를 읽습니다.
        /// </summary>
        /// <returns>정상적으로 읽은 메타데이터이며 파일이 없거나 손상되었으면 null입니다.</returns>
        public static SoundUsageManifestBuildMetadata ReadMetadata()
        {
            if (!File.Exists(MetadataPath))
                return null;

            try
            {
                return JsonUtility.FromJson<SoundUsageManifestBuildMetadata>(
                    File.ReadAllText(MetadataPath, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SoundUsageManifest] 생성 메타데이터를 읽지 못했습니다. error={ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 현재 원본 에셋 지문을 다시 계산합니다.
        /// </summary>
        /// <returns>정렬된 원본 경로에 대한 SHA-256 결합 지문입니다.</returns>
        public static string ComputeCurrentFingerprint()
        {
            return ComputeFingerprint(CollectSourcePaths());
        }

        /// <summary>
        /// 매니페스트 분석에 영향을 줄 수 있는 테이블과 에셋 경로를 중복 없이 수집합니다.
        /// </summary>
        private static IReadOnlyList<string> CollectSourcePaths()
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddKnownTablePaths(paths);
            AddMapPlacementPaths(paths);
            AddAssetSearchResults(paths, "t:Prefab", ConfigAddressablePath.Combine(ConfigAddressablePath.Root, "Characters"));
            AddAssetSearchResults(paths, "t:Prefab", ConfigEditor.PathUIWindow);
            AddAssetSearchResults(paths, "t:SkillRuntimeSequence", "Assets");
            AddSkillTablePaths(paths);
            return paths.Where(File.Exists).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Core의 맵, 캐릭터, UI 및 사운드 관련 테이블 경로를 추가합니다.
        /// </summary>
        private static void AddKnownTablePaths(HashSet<string> paths)
        {
            AddressableAssetInfo[] infos =
            {
                ConfigAddressableTable.TableMap,
                ConfigAddressableTable.TableMapSound,
                ConfigAddressableTable.TableMonster,
                ConfigAddressableTable.TableNpc,
                ConfigAddressableTable.TableAnimation,
                ConfigAddressableTable.TableWindow,
                ConfigAddressableTable.TableSound,
                ConfigAddressableTable.TableSoundBgm,
                ConfigAddressableTable.TableSoundAmbient,
                ConfigAddressableTable.TableSoundSfx,
                ConfigAddressableTable.TableSoundVariant,
            };

            for (int i = 0; i < infos.Length; i++)
            {
                string path = infos[i]?.Path;
                if (!string.IsNullOrWhiteSpace(path))
                    paths.Add(path);
            }
        }

        /// <summary>
        /// 모든 맵의 몬스터와 NPC 배치 JSON 경로를 추가합니다.
        /// </summary>
        private static void AddMapPlacementPaths(HashSet<string> paths)
        {
            TableMap tableMap = TableLoaderManager.LoadMapTable();
            IReadOnlyDictionary<int, StruckTableMap> maps = tableMap?.GetAll();
            if (maps == null)
                return;

            foreach (KeyValuePair<int, StruckTableMap> pair in maps)
            {
                StruckTableMap map = pair.Value;
                if (map == null || string.IsNullOrWhiteSpace(map.FolderName))
                    continue;

                paths.Add(ConfigAddressableMap.GetAssetPathRegenMonster(map.FolderName));
                paths.Add(ConfigAddressableMap.GetAssetPathRegenNpc(map.FolderName));
            }
        }

        /// <summary>
        /// 지정한 폴더에서 AssetDatabase 검색 결과와 각 에셋의 직접 종속성을 추가합니다.
        /// </summary>
        private static void AddAssetSearchResults(
            HashSet<string> paths,
            string filter,
            string searchFolder)
        {
            if (string.IsNullOrWhiteSpace(searchFolder) || !AssetDatabase.IsValidFolder(searchFolder))
                return;

            string[] guids = AssetDatabase.FindAssets(filter, new[] { searchFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                paths.Add(path);
                string[] dependencies = AssetDatabase.GetDependencies(path, true);
                for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
                {
                    string dependency = dependencies[dependencyIndex];
                    if (!string.IsNullOrWhiteSpace(dependency))
                        paths.Add(dependency);
                }
            }
        }

        /// <summary>
        /// 설치된 Skill 패키지가 사용하는 몬스터 스킬 및 차징 단계 테이블을 이름 기반으로 추가합니다.
        /// Core는 Skill 타입을 참조하지 않고 파일명만 검사합니다.
        /// </summary>
        private static void AddSkillTablePaths(HashSet<string> paths)
        {
            if (!Directory.Exists(ConfigAddressablePath.Tables))
                return;

            string[] files = Directory.GetFiles(
                ConfigAddressablePath.Tables,
                "*.txt",
                SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(files[i]);
                if (!string.Equals(fileName, "skill_monster", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "skill_charge_stage", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                paths.Add(ConfigAddressablePath.EnsureForwardSlashes(files[i]));
            }
        }


        /// <summary>
        /// AssetDatabase 종속성 해시만으로 변경을 감지하기 어려운 텍스트 원본인지 확인합니다.
        /// </summary>
        private static bool IsTextSource(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 텍스트 원본 파일의 바이트 내용을 SHA-256으로 계산합니다.
        /// 파일 시간은 사용하지 않아 작업자와 CI 환경 간 결과가 동일하게 유지됩니다.
        /// </summary>
        private static string ComputeFileContentHash(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return string.Empty;

            using SHA256 sha256 = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            byte[] hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }

        /// <summary>
        /// 각 원본의 AssetDatabase 종속성 해시와 텍스트 내용 해시를 정렬하여 SHA-256 지문으로 결합합니다.
        /// </summary>
        private static string ComputeFingerprint(IReadOnlyList<string> sourcePaths)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < sourcePaths.Count; i++)
            {
                string path = sourcePaths[i];
                Hash128 dependencyHash = AssetDatabase.GetAssetDependencyHash(path);
                string contentHash = IsTextSource(path)
                    ? ComputeFileContentHash(path)
                    : string.Empty;

                builder.Append(path).Append('|')
                    .Append(dependencyHash).Append('|')
                    .Append(contentHash).Append('\n');
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
