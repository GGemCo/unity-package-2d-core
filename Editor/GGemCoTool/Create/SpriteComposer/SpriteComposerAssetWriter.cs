using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Composer 결과 텍스처를 PNG 파일로 저장하고 Sprite Import 설정을 적용합니다.
    /// </summary>
    internal static class SpriteComposerAssetWriter
    {
        /// <summary>
        /// Texture2D를 PNG로 저장한 뒤 Unity Sprite 에셋으로 임포트합니다.
        /// </summary>
        /// <param name="texture">저장할 합성 결과 텍스처입니다.</param>
        /// <param name="settings">저장 및 임포트 설정입니다.</param>
        /// <param name="effectivePixelsPerUnit">Sprite Import에 적용할 실제 PPU 값입니다.</param>
        /// <returns>생성된 에셋의 프로젝트 상대 경로입니다.</returns>
        public static string SaveTextureAsSprite(Texture2D texture, SpriteComposerSettings settings, float effectivePixelsPerUnit)
        {
            if (texture == null)
            {
                throw new ArgumentNullException(nameof(texture));
            }

            settings.Normalize();
            EnsureAssetFolder(settings.OutputFolder);

            var assetPath = BuildAssetPath(settings);
            var absolutePath = ToAbsolutePath(assetPath);
            var pngBytes = texture.EncodeToPNG();
            File.WriteAllBytes(absolutePath, pngBytes);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ApplySpriteImportSettings(assetPath, settings, effectivePixelsPerUnit);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return assetPath;
        }

        /// <summary>
        /// OS 절대 폴더 경로를 Unity 프로젝트 내부 Assets 경로로 변환합니다.
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
            if (!normalizedAbsolute.StartsWith(normalizedAssets, StringComparison.Ordinal))
            {
                return false;
            }

            var relative = normalizedAbsolute.Substring(normalizedAssets.Length).TrimStart('/');
            assetFolderPath = string.IsNullOrEmpty(relative) ? "Assets" : "Assets/" + relative;
            return true;
        }

        /// <summary>
        /// 저장할 PNG 에셋 경로를 생성합니다.
        /// </summary>
        /// <param name="settings">출력 폴더와 파일 이름 설정입니다.</param>
        /// <returns>프로젝트 상대 에셋 경로입니다.</returns>
        private static string BuildAssetPath(SpriteComposerSettings settings)
        {
            var fileName = settings.GetSafeFileNameWithoutExtension() + ".png";
            var assetPath = settings.OutputFolder.TrimEnd('/') + "/" + fileName;
            return settings.OverwriteExisting ? assetPath : AssetDatabase.GenerateUniqueAssetPath(assetPath);
        }

        /// <summary>
        /// 프로젝트 상대 Assets 경로를 파일 시스템 절대 경로로 변환합니다.
        /// </summary>
        /// <param name="assetPath">프로젝트 상대 Assets 경로입니다.</param>
        /// <returns>파일 시스템 절대 경로입니다.</returns>
        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
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

            var parts = assetFolder.Replace('\\', '/').Split('/');
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
        /// 저장된 PNG 파일에 Sprite Import 설정을 적용합니다.
        /// </summary>
        /// <param name="assetPath">프로젝트 상대 PNG 경로입니다.</param>
        /// <param name="settings">필터 모드 등 Import 설정입니다.</param>
        /// <param name="effectivePixelsPerUnit">Sprite Import에 적용할 PPU 값입니다.</param>
        private static void ApplySpriteImportSettings(string assetPath, SpriteComposerSettings settings, float effectivePixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("TextureImporter를 찾을 수 없습니다: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Mathf.Max(1f, effectivePixelsPerUnit);
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = settings.FilterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
