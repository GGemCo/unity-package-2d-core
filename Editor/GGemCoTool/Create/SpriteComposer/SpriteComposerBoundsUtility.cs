using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// SpriteRenderer와 UGUI Image 목록의 월드 Bounds를 계산하는 유틸리티입니다.
    /// </summary>
    internal static class SpriteComposerBoundsUtility
    {
        /// <summary>
        /// 여러 SpriteRenderer와 UGUI Image의 전체 월드 Bounds를 계산합니다.
        /// </summary>
        /// <param name="renderers">Bounds를 계산할 SpriteRenderer 목록입니다.</param>
        /// <param name="images">Bounds를 계산할 UGUI Image 목록입니다.</param>
        /// <param name="bounds">계산된 전체 Bounds입니다.</param>
        /// <returns>유효한 Bounds를 계산했으면 true입니다.</returns>
        public static bool TryCalculateWorldBounds(IReadOnlyList<SpriteRenderer> renderers, IReadOnlyList<Image> images, out Bounds bounds)
        {
            bounds = default(Bounds);
            var initialized = false;

            EncapsulateSpriteRendererBounds(renderers, ref bounds, ref initialized);
            EncapsulateImageBounds(images, ref bounds, ref initialized);
            return initialized;
        }

        /// <summary>
        /// 여러 SpriteRenderer의 전체 월드 Bounds를 계산합니다.
        /// </summary>
        /// <param name="renderers">Bounds를 계산할 SpriteRenderer 목록입니다.</param>
        /// <param name="bounds">계산된 전체 Bounds입니다.</param>
        /// <returns>유효한 Bounds를 계산했으면 true입니다.</returns>
        public static bool TryCalculateWorldBounds(IReadOnlyList<SpriteRenderer> renderers, out Bounds bounds)
        {
            return TryCalculateWorldBounds(renderers, null, out bounds);
        }

        /// <summary>
        /// SpriteRenderer 목록의 Bounds를 전체 Bounds에 포함합니다.
        /// </summary>
        /// <param name="renderers">계산할 SpriteRenderer 목록입니다.</param>
        /// <param name="bounds">확장할 전체 Bounds입니다.</param>
        /// <param name="initialized">Bounds 초기화 여부입니다.</param>
        private static void EncapsulateSpriteRendererBounds(IReadOnlyList<SpriteRenderer> renderers, ref Bounds bounds, ref bool initialized)
        {
            if (renderers == null || renderers.Count == 0)
            {
                return;
            }

            for (var i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.sprite == null)
                {
                    continue;
                }

                EncapsulateBounds(ref bounds, ref initialized, GetRendererWorldBounds(renderer));
            }
        }

        /// <summary>
        /// UGUI Image 목록의 RectTransform Bounds를 전체 Bounds에 포함합니다.
        /// </summary>
        /// <param name="images">계산할 UGUI Image 목록입니다.</param>
        /// <param name="bounds">확장할 전체 Bounds입니다.</param>
        /// <param name="initialized">Bounds 초기화 여부입니다.</param>
        private static void EncapsulateImageBounds(IReadOnlyList<Image> images, ref Bounds bounds, ref bool initialized)
        {
            if (images == null || images.Count == 0)
            {
                return;
            }

            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                if (image == null || image.sprite == null)
                {
                    continue;
                }

                EncapsulateBounds(ref bounds, ref initialized, GetImageWorldBounds(image));
            }
        }

        /// <summary>
        /// 전체 Bounds에 새 Bounds를 포함하거나, 첫 Bounds라면 초기화합니다.
        /// </summary>
        /// <param name="bounds">확장할 전체 Bounds입니다.</param>
        /// <param name="initialized">Bounds 초기화 여부입니다.</param>
        /// <param name="targetBounds">새로 포함할 Bounds입니다.</param>
        private static void EncapsulateBounds(ref Bounds bounds, ref bool initialized, Bounds targetBounds)
        {
            if (!initialized)
            {
                bounds = targetBounds;
                initialized = true;
                return;
            }

            bounds.Encapsulate(targetBounds);
        }

        /// <summary>
        /// SpriteRenderer의 Bounds를 월드 좌표 기준으로 계산합니다.
        /// </summary>
        /// <param name="renderer">대상 SpriteRenderer입니다.</param>
        /// <returns>월드 좌표 기준 Bounds입니다.</returns>
        private static Bounds GetRendererWorldBounds(SpriteRenderer renderer)
        {
            var rendererBounds = renderer.bounds;
            if (rendererBounds.size.sqrMagnitude > 0f)
            {
                return rendererBounds;
            }

            return TransformBounds(renderer.transform.localToWorldMatrix, renderer.sprite.bounds);
        }

        /// <summary>
        /// UGUI Image의 RectTransform 네 모서리를 사용해 월드 Bounds를 계산합니다.
        /// </summary>
        /// <param name="image">대상 UGUI Image입니다.</param>
        /// <returns>월드 좌표 기준 Bounds입니다.</returns>
        private static Bounds GetImageWorldBounds(Image image)
        {
            var rectTransform = image.rectTransform;
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var imageBounds = new Bounds(corners[0], Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                imageBounds.Encapsulate(corners[i]);
            }

            return imageBounds;
        }

        /// <summary>
        /// 로컬 Bounds를 Matrix 기준으로 변환하여 월드 Bounds를 계산합니다.
        /// </summary>
        /// <param name="matrix">로컬 좌표를 월드 좌표로 변환할 Matrix입니다.</param>
        /// <param name="localBounds">로컬 좌표 기준 Bounds입니다.</param>
        /// <returns>변환된 월드 Bounds입니다.</returns>
        private static Bounds TransformBounds(Matrix4x4 matrix, Bounds localBounds)
        {
            var center = localBounds.center;
            var extents = localBounds.extents;

            var worldBounds = new Bounds(matrix.MultiplyPoint3x4(center), Vector3.zero);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, -1f, -1f, -1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, -1f, -1f, 1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, -1f, 1f, -1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, -1f, 1f, 1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, 1f, -1f, -1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, 1f, -1f, 1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, 1f, 1f, -1f);
            EncapsulateCorner(ref worldBounds, matrix, center, extents, 1f, 1f, 1f);
            return worldBounds;
        }

        /// <summary>
        /// Bounds의 한 모서리를 월드 좌표로 변환한 뒤 전체 Bounds에 포함합니다.
        /// </summary>
        /// <param name="bounds">확장할 Bounds입니다.</param>
        /// <param name="matrix">좌표 변환 Matrix입니다.</param>
        /// <param name="center">로컬 Bounds 중심입니다.</param>
        /// <param name="extents">로컬 Bounds 반지름입니다.</param>
        /// <param name="xSign">X축 방향 부호입니다.</param>
        /// <param name="ySign">Y축 방향 부호입니다.</param>
        /// <param name="zSign">Z축 방향 부호입니다.</param>
        private static void EncapsulateCorner(ref Bounds bounds, Matrix4x4 matrix, Vector3 center, Vector3 extents, float xSign, float ySign, float zSign)
        {
            var localCorner = center + new Vector3(extents.x * xSign, extents.y * ySign, extents.z * zSign);
            bounds.Encapsulate(matrix.MultiplyPoint3x4(localCorner));
        }
    }
}
