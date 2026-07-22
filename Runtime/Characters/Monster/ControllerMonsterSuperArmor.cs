using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터(또는 캐릭터)의 “경직 무시 스택”을 관리합니다.
    /// - 특정 공격이 스택을 깎는다.
    /// - 스택이 0이 되면 무조건 리액션 발생.
    /// - 일정 시간이 지나면 스택이 회복된다.
    /// </summary>
    public sealed class ControllerMonsterSuperArmor
    {
        public int CurrentStacks => _owner != null ? _owner.CurrentSuperArmor.Value : 0;
        public int MaxStacks => GetMaxSuperArmor();

        /// <summary>스택(슈퍼아머)이 변했을 때(디버그/UI용)</summary>
        public event Action<int, int> StacksChanged; // (current, max)

        /// <summary>브레이크(0 도달)로 리액션이 확정될 때</summary>
        public event Action<CharacterConstants.HitReactionType> BreakTriggered;

        /// <summary>
        /// 브레이크 리셋 정책에 의해 슈퍼아머가 최대값으로 복구된 뒤 발생합니다.
        /// </summary>
        /// <remarks>
        /// 첫 번째 인자는 복구된 현재값이고, 두 번째 인자는 최대값입니다.
        /// 단순 자연 회복과 ResetToMax 연출을 구분해야 하는 UI에서 사용할 수 있습니다.
        /// </remarks>
        public event Action<int, int> RestoredToMax;

        private GGemCoMonsterSettings _config;

        // owner (SSOT: CharacterBase.CurrentSuperArmor)
        private CharacterBase _owner;

        private float _regenDelay;
        private float _regenInterval;
        private int _regenPerTick;
        private CharacterConstants.StaggerBreakResetMode _breakResetMode;

        private float _nextRegenTime;

        // anti multi-hit spam
        private float _perAttackConsumeCooldown;
        private int _lastAttackId;
        private SuperArmorDamageCause _lastDamageCause;
        private float _lastAttackConsumeTime;

        private bool _initialized;
        private bool _isSuperArmorEnabled;
        private bool _isRestoreToMaxPending;
        private float _restoreToMaxAt;

        /// <summary>
        /// 소유자와 몬스터 설정을 기준으로 슈퍼아머 컨트롤러를 초기화합니다.
        /// </summary>
        /// <param name="owner">슈퍼아머 값을 보유한 캐릭터 인스턴스입니다.</param>
        /// <param name="config">
        /// 슈퍼아머 동작 설정입니다.
        /// null이면 기본값으로 초기화하여 안전하게 동작시킵니다.
        /// </param>
        public void Initialize(CharacterBase owner, GGemCoMonsterSettings config = null)
        {
            CancelPendingRestoreToMax();
            _owner = owner;
            _config = config;
            _initialized = false;
            if (!_owner)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다.");
                return;
            }

            if (_config != null)
            {
                ApplyConfig(_config);
            }
            else
            {
                InitializeDefaultData();
            }
        }

        /// <summary>
        /// 테이블/런타임 초기화용.
        /// </summary>
        public void InitializeData(
            float regenDelay,
            float regenInterval,
            int regenPerTick,
            CharacterConstants.StaggerBreakResetMode breakResetMode,
            float perAttackConsumeCooldown = 0f)
        {
            CancelPendingRestoreToMax();
            _regenDelay = regenDelay;
            _regenInterval = regenInterval;
            _regenPerTick = regenPerTick;
            _breakResetMode = breakResetMode;
            _perAttackConsumeCooldown = perAttackConsumeCooldown;

            _nextRegenTime = Time.time + _regenDelay;
            _initialized = true;

            FireStacksChanged();
        }

        /// <summary>
        /// 몬스터 설정 값을 컨트롤러 내부 파라미터로 적용합니다.
        /// </summary>
        /// <param name="monsterSettings">슈퍼아머 재생성/회복/브레이크 관련 설정입니다.</param>
        public void ApplyConfig(GGemCoMonsterSettings monsterSettings)
        {
            if (monsterSettings == null) return;
            _config = monsterSettings;

            InitializeData(
                regenDelay: monsterSettings.regenDelay,
                regenInterval: monsterSettings.regenInterval,
                regenPerTick: monsterSettings.regenPerTick,
                // breakResetMode는 브레이크 시점에 현재 Grade로 다시 판정합니다.
                breakResetMode: monsterSettings.breakResetMode,
                perAttackConsumeCooldown: monsterSettings.perAttackConsumeCooldown);
        }

        /// <summary>
        /// 설정 전달이 누락된 경우에도 런타임 예외 없이 동작하도록 기본값으로 초기화합니다.
        /// </summary>
        private void InitializeDefaultData()
        {
            InitializeData(regenDelay: 0f, regenInterval: 0f, regenPerTick: 0,
                breakResetMode: CharacterConstants.StaggerBreakResetMode.KeepZero,
                perAttackConsumeCooldown: 0f);
        }

        /// <summary>
        /// 현재 컨트롤러 소유자의 등급을 반환합니다.
        /// </summary>
        /// <returns>
        /// 소유자가 Monster면 해당 Grade를 반환하고,
        /// 그 외 타입이면 Grade.None을 반환합니다.
        /// </returns>
        private CharacterConstants.Grade ResolveOwnerGrade()
        {
            if (_owner is Monster monster)
            {
                return monster.Grade;
            }

            return CharacterConstants.Grade.None;
        }

        /// <summary>
        /// 브레이크(스택 0) 시점에 실제 적용할 리셋 모드를 판정합니다.
        /// </summary>
        /// <remarks>
        /// Monster Grade는 런타임 초기화 순서상 Awake 이후에 확정될 수 있으므로,
        /// 설정 주입 시점이 아니라 브레이크 시점에 Grade 기반 마스크를 재평가합니다.
        /// </remarks>
        /// <returns>브레이크 시 Max로 복구해야 하면 true를 반환합니다.</returns>
        private bool ShouldRestoreToMaxOnBreak()
        {
            if (_config != null)
            {
                CharacterConstants.StaggerBreakResetMode resolvedMode =
                    _config.ResolveBreakResetMode(ResolveOwnerGrade());
                return resolvedMode == CharacterConstants.StaggerBreakResetMode.ResetToMax;
            }

            return _breakResetMode == CharacterConstants.StaggerBreakResetMode.ResetToMax;
        }

        /// <summary>
        /// 피격을 입력으로 받아 슈퍼아머(스택)를 갱신하고, 리액션 발생 여부를 반환합니다.
        /// 
        /// - 외부 상태(예: 컷씬/무적 등)로 이번 리액션을 막고 싶다면 ignoreReactionByStatus를 true로 전달하세요.
        /// - 슈퍼아머 소모는 리액션 여부와 분리되어, 기본적으로 항상 소모됩니다(0 도달로 브레이크 유도).
        /// </summary>
        public HitReactionDecision ApplyHit(in HitPayload hit, bool ignoreReactionByStatus = false)
        {
            if (!_isSuperArmorEnabled) return HitReactionDecision.NoReaction(0);

            EnsureInitialized();

            if (_owner == null)
            {
                return HitReactionDecision.NoReaction(0);
            }

            // 리액션 타입이 없으면 아무것도 하지 않는다.
            if (hit.ReactionType == CharacterConstants.HitReactionType.None)
            {
                return HitReactionDecision.NoReaction(CurrentStacks);
            }

            // 강제 리액션
            if (hit.ForceReaction)
            {
                if (!ignoreReactionByStatus)
                {
                    // BreakTriggered?.Invoke(hit.ReactionType);
                    return new HitReactionDecision(false, CurrentStacks, true, hit.ReactionType);
                }

                return HitReactionDecision.NoReaction(CurrentStacks);
            }

            // 스택 피해가 없으면(=슈퍼아머를 깎지 않으면) 리액션은 여기서 발생하지 않는다.
            if (hit.StaggerStackDamage <= 0)
            {
                return HitReactionDecision.NoReaction(CurrentStacks);
            }

            int before = _owner.CurrentSuperArmor.Value;

            // 0인 상태에서의 피격: 슈퍼아머가 없으므로 리액션 발생
            if (before <= 0)
            {
                if (!ignoreReactionByStatus)
                {
                    // BreakTriggered?.Invoke(hit.ReactionType);
                    return new HitReactionDecision(false, 0, true, CharacterConstants.HitReactionType.None);
                }

                return HitReactionDecision.NoReaction(0);
            }

            if (!TryConsumeSuperArmor(
                    hit.StaggerStackDamage,
                    hit.AttackId,
                    SuperArmorDamageCause.IncomingHit,
                    hit.ReactionType,
                    triggerBreak: !ignoreReactionByStatus,
                    out SuperArmorDamageResult damageResult))
            {
                return HitReactionDecision.NoReaction(CurrentStacks);
            }

            // 브레이크: 0 도달 시 외부 상태가 허용하는 경우에만 리액션을 실행한다.
            if (damageResult.WasBroken)
            {
                if (!ignoreReactionByStatus)
                {
                    return new HitReactionDecision(true, CurrentStacks, true, hit.ReactionType);
                }

                return new HitReactionDecision(true, 0, false, CharacterConstants.HitReactionType.None);
            }

            // 아직 슈퍼아머가 남아있으면 리액션 없음
            return new HitReactionDecision(true, CurrentStacks, false, CharacterConstants.HitReactionType.None);
        }

        /// <summary>
        /// 외부 전투 정책에서 전달한 요청으로 슈퍼아머를 차감합니다.
        /// </summary>
        /// <param name="request">적용할 슈퍼아머 차감 요청입니다.</param>
        /// <param name="result">실제 차감 및 브레이크 처리 결과입니다.</param>
        /// <returns>슈퍼아머가 실제로 차감되었으면 <see langword="true"/>입니다.</returns>
        public bool TryApplySuperArmorDamage(
            in SuperArmorDamageRequest request,
            out SuperArmorDamageResult result)
        {
            result = SuperArmorDamageResult.None;
            if (!_isSuperArmorEnabled || request.Amount <= 0)
            {
                return false;
            }

            EnsureInitialized();
            if (!_owner)
            {
                return false;
            }

            return TryConsumeSuperArmor(
                request.Amount,
                request.AttackId,
                request.Cause,
                request.BreakReactionType,
                triggerBreak: true,
                out result);
        }

        /// <summary>
        /// 슈퍼아머 수치를 차감하고 재생 지연, 브레이크, 최대 복구 정책을 공통으로 처리합니다.
        /// </summary>
        /// <param name="amount">차감할 슈퍼아머 수치입니다.</param>
        /// <param name="attackId">동일 공격 판정을 구분하는 식별자입니다.</param>
        /// <param name="cause">슈퍼아머 차감이 발생한 원인입니다.</param>
        /// <param name="breakReactionType">브레이크에 전달할 피격 리액션 타입입니다.</param>
        /// <param name="triggerBreak">0 도달 시 브레이크 이벤트와 복구 정책을 실행할지 여부입니다.</param>
        /// <param name="result">실제 차감 및 브레이크 처리 결과입니다.</param>
        /// <returns>슈퍼아머가 실제로 차감되었으면 <see langword="true"/>입니다.</returns>
        private bool TryConsumeSuperArmor(
            int amount,
            int attackId,
            SuperArmorDamageCause cause,
            CharacterConstants.HitReactionType breakReactionType,
            bool triggerBreak,
            out SuperArmorDamageResult result)
        {
            result = SuperArmorDamageResult.None;
            if (!_owner || amount <= 0)
            {
                return false;
            }

            // 동일 공격의 다중 Collider 또는 다단 판정이 설정된 쿨다운 안에서 중복 차감되지 않도록 합니다.
            if (_perAttackConsumeCooldown > 0f &&
                attackId != 0 &&
                _lastAttackId == attackId &&
                _lastDamageCause == cause)
            {
                float elapsed = Time.time - _lastAttackConsumeTime;
                if (elapsed < _perAttackConsumeCooldown)
                {
                    return false;
                }
            }

            int before = _owner.CurrentSuperArmor.Value;
            if (before <= 0)
            {
                return false;
            }

            // 남은 슈퍼아머보다 큰 요청은 현재 남은 수치만큼만 차감합니다.
            int spend = Mathf.Min(amount, before);
            if (!_owner.TrySpendSuperArmor(spend))
            {
                return false;
            }

            _nextRegenTime = Time.time + _regenDelay;

            if (_perAttackConsumeCooldown > 0f && attackId != 0)
            {
                _lastAttackId = attackId;
                _lastDamageCause = cause;
                _lastAttackConsumeTime = Time.time;
            }

            int after = _owner.CurrentSuperArmor.Value;
            bool wasBroken = after <= 0;
            FireStacksChangedIfDifferent(before, after);

            result = new SuperArmorDamageResult(before, after, before - after, wasBroken);
            if (!wasBroken || !triggerBreak)
            {
                return result.WasApplied;
            }

            BreakTriggered?.Invoke(breakReactionType);

            if (ShouldRestoreToMaxOnBreak() && !TryScheduleRestoreToMaxAfterGroggy())
            {
                RestoreToMax();
            }

            return result.WasApplied;
        }

        /// <summary>
        /// 슈퍼아머 브레이크 이후 예약된 최대 복구 시점을 갱신합니다.
        /// </summary>
        /// <param name="now">현재 스케일 적용 게임 시간입니다.</param>
        /// <remarks>
        /// 이 컨트롤러는 일반 C# 클래스이므로 Unity 메시지를 직접 수신하지 않습니다.
        /// 소유 캐릭터의 Update 흐름에서 명시적으로 호출해야 합니다.
        /// </remarks>
        public void Tick(float now)
        {
            if (!_isRestoreToMaxPending) return;
            if (!_isSuperArmorEnabled || !_initialized || !_owner)
            {
                CancelPendingRestoreToMax();
                return;
            }

            if (now < _restoreToMaxAt) return;

            // RestoreToMax 안에서 외부 이벤트가 실행될 수 있으므로 먼저 예약 상태를 해제합니다.
            CancelPendingRestoreToMax();
            RestoreToMax();
        }

        /// <summary>
        /// 유효한 그로기 Affect 설정을 사용하여 슈퍼아머 최대 복구를 예약합니다.
        /// </summary>
        /// <returns>지연 복구를 예약했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryScheduleRestoreToMaxAfterGroggy()
        {
            if (_config == null) return false;
            if (_config.monsterGroggyAffectUid <= 0) return false;

            float duration = _config.monsterGroggyAffectDuration;
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) return false;

            _restoreToMaxAt = Time.time + duration;
            _isRestoreToMaxPending = true;
            return true;
        }

        /// <summary>
        /// 예약된 슈퍼아머 최대 복구 상태를 취소합니다.
        /// </summary>
        internal void CancelPendingRestoreToMax()
        {
            _isRestoreToMaxPending = false;
            _restoreToMaxAt = 0f;
        }

        private void TickRegen(float now)
        {
            if (_regenInterval <= 0) return;

            int max = GetMaxSuperArmor();
            int cur = _owner.CurrentSuperArmor.Value;

            if (cur >= max) return;
            if (now < _nextRegenTime) return;

            // interval 기반으로 “몇 틱”이 지났는지 계산(프레임 드랍에도 안정)
            float elapsed = now - _nextRegenTime;
            int ticks = 1 + Mathf.FloorToInt(elapsed / _regenInterval);
            int add = ticks * _regenPerTick;

            int before = cur;

            _owner.RestoreSuperArmor(add);

            _nextRegenTime += ticks * _regenInterval;

            int after = _owner.CurrentSuperArmor.Value;
            FireStacksChangedIfDifferent(before, after);
        }

        private int GetMaxSuperArmor()
        {
            if (!_isSuperArmorEnabled) return 0;
            if (_owner != null)
            {
                int max = _owner.TotalSuperArmor.Value;
                if (max > 0) return max;

                // TotalSuperArmor가 0인 특수 케이스(아직 계산 전 등) 대비: 현재 값이라도 max로 본다.
                int cur = _owner.CurrentSuperArmor.Value;
                if (cur > 0) return cur;
            }

            return 0;
        }

        /// <summary>
        /// 현재 슈퍼아머를 최대값까지 복구하고 값 변경 이벤트와 리셋 완료 이벤트를 전달합니다.
        /// </summary>
        /// <returns>실제로 슈퍼아머 값이 증가했으면 <see langword="true"/>를 반환합니다.</returns>
        private bool RestoreToMax()
        {
            if (!_owner)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다.");
                return false;
            }

            int max = GetMaxSuperArmor();
            int cur = _owner.CurrentSuperArmor.Value;
            int delta = max - cur;
            if (delta <= 0) return false;

            _owner.RestoreSuperArmor(delta);

            int restoredValue = _owner.CurrentSuperArmor.Value;
            if (restoredValue <= cur) return false;

            // CurrentSuperArmor 구독자에게 값이 먼저 반영된 뒤 의미 이벤트를 전달해야
            // UI가 활성 아이콘을 복구한 상태에서 start 애니메이션을 재생할 수 있습니다.
            FireStacksChanged();
            RestoredToMax?.Invoke(restoredValue, max);
            return true;
        }

        /// <summary>
        /// 지연 초기화가 필요한 상황에서 최소 1회 초기화를 보장합니다.
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isSuperArmorEnabled) return;
            if (_initialized) return;
            if (!_owner)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다.");
                return;
            }

            if (_config != null)
            {
                ApplyConfig(_config);
            }
            else
            {
                InitializeDefaultData();
            }
        }

        private void FireStacksChanged()
        {
            StacksChanged?.Invoke(CurrentStacks, MaxStacks);
        }

        private void FireStacksChangedIfDifferent(long before, long after)
        {
            if (before == after) return;
            FireStacksChanged();
        }

        public void EnableSuperArmor(bool enable)
        {
            _isSuperArmorEnabled = enable;
            if (!enable)
            {
                CancelPendingRestoreToMax();
            }
        }

        /// <summary>
        /// 예약된 복구 상태와 소유자 참조를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            CancelPendingRestoreToMax();
            _owner = null;
            _config = null;
            _initialized = false;
            _isSuperArmorEnabled = false;
        }

        public bool IsEnableSuperArmor()
        {
            return _isSuperArmorEnabled;
        }
    }
}
