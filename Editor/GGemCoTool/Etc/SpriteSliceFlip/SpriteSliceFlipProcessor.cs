#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 원본 Texture2D의 각 Slice 영역 내부 픽셀만 좌우 반전하여 새 Texture2D를 생성합니다.
    /// </summary>
    internal static class SpriteSliceFlipProcessor
    {
        /// <summary>
        /// Slice 영역 내부 픽셀을 좌우 반전한 Texture2D를 생성합니다.
        /// </summary>
        /// <param name="sourceTexture">원본 텍스처입니다.</param>
        /// <param name="sourceSlices">원본 Sprite Slice 목록입니다.</param>
        /// <param name="settings">처리 옵션입니다.</param>
        /// <param name="processedSlices">실제로 출력 메타데이터에 포함할 Slice 목록입니다.</param>
        /// <param name="skippedTransparentCount">투명 영역으로 건너뛴 Slice 개수입니다.</param>
        /// <returns>좌우 반전된 새 Texture2D입니다.</returns>
        public static Texture2D CreateFlippedTexture(
            Texture2D sourceTexture,
            IReadOnlyList<SpriteSliceInfo> sourceSlices,
            SpriteSliceFlipSettings settings,
            out List<SpriteSliceInfo> processedSlices,
            out int skippedTransparentCount)
        {
            if (sourceTexture == null)
            {
                throw new ArgumentNullException(nameof(sourceTexture));
            }

            if (sourceSlices == null || sourceSlices.Count == 0)
            {
                throw new InvalidOperationException("좌우 반전할 Sprite Slice 정보가 없습니다.");
            }

            var width = sourceTexture.width;
            var height = sourceTexture.height;
            var sourcePixels = sourceTexture.GetPixels32();
            var outputPixels = new Color32[sourcePixels.Length];
            Array.Copy(sourcePixels, outputPixels, sourcePixels.Length);

            processedSlices = new List<SpriteSliceInfo>(sourceSlices.Count);
            skippedTransparentCount = 0;

            for (var i = 0; i < sourceSlices.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Sprite Slice 좌우 반전",
                    $"{i + 1}/{sourceSlices.Count} Slice 처리 중...",
                    (float)i / Mathf.Max(1, sourceSlices.Count)))
                {
                    throw new OperationCanceledException("사용자가 Sprite Slice 좌우 반전 작업을 취소했습니다.");
                }

                var slice = sourceSlices[i];
                var rect = ToClampedIntRect(slice.Rect, width, height);
                if (rect.width <= 0 || rect.height <= 0)
                {
                    continue;
                }

                if (!settings.includeFullyTransparentSprites && IsFullyTransparent(sourcePixels, width, rect))
                {
                    skippedTransparentCount++;
                    continue;
                }

                FlipRectHorizontally(sourcePixels, outputPixels, width, rect);
                processedSlices.Add(slice);
            }

            var outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = sourceTexture.name + settings.outputNameSuffix
            };
            outputTexture.SetPixels32(outputPixels);
            outputTexture.Apply(false, false);
            return outputTexture;
        }

        /// <summary>
        /// Rect 값을 텍스처 픽셀 범위 안의 정수 RectInt로 변환합니다.
        /// </summary>
        /// <param name="rect">Sprite Editor 기준 Rect입니다.</param>
        /// <param name="textureWidth">텍스처 폭입니다.</param>
        /// <param name="textureHeight">텍스처 높이입니다.</param>
        /// <returns>텍스처 범위로 보정된 정수 Rect입니다.</returns>
        private static RectInt ToClampedIntRect(Rect rect, int textureWidth, int textureHeight)
        {
            var x = Mathf.Clamp(Mathf.RoundToInt(rect.x), 0, textureWidth);
            var y = Mathf.Clamp(Mathf.RoundToInt(rect.y), 0, textureHeight);
            var width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 0, textureWidth - x);
            var height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 0, textureHeight - y);
            return new RectInt(x, y, width, height);
        }

        /// <summary>
        /// 지정된 Slice 영역이 완전히 투명한지 확인합니다.
        /// </summary>
        /// <param name="sourcePixels">원본 전체 픽셀 배열입니다.</param>
        /// <param name="textureWidth">텍스처 폭입니다.</param>
        /// <param name="rect">검사할 Slice 영역입니다.</param>
        /// <returns>모든 픽셀 알파가 0이면 true입니다.</returns>
        private static bool IsFullyTransparent(Color32[] sourcePixels, int textureWidth, RectInt rect)
        {
            for (var y = 0; y < rect.height; y++)
            {
                var rowStart = (rect.y + y) * textureWidth + rect.x;
                for (var x = 0; x < rect.width; x++)
                {
                    if (sourcePixels[rowStart + x].a > 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 지정된 Rect 내부의 픽셀만 좌우 반전하여 출력 배열에 기록합니다.
        /// </summary>
        /// <param name="sourcePixels">원본 전체 픽셀 배열입니다.</param>
        /// <param name="outputPixels">결과 전체 픽셀 배열입니다.</param>
        /// <param name="textureWidth">텍스처 폭입니다.</param>
        /// <param name="rect">좌우 반전할 Slice 영역입니다.</param>
        private static void FlipRectHorizontally(Color32[] sourcePixels, Color32[] outputPixels, int textureWidth, RectInt rect)
        {
            for (var y = 0; y < rect.height; y++)
            {
                var rowStart = (rect.y + y) * textureWidth + rect.x;
                for (var x = 0; x < rect.width; x++)
                {
                    var sourceIndex = rowStart + (rect.width - 1 - x);
                    var outputIndex = rowStart + x;
                    outputPixels[outputIndex] = sourcePixels[sourceIndex];
                }
            }
        }
    }
}
#endif
