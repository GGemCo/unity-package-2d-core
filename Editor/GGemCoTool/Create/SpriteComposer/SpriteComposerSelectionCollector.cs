using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Composer가 처리할 Hierarchy 선택 정보와 렌더러 목록을 보관합니다.
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
        /// 선택 정보를 생성합니다.
        /// </summary>
        /// <param name="roots">선택된 최상위 Transform 목록입니다.</param>
        /// <param name="renderers">합성 대상 SpriteRenderer 목록입니다.</param>
        public SpriteComposerSelection(Transform[] roots, SpriteRenderer[] renderers)
        {
            Roots = roots ?? new Transform[0];
            Renderers = renderers ?? new SpriteRenderer[0];
        }

        /// <summary>
        /// 합성 가능한 SpriteRenderer가 하나 이상 있는지 여부입니다.
        /// </summary>
        public bool HasRenderableSprites
        {
            get { return Renderers.Length > 0; }
        }
    }

    /// <summary>
    /// Unity Editor Selection에서 SpriteRenderer 기반 합성 대상을 수집합니다.
    /// </summary>
    internal static class SpriteComposerSelectionCollector
    {
        /// <summary>
        /// 현재 Hierarchy 선택에서 Sprite Composer가 사용할 수 있는 대상들을 수집합니다.
        /// </summary>
        /// <param name="includeInactive">비활성 오브젝트와 비활성 렌더러도 포함할지 여부입니다.</param>
        /// <returns>선택 루트와 SpriteRenderer 목록이 담긴 선택 정보입니다.</returns>
        public static SpriteComposerSelection CollectCurrentSelection(bool includeInactive)
        {
            return Collect(Selection.transforms, includeInactive);
        }

        /// <summary>
        /// 전달받은 Transform 루트 목록에서 중복 없이 SpriteRenderer를 수집합니다.
        /// </summary>
        /// <param name="selectedRoots">Hierarchy에서 선택된 Transform 목록입니다.</param>
        /// <param name="includeInactive">비활성 대상 포함 여부입니다.</param>
        /// <returns>합성에 사용할 선택 정보입니다.</returns>
        public static SpriteComposerSelection Collect(Transform[] selectedRoots, bool includeInactive)
        {
            if (selectedRoots == null || selectedRoots.Length == 0)
            {
                return new SpriteComposerSelection(new Transform[0], new SpriteRenderer[0]);
            }

            var rootList = new List<Transform>();
            var rendererSet = new HashSet<SpriteRenderer>();
            var rendererList = new List<SpriteRenderer>();

            foreach (var root in selectedRoots)
            {
                if (root == null)
                {
                    continue;
                }

                rootList.Add(root);
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

            return new SpriteComposerSelection(rootList.ToArray(), rendererList.ToArray());
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
    }
}
