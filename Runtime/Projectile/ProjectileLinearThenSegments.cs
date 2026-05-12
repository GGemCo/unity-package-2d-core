using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 타겟까지 직선으로 이동한 뒤, 방향/속도/거리 세그먼트를 순서대로 이어서 이동하는 발사체입니다.
    /// - 첫 구간은 기존 MoveSpeed와 SpeedMultiplier를 사용해 StartPoint에서 TargetPoint까지 이동합니다.
    /// - 이후 구간은 MoveSegments에 기록된 방향, 속도, 거리 값을 사용합니다.
    /// - DamageApplyMode가 PeriodicOverlap이면 이동 중 일정 주기로 겹친 대상에게 데미지를 적용합니다.
    /// </summary>
    public sealed class ProjectileLinearThenSegments : ProjectileBase
    {
        private readonly HashSet<CharacterBase> _targetsInTick = new();
        private RouteLeg[] _legs;
        private float _totalDuration;
        private float _tickElapsed;
        private bool _spawnTickApplied;
        private Vector2 _initialDirection;

        /// <summary>
        /// 이동 경로의 한 구간을 표현합니다.
        /// - ProjectileBase의 진행률은 전체 시간 기준으로 해석하고, 각 구간은 별도 duration을 가집니다.
        /// </summary>
        private readonly struct RouteLeg
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;
            public readonly Vector2 Direction;
            public readonly float Duration;

            /// <summary>
            /// 이동 구간 데이터를 생성합니다.
            /// </summary>
            /// <param name="start">구간 시작 위치입니다.</param>
            /// <param name="end">구간 종료 위치입니다.</param>
            /// <param name="direction">구간 이동 방향입니다.</param>
            /// <param name="duration">구간 이동 시간입니다.</param>
            public RouteLeg(Vector2 start, Vector2 end, Vector2 direction, float duration)
            {
                Start = start;
                End = end;
                Direction = direction;
                Duration = duration;
            }
        }

        /// <summary>
        /// 즉시 충돌 데미지를 사용할지 결정합니다.
        /// - OnHitDestroy이면서 충돌 즉시 제거 정책일 때만 기존 방식처럼 충돌과 동시에 종료합니다.
        /// - 경로 끝까지 유지하는 정책에서는 즉시 충돌 처리 대신 라우트 완주를 우선합니다.
        /// </summary>
        protected override bool ShouldHandleImmediateCollisionDamage
        {
            get
            {
                return EffectiveDamageApplyMode == ProjectileConstants.DamageApplyMode.OnHitDestroy &&
                       EffectiveHitLifetimeMode == ProjectileConstants.HitLifetimeMode.DestroyOnTargetHit;
            }
        }

        /// <summary>
        /// 카메라 밖 제거 여부를 결정합니다.
        /// - 마지막 경로 지점까지 유지하는 정책이면 카메라 밖으로 나가더라도 자동 제거하지 않습니다.
        /// </summary>
        protected override bool ShouldDestroyWhenOutOfView
            => EffectiveHitLifetimeMode != ProjectileConstants.HitLifetimeMode.KeepUntilRouteEnd;

        /// <summary>
        /// 좌표 타겟까지 직선으로 이동한 뒤 세그먼트 이동 경로를 구성합니다.
        /// </summary>
        /// <param name="targetPos">첫 직선 이동의 목표 월드 좌표입니다.</param>
        public override void Launch(Vector2 targetPos)
        {
            base.Launch(targetPos);

            _initialDirection = Direction.sqrMagnitude > 1e-6f ? Direction : Vector2.right;
            BuildRoute();
            ApplySpawnTickIfNeeded();
        }

        /// <summary>
        /// 전체 이동 시간 진행률을 기준으로 현재 위치를 계산합니다.
        /// </summary>
        /// <param name="t">0~1 사이의 전체 시간 진행률입니다.</param>
        /// <returns>현재 이동 경로 위의 월드 위치입니다.</returns>
        protected override Vector2 ComputePosition(float t)
        {
            if (_legs == null || _legs.Length == 0 || _totalDuration <= 0f)
                return TargetPoint;

            float elapsed = Mathf.Clamp01(t) * _totalDuration;
            float accumulated = 0f;

            for (int i = 0; i < _legs.Length; i++)
            {
                RouteLeg leg = _legs[i];
                if (leg.Duration <= 0f)
                    continue;

                if (elapsed <= accumulated + leg.Duration)
                {
                    float legT = (elapsed - accumulated) / leg.Duration;
                    Direction = leg.Direction;
                    return Vector2.Lerp(leg.Start, leg.End, Mathf.Clamp01(legT));
                }

                accumulated += leg.Duration;
            }

            RouteLeg last = _legs[^1];
            Direction = last.Direction;
            return last.End;
        }

        /// <summary>
        /// 발사체 이동 후 주기 데미지 틱을 갱신합니다.
        /// </summary>
        /// <param name="newPos">이번 스텝에서 적용된 새 위치입니다.</param>
        /// <param name="delta">이전 위치에서 새 위치까지의 이동량입니다.</param>
        /// <param name="normalizedTime">전체 이동 기준 진행률입니다.</param>
        protected override void OnProjectileMoved(Vector2 newPos, Vector2 delta, float normalizedTime)
        {
            base.OnProjectileMoved(newPos, delta, normalizedTime);

            if (EffectiveDamageApplyMode != ProjectileConstants.DamageApplyMode.PeriodicOverlap)
                return;

            float interval = EffectiveTickDamageInterval;
            if (interval <= 0f)
                return;

            _tickElapsed += Time.fixedDeltaTime;
            if (_tickElapsed < interval)
                return;

            _tickElapsed = 0f;
            ApplyPeriodicOverlapDamage();
        }

        /// <summary>
        /// 첫 타겟 직선 구간과 이후 세그먼트 구간을 하나의 시간 기반 경로로 구성합니다.
        /// </summary>
        private void BuildRoute()
        {
            var legs = new List<RouteLeg>();
            Vector2 current = StartPoint;

            AddLeg(legs, current, TargetPoint, Speed);
            current = TargetPoint;

            ProjectileMoveSegment[] segments = Info != null ? Info.MoveSegments : null;
            if (segments != null)
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    ProjectileMoveSegment segment = segments[i];
                    Vector2 dir = ResolveSegmentDirection(segment.Direction);
                    float distance = Mathf.Max(0f, segment.Distance);
                    if (distance <= 0f)
                        continue;

                    float speed = ResolveSegmentSpeed(segment.Speed);
                    Vector2 next = current + (dir * distance);
                    AddLeg(legs, current, next, speed);
                    current = next;
                }
            }

            _legs = legs.ToArray();
            _totalDuration = 0f;
            for (int i = 0; i < _legs.Length; i++)
                _totalDuration += Mathf.Max(0f, _legs[i].Duration);

            TargetPoint = current;
            JourneyLength = _totalDuration;
            Speed = 1f;
            StartTime = Time.time;
            PrevPos = StartPoint;
            transform.position = StartPoint;
        }

        /// <summary>
        /// 경로 목록에 한 구간을 추가합니다.
        /// </summary>
        /// <param name="legs">구간을 누적할 목록입니다.</param>
        /// <param name="start">구간 시작 위치입니다.</param>
        /// <param name="end">구간 종료 위치입니다.</param>
        /// <param name="speed">구간 이동 속도입니다.</param>
        private static void AddLeg(List<RouteLeg> legs, Vector2 start, Vector2 end, float speed)
        {
            if (legs == null)
                return;

            float distance = Vector2.Distance(start, end);
            if (distance <= 0f)
                return;

            float safeSpeed = Mathf.Max(0.01f, speed);
            Vector2 direction = (end - start).normalized;
            legs.Add(new RouteLeg(start, end, direction, distance / safeSpeed));
        }

        /// <summary>
        /// 세그먼트 방향을 테이블의 방향 기준에 맞춰 월드 방향으로 변환합니다.
        /// </summary>
        /// <param name="sourceDirection">테이블에 기록된 방향 벡터입니다.</param>
        /// <returns>정규화된 월드 방향입니다.</returns>
        private Vector2 ResolveSegmentDirection(Vector2 sourceDirection)
        {
            if (sourceDirection.sqrMagnitude <= 1e-6f)
                return ResolveDefaultSegmentDirection();

            Vector2 dir = sourceDirection.normalized;

            if (Info == null ||
                Info.SegmentDirectionMode != ProjectileConstants.SegmentDirectionMode.InitialDirectionRelative)
            {
                return dir;
            }

            switch (Info.SegmentRelativeAxesMode)
            {
                case ProjectileConstants.SegmentRelativeAxesMode.HorizontalMirror:
                {
                    float horizontalSign = ResolveInitialHorizontalSign();
                    Vector2 worldDir = new Vector2(dir.x * horizontalSign, dir.y);
                    return worldDir.sqrMagnitude > 1e-6f
                        ? worldDir.normalized
                        : ResolveDefaultSegmentDirection();
                }

                case ProjectileConstants.SegmentRelativeAxesMode.Full2D:
                default:
                {
                    Vector2 forward = _initialDirection.sqrMagnitude > 1e-6f ? _initialDirection.normalized : Vector2.right;
                    Vector2 left = new Vector2(-forward.y, forward.x);
                    Vector2 worldDir = (forward * dir.x) + (left * dir.y);
                    return worldDir.sqrMagnitude > 1e-6f ? worldDir.normalized : forward;
                }
            }
        }

        /// <summary>
        /// 방향 입력이 비어 있을 때 사용할 기본 세그먼트 방향을 반환합니다.
        /// - World/Full2D는 최초 타겟 방향을 그대로 사용합니다.
        /// - HorizontalMirror는 사이드뷰 authoring 편의를 위해 좌/우만 반영한 수평 전진 방향을 사용합니다.
        /// </summary>
        /// <returns>기본 세그먼트 이동 방향입니다.</returns>
        private Vector2 ResolveDefaultSegmentDirection()
        {
            if (Info != null &&
                Info.SegmentDirectionMode == ProjectileConstants.SegmentDirectionMode.InitialDirectionRelative &&
                Info.SegmentRelativeAxesMode == ProjectileConstants.SegmentRelativeAxesMode.HorizontalMirror)
            {
                return new Vector2(ResolveInitialHorizontalSign(), 0f);
            }

            return _initialDirection.sqrMagnitude > 1e-6f ? _initialDirection.normalized : Vector2.right;
        }

        /// <summary>
        /// HorizontalMirror 모드에서 사용할 초기 수평 방향 부호를 결정합니다.
        /// - 최초 타겟 방향의 X 성분이 유효하면 이를 우선 사용합니다.
        /// - X 성분이 거의 0이면 발사 주체의 현재 Facing을 대체 기준으로 사용합니다.
        /// - 둘 다 사용할 수 없으면 우측(+1) 기준으로 보정합니다.
        /// </summary>
        /// <returns>좌측은 -1, 우측은 1입니다.</returns>
        private float ResolveInitialHorizontalSign()
        {
            if (Mathf.Abs(_initialDirection.x) > 1e-4f)
                return Mathf.Sign(_initialDirection.x);

            if (FromCharacter)
            {
                Vector2 facing = CharacterConstants.FacingToVector2(FromCharacter.CurrentFacing);
                if (Mathf.Abs(facing.x) > 1e-4f)
                    return Mathf.Sign(facing.x);
            }

            return 1f;
        }

        /// <summary>
        /// 세그먼트 속도에 런타임 속도 배율을 반영합니다.
        /// </summary>
        /// <param name="segmentSpeed">테이블에 기록된 세그먼트 속도입니다.</param>
        /// <returns>런타임 배율이 적용된 안전한 속도입니다.</returns>
        private float ResolveSegmentSpeed(float segmentSpeed)
        {
            float tableSpeed = segmentSpeed > 0f ? segmentSpeed : Info != null ? Info.MoveSpeed : 0f;
            float speedMultiplier = Runtime != null ? Runtime.SpeedMultiplier : 1f;
            return Mathf.Max(0.01f, tableSpeed * Mathf.Max(0.01f, speedMultiplier));
        }

        /// <summary>
        /// TickOnSpawn이 켜져 있으면 발사 직후 1회 주기 데미지를 적용합니다.
        /// </summary>
        private void ApplySpawnTickIfNeeded()
        {
            if (_spawnTickApplied)
                return;

            if (Info == null ||
                EffectiveDamageApplyMode != ProjectileConstants.DamageApplyMode.PeriodicOverlap ||
                !Info.TickOnSpawn)
            {
                return;
            }

            _spawnTickApplied = true;
            ApplyPeriodicOverlapDamage();
        }

        /// <summary>
        /// 현재 발사체 Collider와 겹친 대상에게 Tick 단위로 데미지를 적용합니다.
        /// - 같은 Tick 안에서 동일 캐릭터가 여러 HitArea Collider로 잡혀도 1회만 처리합니다.
        /// </summary>
        private void ApplyPeriodicOverlapDamage()
        {
            Collider2D[] results = GetOverlapResultsBuffer();
            int count = OverlapHitCollider(results);
            if (count <= 0)
                return;

            _targetsInTick.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider2D col = results[i];
                if (!col || col == HitCollider)
                    continue;

                if (!TryResolveDamageTarget(col, out CharacterBase target))
                    continue;

                if (!_targetsInTick.Add(target))
                    continue;

                TryApplyDamageToCollider(col, playHitVisual: true);
            }
        }
    }
}
