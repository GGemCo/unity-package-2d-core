using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터의 스폰 홈을 보관하고 Soft/Hard Leash 판정, Evade, 홈 복귀를 실행하는 컨트롤러입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterHomeLeashController : MonoBehaviour, IMonsterPoolLifecycle
    {
        private const float MonitorIntervalSeconds = 0.1f;
        private const float ReturnMoveRefreshIntervalSeconds = 0.1f;

        private readonly List<IMonsterLeashLifecycle> _leashLifecycles = new(8);

        private Monster _owner;
        private ControllerMonster _movementDriver;
        private MonsterLeashProfile _profile;
        private MonsterHomeContext _home;
        private MonsterLeashState _state = MonsterLeashState.Disabled;
        private MonsterLeashTrigger _lastTrigger;
        private float _softLimitDeadline;
        private float _returnStartedTime;
        private float _returnDelayDeadline;
        private float _nextMonitorTime;
        private float _nextReturnMoveRefreshTime;

        /// <summary>현재 Leash 런타임 상태입니다.</summary>
        public MonsterLeashState State => _state;

        /// <summary>마지막으로 Evade를 시작한 원인입니다.</summary>
        public MonsterLeashTrigger LastTrigger => _lastTrigger;

        /// <summary>현재 홈 컨텍스트입니다.</summary>
        public MonsterHomeContext Home => _home;

        /// <summary>현재 홈 복귀 또는 재활성 대기 중인지 여부입니다.</summary>
        public bool IsReturnLocked =>
            _state == MonsterLeashState.ReturningHome ||
            _state == MonsterLeashState.ReturnDelay;

        /// <summary>현재 Leash 정책으로 피해를 무시해야 하는지 여부입니다.</summary>
        public bool IsDamageImmune => IsReturnLocked && _profile.InvulnerableDuringReturn;

        /// <summary>
        /// 소유 몬스터와 이동 드라이버를 연결하고 현재 위치를 임시 홈으로 초기화합니다.
        /// </summary>
        /// <param name="owner">Leash를 관리할 몬스터입니다.</param>
        /// <param name="movementDriver">홈 복귀 이동을 실행할 몬스터 컨트롤러입니다.</param>
        public void Initialize(Monster owner, ControllerMonster movementDriver)
        {
            _owner = owner;
            _movementDriver = movementDriver;
            if (_owner != null)
            {
                _home = new MonsterHomeContext(
                    _owner.transform.position,
                    _owner.IsFlipped(),
                    _owner.CharacterRegenData != null ? _owner.CharacterRegenData.MapUid : 0,
                    isValid: true);
            }

            ResetRuntimeState();
        }

        /// <summary>
        /// 테이블에서 정규화한 Leash 정책을 적용합니다.
        /// </summary>
        /// <param name="profile">적용할 Leash 프로필입니다.</param>
        public void Configure(MonsterLeashProfile profile)
        {
            _profile = profile;
            if (!_profile.IsEnabled)
            {
                ResetRuntimeState();
                _state = MonsterLeashState.Disabled;
                return;
            }

            if (!IsReturnLocked)
            {
                _state = MonsterLeashState.Monitoring;
            }
        }

        /// <summary>
        /// 리젠 데이터 또는 현재 Transform을 기준으로 홈 위치와 초기 방향을 갱신합니다.
        /// </summary>
        /// <param name="regenData">맵 배치 또는 풀 대여 시 전달된 리젠 데이터입니다.</param>
        public void CaptureHome(CharacterRegenData regenData)
        {
            if (_owner == null)
            {
                return;
            }

            if (regenData != null)
            {
                _home = new MonsterHomeContext(
                    new Vector3(regenData.x, regenData.y, _owner.transform.position.z),
                    regenData.IsFlip,
                    regenData.MapUid,
                    isValid: true);
            }
            else
            {
                _home = new MonsterHomeContext(
                    _owner.transform.position,
                    _owner.IsFlipped(),
                    mapUid: 0,
                    isValid: true);
            }

            ResetRuntimeState();
            _state = _profile.IsEnabled
                ? MonsterLeashState.Monitoring
                : MonsterLeashState.Disabled;
        }

        /// <summary>
        /// 현재 몬스터와 전투 타겟의 홈 이탈 거리를 감시하거나 홈 복귀 이동을 갱신합니다.
        /// </summary>
        private void Update()
        {
            if (_owner == null || !_owner.isActiveAndEnabled || _owner.IsStatusDead())
            {
                return;
            }

            if (_state == MonsterLeashState.ReturningHome)
            {
                TickReturnHome();
                return;
            }

            if (_state == MonsterLeashState.ReturnDelay)
            {
                if (Time.time >= _returnDelayDeadline)
                {
                    CompleteReturn();
                }
                return;
            }

            if (!_profile.IsEnabled || !_home.IsValid)
            {
                _state = MonsterLeashState.Disabled;
                return;
            }

            if (Time.time < _nextMonitorTime)
            {
                return;
            }

            _nextMonitorTime = Time.time + MonitorIntervalSeconds;
            TickMonitoring();
        }

        /// <summary>
        /// 전투 중 몬스터와 현재 타겟의 홈 이탈 거리를 기준으로 Soft/Hard Leash를 평가합니다.
        /// </summary>
        private void TickMonitoring()
        {
            bool isEngaged = _owner.IsAggro() || _owner.ThreatTargetCount > 0;
            if (!isEngaged)
            {
                CancelSoftLimitPending();
                return;
            }

            float ownerDistance = GetOwnerDistanceFromHome();
            float targetDistance = GetCurrentTargetDistanceFromHome();
            float leashDistance = Mathf.Max(ownerDistance, targetDistance);

            if (_profile.HasHardLimit && leashDistance > _profile.HardLeashRange)
            {
                BeginEvade(MonsterLeashTrigger.HardLimit);
                return;
            }

            if (!_profile.HasSoftLimit || leashDistance <= _profile.SoftLeashRange)
            {
                CancelSoftLimitPending();
                return;
            }

            if (_state != MonsterLeashState.SoftLimitPending)
            {
                _state = MonsterLeashState.SoftLimitPending;
                _softLimitDeadline = Time.time + _profile.SoftLeashGraceSeconds;
                return;
            }

            if (Time.time >= _softLimitDeadline)
            {
                BeginEvade(MonsterLeashTrigger.SoftLimit);
            }
        }

        /// <summary>
        /// 전투와 Threat를 종료하고 홈 복귀 상태를 시작합니다.
        /// </summary>
        /// <param name="trigger">Evade를 시작한 원인입니다.</param>
        /// <returns>새로운 Evade가 시작되었으면 <see langword="true"/>입니다.</returns>
        public bool BeginEvade(MonsterLeashTrigger trigger)
        {
            if (_owner == null || _owner.IsStatusDead() || !_home.IsValid || !_profile.IsEnabled)
            {
                return false;
            }

            if (IsReturnLocked)
            {
                return false;
            }

            _lastTrigger = trigger == MonsterLeashTrigger.None
                ? MonsterLeashTrigger.Manual
                : trigger;
            _state = MonsterLeashState.ReturningHome;
            _softLimitDeadline = 0f;
            _returnStartedTime = Time.time;
            _nextReturnMoveRefreshTime = 0f;

            _movementDriver?.StopAttackCoroutine();
            _movementDriver?.RequestStopMoveIntent();
            _owner.ClearAllThreats();
            _owner.Stop(isForce: true);

            CancelExternalMotions();
            if (_profile.ClearAffectsOnEvade)
            {
                AffectRuntimeBridge.RemoveAll(_owner.gameObject);
            }

            if (_profile.RecoveryPolicy == MonsterLeashRecoveryPolicy.OnEvadeStart)
            {
                RestoreOwnerResources();
            }

            NotifyEvadeStarted();

            if (GetOwnerDistanceFromHome() <= _profile.ReturnStopDistance)
            {
                ArriveHome();
            }

            return true;
        }

        /// <summary>
        /// 홈을 향한 이동 요청을 갱신하고 제한 시간 초과 시 안전하게 홈으로 보정합니다.
        /// </summary>
        private void TickReturnHome()
        {
            float distance = GetOwnerDistanceFromHome();
            if (distance <= _profile.ReturnStopDistance)
            {
                ArriveHome();
                return;
            }

            if (Time.time - _returnStartedTime >= _profile.ReturnTimeoutSeconds)
            {
                SnapToHome();
                ArriveHome();
                return;
            }

            if (Time.time < _nextReturnMoveRefreshTime)
            {
                return;
            }

            _nextReturnMoveRefreshTime = Time.time + ReturnMoveRefreshIntervalSeconds;
            Vector2 direction = _home.Position - _owner.transform.position;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                ArriveHome();
                return;
            }

            if (_movementDriver == null)
            {
                SnapToHome();
                ArriveHome();
                return;
            }

            if (!_movementDriver.TryRequestLeashMove(
                    direction.normalized,
                    _profile.ReturnMoveSpeedMultiplier,
                    out MonsterMoveRequestFailureReason failureReason) &&
                (failureReason == MonsterMoveRequestFailureReason.AxisLocked ||
                 failureReason == MonsterMoveRequestFailureReason.SpeedNonPositive))
            {
                SnapToHome();
                ArriveHome();
            }
        }

        /// <summary>
        /// 홈 도착 상태를 적용하고 설정된 재활성 대기 단계로 전환합니다.
        /// </summary>
        private void ArriveHome()
        {
            SnapToHome();
            _movementDriver?.RequestWait();
            if (_profile.RecoveryPolicy == MonsterLeashRecoveryPolicy.OnHomeReached)
            {
                RestoreOwnerResources();
            }

            if (_profile.ReturnDelaySeconds > 0f)
            {
                _state = MonsterLeashState.ReturnDelay;
                _returnDelayDeadline = Time.time + _profile.ReturnDelaySeconds;
                return;
            }

            CompleteReturn();
        }

        /// <summary>
        /// 홈 복귀 잠금을 해제하고 상위 시스템에 재활성 완료를 알립니다.
        /// </summary>
        private void CompleteReturn()
        {
            _state = _profile.IsEnabled
                ? MonsterLeashState.Monitoring
                : MonsterLeashState.Disabled;
            _lastTrigger = MonsterLeashTrigger.None;
            _returnDelayDeadline = 0f;
            _returnStartedTime = 0f;
            _movementDriver?.RequestStopMoveIntent();
            NotifyReturnCompleted();
        }

        /// <summary>
        /// 몬스터의 위치, 물리 속도, 초기 방향을 홈 상태로 정확히 보정합니다.
        /// </summary>
        private void SnapToHome()
        {
            if (_owner == null || !_home.IsValid)
            {
                return;
            }

            _owner.transform.position = _home.Position;
            _owner.SetFlip(_home.IsFlip);
            if (_owner.characterRigidbody2D != null)
            {
                _owner.characterRigidbody2D.linearVelocity = Vector2.zero;
                _owner.characterRigidbody2D.angularVelocity = 0f;
            }

            _owner.TrySeparateCharacterBodyOverlaps();
        }

        /// <summary>
        /// 몬스터의 HP, MP, Stamina, Super Armor를 현재 최대값으로 회복합니다.
        /// </summary>
        private void RestoreOwnerResources()
        {
            if (_owner == null || _owner.IsStatusDead())
            {
                return;
            }

            _owner.RestoreResourcesForLeash();
        }

        /// <summary>
        /// 스킬과 Crowd Control이 등록한 강제 이동을 중단하여 홈 복귀 이동과 충돌하지 않게 합니다.
        /// </summary>
        private void CancelExternalMotions()
        {
            if (_owner == null)
            {
                return;
            }

            ICharacterMotionController motion = _owner.GetComponent<ICharacterMotionController>();
            motion?.CancelMotion(MotionChannel.Skill, reason: 9941);
            motion?.CancelMotion(MotionChannel.CrowdControl, reason: 9942);

            CharacterPhysicsOverrideController physicsOverride =
                _owner.GetComponent<CharacterPhysicsOverrideController>();
            physicsOverride?.ForceRestoreBaseGravity();
        }

        /// <summary>
        /// 현재 몬스터가 홈에서 떨어진 2D 거리를 반환합니다.
        /// </summary>
        /// <returns>홈 정보가 없으면 0을 반환합니다.</returns>
        public float GetOwnerDistanceFromHome()
        {
            if (_owner == null || !_home.IsValid)
            {
                return 0f;
            }

            return Vector2.Distance(_owner.transform.position, _home.Position);
        }

        /// <summary>
        /// 현재 Threat 타겟이 홈에서 떨어진 2D 거리를 반환합니다.
        /// </summary>
        /// <returns>현재 타겟이 없으면 0을 반환합니다.</returns>
        public float GetCurrentTargetDistanceFromHome()
        {
            if (_owner == null || !_home.IsValid ||
                !_owner.TryGetCurrentCombatTarget(out CharacterBase target) || target == null)
            {
                return 0f;
            }

            return Vector2.Distance(target.transform.position, _home.Position);
        }

        /// <summary>
        /// 소프트 Leash 유예 상태를 취소하고 정상 감시 상태로 복귀합니다.
        /// </summary>
        private void CancelSoftLimitPending()
        {
            _softLimitDeadline = 0f;
            if (_profile.IsEnabled && !IsReturnLocked)
            {
                _state = MonsterLeashState.Monitoring;
            }
        }

        /// <summary>
        /// Leash 생명주기 구독자에게 Evade 시작을 알립니다.
        /// </summary>
        private void NotifyEvadeStarted()
        {
            CollectLeashLifecycles();
            for (int i = 0; i < _leashLifecycles.Count; i++)
            {
                _leashLifecycles[i]?.OnLeashEvadeStarted(_owner, _lastTrigger);
            }
        }

        /// <summary>
        /// Leash 생명주기 구독자에게 홈 복귀 완료를 알립니다.
        /// </summary>
        private void NotifyReturnCompleted()
        {
            CollectLeashLifecycles();
            for (int i = 0; i < _leashLifecycles.Count; i++)
            {
                _leashLifecycles[i]?.OnLeashReturnCompleted(_owner);
            }
        }

        /// <summary>
        /// 현재 몬스터에 부착된 Leash 생명주기 구현체를 수집합니다.
        /// </summary>
        private void CollectLeashLifecycles()
        {
            _leashLifecycles.Clear();
            GetComponents(_leashLifecycles);
        }

        /// <summary>
        /// 풀 대여 시 새 리젠 위치를 홈으로 사용하고 Leash 상태를 초기화합니다.
        /// </summary>
        /// <param name="owner">대여된 몬스터입니다.</param>
        public void OnPoolRent(Monster owner)
        {
            _owner = owner;
            _movementDriver = owner != null ? owner.GetComponent<ControllerMonster>() : null;
            CaptureHome(owner != null ? owner.CharacterRegenData : null);
        }

        /// <summary>
        /// 풀 반납 전 홈 복귀 이동과 타이머 상태를 제거합니다.
        /// </summary>
        /// <param name="owner">반환되는 몬스터입니다.</param>
        public void OnPoolReturn(Monster owner)
        {
            _movementDriver?.RequestStopMoveIntent();
            ResetRuntimeState();
            _state = MonsterLeashState.Disabled;
        }

        /// <summary>
        /// Leash 런타임 타이머와 마지막 원인을 초기 상태로 되돌립니다.
        /// </summary>
        private void ResetRuntimeState()
        {
            _lastTrigger = MonsterLeashTrigger.None;
            _softLimitDeadline = 0f;
            _returnStartedTime = 0f;
            _returnDelayDeadline = 0f;
            _nextMonitorTime = Time.time + ResolveInitialMonitorDelay();
            _nextReturnMoveRefreshTime = 0f;
            _state = _profile.IsEnabled
                ? MonsterLeashState.Monitoring
                : MonsterLeashState.Disabled;
            _movementDriver?.RequestStopMoveIntent();
        }

        /// <summary>
        /// 여러 몬스터의 Leash 검사가 같은 프레임에 집중되지 않도록 첫 검사 시간을 분산합니다.
        /// </summary>
        /// <returns>0 이상 감시 주기 미만의 초기 지연 시간입니다.</returns>
        private float ResolveInitialMonitorDelay()
        {
            int phase = Mathf.Abs(GetInstanceID() % 1000);
            return phase / 1000f * MonitorIntervalSeconds;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 선택된 몬스터의 홈, Soft Leash, Hard Leash 범위를 Scene 뷰에 표시합니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Monster owner = _owner != null ? _owner : GetComponent<Monster>();
            if (owner == null)
            {
                return;
            }

            MonsterHomeContext home = _home.IsValid
                ? _home
                : new MonsterHomeContext(owner.transform.position, owner.IsFlipped(), 0, true);
            MonsterLeashProfile profile = owner.LeashProfile;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(home.Position, 0.15f);

            if (profile.HasSoftLimit)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(home.Position, profile.SoftLeashRange);
            }

            if (profile.HasHardLimit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(home.Position, profile.HardLeashRange);
            }
        }
#endif
    }
}
