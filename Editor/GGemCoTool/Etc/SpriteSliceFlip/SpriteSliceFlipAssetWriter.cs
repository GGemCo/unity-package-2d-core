#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 좌우 반전된 Sprite Atlas PNG를 저장하고 Import 설정과 Slice 메타데이터를 적용합니다.
    /// </summary>
    internal static class SpriteSliceFlipAssetWriter
    {
        /// <summary>
        /// 좌우 반전된 Texture2D를 PNG로 저장하고 Multiple Sprite로 Import합니다.
        /// </summary>
        /// <param name="sourceTexture">원본 텍스처입니다.</param>
        /// <param name="flippedTexture">좌우 반전된 출력 텍스처입니다.</param>
        /// <param name="processedSlices">출력 PNG에 적용할 Slice 목록입니다.</param>
        /// <param name="settings">출력 및 메타데이터 설정입니다.</param>
        /// <returns>생성된 PNG의 프로젝트 상대 경로입니다.</returns>
        public static string SaveFlippedAtlas(
            Texture2D sourceTexture,
            Texture2D flippedTexture,
            IReadOnlyList<SpriteSliceInfo> processedSlices,
            SpriteSliceFlipSettings settings)
        {
            if (sourceTexture == null)
            {
                throw new ArgumentNullException(nameof(sourceTexture));
            }

            if (flippedTexture == null)
            {
                throw new ArgumentNullException(nameof(flippedTexture));
            }

            if (processedSlices == null || processedSlices.Count == 0)
            {
                throw new InvalidOperationException("저장할 Sprite Slice 정보가 없습니다.");
            }

            settings.Normalize();
            EnsureAssetFolder(settings.outputFolder);

            var assetPath = BuildAssetPath(sourceTexture, settings);
            var absolutePath = ToAbsolutePath(assetPath);
            var pngBytes = flippedTexture.EncodeToPNG();
            File.WriteAllBytes(absolutePath, pngBytes);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(sourceTexture, assetPath);
            SpriteSliceFlipMetadataUtility.WriteSlices(assetPath, processedSlices, settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return assetPath;
        }

        /// <summary>
        /// OS 절대 폴더 경로를 Unity 프로젝트 내부 Assets 상대 경로로 변환합니다.
        /// </summary>
        /// <param name="absoluteFolderPath">변환할 절대 폴더 경로입니다.</param>
        /// <param name="assetFolderPath">변환된 Assets 상대 경로입니다.</param>
        /// <returns>프로젝트 내부 경로로 변환할 수 있으면 true입니다.</returns>
        public static bool TryConvertToAssetFolderPath(string absoluteFolderPath, out string assetFolderPath)
        {
            assetFolderPath = null;
            if (string.IsNullOrWhiteSpace(absoluteFolderPath))
            {
                return false;
            }

            var normalizedAbsolute = absoluteFolderPath.Replace('\\', '/').TrimEnd('/');
            var normalizedAssets = Application.dataPath.Replace('\\', '/').TrimEnd('/');
            if (!IsSameOrChildPath(normalizedAbsolute, normalizedAssets))
            {
                return false;
            }

            var relative = normalizedAbsolute.Length == normalizedAssets.Length
                ? string.Empty
                : normalizedAbsolute.Substring(normalizedAssets.Length).TrimStart('/');
            assetFolderPath = string.IsNullOrEmpty(relative) ? "Assets" : "Assets/" + relative;
            return true;
        }

        /// <summary>
        /// 대상 경로가 기준 경로와 같거나 기준 경로 하위인지 확인합니다.
        /// </summary>
        /// <param name="targetPath">검사할 경로입니다.</param>
        /// <param name="rootPath">기준 경로입니다.</param>
        /// <returns>같은 경로이거나 하위 경로이면 true입니다.</returns>
        private static bool IsSameOrChildPath(string targetPath, string rootPath)
        {
            return string.Equals(targetPath, rootPath, StringComparison.OrdinalIgnoreCase)
                   || targetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 출력 PNG의 프로젝트 상대 경로를 생성합니다.
        /// </summary>
        /// <param name="sourceTexture">원본 텍스처입니다.</param>
        /// <param name="settings">출력 설정입니다.</param>
        /// <returns>생성할 PNG 경로입니다.</returns>
        private static string BuildAssetPath(Texture2D sourceTexture, SpriteSliceFlipSettings settings)
        {
            var fileName = settings.BuildSafeOutputFileNameWithoutExtension(sourceTexture.name) + ".png";
            var assetPath = settings.outputFolder.TrimEnd('/') + "/" + fileName;
            return settings.overwriteExisting ? assetPath : AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }

        /// <summary>
        /// 프로젝트 상대 Assets 경로를 파일 시스템 절대 경로로 변환합니다.
        /// </summary>
        /// <param name="assetPath">프로젝트 상대 Assets 경로입니다.</param>
        /// <returns>파일 시스템 절대 경로입니다.</returns>
        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                throw new InvalidOperationException("Unity 프로젝트 루트 경로를 찾을 수 없습니다.");
            }

            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        /// <summary>
        /// Assets 하위 폴더가 없으면 단계별로 생성합니다.
        /// </summary>
        /// <param name="assetFolder">생성할 프로젝트 상대 폴더 경로입니다.</param>
        private static void EnsureAssetFolder(string assetFolder)
        {
            if (string.IsNullOrWhiteSpace(assetFolder) || assetFolder == "Assets")
            {
                return;
            }

            var normalized = assetFolder.Replace('\\', '/').TrimEnd('/');
            var parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new InvalidOperationException("출력 폴더는 Assets 폴더 하위여야 합니다.");
            }

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                var next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }

                current = next;
            }
        }

        /// <summary>
        /// 원본 TextureImporter 설정을 대상 PNG Importer에 복사합니다.
        /// </summary>
        /// <param name="sourceTexture">원본 텍스처입니다.</param>
        /// <param name="targetAssetPath">대상 PNG의 프로젝트 상대 경로입니다.</param>
        private static void ApplyImportSettings(Texture2D sourceTexture, string targetAssetPath)
        {
            var sourceAssetPath = AssetDatabase.GetAssetPath(sourceTexture);
            var sourceImporter = AssetImporter.GetAtPath(sourceAssetPath) as TextureImporter;
            var targetImporter = AssetImporter.GetAtPath(targetAssetPath) as TextureImporter;
            if (sourceImporter == null)
            {
                throw new InvalidOperationException("원본 TextureImporter를 찾을 수 없습니다: " + sourceAssetPath);
            }

            if (targetImporter == null)
            {
                throw new InvalidOperationException("대상 TextureImporter를 찾을 수 없습니다: " + targetAssetPath);
            }

            SpriteSliceFlipMetadataUtility.CopySpriteImportSettings(sourceImporter, targetImporter);
            targetImporter.SaveAndReimport();
        }
    }
}
#endif
