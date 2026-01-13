using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터/툴링에서 사용하는 파일 및 디렉터리 복사 유틸리티를 제공합니다.
    /// Unity 프로젝트에서 에셋 복사 시 .meta 파일 포함 여부를 선택할 수 있습니다.
    /// </summary>
    public static class HelperFile
    {
        /// <summary>
        /// 원본 디렉터리의 모든 파일과 하위 디렉터리를 대상 디렉터리로 재귀적으로 복사합니다.
        /// </summary>
        /// <param name="sourceDir">복사할 원본 디렉터리 경로</param>
        /// <param name="targetDir">복사 대상 디렉터리 경로</param>
        /// <param name="copyMetafile">true면 .meta 파일도 함께 복사합니다.</param>
        /// <param name="forceUpdate">강제로 Directory를 동기화 할 것인지</param>
        /// <remarks>
        /// - 대상 디렉터리가 없으면 생성합니다.
        /// - sourceDir 하위의 파일/폴더 구조를 targetDir에 그대로 반영합니다.
        /// - Unity 에셋 복사에서 .meta 파일을 포함하면 GUID가 유지되므로, 의도에 따라 copyMetafile을 선택해야 합니다.
        /// </remarks>
        /// <exception cref="DirectoryNotFoundException">sourceDir가 존재하지 않는 경우</exception>
        /// <exception cref="IOException">파일/디렉터리 접근 중 IO 오류가 발생한 경우</exception>
        /// <exception cref="UnauthorizedAccessException">권한 문제로 접근할 수 없는 경우</exception>
        public static void CopyDirectory(string sourceDir, string targetDir, bool copyMetafile = true, bool forceUpdate = false)
        {
            // NOTE: Directory.GetFiles/GetDirectories는 sourceDir이 없으면 DirectoryNotFoundException을 던집니다.
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);

                if (forceUpdate)
                {
                    if (TryGetAssetsPath(targetDir, out string assetsPath))
                        AssetDatabase.ImportAsset(assetsPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
            }
            
            // 파일 목록을 정렬: .meta는 항상 뒤로
            var files = Directory.GetFiles(sourceDir);
            Array.Sort(files, (a, b) =>
            {
                bool am = a.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
                bool bm = b.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
                if (am == bm) return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                return am ? 1 : -1; // meta는 뒤
            });
            
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                if (!copyMetafile && file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                CopyFile(file, destFile, forceUpdate);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);
                string targetSubDir = Path.Combine(targetDir, dirName);
                CopyDirectory(directory, targetSubDir, copyMetafile, forceUpdate);
            }
        }

        /// <summary>
        /// 파일을 복사합니다. 대상 폴더가 없으면 생성하며, 동일 경로 복사를 방지합니다.
        /// 필요 시 원자적으로 교체하여(가능한 경우) 부분 복사/충돌 위험을 줄입니다.
        /// </summary>
        /// <param name="sourceFile">복사할 원본 파일 경로</param>
        /// <param name="targetFile">복사 대상 파일 경로</param>
        /// <param name="forceUpdate">강제로 파일을 동기화 처리 할 것인지</param>
        /// <remarks>
        /// 동작:
        /// - sourceFile/targetFile을 절대 경로로 정규화한 뒤 동일 경로면 아무 것도 하지 않습니다.
        /// - 대상 파일이 존재하고 ReadOnly면 가능한 경우 속성을 해제한 뒤 진행합니다.
        /// - 우선 임시 파일(.tmp)로 복사 후, 가능하면 File.Replace/Move로 교체하여 원자성을 확보합니다.
        /// - 교체가 실패하면 마지막으로 File.Copy(overwrite: true)로 fallback 합니다.
        /// </remarks>
        /// <exception cref="ArgumentException">sourceFile 또는 targetFile이 null/공백인 경우</exception>
        /// <exception cref="FileNotFoundException">원본 파일이 존재하지 않는 경우</exception>
        /// <exception cref="IOException">복사/교체 중 IO 오류가 발생한 경우</exception>
        /// <exception cref="UnauthorizedAccessException">권한 문제로 접근할 수 없는 경우</exception>
        public static void CopyFile(string sourceFile, string targetFile, bool forceUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                throw new ArgumentException("sourceFile is null or empty.", nameof(sourceFile));
            if (string.IsNullOrWhiteSpace(targetFile))
                throw new ArgumentException("targetFile is null or empty.", nameof(targetFile));

            // 절대 경로로 정규화 (동일 파일 판단/안정성)
            string srcPath = Path.GetFullPath(sourceFile);
            string dstPath = Path.GetFullPath(targetFile);

            if (!File.Exists(srcPath))
                throw new FileNotFoundException($"Source file not found: {srcPath}", srcPath);

            // 동일 경로(또는 같은 파일) 복사 방지
            if (string.Equals(srcPath, dstPath, StringComparison.OrdinalIgnoreCase))
                return;

            // 대상 폴더 생성
            string dstDir = Path.GetDirectoryName(dstPath);
            if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
            {
                Directory.CreateDirectory(dstDir);
                if (forceUpdate)
                {
                    if (TryGetAssetsPath(dstDir, out string assetsPath))
                        AssetDatabase.ImportAsset(assetsPath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }
            }

            // 대상이 ReadOnly일 수 있으니 제거 시도(가능한 경우)
            if (File.Exists(dstPath))
            {
                var attrs = File.GetAttributes(dstPath);
                if ((attrs & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(dstPath, attrs & ~FileAttributes.ReadOnly);
            }

            // 가능한 경우 "원자적 교체" 시도: 같은 볼륨이면 File.Replace가 안전
            // (다른 볼륨이면 Replace가 실패할 수 있어 fallback)
            string tempPath = dstPath + ".tmp";

            try
            {
                // 1) 임시 파일로 복사
                File.Copy(srcPath, tempPath, true);

                // 2) 임시 파일을 목적지로 교체
                if (File.Exists(dstPath))
                {
                    // backup 없이 교체(backup 경로가 필요하면 확장 가능)
                    File.Replace(tempPath, dstPath, null);
                }
                else
                {
                    File.Move(tempPath, dstPath);
                }
                // meta는 복사만 하고 ImportAsset은 하지 않음
                bool isMeta = dstPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
                if (forceUpdate && !isMeta)
                {
                    if (TryGetAssetsPath(dstPath, out string assetsPath))
                    {
                        AssetDatabase.ImportAsset(assetsPath,
                            ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        // Debug.Log($"강제 새로 고침. path: {assetsPath}");
                    }
                }
            }
            catch (Exception)
            {
                // fallback: 단순 Copy로 재시도 (Replace/Move 실패 케이스 대비)
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore
                }

                File.Copy(srcPath, dstPath, true);
            }
            finally
            {
                // 남아있는 임시 파일 정리
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                    // ignore
                }
            }
        }
        /// <summary>
        /// OS 절대 경로를 Unity AssetDatabase용 경로("Assets/..." 형태)로 변환합니다.
        /// </summary>
        /// <param name="fullPath">
        /// Path.GetFullPath 로 얻은 절대 경로
        /// </param>
        /// <param name="assetPath">
        /// 변환된 Asset 경로 ("Assets/xxx")
        /// </param>
        /// <returns>
        /// Assets 폴더 내부일 경우 true, 아니면 false
        /// </returns>
        public static bool TryGetAssetsPath(string fullPath, out string assetPath)
        {
            assetPath = null;

            if (string.IsNullOrEmpty(fullPath))
                return false;

            // OS 경로 정규화
            fullPath = Path.GetFullPath(fullPath)
                .Replace('\\', '/');

            // Application.dataPath == "<Project>/Assets"
            string assetsRoot = Path.GetFullPath(Application.dataPath)
                .Replace('\\', '/');

            if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            // "<Project>/Assets/xxx.png" → "Assets/xxx.png"
            assetPath = "Assets" + fullPath.Substring(assetsRoot.Length);
            return true;
        }
    }
}
