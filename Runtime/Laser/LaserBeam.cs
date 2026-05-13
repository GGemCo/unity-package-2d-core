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
        private bool _hasTargetPoint;
        private Vector2 _launchRaycastDirection;
        private Vector2 _launchVisualDirection;
        private bool _initialized;
        private bool _launched;
        private bool _isWaitingForEndVisual;
        private float _durationSeconds;
        private float _damageStartDelaySeconds;
        private float _damageActiveDurationSeconds;
        private float _damageTickIntervalSeconds;
        private bool _damageTickOnStart;
        private float _maxDistance;
        private float _elapsed;
        private bool _hasCachedStartPoint;
        private Vector2 _cachedStartPoint;
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
            _hasTargetPoint = metadata.UseTargetPositionOverride;
            _durationSeconds = ResolveDurationSeconds(info, metadata);
            _damageStartDelaySeconds = ResolveDamageStartDelaySeconds(info, metadata);
            _damageActiveDurationSeconds = ResolveDamageActiveDurationSeconds(info, metadata);
            _damageTickIntervalSeconds = ResolveDamageTickIntervalSeconds(info, metadata);
            _damageTickOnStart = ResolveDamageTickOnStart(info, metadata);
            _maxDistance = ResolveConfiguredMaxDistance(info, metadata);
            _hasCachedStartPoint = false;
            _cachedStartPoint = default;

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
                Launch(transform.position);
                return;
            }

            _targetObject = target;
            Launch(target.transform.position);
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
            _hasTargetPoint = true;
            Vector2 start = ResolveCurrentStartPoint();
            Vector2 raycastDirectionOnLaunch = ResolveDirectionByRaycastPolicy(start);
            if (raycastDirectionOnLaunch.sqrMagnitude <= 1e-6f)
                raycastDirectionOnLaunch = ResolveOwnerFacingDirection();

            _launchRaycastDirection = raycastDirectionOnLaunch;
            _launchVisualDirection = raycastDirectionOnLaunch;

            Vector2 visualDirectionOnLaunch = ResolveCurrentVisualDirection(raycastDirectionOnLaunch);
            if (visualDirectionOnLaunch.sqrMagnitude <= 1e-6f)
                visualDirectionOnLaunch = raycastDirectionOnLaunch;

            transform.position = start;
            ApplyVisualRotation(visualDirectionOnLaunch);
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
        /// 현재 프레임의 레이저 시작점/판정 방향/표현 방향을 갱신합니다.
        /// - 판정 방향은 RaycastDirectionMode 정책을 따릅니다.
        /// - 표현 방향은 VfxAngleSyncMode 정책을 따릅니다.
        /// </summary>
        /// <param name="now">현재 시간입니다.</param>
        private void EvaluateLaser(float now)
        {
            Vector3 start = ResolveCurrentStartPoint();
            Vector3 raycastDirection = ResolveCurrentRaycastDirection(start);
            if (raycastDirection.sqrMagnitude <= 1e-6f)
                raycastDirection = ResolveOwnerFacingDirection();

            Vector3 visualDirection = ResolveCurrentVisualDirection(raycastDirection);
            if (visualDirection.sqrMagnitude <= 1e-6f)
                visualDirection = raycastDirection;

            ApplyVisualRotation(visualDirection);

            RaycastHit2D bestHit = default;
            bool canApplyDamage = IsDamageWindowActive();
            bool hasBestHit = TryRaycastNearestValidHit(start, raycastDirection, _maxDistance, canApplyDamage, now, out bestHit);
            float beamDistance = hasBestHit ? Mathf.Max(0f, bestHit.distance) : _maxDistance;
            Vector3 end = start + visualDirection * beamDistance;

            if (ResolveVfxAngleSyncMode() == LaserConstants.VfxAngleSyncMode.FollowRaycast
                && hasBestHit
                && bestHit.point != Vector2.zero)
            {
                end = bestHit.point;
            }

            ReleaseExitedTargets();
            CleanupStaleTickEntries();

            transform.position = start;
            _laserVisual?.SetEndpoints(start, end);
            _visual?.OnUpdate(new ProjectileVisualUpdateContext(start, end, start, Vector2.zero, visualDirection));
        }

        /// <summary>
        /// 현재 시작점을 해석합니다.
        /// - 기본은 시전자 위치 + laser 테이블 StartPosition 입니다.
        /// - 런타임 시작점 오버라이드가 있으면 해당 정책을 우선 적용합니다.
        /// - SnapshotAtLaunch 모드면 발사 시점의 시작점을 캐시하여 유지합니다.
        /// </summary>
        /// <returns>현재 프레임의 레이저 시작점입니다.</returns>
        private Vector2 ResolveCurrentStartPoint()
        {
            if (_runtime is { StartPointUpdateMode: LaserConstants.StartPointUpdateMode.SnapshotAtLaunch }
                && _hasCachedStartPoint)
            {
                return _cachedStartPoint;
            }

            Vector2 start = LaserStartPointResolver.ResolveCurrentStartPoint(_info, _runtime, transform.position);
            if (_runtime is { StartPointUpdateMode: LaserConstants.StartPointUpdateMode.SnapshotAtLaunch })
            {
                _cachedStartPoint = start;
                _hasCachedStartPoint = true;
            }

            return start;
        }

        /// <summary>
        /// 현재 프레임의 레이캐스트 방향을 해석합니다.
        /// </summary>
        /// <param name="start">기준 시작점입니다.</param>
        /// <returns>해석된 정규화 Raycast 방향입니다.</returns>
        private Vector2 ResolveCurrentRaycastDirection(Vector2 start)
        {
            if (ShouldUpdateAimContinuously())
            {
                Vector2 continuousDirection = ResolveDirectionByRaycastPolicy(start, false);
                if (continuousDirection.sqrMagnitude > 1e-6f)
                    return continuousDirection.normalized;
            }

            if (_launchRaycastDirection.sqrMagnitude > 1e-6f)
                return _launchRaycastDirection.normalized;

            return ResolveDirectionByRaycastPolicy(start);
        }

        /// <summary>
        /// 현재 프레임의 비주얼 방향을 해석합니다.
        /// - FollowRaycast면 판정 방향과 동일하게 갱신합니다.
        /// - LockAtLaunch면 발사 시점 방향을 유지합니다.
        /// </summary>
        /// <param name="raycastDirection">현재 프레임의 Raycast 방향입니다.</param>
        /// <returns>해석된 정규화 비주얼 방향입니다.</returns>
        private Vector2 ResolveCurrentVisualDirection(Vector2 raycastDirection)
        {
            switch (ResolveVfxAngleSyncMode())
            {
                case LaserConstants.VfxAngleSyncMode.None:
                    return ResolveUnrotatedVisualDirection();
                case LaserConstants.VfxAngleSyncMode.FollowRaycast:
                    return raycastDirection;
            }

            if (_launchVisualDirection.sqrMagnitude > 1e-6f)
                return _launchVisualDirection.normalized;

            return raycastDirection;
        }

        /// <summary>
        /// VFX 각도 미적용 정책에서 사용할 기본 시각 방향을 반환합니다.
        /// </summary>
        /// <returns>현재 Transform의 오른쪽 방향입니다. 유효하지 않으면 월드 오른쪽을 반환합니다.</returns>
        private Vector2 ResolveUnrotatedVisualDirection()
        {
            Vector2 direction = transform.right;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.right;
        }

        /// <summary>
        /// 현재 정책에 맞춰 Raycast 방향을 즉시 계산합니다.
        /// </summary>
        /// <param name="start">기준 시작점입니다.</param>
        /// <param name="allowFallbackToOwnerFacing">타겟 기반 해석이 불가할 때 소유자 바라보기 방향으로 보정할지 여부입니다.</param>
        /// <returns>정규화된 Raycast 방향입니다.</returns>
        private Vector2 ResolveDirectionByRaycastPolicy(Vector2 start, bool allowFallbackToOwnerFacing = true)
        {
            bool hasTargetPoint = false;
            Vector2 targetPoint = default;

            if (_runtime is { UseTargetPositionOverride: true })
            {
                hasTargetPoint = true;
                targetPoint = _runtime.TargetPositionOverride;
            }
            else if (_hasTargetPoint)
            {
                hasTargetPoint = true;
                targetPoint = _targetPoint;
            }

            return LaserAimPolicyUtility.ResolveRaycastDirection(
                _info,
                _runtime,
                _owner,
                _targetObject,
                hasTargetPoint,
                targetPoint,
                start,
                allowFallbackToOwnerFacing);
        }

        /// <summary>
        /// 에임을 지속적으로 갱신할지 여부를 계산합니다.
        /// - 런타임 오버라이드가 우선하며, 없으면 테이블 AimUpdateMode를 사용합니다.
        /// </summary>
        private bool ShouldUpdateAimContinuously()
        {
            if (_runtime is { UpdateAimContinuously: true })
                return true;

            return _info is { AimUpdateMode: LaserConstants.AimUpdateMode.Continuous };
        }

        /// <summary>
        /// Raycast 방향 계산 모드를 해석합니다.
        /// - 런타임 오버라이드가 우선하며, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <returns>적용할 Raycast 방향 계산 모드입니다.</returns>
        private LaserConstants.RaycastDirectionMode ResolveRaycastDirectionMode()
        {
            return LaserAimPolicyUtility.ResolveRaycastDirectionMode(_info, _runtime);
        }

        /// <summary>
        /// VFX 각도 동기화 모드를 해석합니다.
        /// - 런타임 오버라이드가 우선하며, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <returns>적용할 VFX 각도 동기화 모드입니다.</returns>
        private LaserConstants.VfxAngleSyncMode ResolveVfxAngleSyncMode()
        {
            return LaserAimPolicyUtility.ResolveVfxAngleSyncMode(_info, _runtime);
        }

        /// <summary>
        /// Raycast 각도 설정값(도)을 해석합니다.
        /// - 런타임 오버라이드가 우선하며, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <returns>적용할 Raycast 각도(도)입니다.</returns>
        private float ResolveRaycastAngleDeg()
        {
            return LaserAimPolicyUtility.ResolveRaycastAngleDeg(_info, _runtime);
        }

        /// <summary>
        /// 설정된 각도를 기준으로 Raycast 방향을 계산합니다.
        /// - 기본은 +X(오른쪽) 기준입니다.
        /// - 시전자가 좌우 반전된 상태면 각도 부호를 반전하여 좌우 미러링 일관성을 유지합니다.
        /// </summary>
        /// <returns>각도 기반 정규화 방향입니다.</returns>
        private Vector2 ResolveDirectionByConfiguredAngle()
        {
            return LaserAimPolicyUtility.ResolveDirectionByConfiguredAngle(_info, _runtime, _owner);
        }

        /// <summary>
        /// 타겟 정보가 없거나 방향 해석에 실패했을 때 사용할 기본 바라보기 방향을 반환합니다.
        /// </summary>
        /// <returns>시전자 기준 기본 방향(오른쪽/왼쪽)입니다.</returns>
        private Vector2 ResolveOwnerFacingDirection()
        {
            return LaserAimPolicyUtility.ResolveOwnerFacingDirection(_owner);
        }

        /// <summary>
        /// 시작점과 목표점으로부터 방향 벡터를 계산합니다.
        /// </summary>
        /// <param name="start">시작점입니다.</param>
        /// <param name="targetPosition">목표점입니다.</param>
        /// <returns>정규화된 방향 벡터입니다.</returns>
        private Vector2 ResolveDirection(Vector2 start, Vector2 targetPosition)
        {
            return LaserAimPolicyUtility.ResolveDirection(_owner, start, targetPosition);
        }

        /// <summary>
        /// 레이저 방향에 맞게 Transform 회전을 갱신합니다.
        /// </summary>
        /// <param name="direction">적용할 방향 벡터입니다.</param>
        private void ApplyRotation(Vector2 direction)
        {
            if (_info is { RotateByMoveDirection: false })
                return;

            if (direction.sqrMagnitude <= 1e-6f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// VfxAngleSyncMode 정책에 따라 레이저 시각 Transform의 회전 적용 여부를 결정합니다.
        /// </summary>
        /// <param name="direction">적용할 시각 방향 벡터입니다.</param>
        private void ApplyVisualRotation(Vector2 direction)
        {
            if (ResolveVfxAngleSyncMode() == LaserConstants.VfxAngleSyncMode.None)
                return;

            ApplyRotation(direction);
        }

        /// <summary>
        /// 현재 레이저 생존 시간이 데미지 적용 가능 구간에 포함되는지 확인합니다.
        /// </summary>
        /// <returns>데미지 시작 지연을 지났고 활성 지속 시간이 남아 있으면 true를 반환합니다.</returns>
        private bool IsDamageWindowActive()
        {
            if (_elapsed < _damageStartDelaySeconds)
                return false;

            if (_damageActiveDurationSeconds <= 0f)
                return true;

            return _elapsed <= _damageStartDelaySeconds + _damageActiveDurationSeconds;
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
        /// <param name="canApplyDamage">이번 평가에서 데미지를 적용할 수 있는지 여부입니다.</param>
        /// <param name="now">현재 시간입니다.</param>
        /// <param name="bestHit">최종 끝점을 결정하는 유효 적중 결과입니다.</param>
        /// <returns>끝점을 제한하는 유효 적중이 있으면 true를 반환합니다.</returns>
        private bool TryRaycastNearestValidHit(Vector2 start, Vector2 direction, float distance, bool canApplyDamage, float now, out RaycastHit2D bestHit)
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

            LaserConstants.BlockMode blockMode = _info?.BlockMode ?? LaserConstants.BlockMode.StopAtGroundOrHostile;
            LaserConstants.HitMode hitMode = _info?.HitMode ?? LaserConstants.HitMode.FirstHitOnly;

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

                if (canApplyDamage && hitMode == LaserConstants.HitMode.PierceHostiles)
                    TryApplyDamage(col, hit.point, now);
            }

            if (canApplyDamage && hitMode == LaserConstants.HitMode.FirstHitOnly && hasNearestHostile)
                TryApplyDamage(nearestHostile.collider, nearestHostile.point, now);

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
        /// - 데미지 반복 간격이 0 이하이면 진입 시 1회만 적용합니다.
        /// - 데미지 반복 간격이 0보다 크면 대상별 마지막 적용 시각을 기준으로 주기 데미지를 적용합니다.
        /// </summary>
        /// <param name="col">적중 Collider입니다.</param>
        /// <param name="hitPosition">레이저 적중 지점입니다.</param>
        /// <param name="now">현재 시간입니다.</param>
        private void TryApplyDamage(Collider2D col, Vector2 hitPosition, float now)
        {
            if (!CombatHitTargetUtility.TryResolveHostileTarget(_owner, col, out CharacterBase target))
                return;

            _currentTargets.Add(target);

            if (_damageTickIntervalSeconds <= 0f)
            {
                if (!_latchedTargets.Add(target))
                    return;

                ApplyDamageToTarget(target, col, hitPosition);
                return;
            }

            if (!_lastTickDamageTimes.TryGetValue(target, out float lastTime))
            {
                if (!_damageTickOnStart)
                {
                    _lastTickDamageTimes[target] = now;
                    return;
                }
            }
            else if (now - lastTime < _damageTickIntervalSeconds)
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
                ElementGaugeApplications = _runtime?.ElementGaugeApplications,
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
                VfxPresentationPolicy = info.VfxPresentationPolicy,
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
            if (metadata is { UseDurationOverride: true })
                return metadata.DurationOverride >= 0f ? metadata.DurationOverride : -1f;

            if (info != null)
                return Mathf.Max(0f, info.Duration);

            return DefaultDurationSeconds;
        }

        /// <summary>
        /// 레이저 발사 후 데미지 적용을 시작할 지연 시간을 해석합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 데이터입니다.</param>
        /// <param name="metadata">레이저 런타임 메타데이터입니다.</param>
        /// <returns>데미지 시작 지연 시간입니다.</returns>
        private static float ResolveDamageStartDelaySeconds(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata is { UseDamageTimingOverride: true })
                return Mathf.Max(0f, metadata.DamageStartDelayOverride);

            return info != null ? Mathf.Max(0f, info.DamageStartDelay) : 0f;
        }

        /// <summary>
        /// 데미지 판정을 유지할 시간을 해석합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 데이터입니다.</param>
        /// <param name="metadata">레이저 런타임 메타데이터입니다.</param>
        /// <returns>데미지 활성 지속 시간입니다. 0 이하이면 레이저 종료까지 유지합니다.</returns>
        private static float ResolveDamageActiveDurationSeconds(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata is { UseDamageTimingOverride: true })
                return metadata.DamageActiveDurationOverride <= 0f ? -1f : metadata.DamageActiveDurationOverride;

            if (info == null)
                return -1f;

            return info.DamageActiveDuration <= 0f ? -1f : info.DamageActiveDuration;
        }

        /// <summary>
        /// 같은 대상에게 반복 데미지를 줄 간격을 해석합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 데이터입니다.</param>
        /// <param name="metadata">레이저 런타임 메타데이터입니다.</param>
        /// <returns>데미지 틱 간격입니다. 0이면 진입 시 1회만 적용합니다.</returns>
        private static float ResolveDamageTickIntervalSeconds(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata is { UseDamageTimingOverride: true })
                return Mathf.Max(0f, metadata.DamageTickIntervalOverride);

            return info != null ? Mathf.Max(0f, info.DamageTickInterval) : 0f;
        }

        /// <summary>
        /// 데미지 활성 구간에서 처음 감지된 대상에게 즉시 데미지를 줄지 해석합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 데이터입니다.</param>
        /// <param name="metadata">레이저 런타임 메타데이터입니다.</param>
        /// <returns>처음 감지된 대상에게 즉시 데미지를 주면 true를 반환합니다.</returns>
        private static bool ResolveDamageTickOnStart(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata is { UseDamageTimingOverride: true })
                return metadata.DamageTickOnStartOverride;

            return info == null || info.DamageTickOnStart;
        }

        /// <summary>
        /// 레이저 최대 사거리를 해석합니다.
        /// </summary>
        private float ResolveConfiguredMaxDistance(StruckTableLaser info, MetadataLaser metadata)
        {
            if (metadata is { UseMaxDistanceOverride: true, MaxDistanceOverride: > 0f })
                return metadata.MaxDistanceOverride;

            if (info is { MaxDistance: > 0f })
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
