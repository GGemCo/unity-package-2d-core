using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 등속 직진형 발사체.
    /// - 기본 모드: 기존 구현(진행률 t 기반 보간) 동작을 그대로 사용합니다.
    /// - Bounce 모드: 카메라 화면 경계에 닿으면 반사(Reflect)하며 이동합니다.
    ///   (Type: Linear + BoundaryMode=Bounce)
    /// </summary>
    public class ProjectileLinear : ProjectileBase
    {
        private int _bounceCount;

        public override void Initialize(StruckTableProjectile info, MetadataProjectile metadata)
        {
            base.Initialize(info, metadata);
            _bounceCount = 0;
        }

        /// <summary>
        /// 직선형: StartPoint→TargetPoint 를 t로 선형 보간.
        /// </summary>
        protected override Vector2 ComputePosition(float t)
        {
            return Vector2.Lerp(StartPoint, TargetPoint, t);
        }

        protected override void FixedUpdate()
        {
            if (!Initialized) return;

            // 레거시/기본: 기존 ProjectileBase.Update 로직을 사용(화면 이탈 시 파괴).
            if (Info == null ||
                Info.BoundaryMode != ProjectileConstants.BoundaryMode.Bounce ||
                Info.BounceMaxCount <= 0)
            {
                base.FixedUpdate();
                return;
            }

            // Bounce: 속도 기반 이동(경계 충돌 시 방향 반사)
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector2 pos = transform.position;
            Vector2 nextPos = pos + (Direction * (Speed * dt));

            // 카메라가 없으면 기존 정책과 동일하게 화면 이탈 시 파괴(= 카메라 판정 불가)
            if (!TryGetCameraWorldRect(out Rect camRect, Info.BoundaryPadding))
            {
                // 기존 base.Update의 IsInCameraView 처리와 동일한 의미를 유지하기 위해
                // 카메라가 없을 때는 진행은 하되, 별도 이탈 파괴는 하지 않습니다.
                // (SceneGame.mainCamera가 셋팅되는 씬에서 사용을 권장)
                ApplyStep(pos, nextPos);
                return;
            }

            bool bounced = false;

            // X boundary
            if (nextPos.x < camRect.xMin)
            {
                nextPos.x = camRect.xMin;
                Direction.x = -Direction.x;
                bounced = true;
            }
            else if (nextPos.x > camRect.xMax)
            {
                nextPos.x = camRect.xMax;
                Direction.x = -Direction.x;
                bounced = true;
            }

            // Y boundary
            if (nextPos.y < camRect.yMin)
            {
                nextPos.y = camRect.yMin;
                Direction.y = -Direction.y;
                bounced = true;
            }
            else if (nextPos.y > camRect.yMax)
            {
                nextPos.y = camRect.yMax;
                Direction.y = -Direction.y;
                bounced = true;
            }

            if (bounced)
            {
                _bounceCount++;

                // 속도 감쇠(기본 1.0)
                Speed *= Mathf.Max(0.01f, Info.BounceSpeedMultiplier);

                if (_bounceCount > Info.BounceMaxCount)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            ApplyStep(pos, nextPos);
        }

        private void ApplyStep(Vector2 prevPos, Vector2 nextPos)
        {
            Vector2 delta = nextPos - prevPos;

            ApplyRotationByDelta(delta);

            PrevPos = nextPos;
            transform.position = nextPos;

            // Visual update (flip 등)
            UpdateVisual(nextPos, delta);
        }
    }
}
