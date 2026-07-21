using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class VfxFadeController : MonoBehaviour
    {
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");

        private readonly List<SpriteEntry> _sprites = new List<SpriteEntry>();
        private readonly List<RendererEntry> _renderers = new List<RendererEntry>();
        private readonly List<CanvasGroup> _canvasGroups = new List<CanvasGroup>();
        private MaterialPropertyBlock _propertyBlock;
        private bool _initialized;
        private float _currentAlpha = 1f;

        public float CurrentAlpha => _currentAlpha;

        private struct SpriteEntry
        {
            public SpriteRenderer Renderer;
            public Color OriginalColor;
        }

        private struct RendererEntry
        {
            public Renderer Renderer;
            public int ColorPropertyId;
            public Color OriginalColor;
        }

        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            CacheTargets();
        }

        public void SetAlpha(float alpha)
        {
            EnsureInitialized();
            alpha = Mathf.Clamp01(alpha);
            _currentAlpha = alpha;

            for (int i = 0; i < _sprites.Count; i++)
            {
                var entry = _sprites[i];
                if (entry.Renderer == null)
                    continue;

                var color = entry.OriginalColor;
                color.a *= alpha;
                entry.Renderer.color = color;
            }

            for (int i = 0; i < _renderers.Count; i++)
            {
                var entry = _renderers[i];
                if (entry.Renderer == null)
                    continue;

                entry.Renderer.GetPropertyBlock(_propertyBlock);
                var color = entry.OriginalColor;
                color.a *= alpha;
                _propertyBlock.SetColor(entry.ColorPropertyId, color);
                entry.Renderer.SetPropertyBlock(_propertyBlock);
                _propertyBlock.Clear();
            }

            for (int i = 0; i < _canvasGroups.Count; i++)
            {
                var canvasGroup = _canvasGroups[i];
                if (canvasGroup == null)
                    continue;

                canvasGroup.alpha = alpha;
            }
        }

        public void RestoreFullAlpha()
        {
            SetAlpha(1f);
        }

        /// <summary>
        /// 외부 색상 변경을 Fade 기준색에 반영하되, 완전히 투명한 Fade 결과를 원본 alpha로 저장하지 않습니다.
        /// </summary>
        /// <remarks>
        /// 현재 Fade alpha가 0이면 표시색만으로 원래 alpha를 역산할 수 없습니다.
        /// 이 경우 기존 원본 alpha를 유지하여 풀 재사용 후에도 <see cref="RestoreFullAlpha"/>가 가시 상태를 복구하도록 합니다.
        /// </remarks>
        public void RefreshOriginalColorsFromCurrentState()
        {
            EnsureInitialized();

            for (int i = 0; i < _sprites.Count; i++)
            {
                var entry = _sprites[i];
                if (entry.Renderer == null)
                    continue;

                Color currentColor = entry.Renderer.color;
                if (_currentAlpha <= Mathf.Epsilon)
                    currentColor.a = entry.OriginalColor.a;

                entry.OriginalColor = currentColor;
                _sprites[i] = entry;
            }

            for (int i = 0; i < _renderers.Count; i++)
            {
                var entry = _renderers[i];
                if (entry.Renderer == null)
                    continue;

                Color currentColor = entry.OriginalColor;
                var sharedMaterial = entry.Renderer.sharedMaterial;
                if (sharedMaterial != null && sharedMaterial.HasProperty(entry.ColorPropertyId))
                {
                    try
                    {
                        currentColor = sharedMaterial.GetColor(entry.ColorPropertyId);
                    }
                    catch
                    {
                        currentColor = entry.OriginalColor;
                    }
                }

                entry.OriginalColor = currentColor;
                _renderers[i] = entry;
            }
        }

        private void CacheTargets()
        {
            _sprites.Clear();
            _renderers.Clear();
            _canvasGroups.Clear();

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var renderer = spriteRenderers[i];
                if (renderer == null)
                    continue;

                _sprites.Add(new SpriteEntry
                {
                    Renderer = renderer,
                    OriginalColor = renderer.color,
                });
            }

            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer is SpriteRenderer)
                    continue;

                var sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial == null)
                    continue;

                int colorPropertyId = 0;
                if (sharedMaterial.HasProperty(ColorProp))
                    colorPropertyId = ColorProp;
                else if (sharedMaterial.HasProperty(BaseColorProp))
                    colorPropertyId = BaseColorProp;

                if (colorPropertyId == 0)
                    continue;

                Color originalColor = Color.white;
                try
                {
                    originalColor = sharedMaterial.GetColor(colorPropertyId);
                }
                catch
                {
                    originalColor = Color.white;
                }

                _renderers.Add(new RendererEntry
                {
                    Renderer = renderer,
                    ColorPropertyId = colorPropertyId,
                    OriginalColor = originalColor,
                });
            }

            var canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                if (canvasGroups[i] != null)
                    _canvasGroups.Add(canvasGroups[i]);
            }
        }
    }
}
