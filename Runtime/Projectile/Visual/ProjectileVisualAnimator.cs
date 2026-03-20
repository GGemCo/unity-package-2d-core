using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Animator + SpriteRenderer 기반 프로젝타일 표현.
    /// AnimatorController는 런타임 메타데이터에서 주입한다(Q2=A).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualAnimator : MonoBehaviour, IProjectileVisual
    {
        private SpriteRenderer _renderer;
        private Animator _animator;

        public void OnSpawn(in ProjectileVisualSpawnContext context)
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);

            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = gameObject.AddComponent<Animator>();

            var controller = context.RuntimeData?.VisualAnimatorController;
            if (controller != null)
                _animator.runtimeAnimatorController = controller;

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
            // Animator 파라미터를 사용하지 않는 정책이라면, Clip 전환은
            // RuntimeAnimatorController 상태머신으로 처리하거나, 별도 트리거를 추가 구현한다.
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