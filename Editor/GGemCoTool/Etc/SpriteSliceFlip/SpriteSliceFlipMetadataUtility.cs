#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Editor에 저장된 Slice 메타데이터를 읽고 쓰는 유틸리티입니다.
    /// </summary>
    internal static class SpriteSliceFlipMetadataUtility
    {
        /// <summary>
        /// Texture2D에서 Sprite Editor Slice 정보를 읽어옵니다.
        /// </summary>
        /// <param name="sourceTexture">Slice 정보를 읽을 원본 텍스처입니다.</param>
        /// <returns>원본 텍스처에 저장된 Slice 정보 목록입니다.</returns>
        public static IReadOnlyList<SpriteSliceInfo> ReadSlices(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                throw new ArgumentNullException(nameof(sourceTexture));
            }

            var dataProvider = CreateDataProvider(sourceTexture);
            dataProvider.InitSpriteEditorDataProvider();
            var spriteRects = dataProvider.GetSpriteRects();
            if (spriteRects == null || spriteRects.Length == 0)
            {
                return Array.Empty<SpriteSliceInfo>();
            }

            var result = new List<SpriteSliceInfo>(spriteRects.Length);
            foreach (var spriteRect in spriteRects)
            {
                var info = new SpriteSliceInfo(
                    spriteRect.name,
                    spriteRect.rect,
                    spriteRect.pivot,
                    spriteRect.border,
                    spriteRect.alignment);

                if (info.IsValid)
                {
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>
        /// 대상 PNG TextureImporter에 Multiple Sprite Slice 메타데이터를 적용합니다.
        /// </summary>
        /// <param name="assetPath">대상 PNG의 프로젝트 상대 경로입니다.</param>
        /// <param name="slices">출력 PNG에 적용할 원본 Slice 정보입니다.</param>
        /// <param name="settings">좌우 반전 메타데이터 보정 옵션입니다.</param>
        public static void WriteSlices(string assetPath, IReadOnlyList<SpriteSliceInfo> slices, SpriteSliceFlipSettings settings)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("대상 에셋 경로가 비어 있습니다.", nameof(assetPath));
            }

            if (slices == null || slices.Count == 0)
            {
                throw new InvalidOperationException("적용할 Sprite Slice 정보가 없습니다.");
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("TextureImporter를 찾을 수 없습니다: " + assetPath);
            }

            var dataProvider = CreateDataProvider(importer);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new SpriteRect[slices.Count];
            for (var i = 0; i < slices.Count; i++)
            {
                var slice = slices[i];
                var pivot = settings.mirrorPivot
                    ? new Vector2(1f - slice.Pivot.x, slice.Pivot.y)
                    : slice.Pivot;
                var border = settings.mirrorBorder
                    ? new Vector4(slice.Border.z, slice.Border.y, slice.Border.x, slice.Border.w)
                    : slice.Border;

                spriteRects[i] = new SpriteRect
                {
                    name = settings.BuildOutputSpriteName(slice.Name),
                    rect = slice.Rect,
                    pivot = pivot,
                    border = border,
                    alignment = settings.mirrorPivot ? SpriteAlignment.Custom : slice.Alignment,
                    spriteID = GUID.Generate()
                };
            }

            dataProvider.SetSpriteRects(spriteRects);
            SyncNameFileIdPairs(dataProvider, spriteRects);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        /// <summary>
        /// TextureImporter 기본 Sprite Import 설정을 원본에서 대상으로 복사합니다.
        /// </summary>
        /// <param name="sourceImporter">원본 TextureImporter입니다.</param>
        /// <param name="targetImporter">대상 TextureImporter입니다.</param>
        public static void CopySpriteImportSettings(TextureImporter sourceImporter, TextureImporter targetImporter)
        {
            if (sourceImporter == null)
            {
                throw new ArgumentNullException(nameof(sourceImporter));
            }

            if (targetImporter == null)
            {
                throw new ArgumentNullException(nameof(targetImporter));
            }

            targetImporter.textureType = TextureImporterType.Sprite;
            targetImporter.spriteImportMode = SpriteImportMode.Multiple;
            targetImporter.spritePixelsPerUnit = Mathf.Max(1f, sourceImporter.spritePixelsPerUnit);
            targetImporter.alphaIsTransparency = sourceImporter.alphaIsTransparency;
            targetImporter.mipmapEnabled = sourceImporter.mipmapEnabled;
            targetImporter.filterMode = sourceImporter.filterMode;
            targetImporter.wrapMode = sourceImporter.wrapMode;
            targetImporter.textureCompression = sourceImporter.textureCompression;
            targetImporter.maxTextureSize = sourceImporter.maxTextureSize;
            targetImporter.npotScale = sourceImporter.npotScale;
            // targetImporter.spriteMeshType = sourceImporter.spriteMeshType;
        }

        /// <summary>
        /// SpriteRect 이름과 GUID 매핑 정보를 동기화합니다.
        /// Unity 2021.2 이상에서는 SpriteRect 추가 후 이 매핑이 필요합니다.
        /// </summary>
        /// <param name="dataProvider">Sprite Editor 데이터 제공자입니다.</param>
        /// <param name="spriteRects">적용할 SpriteRect 배열입니다.</param>
        private static void SyncNameFileIdPairs(ISpriteEditorDataProvider dataProvider, IReadOnlyList<SpriteRect> spriteRects)
        {
#if UNITY_2021_2_OR_NEWER
            if (dataProvider == null || spriteRects == null)
            {
                return;
            }

            var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileIdProvider == null)
            {
                return;
            }

            var pairs = spriteRects
                .Select(spriteRect => new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID))
                .ToList();
            nameFileIdProvider.SetNameFileIdPairs(pairs);
#endif
        }

        /// <summary>
        /// Texture 또는 Importer에서 Sprite Editor 데이터 제공자를 생성합니다.
        /// </summary>
        /// <param name="targetObject">Texture2D 또는 TextureImporter입니다.</param>
        /// <returns>초기화 전 Sprite Editor 데이터 제공자입니다.</returns>
        private static ISpriteEditorDataProvider CreateDataProvider(UnityEngine.Object targetObject)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(targetObject);
            if (dataProvider == null)
            {
                throw new InvalidOperationException("Sprite Editor Data Provider를 생성할 수 없습니다.");
            }

            return dataProvider;
        }
    }
}
#endif
