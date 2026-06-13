using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 사운드 테이블 행을 실제 AudioClip 에셋과 Addressables 엔트리로 해석하는 에디터 공용 유틸리티입니다.
    /// </summary>
    internal static class SoundEditorAssetUtility
    {
        /// <summary>
        /// sound_bgm, sound_ambient, sound_sfx 테이블의 실제 리소스 행을 한 목록으로 수집합니다.
        /// </summary>
        /// <param name="forceReload">테이블을 강제로 다시 읽을지 여부입니다.</param>
        /// <returns>UID가 유효한 실제 사운드 리소스 행 목록입니다.</returns>
        public static IReadOnlyList<StruckTableSoundResource> CollectResourceRows(bool forceReload)
        {
            List<StruckTableSoundResource> result = new List<StruckTableSoundResource>();
            AppendRows(result, TableLoaderManager.LoadSoundBgmTable(forceReload)?.GetDatas());
            AppendRows(result, TableLoaderManager.LoadSoundAmbientTable(forceReload)?.GetDatas());
            AppendRows(result, TableLoaderManager.LoadSoundSfxTable(forceReload)?.GetDatas());
            return result;
        }

        /// <summary>
        /// 사운드 리소스 행의 FileName을 Unity 프로젝트 기준 에셋 경로로 변환합니다.
        /// </summary>
        /// <param name="row">경로를 계산할 실제 사운드 리소스 행입니다.</param>
        /// <returns>Assets로 시작하는 정규화된 경로입니다.</returns>
        public static string ResolveAssetPath(StruckTableSoundResource row)
        {
            if (row == null)
                return string.Empty;

            string normalizedFileName = NormalizePath(row.FileName);
            if (string.IsNullOrWhiteSpace(normalizedFileName))
                return string.Empty;

            if (Path.IsPathRooted(normalizedFileName) ||
                normalizedFileName.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFileName;
            }

            if (normalizedFileName.StartsWith("Sounds/", StringComparison.OrdinalIgnoreCase))
                return ConfigAddressablePath.Combine(ConfigAddressablePath.Root, normalizedFileName);

            string basePath = ConfigAddressablePath.BuildSoundPath(row.Type, row.SubType);
            if (string.IsNullOrWhiteSpace(basePath))
                basePath = ConfigAddressablePath.Sounds;

            return ConfigAddressablePath.Combine(basePath, normalizedFileName);
        }

        /// <summary>
        /// 지정한 Addressables 설정에서 address 문자열이 일치하는 엔트리를 찾습니다.
        /// </summary>
        /// <param name="settings">검색할 Addressables 설정입니다.</param>
        /// <param name="address">검색할 address 값입니다.</param>
        /// <returns>일치하는 엔트리이며 없으면 null입니다.</returns>
        public static AddressableAssetEntry FindEntryByAddress(
            AddressableAssetSettings settings,
            string address)
        {
            if (settings == null || string.IsNullOrWhiteSpace(address))
                return null;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry != null && string.Equals(
                            entry.address,
                            address,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 실제 사운드 리소스 행 사전을 공용 목록에 추가합니다.
        /// </summary>
        private static void AppendRows<TResource>(
            List<StruckTableSoundResource> target,
            IReadOnlyDictionary<int, TResource> rows)
            where TResource : StruckTableSoundResource
        {
            if (target == null || rows == null)
                return;

            foreach (KeyValuePair<int, TResource> pair in rows)
            {
                TResource row = pair.Value;
                if (row != null && row.Uid > 0)
                    target.Add(row);
            }
        }

        /// <summary>
        /// 경로 문자열의 따옴표, 공백 및 디렉터리 구분자를 정규화합니다.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return ConfigAddressablePath.EnsureForwardSlashes(path.Trim().Trim('"'));
        }
    }
}
