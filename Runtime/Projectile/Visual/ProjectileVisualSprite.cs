using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 단일 SpriteRenderer 기반 프로젝타일 표현.
    /// Sprite는 런타임 메타데이터에서 주입한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualSprite : MonoBehaviour, IProjectileVisual
    {
        private SpriteRenderer _renderer;

        public void OnSpawn(in ProjectileVisualSpawnContext context)
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);

            var sprite = context.RuntimeData?.VisualSprite;
            if (sprite != null)
                _renderer.sprite = sprite;

            float scale = context.RuntimeData != null ? Mathf.Max(0.01f, context.RuntimeData.ScaleMultiplier) : 1f;
            transform.localScale = transform.localScale * scale;
        }

        public void OnUpdate(in ProjectileVisualUpdateContext context)
        {
            // 좌우 Flip: 이동 방향 기준
            if (_renderer == null) return;

            if (context.Direction.x < -0.001f)
                _renderer.flipX = true;
            else if (context.Direction.x > 0.001f)
                _renderer.flipX = false;
        }

        public void OnHit(in ProjectileVisualHitContext context)
        {
            // 필요 시 스프라이트 교체/히트 애니메이션 등을 여기서 처리
        }

        public void OnDespawn()
        {
        }

        public bool TryPlayEnd(Action onComplete)
        {
            return false;
        }
    }
}