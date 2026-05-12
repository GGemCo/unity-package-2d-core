using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 분리된 레이저 시스템의 실제 런타임 인스턴스입니다.
    /// - ProjectileBase를 상속하지 않는 독립 수명주기를 사용합니다.
    /// - 판정은 Raycast 기반으로 수행합니다.
    /// - 시각 표현은 기존 ProjectileVisualFactory를 재사용합니다.
    /// </summary>
    public sealed class LaserBeam : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 0.25f;
        private const float DefaultMaxDistance = 10f;
        private const int RaycastBufferSize = 32;

        private StruckTableLaser _info;
        private MetadataLaser _runtime;
        private CharacterBase _owner;
        private CharacterBase _targetObject;
        private ConfigCommon.DamageType _damageType;
        private long _damage;
        private int _skillUid;
        private int _attackId;
        private Vector2 _targetPoint;
        private Vector2 _launchDirection;
        private bool _initialized;
        private bool _launched;
        private bool _isWaitingForEndVisual;
        private float _durationSeconds;
        private float _tickIntervalSeconds;
        private float _maxDistance;
        private float _elapsed;
        private readonly RaycastHit2D[] _raycastResults = new RaycastHit2D[RaycastBufferSize];
        private readonly HashSet<CharacterBase> _latchedTargets = new();
        private readonly HashSet<CharacterBase> _currentTargets = new();
        private readonly List<CharacterBase> _releasedTargets = new();
        private readonly Dictionary<CharacterBase, float> _lastTickDamageTimes = new();
        private IProjectileVisual _visual;
        private IProjectileLaserVisual _laserVisual;

        /// <summary>
        /// 레이저를 초기화합니다.
        /// </summary>
        /// <param name="info">정적 레이저 테이블 데이터입니다.</param>
        /// <param name="metadata">런타임 레이저 메타데이터입니다.</param>
        public void Initialize(StruckTableLaser info, MetadataLaser metadata)
        {
            if (info == null || metadata == null)
            {
                Destroy(gameObject);
                return;
            }

            _info = info;
            _runtime = metadata;
            _owner = metadata.Owner;
            _targetObject = metadata.Target;
            _damageType = metadata.DamageType;
            _damage = metadata.Damage;
            _skillUid = metadata.SkillUid;
            _attackId = metadata.AttackId;
            _targetPoint = metadata.TargetPositionOverride;
            _durationSeconds = ResolveDurationSeconds(info, metadata);
            _tickIntervalSeconds = ResolveTickIntervalSeconds(info, metadata);
            _maxDistance = ResolveConfiguredMaxDistance(info, metadata);

            if (_owner != null)
                gameObject.layer = _owner.gameObject.layer;

            StruckTableProjectile visualInfo = CreateVisualProjectileInfo(info);
            _visual = ProjectileVisualFactory.Attach(transform, visualInfo, metadata.ToVisualMetadata());
            _laserVisual = _visual as IProjectileLaserVisual;
            _visual?.OnSpawn(new ProjectileVisualSpawnContext(transform, visualInfo, metadata.ToVisualMetadata()));

            _initialized = true;
        }

        /// <summary>
        /// 고정 타겟 캐릭터를 기준으로 레이저를 발사합니다.
        /// </summary>
        /// <param name="target">조준할 타겟 캐릭터입니다.</param>
        public void Launch(CharacterBase target)
        {
            if (!target)
            {
                Launch((Vector2)transform.position);
                return;
            }

            _targetObject = target;
            Launch((Vector2)target.transform.position);
        }

        /// <summary>
        /// 좌표를 기준으로 레이저를 발사합니다.
        /// </summary>
        /// <param name="targetPosition">조준할 월드 좌표입니다.</param>
        public void Launch(Vector2 targetPosition)
        {
            if (!_initialized)
                return;

            _targetPoint = targetPosition;
            Vector2 start = ResolveCurrentStartPoint();
            _launchDirection = ResolveDirection(start, targetPosition);
            transform.position = start;
            ApplyRotation(_launchDirection);
            _launched = true;
            _elapsed = 0f;

            EvaluateLaser(Time.time);
        }

        /// <summary>
        /// 레이저 수명 동안 판정과 시각 표현을 갱신합니다.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_initialized || !_launched || _isWaitingForEndVisual)
                return;

            _elapsed += Time.fixedDeltaTime;
            EvaluateLaser(Time.time);

            if (_durationSeconds >= 0f && _elapsed >= _durationSeconds)
            {
                if (TryPlayEndAndDestroy())
                    return;

                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 현재 프레임의 레이저 시작점/방향/판정을 갱신합니다.
        /// </summary>
        /// <param name="now">현재 시간입니다.</param>
        private void EvaluateLaser(float now)
        {
            Vector2 start = ResolveCurrentStartPoint();
            Vector2 direction = ResolveCurrentDirection(start);
            if (direction.sqrMagnitude <= 1e-6f)
                direction = Vector2.right;

            ApplyRotation(direction);

            RaycastHit2D bestHit = default;
            bool hasBestHit = TryRaycastNearestValidHit(start, direction, _maxDistance, out bestHit);
            Vector3 end = hasBestHit
                ? (bestHit.point != Vector2.zero ? (Vector3)bestHit.point : (Vector3)(start + direction * bestHit.distance))
                : (Vector3)(start + direction * _maxDistance);

            ReleaseExitedTargets();
            CleanupStaleTickEntries();

            transform.position = start;
            _laserVisual?.SetEndpoints(start, end);
            _visual?.OnUpdate(new ProjectileVisualUpdateContext(start, (Vector2)end, start, Vector2.zero, direction));
        }

        /// <summary>
        /// 현재 시작점을 해석합니다.
        /// - 시전자가 있으면 매 프레임 시전자 위치를 기준으로 갱신합니다.
        /// - StartPosition은 테이블 오프셋을 그대로 재사용합니다.
        /// </summary>
        /// <returns>현재 프레임의 레이저 시작점입니다.</returns>
        private Vector2 ResolveCurrentStartPoint()
        {
            Vector2 start = transform.position;
            if (_owner != null)
                start = _owner.transform.position;

            if (_info != null)
                start += _info.StartPosition;

            return start;
        }

        /// <summary>
        /// 현재 프레임의 발사 방향을 해석합니다.
        /// </summary>
        /// <param name="start">기준 시작점입니다.</param>
        /// <returns>해석된 정규화 방향입니다.</returns>
        private Vector2 ResolveCurrentDirection(Vector2 start)
        {
            if (ShouldUpdateAimContinuously())
            {
                if (_targetObject != null)
                    return ResolveDirection(start, _targetObject.transform.position);

                if (_runtime.UseTargetPositionOverride)
                    return ResolveDirection(start, _runtime.TargetPositionOverride);
            }

            if (_launchDirection.sqrMagnitude > 1e-6f)
                return _launchDirection.normalized;

            if (_targetObject != null)
                return ResolveDirection(start, _targetObject.transform.position);

            if (_runtime != null && _runtime.UseTargetPositionOverride)
                return ResolveDirection(start, _runtime.TargetPositionOverride);

            Vector2 fallback = Vector2.right;
            if (_owner != null && _owner.IsFlipped())
                fallback = Vector2.left;

            return fallback;
        }

        /// <summary>
        /// 에임을 지속적으로 갱신할지 여부를 계산합니다.
        /// - 런타임 오버라이드가 우선하며, 없으면 테이블 AimUpdateMode를 사용합니다.
        /// </summary>
        private bool ShouldUpdateAimContinuously()
        {
            if (_runtime != null && _runtime.UpdateAimContinuously)
                return true;

            return _info != null && _info.AimUpdateMode == LaserConstants.AimUpdateMode.Continuous;
        }

        /// <summary>
        /// 시작점과 목표점으로부터 방향 벡터를 계산합니다.
        /// </summary>
        /// <param name="start">시작점입니다.</param>
        /// <param name="targetPosition">목표점입니다.</param>
        /// <returns>정규화된 방향 벡터입니다.</returns>
        private Vector2 ResolveDirection(Vector2 start, Vector2 targetPosition)
        {
            Vector2 direction = (targetPosition - start);
            if (direction.sqrMagnitude <= 1e-6f)
            {
                if (_owner != null && _owner.IsFlipped())
                    return Vector2.left;

                return Vector2.right;
            }

            return direction.normalized;
        }

        /// <summary>
        /// 레이저 방향에 맞게 Transform 회전을 갱신합니다.
        /// </summary>
        /// <param name="direction">적용할 방향 벡터입니다.</param>
        private void ApplyRotation(Vector2 direction)
        {
            if (_info != null && !_info.RotateByMoveDirection)
                return;

            if (direction.sqrMagnitude <= 1e-6f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 현재 정책에 맞는 레이캐스트 적중 결과를 찾습니다.
        /// - FirstHitOnly는 가장 가까운 적대 대상 하나만 데미지를 적용합니다.
        /// - PierceHostiles는 사거리 내의 모든 적대 대상을 처리합니다.
        /// - 빔의 최종 끝점은 BlockMode와 HitMode를 함께 고려해 계산합니다.
        /// </summary>
        /// <param name="start">레이캐스트 시작점입니다.</param>
        /// <param name="direction">레이캐스트 방향입니다.</param>
        /// <param name="distance">최대 사거리입니다.</param>
        /// <param name="bestHit">최종 끝점을 결정하는 유효 적중 결과입니다.</param>
        /// <returns>끝점을 제한하는 유효 적중이 있으면 true를 반환합니다.</returns>
        private bool TryRaycastNearestValidHit(Vector2 start, Vector2 direction, float distance, out RaycastHit2D bestHit)
        {
            bestHit = default;

            int layerMask = Physics2D.GetLayerCollisionMask(gameObject.layer);
            ContactFilter2D filter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true
            };
            filter.SetLayerMask(layerMask);

            int count = Physics2D.Raycast(start, direction, filter, _raycastResults, distance);
            if (count <= 0)
                return false;

            _currentTargets.Clear();

            bool hasNearestGround = false;
            RaycastHit2D nearestGround = default;
            bool hasNearestHostile = false;
            RaycastHit2D nearestHostile = default;

            LaserConstants.BlockMode blockMode = _info != null
                ? _info.BlockMode
                : LaserConstants.BlockMode.StopAtGroundOrHostile;
            LaserConstants.HitMode hitMode = _info != null
                ? _info.HitMode
                : LaserConstants.HitMode.FirstHitOnly;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _raycastResults[i];
                Collider2D col = hit.collider;
                if (!col)
                    continue;

                if (_owner != null)
                {
                    CharacterBase hitCharacter = CombatHitTargetUtility.ResolveTargetCharacter(col);
                    if (hitCharacter == _owner)
                        continue;
                }

                bool isGround = col.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround));
                if (isGround)
                {
                    if (!hasNearestGround || hit.distance < nearestGround.distance)
                    {
                        hasNearestGround = true;
                        nearestGround = hit;
                    }

                    continue;
                }

                if (!CombatHitTargetUtility.TryResolveHostileTarget(_owner, col, out _))
                    continue;

                if (!hasNearestHostile || hit.distance < nearestHostile.distance)
                {
                    hasNearestHostile = true;
                    nearestHostile = hit;
                }

                if (hitMode == LaserConstants.HitMode.PierceHostiles)
                    TryApplyDamage(col, hit.point, Time.time);
            }

            if (hitMode == LaserConstants.HitMode.FirstHitOnly && hasNearestHostile)
                TryApplyDamage(nearestHostile.collider, nearestHostile.point, Time.time);

            switch (blockMode)
            {
                case LaserConstants.BlockMode.StopAtGround:
                    if (hasNearestGround)
                    {
                        bestHit = nearestGround;
                        return true;
                    }
                    break;

                case LaserConstants.BlockMode.StopAtHostile:
                    if (hitMode == LaserConstants.HitMode.FirstHitOnly && hasNearestHostile)
                    {
                        bestHit = nearestHostile;
                        return true;
                    }
                    break;

                case LaserConstants.BlockMode.StopAtGroundOrHostile:
                    if (hitMode == LaserConstants.HitMode.FirstHitOnly && hasNearestHostile)
                    {
                        if (!hasNearestGround || nearestHostile.distance <= nearestGround.distance)
                        {
                            bestHit = nearestHostile;
                            return true;
                        }
                    }

                    if (hasNearestGround)
                    {
                        bestHit = nearestGround;
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 현재 적중한 대상에게 데미지를 적용합니다.
        /// - tickInterval이 0 이하이면 진입 시 1회만 적용합니다.
        /// - tickInterval이 0보다 크면 대상별 마지막 적용 시각을 기준으로 주기 데미지를 적용합니다.
        /// </summary>
        /// <param name="col">적중 Collider입니다.</param>
        /// <param name="hitPosition">레이저 적중 지점입니다.</param>
        /// <param name="now">현재 시간입니다.</param>
        private void TryApplyDamage(Collider2D col, Vector2 hitPosition, float now)
        {
            if (!CombatHitTargetUtility.TryResolveHostileTarget(_owner, col, out CharacterBase target))
                return;

            _currentTargets.Add(target);

            if (_tickIntervalSeconds <= 0f)
            {
                if (_latchedTargets.Contains(target))
                    return;

                _latchedTargets.Add(target);
                ApplyDamageToTarget(target, col, hitPosition);
                return;
            }

            if (!_lastTickDamageTimes.TryGetValue(target, out float lastTime))
            {
                if (_info != null && !_info.TickOnSpawn)
                {
                    _lastTickDamageTimes[target] = now;
                    return;
                }
            }
            else if (now - lastTime < _tickIntervalSeconds)
            {
                return;
            }

            _lastTickDamageTimes[target] = now;
            ApplyDamageToTarget(target, col, hitPosition);
        }

        /// <summary>
        /// 지정한 타겟에게 실제 데미지 및 히트 연출을 적용합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 타겟 캐릭터입니다.</param>
        /// <param name="hitCollider">적중 Collider입니다.</param>
        /// <param name="hitPosition">적중 지점입니다.</param>
        private void ApplyDamageToTarget(CharacterBase target, Collider2D hitCollider, Vector2 hitPosition)
        {
            if (!target)
                return;

            _visual?.OnHit(new ProjectileVisualHitContext(hitPosition, _owner, hitCollider));

            MetadataDamage metadataDamage = new MetadataDamage
            {
                damage = _damage,
                attacker = _owner ? _owner.gameObject : gameObject,
                damageType = _damageType,
                SkillUid = _skillUid,
                AttackId = _attackId,
                ElementGaugeApplications = _runtime != null ? _runtime.ElementGaugeApplications : null,
            };

            target.TakeDamage(metadataDamage);
        }

        /// <summary>
        /// 이번 프레임에 더 이상 빔 위에 존재하지 않는 타겟의 1회성 적중 잠금을 해제합니다.
        /// </summary>
        private void ReleaseExitedTargets()
        {
            if (_latchedTargets.Count == 0)
                return;

            _releasedTargets.Clear();
            foreach (CharacterBase target in _latchedTargets)
            {
                if (!target || !_currentTargets.Contains(target))
                    _releasedTargets.Add(target);
            }

            for (int i = 0; i < _releasedTargets.Count; i++)
                _latchedTargets.Remove(_releasedTargets[i]);
        }

        /// <summary>
        /// 더 이상 빔 위에 없는 타겟의 틱 데미지 기록을 정리합니다.
        /// </summary>
        private void CleanupStaleTickEntries()
        {
            if (_lastTickDamageTimes.Count == 0)
                return;

            _releasedTargets.Clear();
            foreach (KeyValuePair<CharacterBase, float> pair in _lastTickDamageTimes)
            {
                CharacterBase target = pair.Key;
                if (!target || !_currentTargets.Contains(target))
                    _releasedTargets.Add(target);
            }

            for (int i = 0; i < _releasedTargets.Count; i++)
                _lastTickDamageTimes.Remove(_releasedTargets[i]);
        }

        /// <summary>
        /// 레이저 종료 시 End 비주얼이 있으면 재생 후 제거합니다.
        /// </summary>
        /// <returns>End 비주얼 재생을 시작했으면 true를 반환합니다.</returns>
        private bool TryPlayEndAndDestroy()
        {
            if (_isWaitingForEndVisual || _visual == null)
                return _isWaitingForEndVisual;

            _isWaitingForEndVisual = _visual.TryPlayEnd(HandleEndVisualComplete);
            return _isWaitingForEndVisual;
        }

        /// <summary>
        /// End 비주얼이 끝났을 때 실제 GameObject를 제거합니다.
        /// </summary>
        private void HandleEndVisualComplete()
        {
            _isWaitingForEndVisual = false;

            if (this == null || gameObject == null)
                return;

            Destroy(gameObject);
        }


        /// <summary>
        /// 레이저 전용 정적 데이터를 비주얼 시스템에서 요구하는 Projectile 정적 데이터로 변환합니다.
        /// </summary>
        /// <param name="info">레이저 정적 데이터입니다.</param>
        /// <returns>비주얼 표현에 필요한 Projectile 호환 정적 데이터입니다.</returns>
        private static StruckTableProjectile CreateVisualProjectileInfo(StruckTableLaser info)
        {
            if (info == null)
                return new StruckTableProjectile();

            return new StruckTableProjectile
            {
                Uid = info.Uid,
                Type = ProjectileConstants.Type.Laser,
                Name = info.Name,
                VfxUid = info.VfxUid,
                VfxScale = info.VfxScale,
                StartPosition = info.StartPosition,
                HitVfxUid = info.HitVfxUid,
                RotateByMoveDirection = info.RotateByMoveDirection,
            };
        }

        /// <summary>
        /// 레이저 지속 시간을 해석합니다.
        /// </summary>
        private static float ResolveDurationSeconds(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata != null && metadata.UseDurationOverride)
                return metadata.DurationOverride >= 0f ? metadata.DurationOverride : -1f;

            if (info != null)
                return Mathf.Max(0f, info.Duration);

            return DefaultDurationSeconds;
        }

        /// <summary>
        /// 레이저 틱 간격을 해석합니다.
        /// </summary>
        private static float ResolveTickIntervalSeconds(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata != null && metadata.UseTickIntervalOverride)
                return Mathf.Max(0f, metadata.TickIntervalOverride);

            if (info != null)
                return Mathf.Max(0f, info.TickInterval);

            return 0f;
        }

        /// <summary>
        /// 레이저 최대 사거리를 해석합니다.
        /// </summary>
        private float ResolveConfiguredMaxDistance(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata != null && metadata.UseMaxDistanceOverride && metadata.MaxDistanceOverride > 0f)
                return metadata.MaxDistanceOverride;

            if (info != null && info.MaxDistance > 0f)
                return info.MaxDistance;

            Vector2 start = ResolveCurrentStartPoint();
            if (metadata != null)
            {
                if (metadata.Target != null)
                    return Mathf.Max(0.01f, Vector2.Distance(start, metadata.Target.transform.position));

                if (metadata.UseTargetPositionOverride)
                    return Mathf.Max(0.01f, Vector2.Distance(start, metadata.TargetPositionOverride));
            }

            return DefaultMaxDistance;
        }


        /// <summary>
        /// 레이저가 제거될 때 비주얼 정리를 수행합니다.
        /// </summary>
        private void OnDestroy()
        {
            _visual?.OnDespawn();
        }
    }
}
