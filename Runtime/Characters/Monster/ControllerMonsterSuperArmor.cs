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
        private float _lastAttackConsumeTime;

        private bool _initialized;
        private bool _isSuperArmorEnabled;

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

            // 같은 AttackId 다단 히트 과소/과대 방지(선택)
            if (_perAttackConsumeCooldown > 0f && hit.AttackId != 0)
            {
                if (_lastAttackId == hit.AttackId)
                {
                    float dt = Time.time - _lastAttackConsumeTime;
                    if (dt < _perAttackConsumeCooldown)
                        return HitReactionDecision.NoReaction(CurrentStacks);
                }
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
            
            // 슈퍼아머 소모(overspend 방지: 남은 만큼만 소비)
            int spend = hit.StaggerStackDamage;
            if (spend > before) spend = before;

            _owner.TrySpendSuperArmor(spend);

            _nextRegenTime = Time.time + _regenDelay;

            if (_perAttackConsumeCooldown > 0f && hit.AttackId != 0)
            {
                _lastAttackId = hit.AttackId;
                _lastAttackConsumeTime = Time.time;
            }
            
            long after = _owner.CurrentSuperArmor.Value;
            
            // UI/디버그
            FireStacksChangedIfDifferent(before, after);
            
            // 브레이크: 0 도달 시 “무조건” 리액션
            if (after <= 0)
            {
                if (!ignoreReactionByStatus)
                {
                    BreakTriggered?.Invoke(hit.ReactionType);
                
                    if (ShouldRestoreToMaxOnBreak())
                    {
                        RestoreToMax();
                        FireStacksChanged();
                    }
                
                    return new HitReactionDecision(true, CurrentStacks, true, hit.ReactionType);
                }

                return new HitReactionDecision(true, 0, false, CharacterConstants.HitReactionType.None);
            }
            // 아직 슈퍼아머가 남아있으면 리액션 없음
            return new HitReactionDecision(true, CurrentStacks, false, CharacterConstants.HitReactionType.None);
        }

        private void Update()
        {
            if (!_isSuperArmorEnabled) return;
            if (!_initialized) return;
            TickRegen(Time.time);
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
        private void RestoreToMax()
        {
            if (!_owner)
            {
                GcLogger.LogError($"연결된 몬스터가 없습니다.");
                return;
            }

            int max = GetMaxSuperArmor();
            int cur = _owner.CurrentSuperArmor.Value;
            int delta = max - cur;
            if (delta <= 0) return;

            _owner.RestoreSuperArmor(delta);
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
        }

        public bool IsEnableSuperArmor()
        {
            return _isSuperArmorEnabled;
        }
    }
}
