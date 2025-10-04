using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 포물선 궤적의 곡선형 발사체.
    /// - height = H * 4 * (t - t*t)
    /// - H는 테이블의 Min~Max에서 1회 샘플링
    /// </summary>
    public class ProjectileArc : ProjectileBase
    {
        private float _arcHeight;

        public override void Initialize(StruckTableProjectile info)
        {
            base.Initialize(info);

            _arcHeight = info.ArcHeightMin;
            if (info.ArcHeightMin != info.ArcHeightMax)
                _arcHeight = Random.Range(info.ArcHeightMin, info.ArcHeightMax);
        }

        protected override Vector2 ComputePosition(float t)
        {
            Vector2 pos = Vector2.Lerp(StartPoint, TargetPoint, t);

            if (_arcHeight > 0f)
            {
                float h = _arcHeight * 4f * (t - t * t);
                pos.y += h;
            }
            return pos;
        }
    }
}