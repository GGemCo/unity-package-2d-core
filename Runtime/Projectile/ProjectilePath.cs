using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 지정된 경로 점을 따라 이동하는 프로젝타일입니다.
    /// - PathPoints를 폴리라인으로 해석해 거리 기준으로 이동합니다.
    /// - DamageApplyMode가 PeriodicOverlap이면 이동 중 TickDamageInterval마다 겹친 대상에게 데미지를 줍니다.
    /// </summary>
    public sealed class ProjectilePath : ProjectileBase
    {
        private Vector2[] _worldPathPoints;
        private float[] _segmentLengths;
        private float _totalPathLength;
        private float _tickElapsed;
        private bool _spawnTickApplied;
        private readonly HashSet<CharacterBase> _targetsInTick = new();

        /// <summary>
        /// Path 타입이 즉시 충돌 데미지를 사용할지 확인합니다.
        /// - PeriodicOverlap/None은 이동 중 충돌로 제거되지 않아야 하므로 false를 반환합니다.
        /// </summary>
        protected override bool ShouldHandleImmediateCollisionDamage
        {
            get
            {
                if (Info == null)
                    return false;

                return Info.DamageApplyMode == ProjectileConstants.DamageApplyMode.OnHitDestroy;
            }
        }

        /// <summary>
        /// 좌표 타겟으로 발사하고, 발사 시작점/목표점을 기준으로 경로를 구성합니다.
        /// </summary>
        /// <param name="targetPos">발사 목표 월드 좌표입니다.</param>
        public override void Launch(Vector2 targetPos)
        {
            base.Launch(targetPos);
            BuildWorldPath();
            ApplyDurationOverrideIfNeeded();
            ApplySpawnTickIfNeeded();
        }

        /// <summary>
        /// 진행률에 해당하는 경로상의 월드 위치를 계산합니다.
        /// </summary>
        /// <param name="t">0~1 이동 진행률입니다.</param>
        /// <returns>경로 위의 월드 위치입니다.</returns>
        protected override Vector2 ComputePosition(float t)
        {
            if (_worldPathPoints == null || _worldPathPoints.Length == 0)
                return Vector2.Lerp(StartPoint, TargetPoint, Mathf.Clamp01(t));

            if (_worldPathPoints.Length == 1 || _totalPathLength <= 0f)
                return _worldPathPoints[0];

            float distance = Mathf.Clamp01(t) * _totalPathLength;
            float accumulated = 0f;

            for (int i = 0; i < _segmentLengths.Length; i++)
            {
                float segmentLength = _segmentLengths[i];
                if (segmentLength <= 0f)
                    continue;

                if (distance <= accumulated + segmentLength)
                {
                    float segmentT = (distance - accumulated) / segmentLength;
                    return Vector2.Lerp(_worldPathPoints[i], _worldPathPoints[i + 1], segmentT);
                }

                accumulated += segmentLength;
            }

            return _worldPathPoints[^1];
        }

        /// <summary>
        /// 이동 후 주기 데미지 Tick을 갱신합니다.
        /// </summary>
        /// <param name="newPos">이번 스텝에서 적용된 새 위치입니다.</param>
        /// <param name="delta">이전 위치에서 새 위치까지의 이동량입니다.</param>
        /// <param name="normalizedTime">전체 이동 기준 진행률입니다.</param>
        protected override void OnProjectileMoved(Vector2 newPos, Vector2 delta, float normalizedTime)
        {
            base.OnProjectileMoved(newPos, delta, normalizedTime);

            if (Info == null || Info.DamageApplyMode != ProjectileConstants.DamageApplyMode.PeriodicOverlap)
                return;

            float interval = Mathf.Max(0f, Info.TickDamageInterval);
            if (interval <= 0f)
                return;

            _tickElapsed += Time.fixedDeltaTime;
            if (_tickElapsed < interval)
                return;

            _tickElapsed = 0f;
            ApplyPeriodicOverlapDamage();
        }

        /// <summary>
        /// 테이블의 PathPoints를 월드 좌표 배열로 변환하고, 세그먼트 길이를 캐시합니다.
        /// </summary>
        private void BuildWorldPath()
        {
            Vector2[] source = Info != null ? Info.PathPoints : null;
            if (source == null || source.Length == 0)
            {
                _worldPathPoints = new[] { StartPoint, TargetPoint };
            }
            else
            {
                _worldPathPoints = new Vector2[source.Length];
                for (int i = 0; i < source.Length; i++)
                    _worldPathPoints[i] = ResolveWorldPathPoint(source[i]);
            }

            EnsurePathHasAtLeastTwoPoints();
            CacheSegmentLengths();

            if (_worldPathPoints.Length > 1)
            {
                Direction = (_worldPathPoints[1] - _worldPathPoints[0]).normalized;
                if (Direction.sqrMagnitude < 1e-6f)
                    Direction = Vector2.right;
            }

            StartPoint = _worldPathPoints[0];
            TargetPoint = _worldPathPoints[^1];
            JourneyLength = _totalPathLength;
            PrevPos = StartPoint;
            transform.position = StartPoint;
        }

        /// <summary>
        /// PathCoordinateMode에 따라 테이블 경로 점을 월드 좌표로 변환합니다.
        /// </summary>
        /// <param name="point">테이블에 기록된 경로 점입니다.</param>
        /// <returns>월드 좌표로 변환된 경로 점입니다.</returns>
        private Vector2 ResolveWorldPathPoint(Vector2 point)
        {
            if (Info == null)
                return point;

            return Info.PathCoordinateMode switch
            {
                ProjectileConstants.PathCoordinateMode.World => point,
                ProjectileConstants.PathCoordinateMode.TargetRelative => TargetPoint + point,
                _ => StartPoint + point,
            };
        }

        /// <summary>
        /// 경로 점이 부족할 때 시작점과 목표점으로 최소 경로를 보정합니다.
        /// </summary>
        private void EnsurePathHasAtLeastTwoPoints()
        {
            if (_worldPathPoints == null || _worldPathPoints.Length == 0)
            {
                _worldPathPoints = new[] { StartPoint, TargetPoint };
                return;
            }

            if (_worldPathPoints.Length == 1)
                _worldPathPoints = new[] { _worldPathPoints[0], TargetPoint };
        }

        /// <summary>
        /// 경로 세그먼트 길이와 전체 길이를 계산해 캐시합니다.
        /// </summary>
        private void CacheSegmentLengths()
        {
            int segmentCount = Mathf.Max(0, _worldPathPoints.Length - 1);
            _segmentLengths = new float[segmentCount];
            _totalPathLength = 0f;

            for (int i = 0; i < segmentCount; i++)
            {
                float length = Vector2.Distance(_worldPathPoints[i], _worldPathPoints[i + 1]);
                _segmentLengths[i] = length;
                _totalPathLength += length;
            }
        }

        /// <summary>
        /// PathDuration이 설정되어 있으면 전체 경로를 해당 시간에 이동하도록 Speed를 보정합니다.
        /// </summary>
        private void ApplyDurationOverrideIfNeeded()
        {
            if (Info == null || Info.PathDuration <= 0f || _totalPathLength <= 0f)
                return;

            Speed = _totalPathLength / Mathf.Max(0.01f, Info.PathDuration);
        }

        /// <summary>
        /// TickOnSpawn이 켜져 있으면 발사 직후 1회 주기 데미지를 적용합니다.
        /// </summary>
        private void ApplySpawnTickIfNeeded()
        {
            if (_spawnTickApplied)
                return;

            if (Info == null ||
                Info.DamageApplyMode != ProjectileConstants.DamageApplyMode.PeriodicOverlap ||
                !Info.TickOnSpawn)
            {
                return;
            }

            _spawnTickApplied = true;
            ApplyPeriodicOverlapDamage();
        }

        /// <summary>
        /// 현재 프로젝타일 Collider와 겹친 대상에게 중복 없이 데미지를 적용합니다.
        /// - 같은 Tick 안에서 한 캐릭터가 여러 HitArea Collider로 잡혀도 1회만 처리합니다.
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
