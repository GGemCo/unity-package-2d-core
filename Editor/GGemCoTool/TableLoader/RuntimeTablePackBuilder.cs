using System.Collections.Generic;
using System.IO;
using System.Text;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터에서 개별 txt 테이블을 패키지별 런타임 테이블 팩(.bytes)으로 생성합니다.
    /// </summary>
    /// <remarks>
    /// 원본 txt 파일은 그대로 유지하고, 런타임 Addressables 요청 수를 줄이기 위한 파생 산출물만 생성합니다.
    /// </remarks>
    public static class RuntimeTablePackBuilder
    {
        /// <summary>
        /// 지정한 테이블 목록을 하나의 런타임 테이블 팩 파일로 저장합니다.
        /// </summary>
        /// <param name="packageId">패키지 식별자입니다. 예: core, skill, affect.</param>
        /// <param name="packInfo">생성할 팩 파일의 Addressables 정보입니다.</param>
        /// <param name="tables">팩에 포함할 개별 테이블 목록입니다.</param>
        /// <param name="ctx">자동 설정 실행 컨텍스트입니다.</param>
        /// <returns>팩 파일 생성에 성공하면 true를 반환합니다.</returns>
        public static bool Build(
            string packageId,
            AddressableAssetInfo packInfo,
            IReadOnlyList<AddressableAssetInfo> tables,
            EditorSetupContext ctx = null)
        {
            if (packInfo == null)
            {
                HelperLog.Error("[RuntimeTablePackBuilder] packInfo가 없습니다.", ctx);
                return false;
            }

            var entries = new List<RuntimeTablePackEntry>();
            int missingCount = 0;

            if (tables != null)
            {
                for (int i = 0; i < tables.Count; i++)
                {
                    AddressableAssetInfo table = tables[i];
                    if (table == null || string.IsNullOrWhiteSpace(table.Path))
                    {
                        missingCount++;
                        continue;
                    }

                    string tablePath = ConfigAddressablePath.EnsureForwardSlashes(table.Path);
                    if (!File.Exists(tablePath))
                    {
                        missingCount++;
                        HelperLog.Warn($"테이블 파일을 찾을 수 없어 팩에서 제외합니다. path={tablePath}", ctx);
                        continue;
                    }

                    // 기존 파서가 기대하는 txt 원문을 그대로 보존합니다.
                    string content = File.ReadAllText(tablePath, Encoding.UTF8);
                    entries.Add(RuntimeTablePackEntry.FromAddressableInfo(table, content));
                }
            }

            if (entries.Count == 0)
            {
                HelperLog.Error($"테이블 팩에 포함할 파일이 없습니다. package={packageId}", ctx);
                return false;
            }

            string packPath = ConfigAddressablePath.EnsureForwardSlashes(packInfo.Path);
            string directory = ConfigAddressablePath.EnsureForwardSlashes(Path.GetDirectoryName(packPath));
            if (!string.IsNullOrEmpty(directory))
            {
                EnsureAssetFolder(directory);
            }

            byte[] bytes = RuntimeTablePackCodec.Encode(packageId, entries);
            File.WriteAllBytes(packPath, bytes);
            AssetDatabase.ImportAsset(packPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            if (missingCount > 0)
            {
                HelperLog.Warn($"테이블 팩 생성 완료. 일부 테이블은 누락되어 제외되었습니다. package={packageId}, missing={missingCount}", ctx);
            }
            else
            {
                HelperLog.Info($"테이블 팩 생성 완료. package={packageId}, count={entries.Count}", ctx);
            }

            return true;
        }

        /// <summary>
        /// 지정한 Assets 하위 폴더가 없으면 Unity AssetDatabase를 통해 단계적으로 생성합니다.
        /// </summary>
        /// <param name="assetFolderPath">생성할 프로젝트 상대 폴더 경로입니다. 예: Assets/GGemCo/DataAddressable/TablePacks.</param>
        private static void EnsureAssetFolder(string assetFolderPath)
        {
            string normalized = ConfigAddressablePath.EnsureForwardSlashes(assetFolderPath);
            if (string.IsNullOrEmpty(normalized) || AssetDatabase.IsValidFolder(normalized))
                return;

            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
