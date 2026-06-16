using System.Collections.Generic;
using R3;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 액션 상태와 전투 상태 전이를 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        private readonly CharacterStateTracker _stateTracker = new();
        private readonly HashSet<object> _controlLockTokens = new();
        private readonly HashSet<object> _movementLockTokens = new();
        private readonly HashSet<object> _brainLockTokens = new();
        private readonly Dictionary<object, CharacterInputAllowMask> _inputAllowLockTokens = new();
        private bool _isAggro;

        /// <summary>
        /// Affect, 컷씬, 대화 등 외부 시스템이 캐릭터 조작을 잠글 때 사용하는 토큰을 획득합니다.
        /// </summary>
        /// <param name="owner">잠금 요청 소유자입니다. null이면 새 토큰을 생성합니다.</param>
        /// <returns>해제 시 사용할 잠금 토큰입니다.</returns>
        public object AcquireControlLock(object owner = null)
        {
            object token = owner ?? new object();
            _controlLockTokens.Add(token);
            return token;
        }

        /// <summary>
        /// 이전에 획득한 외부 제어 잠금을 해제합니다.
        /// </summary>
        /// <param name="token">해제할 잠금 토큰입니다.</param>
        public void ReleaseControlLock(object token)
        {
            if (token == null)
            {
                return;
            }

            _controlLockTokens.Remove(token);
        }

        /// <summary>
        /// 이동 입력과 이동 실행만 일시적으로 막기 위한 이동 잠금 토큰을 획득합니다.
        /// 전체 조작 잠금과 달리 공격, 가드, 상호작용 같은 비이동 입력은 차단하지 않습니다.
        /// </summary>
        /// <param name="owner">잠금 요청 소유자입니다. null이면 새 토큰을 생성합니다.</param>
        /// <returns>해제 시 사용할 이동 잠금 토큰입니다.</returns>
        public object AcquireMovementLock(object owner = null)
        {
            object token = owner ?? new object();
            _movementLockTokens.Add(token);
            return token;
        }

        /// <summary>
        /// 이전에 획득한 이동 잠금 토큰을 해제합니다.
        /// </summary>
        /// <param name="token">해제할 이동 잠금 토큰입니다.</param>
        public void ReleaseMovementLock(object token)
        {
            if (token == null)
            {
                return;
            }

            _movementLockTokens.Remove(token);
        }

        /// <summary>
        /// 지정한 입력만 허용하고 나머지 입력을 차단하는 잠금 토큰을 획득합니다.
        /// </summary>
        /// <param name="allowedInputs">잠금 중 허용할 입력 마스크입니다.</param>
        /// <param name="owner">잠금 요청 소유자입니다. null이면 새 토큰을 생성합니다.</param>
        /// <returns>해제 시 사용할 입력 잠금 토큰입니다.</returns>
        public object AcquireInputAllowLock(CharacterInputAllowMask allowedInputs, object owner = null)
        {
            object token = owner ?? new object();
            _inputAllowLockTokens[token] = allowedInputs;
            return token;
        }

        /// <summary>
        /// 이전에 획득한 입력 허용 잠금 토큰을 해제합니다.
        /// </summary>
        /// <param name="token">해제할 입력 잠금 토큰입니다.</param>
        public void ReleaseInputAllowLock(object token)
        {
            if (token == null)
            {
                return;
            }

            _inputAllowLockTokens.Remove(token);
        }

        /// <summary>
        /// 현재 입력 허용 잠금 상태에서 지정한 입력을 차단해야 하는지 확인합니다.
        /// </summary>
        /// <param name="inputType">검사할 플레이어 입력 타입입니다.</param>
        /// <returns>하나 이상의 입력 잠금이 해당 입력을 허용하지 않으면 <see langword="true"/>입니다.</returns>
        public bool ShouldBlockInputByInputAllowLock(AutoMoveInputType inputType)
        {
            if (_inputAllowLockTokens.Count == 0)
            {
                return false;
            }

            CharacterInputAllowMask inputMask = ResolveInputAllowMask(inputType);
            foreach (var pair in _inputAllowLockTokens)
            {
                if ((pair.Value & inputMask) != 0)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 자동 이동 입력 타입을 캐릭터 입력 허용 마스크로 변환합니다.
        /// </summary>
        /// <param name="inputType">변환할 입력 타입입니다.</param>
        /// <returns>입력 타입에 대응하는 허용 마스크입니다.</returns>
        private static CharacterInputAllowMask ResolveInputAllowMask(AutoMoveInputType inputType)
        {
            return inputType switch
            {
                AutoMoveInputType.Move => CharacterInputAllowMask.Move,
                AutoMoveInputType.Attack => CharacterInputAllowMask.Attack,
                AutoMoveInputType.Guard => CharacterInputAllowMask.Guard,
                AutoMoveInputType.Jump => CharacterInputAllowMask.Jump,
                AutoMoveInputType.Dash => CharacterInputAllowMask.Dash,
                AutoMoveInputType.Interaction => CharacterInputAllowMask.Interaction,
                AutoMoveInputType.SimulationTool => CharacterInputAllowMask.SimulationTool,
                _ => CharacterInputAllowMask.Other,
            };
        }

        /// <summary>
        /// 이동 전용 잠금 또는 전체 제어 잠금으로 이동 조작이 막혀 있는지 확인합니다.
        /// </summary>
        /// <returns>이동 조작이 막혀 있으면 <see langword="true"/>입니다.</returns>
        public bool IsMovementLocked()
        {
            return _movementLockTokens.Count > 0 || _controlLockTokens.Count > 0;
        }

        /// <summary>
        /// 몬스터 Brain 또는 BT 판단을 일시정지하는 토큰을 획득합니다.
        /// </summary>
        /// <param name="owner">잠금 요청 소유자입니다. null이면 새 토큰을 생성합니다.</param>
        /// <returns>해제 시 사용할 Brain 잠금 토큰입니다.</returns>
        public object AcquireBrainLock(object owner = null)
        {
            object token = owner ?? new object();
            _brainLockTokens.Add(token);
            return token;
        }

        /// <summary>
        /// 이전에 획득한 몬스터 Brain 또는 BT 판단 잠금 토큰을 해제합니다.
        /// </summary>
        /// <param name="token">해제할 Brain 잠금 토큰입니다.</param>
        public void ReleaseBrainLock(object token)
        {
            if (token == null)
            {
                return;
            }

            _brainLockTokens.Remove(token);
        }

        /// <summary>
        /// 몬스터 Brain 또는 BT 판단이 외부 시스템에 의해 일시정지되어 있는지 확인합니다.
        /// </summary>
        /// <returns>Brain 판단 잠금 토큰이 하나 이상 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsBrainLocked()
        {
            return _brainLockTokens.Count > 0;
        }

        /// <summary>
        /// HitStop, CrowdControl, 스킬 사용, 외부 제어 잠금 중 하나라도 활성화되어 있으면 조작 불가로 판단합니다.
        /// </summary>
        /// <returns>현재 캐릭터를 조작할 수 없으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsDontControl()
        {
            return HitStopController.IsActive ||
                   _crowdControlController.IsActive ||
                   _controlLockTokens.Count > 0;
        }
        /// <summary>
        /// 현재 상태가 사망인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 사망 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusDead() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Dead;

        /// <summary>
        /// 현재 상태가 공격 중인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 공격 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusAttack() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Attack;

        /// <summary>
        /// 현재 상태가 공격 콤보 대기인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 공격 콤보 대기 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusAttackComboWait() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.AttackComboWait;

        /// <summary>
        /// 현재 상태가 이동 금지인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 이동 금지 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusDontMove() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.DontMove;

        // /// <summary>
        // /// 현재 상태가 조작 금지인지 확인합니다.
        // /// </summary>
        // /// <returns>현재 상태가 조작 금지 상태이면 <see langword="true"/>를 반환합니다.</returns>
        // public bool IsStatusDontControl() => _dontControl == true;

        /// <summary>
        /// 현재 상태가 달리기인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 달리기 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusRun() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Run;

        /// <summary>
        /// 현재 상태가 Idle인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 Idle이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusIdle() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Idle;

        /// <summary>
        /// 현재 상태가 None인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 None이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusNone() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.None;

        /// <summary>
        /// 현재 상태가 강제 이동인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 강제 이동 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusMoveForce() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.MoveForce;

        /// <summary>
        /// 현재 상태가 데미지 반응인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 데미지 반응 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusDamage() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Damage;

        /// <summary>
        /// 현재 상태가 점프인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 점프 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusJump() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Jump;

        // /// <summary>
        // /// 현재 상태가 넉백인지 확인합니다.
        // /// </summary>
        // /// <returns>현재 상태가 넉백 상태이면 <see langword="true"/>를 반환합니다.</returns>
        // public bool IsStatusKnockback() => _knockback == true;

        /// <summary>
        /// 현재 상태가 대시인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 대시 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusDash() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Dash;

        /// <summary>
        /// 현재 상태가 오르기인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 오르기 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusClimb() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Climb;

        /// <summary>
        /// 현재 상태가 밀기인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 밀기 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusPush() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.Push;

        /// <summary>
        /// 현재 상태가 시뮬레이션 도구 사용인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 시뮬레이션 도구 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusSimulationTool() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.SimulationTool;

        /// <summary>
        /// 현재 상태가 스킬 캐스팅인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 스킬 캐스팅 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusCastingSkill() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.CastingSkill;

        /// <summary>
        /// 현재 상태가 스킬 사용 중인지 확인합니다.
        /// </summary>
        /// <returns>현재 상태가 스킬 사용 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsStatusUseSkill() => _stateTracker.CurrentStatus == CharacterConstants.CharacterStatus.UseSkill;

        /// <summary>
        /// 현재 액션 상태를 반환합니다.
        /// </summary>
        /// <returns>현재 캐릭터 액션 상태입니다.</returns>
        public CharacterConstants.CharacterStatus GetCurrentStatus() => _stateTracker.CurrentStatus;

        /// <summary>
        /// 현재 전투 상태를 반환합니다.
        /// </summary>
        /// <returns>현재 전투 상태입니다.</returns>
        public CharacterConstants.BattleStatus GetBattleStatus() => _stateTracker.CurrentBattleStatus;

        /// <summary>
        /// 현재 전투 중 상태인지 확인합니다.
        /// </summary>
        /// <returns>전투 상태가 InBattle이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsInBattle() => _stateTracker.CurrentBattleStatus == CharacterConstants.BattleStatus.InBattle;

        /// <summary>
        /// 내부 액션 상태를 지정한 값으로 갱신합니다.
        /// </summary>
        /// <param name="value">적용할 액션 상태입니다.</param>
        private void SetStatus(CharacterConstants.CharacterStatus value) => _stateTracker.SetStatus(value);

        /// <summary>
        /// 내부 전투 상태를 갱신하고 변경 시 구독 스트림을 발행합니다.
        /// </summary>
        /// <param name="value">적용할 전투 상태입니다.</param>
        private void SetBattleStatus(CharacterConstants.BattleStatus value)
        {
            _stateTracker.SetBattleStatus(value, CurrentBattleStatus);
        }

        /// <summary>
        /// 전투 상태를 None으로 설정합니다.
        /// </summary>
        public void SetBattleStatusNone() => SetBattleStatus(CharacterConstants.BattleStatus.None);

        /// <summary>
        /// 전투 상태를 InBattle로 설정합니다.
        /// </summary>
        public void SetBattleStatusInBattle() => SetBattleStatus(CharacterConstants.BattleStatus.InBattle);

        /// <summary>
        /// 액션 상태를 None으로 설정합니다.
        /// </summary>
        public void SetStatusNone() => SetStatus(CharacterConstants.CharacterStatus.None);

        /// <summary>
        /// 액션 상태를 Dead로 설정합니다.
        /// </summary>
        public void SetStatusDead() => SetStatus(CharacterConstants.CharacterStatus.Dead);

        /// <summary>
        /// 액션 상태를 Idle로 설정합니다.
        /// </summary>
        public void SetStatusIdle() => SetStatus(CharacterConstants.CharacterStatus.Idle);

        /// <summary>
        /// 액션 상태를 Run으로 설정합니다.
        /// </summary>
        public void SetStatusRun() => SetStatus(CharacterConstants.CharacterStatus.Run);

        /// <summary>
        /// 액션 상태를 Attack으로 설정합니다.
        /// </summary>
        public void SetStatusAttack() => SetStatus(CharacterConstants.CharacterStatus.Attack);

        /// <summary>
        /// 액션 상태를 AttackComboWait로 설정합니다.
        /// </summary>
        public void SetStatusAttackComboWait() => SetStatus(CharacterConstants.CharacterStatus.AttackComboWait);

        /// <summary>
        /// 액션 상태를 DontMove로 설정합니다.
        /// </summary>
        public void SetStatusDontMove() => SetStatus(CharacterConstants.CharacterStatus.DontMove);

        // /// <summary>
        // /// 액션 상태를 DontControl로 설정합니다.
        // /// </summary>
        // public void SetStatusDontControl(bool value = true)
        // {
        //     GcLogger.Log($"SetStatusDontControl {_dontControl} -> {value}");
        //     _dontControl = value;
        // }

        /// <summary>
        /// 액션 상태를 MoveForce로 설정합니다.
        /// </summary>
        public void SetStatusMoveForce() => SetStatus(CharacterConstants.CharacterStatus.MoveForce);

        /// <summary>
        /// 액션 상태를 CastingSkill로 설정합니다.
        /// </summary>
        public void SetStatusCastingSkill() => SetStatus(CharacterConstants.CharacterStatus.CastingSkill);

        /// <summary>
        /// 액션 상태를 UseSkill로 설정합니다.
        /// </summary>
        public void SetStatusUseSkill() => SetStatus(CharacterConstants.CharacterStatus.UseSkill);

        /// <summary>
        /// 액션 상태를 Damage로 설정합니다.
        /// </summary>
        public void SetStatusDamage() => SetStatus(CharacterConstants.CharacterStatus.Damage);

        /// <summary>
        /// 액션 상태를 Jump로 설정합니다.
        /// </summary>
        public void SetStatusJump() => SetStatus(CharacterConstants.CharacterStatus.Jump);

        // /// <summary>
        // /// 액션 상태를 Knockback으로 설정합니다.
        // /// </summary>
        // public void SetStatusKnockback(bool value = true)
        // {
        //     _knockback = value;
        // }

        /// <summary>
        /// 액션 상태를 Dash로 설정합니다.
        /// </summary>
        public void SetStatusDash() => SetStatus(CharacterConstants.CharacterStatus.Dash);

        /// <summary>
        /// 액션 상태를 Climb로 설정합니다.
        /// </summary>
        public void SetStatusClimb() => SetStatus(CharacterConstants.CharacterStatus.Climb);

        /// <summary>
        /// 액션 상태를 Push로 설정합니다.
        /// </summary>
        public void SetStatusPush() => SetStatus(CharacterConstants.CharacterStatus.Push);

        /// <summary>
        /// 액션 상태를 SimulationTool로 설정합니다.
        /// </summary>
        public void SetStatusSimulationTool() => SetStatus(CharacterConstants.CharacterStatus.SimulationTool);

        /// <summary>
        /// 캐릭터의 어그로 상태를 갱신하고 전투 상태를 동기화합니다.
        /// </summary>
        /// <param name="set">적용할 어그로 여부입니다.</param>
        public void SetAggro(bool set)
        {
            bool shouldNotifyStateChanged = _isAggro != set || (!set && attackerTransform != null);
            _isAggro = set;

            if (set)
            {
                SetBattleStatusInBattle();
                if (shouldNotifyStateChanged)
                {
                    OnAggroStateChanged(true);
                }

                return;
            }

            // 어그로 대상이 지워지기 전에 파생 클래스가 전투 참여 관계를 정리할 수 있도록 알립니다.
            if (shouldNotifyStateChanged)
            {
                OnAggroStateChanged(false);
            }

            SetBattleStatusNone();
            SetAttackerTarget(null);
        }

        /// <summary>
        /// 어그로 상태 변경 직후 파생 클래스가 추가 전투 관계를 동기화할 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="isAggro">변경된 어그로 활성 여부입니다.</param>
        protected virtual void OnAggroStateChanged(bool isAggro)
        {
        }

        /// <summary>
        /// 현재 어그로 상태를 반환합니다.
        /// </summary>
        /// <returns>어그로가 활성화되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsAggro()
        {
            return _isAggro;
        }

        /// <summary>
        /// 현재 액션을 정지하고 Idle 상태로 복귀시킵니다.
        /// </summary>
        /// <param name="isForce">정지 가능 조건을 무시하고 강제로 정지할지 여부입니다.</param>
        public void Stop(bool isForce = false)
        {
            if (!CanStopCurrentAction(isForce))
                return;

            SetStatusIdle();
            CharacterAnimationController?.PlayWaitAnimation();

            var e = new EventArgsOnStop { Handled = false };
            OnStop?.Invoke(this, e);
        }

        /// <summary>
        /// 외부 시스템의 액션 요청을 현재 캐릭터 상태에 반영합니다.
        /// </summary>
        /// <param name="request">적용할 액션 요청 정보입니다.</param>
        /// <returns>요청이 수락되어 상태가 변경되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool RequestAction(in CharacterActionRequest request)
        {
            if (IsStatusDead())
                return false;

            if (request.StopMove)
            {
                directionNormalize = Vector3.zero;
            }

            SetStatus(request.Status);
            return true;
        }

        /// <summary>
        /// 지정한 상태가 현재 상태일 때 액션 종료를 처리합니다.
        /// </summary>
        /// <param name="status">해제 대상으로 판단할 상태입니다.</param>
        public void ClearAction(CharacterConstants.CharacterStatus status)
        {
            if (GetCurrentStatus() != status) return;
        }

        /// <summary>
        /// 현재 상태에서 정지 요청을 수락할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="isForce">강제 정지 여부입니다.</param>
        /// <returns>정지 요청을 처리할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool CanStopCurrentAction(bool isForce)
        {
            if (IsStatusDead()) return false;
            if (isForce) return true;
            if (IsStatusIdle()) return false;
            return true;
        }

        /// <summary>
        /// CharacterBase 내부 상태 값을 보관하는 경량 상태 저장소입니다.
        /// </summary>
        private sealed class CharacterStateTracker
        {
            /// <summary>
            /// 현재 액션 상태입니다.
            /// </summary>
            public CharacterConstants.CharacterStatus CurrentStatus { get; private set; } = CharacterConstants.CharacterStatus.None;

            /// <summary>
            /// 현재 전투 상태입니다.
            /// </summary>
            public CharacterConstants.BattleStatus CurrentBattleStatus { get; private set; } = CharacterConstants.BattleStatus.None;

            /// <summary>
            /// 현재 액션 상태를 지정된 값으로 갱신합니다.
            /// </summary>
            /// <param name="status">저장할 액션 상태입니다.</param>
            public void SetStatus(CharacterConstants.CharacterStatus status)
            {
                CurrentStatus = status;
            }

            /// <summary>
            /// 현재 전투 상태를 갱신하고 변경 사항을 구독 스트림에 발행합니다.
            /// </summary>
            /// <param name="status">저장할 전투 상태입니다.</param>
            /// <param name="stream">상태 변경을 알릴 Reactive 스트림입니다.</param>
            public void SetBattleStatus(CharacterConstants.BattleStatus status, BehaviorSubject<CharacterConstants.BattleStatus> stream)
            {
                if (CurrentBattleStatus == status) return;

                CurrentBattleStatus = status;
                stream?.OnNext(CurrentBattleStatus);
            }
        }
    }
}
