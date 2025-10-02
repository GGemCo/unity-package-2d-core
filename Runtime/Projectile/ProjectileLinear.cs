using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 등속 직진형 발사체.
    /// </summary>
    public class ProjectileLinear : ProjectileBase
    {
        /// <summary>
        /// 직선형: StartPoint→TargetPoint 를 t로 선형 보간.
        /// </summary>
        protected override Vector2 ComputePosition(float t)
        {
            return Vector2.Lerp(StartPoint, TargetPoint, t);
        }
    }
}