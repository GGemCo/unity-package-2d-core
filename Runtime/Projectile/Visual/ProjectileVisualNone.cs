using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 표현이 없는 Visual.
    /// </summary>
    public sealed class ProjectileVisualNone : MonoBehaviour, IProjectileVisual
    {
        public void OnSpawn(in ProjectileVisualSpawnContext context) { }
        public void OnUpdate(in ProjectileVisualUpdateContext context) { }
        public void OnHit(in ProjectileVisualHitContext context) { }
        public void OnDespawn() { }
    }
}