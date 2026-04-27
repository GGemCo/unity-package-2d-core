using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Composer가 처리할 Hierarchy 선택 정보와 렌더링 대상 목록을 보관합니다.
    /// </summary>
    internal sealed class SpriteComposerSelection
    {
        /// <summary>
        /// Hierarchy에서 선택된 최상위 Transform 목록입니다.
        /// </summary>
        public readonly Transform[] Roots;

        /// <summary>
        /// 합성 대상 SpriteRenderer 목록입니다.
        /// </summary>
        public readonly SpriteRenderer[] Renderers;

        /// <summary>
        /// 합성 대상 UGUI Image 목록입니다.
        /// </summary>
        public readonly Image[] Images;

        /// <summary>
        /// 선택 정보를 생성합니다.
        /// </summary>
        /// <param name="roots">선택된 최상위 Transform 목록입니다.</param>
        /// <param name="renderers">합성 대상 SpriteRenderer 목록입니다.</param>
        /// <param name="images">합성 대상 UGUI Image 목록입니다.</param>
        public SpriteComposerSelection(Transform[] roots, SpriteRenderer[] renderers, Image[] images)
        {
            Roots = roots ?? new Transform[0];
            Renderers = renderers ?? new SpriteRenderer[0];
            Images = images ?? new Image[0];
        }

        /// <summary>
        /// 합성 가능한 SpriteRenderer가 하나 이상 있는지 여부입니다.
        /// </summary>
        public bool HasRenderableSprites
        {
            get { return Renderers.Length > 0; }
        }

        /// <summary>
        /// 합성 가능한 UGUI Image가 하나 이상 있는지 여부입니다.
        /// </summary>
        public bool HasRenderableImages
        {
            get { return Images.Length > 0; }
        }

        /// <summary>
        /// 합성 가능한 대상이 하나 이상 있는지 여부입니다.
        /// </summary>
        public bool HasRenderableItems
        {
            get { return HasRenderableSprites || HasRenderableImages; }
        }
    }

    /// <summary>
    /// Unity Editor Selection에서 SpriteRenderer와 UGUI Image 기반 합성 대상을 수집합니다.
    /// </summary>
    internal static class SpriteComposerSelectionCollector
    {
        /// <summary>
        /// 현재 Hierarchy 선택에서 Sprite Composer가 사용할 수 있는 대상들을 수집합니다.
        /// </summary>
        /// <param name="includeInactive">비활성 오브젝트와 비활성 렌더링 컴포넌트도 포함할지 여부입니다.</param>
        /// <returns>선택 루트와 합성 대상 목록이 담긴 선택 정보입니다.</returns>
        public static SpriteComposerSelection CollectCurrentSelection(bool includeInactive)
        {
            return Collect(Selection.transforms, includeInactive);
        }

        /// <summary>
        /// 전달받은 Transform 루트 목록에서 중복 없이 SpriteRenderer와 UGUI Image를 수집합니다.
        /// </summary>
        /// <param name="selectedRoots">Hierarchy에서 선택된 Transform 목록입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <returns>합성에 사용할 선택 정보입니다.</returns>
        public static SpriteComposerSelection Collect(Transform[] selectedRoots, bool includeInactive)
        {
            if (selectedRoots == null || selectedRoots.Length == 0)
            {
                return new SpriteComposerSelection(new Transform[0], new SpriteRenderer[0], new Image[0]);
            }

            var rootList = new List<Transform>();
            var rendererSet = new HashSet<SpriteRenderer>();
            var rendererList = new List<SpriteRenderer>();
            var imageSet = new HashSet<Image>();
            var imageList = new List<Image>();

            foreach (var root in selectedRoots)
            {
                if (root == null)
                {
                    continue;
                }

                rootList.Add(root);
                CollectSpriteRenderers(root, includeInactive, rendererSet, rendererList);
                CollectImages(root, includeInactive, imageSet, imageList);
            }

            imageList.Sort(CompareImagesByHierarchyOrder);
            return new SpriteComposerSelection(rootList.ToArray(), rendererList.ToArray(), imageList.ToArray());
        }

        /// <summary>
        /// 지정한 루트 하위에서 SpriteRenderer 합성 대상을 수집합니다.
        /// </summary>
        /// <param name="root">검색을 시작할 루트 Transform입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <param name="rendererSet">중복 제거에 사용할 Set입니다.</param>
        /// <param name="rendererList">수집 결과를 추가할 목록입니다.</param>
        private static void CollectSpriteRenderers(Transform root, bool includeInactive, HashSet<SpriteRenderer> rendererSet, List<SpriteRenderer> rendererList)
        {
            var renderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive);
            foreach (var renderer in renderers)
            {
                if (!IsRenderable(renderer, includeInactive) || !rendererSet.Add(renderer))
                {
                    continue;
                }

                rendererList.Add(renderer);
            }
        }

        /// <summary>
        /// 지정한 루트 하위에서 UGUI Image 합성 대상을 수집합니다.
        /// </summary>
        /// <param name="root">검색을 시작할 루트 Transform입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <param name="imageSet">중복 제거에 사용할 Set입니다.</param>
        /// <param name="imageList">수집 결과를 추가할 목록입니다.</param>
        private static void CollectImages(Transform root, bool includeInactive, HashSet<Image> imageSet, List<Image> imageList)
        {
            var images = root.GetComponentsInChildren<Image>(includeInactive);
            foreach (var image in images)
            {
                if (!IsRenderable(image, includeInactive) || !imageSet.Add(image))
                {
                    continue;
                }

                imageList.Add(image);
            }
        }

        /// <summary>
        /// SpriteRenderer가 현재 설정 기준으로 합성 가능한 대상인지 검사합니다.
        /// </summary>
        /// <param name="renderer">검사할 SpriteRenderer입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <returns>합성 가능하면 true입니다.</returns>
        private static bool IsRenderable(SpriteRenderer renderer, bool includeInactive)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return false;
            }

            if (includeInactive)
            {
                return true;
            }

            return renderer.enabled && renderer.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// UGUI Image가 현재 설정 기준으로 합성 가능한 대상인지 검사합니다.
        /// </summary>
        /// <param name="image">검사할 UGUI Image입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <returns>합성 가능하면 true입니다.</returns>
        private static bool IsRenderable(Image image, bool includeInactive)
        {
            if (image == null || image.sprite == null)
            {
                return false;
            }

            if (includeInactive)
            {
                return true;
            }

            return image.enabled && image.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// UGUI Image를 원본 Hierarchy 순서에 가깝게 정렬합니다.
        /// </summary>
        /// <param name="left">왼쪽 비교 대상입니다.</param>
        /// <param name="right">오른쪽 비교 대상입니다.</param>
        /// <returns>정렬 비교 결과입니다.</returns>
        private static int CompareImagesByHierarchyOrder(Image left, Image right)
        {
            var leftOrder = GetHierarchyOrderKey(left != null ? left.transform : null);
            var rightOrder = GetHierarchyOrderKey(right != null ? right.transform : null);
            return string.CompareOrdinal(leftOrder, rightOrder);
        }

        /// <summary>
        /// Transform의 부모/자식 인덱스를 문자열 키로 만들어 Hierarchy 순서를 비교할 수 있게 합니다.
        /// </summary>
        /// <param name="transform">순서 키를 만들 Transform입니다.</param>
        /// <returns>Hierarchy 순서 비교용 문자열 키입니다.</returns>
        private static string GetHierarchyOrderKey(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var indices = new Stack<int>();
            var current = transform;
            while (current != null)
            {
                indices.Push(current.GetSiblingIndex());
                current = current.parent;
            }

            var result = string.Empty;
            while (indices.Count > 0)
            {
                result += indices.Pop().ToString("D6") + "/";
            }

            return result;
        }
    }
}
