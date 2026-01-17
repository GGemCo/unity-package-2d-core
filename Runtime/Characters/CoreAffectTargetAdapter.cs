using System;
using System.Collections.Generic;
using UnityEngine;
using GGemCo2DAffect;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core 캐릭터(CharacterBase)를 com.ggemco.2d.affect 런타임(IAffectTarget) 계약에 연결하는 어댑터.
    /// </summary>
    /// <remarks>
    /// - CharacterBase(플레이어/몬스터/NPC 공용)에서 자동 부착(EnsureAffectSystem)되는 것을 전제로 한다.
    /// - Stat: Core의 CharacterStat modifier(플랫/퍼센트) 파이프라인을 사용한다.
    /// - State: Core에 정식 상태이상 시스템이 없는 상태를 고려하여, 최소 구현(플래그+만료는 Affect가 관리)을 제공한다.
    /// - Damage: Core의 CharacterBase.TakeDamage(MetadataDamage) 경로로 위임한다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CoreAffectTargetAdapter : MonoBehaviour, IAffectTarget
    {
        private CharacterBase _character;
        private CoreStatMutable _stats;
        private CoreStateMutable _states;
        private CoreDamageReceiver _damage;

        public Transform Transform => _character != null ? _character.transform : transform;
        public bool IsAlive => _character != null && !_character.IsStatusDead();

        public IStatMutable Stats => _stats;
        public IStateMutable States => _states;
        public IDamageReceiver Damage => _damage;

        private void Awake()
        {
            _character = GetComponent<CharacterBase>();
            if (_character == null)
            {
                Debug.LogError($"[Affect][CoreAffectTargetAdapter] CharacterBase가 없습니다. name={name}");
                enabled = false;
                return;
            }

            _stats = new CoreStatMutable(_character);
            _states = new CoreStateMutable();
            _damage = new CoreDamageReceiver(_character);
        }

        private sealed class StatToken
        {
            public readonly List<ConfigCommon.StruckStatus> Modifiers;
            public StatToken(List<ConfigCommon.StruckStatus> modifiers) => Modifiers = modifiers;
        }

        private sealed class CoreStatMutable : IStatMutable
        {
            private readonly CharacterBase _character;

            public CoreStatMutable(CharacterBase character) => _character = character;

            public object ApplyModifier(string statId, float value, GGemCo2DAffect.ValueType valueType, StatOperation operation)
            {
                if (string.IsNullOrWhiteSpace(statId)) return null;

                // Core는 "Plus/Minus/Increase/Decrease" 기반이므로, Affect의 Add/Multiply/Override를
                // Flat/Percent 값으로 매핑한다.
                var list = new List<ConfigCommon.StruckStatus>(capacity: 1);

                // Override는 Core의 단일 modifier 체계로는 정확히 표현하기 어려우므로,
                // 일단 Add로 처리한다(필요 시 별도 Stat 시스템으로 확장 권장).
                if (operation == StatOperation.Override)
                    operation = StatOperation.Add;

                if (operation == StatOperation.Multiply || valueType == GGemCo2DAffect.ValueType.Percent)
                {
                    // 퍼센트 적용
                    var suffix = value >= 0 ? ConfigCommon.SuffixType.Increase : ConfigCommon.SuffixType.Decrease;
                    list.Add(new ConfigCommon.StruckStatus(statId, suffix, Math.Abs(value)));
                }
                else
                {
                    // 플랫 적용
                    var suffix = value >= 0 ? ConfigCommon.SuffixType.Plus : ConfigCommon.SuffixType.Minus;
                    list.Add(new ConfigCommon.StruckStatus(statId, suffix, Math.Abs(value)));
                }

                _character.ApplyStatModifiers(list);
                _character.RecalculateStats();
                return new StatToken(list);
            }

            public void RemoveModifier(object token)
            {
                if (token is not StatToken t || t.Modifiers == null) return;
                _character.RemoveStatModifiers(t.Modifiers);
                _character.RecalculateStats();
            }

            public void Recalculate() => _character.RecalculateStats();

            public float GetValue(string statId)
            {
                if (string.IsNullOrWhiteSpace(statId)) return 0f;

                // Core의 대표 스탯만 매핑. (프로젝트 확장 시 여기서 추가)
                if (statId == ConfigCommon.StatusStatAtk) return _character.TotalAtk.Value;
                if (statId == ConfigCommon.StatusStatDef) return _character.TotalDef.Value;
                if (statId == ConfigCommon.StatusStatHp) return _character.TotalHp.Value;
                if (statId == ConfigCommon.StatusStatMp) return _character.TotalMp.Value;
                if (statId == ConfigCommon.StatusStatMoveSpeed) return _character.TotalMoveSpeed.Value;
                if (statId == ConfigCommon.StatusStatAttackSpeed) return _character.TotalAttackSpeed.Value;
                if (statId == ConfigCommon.StatusStatCriticalDamage) return _character.TotalCriticalDamage.Value;
                if (statId == ConfigCommon.StatusStatCriticalProbability) return _character.TotalCriticalProbability.Value;

                // 저항(기존 Core 구현: Fire/Cold/Lightning)
                if (statId == ConfigCommon.StatusRegistFire) return _character.TotalRegistFire.Value;
                if (statId == ConfigCommon.StatusRegistCold) return _character.TotalRegistCold.Value;
                if (statId == ConfigCommon.StatusRegistLightning) return _character.TotalRegistLightning.Value;

                return 0f;
            }
        }

        private sealed class StateToken
        {
            public readonly string StateId;
            public StateToken(string stateId) => StateId = stateId;
        }

        private sealed class CoreStateMutable : IStateMutable
        {
            private readonly HashSet<string> _states = new HashSet<string>();

            public bool HasState(string stateId) => !string.IsNullOrWhiteSpace(stateId) && _states.Contains(stateId);

            public object ApplyState(string stateId, float duration)
            {
                if (string.IsNullOrWhiteSpace(stateId)) return null;
                _states.Add(stateId);
                return new StateToken(stateId);
            }

            public void RemoveState(object token)
            {
                if (token is not StateToken t || string.IsNullOrWhiteSpace(t.StateId)) return;
                _states.Remove(t.StateId);
            }

            public bool IsImmune(string stateId)
            {
                // Core 정식 면역 시스템이 연결되면 여기서 구현.
                return false;
            }
        }

        private sealed class CoreDamageReceiver : IDamageReceiver
        {
            private readonly CharacterBase _character;

            public CoreDamageReceiver(CharacterBase character) => _character = character;

            public void ApplyDamage(string damageTypeId, float amount, bool canCrit, bool isDot, object source)
            {
                if (amount <= 0) return;

                // Core는 SkillConstants.DamageType enum 기반.
                var dt = SkillConstants.DamageType.None;
                if (!string.IsNullOrWhiteSpace(damageTypeId))
                {
                    var k = damageTypeId.Trim().ToLowerInvariant();
                    if (k.Contains("fire")) dt = SkillConstants.DamageType.Fire;
                    else if (k.Contains("cold") || k.Contains("ice")) dt = SkillConstants.DamageType.Cold;
                    else if (k.Contains("lightning") || k.Contains("electric")) dt = SkillConstants.DamageType.Lightning;
                }

                var md = new MetadataDamage
                {
                    damage = (long)Mathf.Ceil(amount),
                    damageType = dt,
                    attacker = source as GameObject,
                    affectUid = 0
                };

                _character.TakeDamage(md);
            }

            public void ApplyHeal(float amount, object source)
            {
                if (amount <= 0) return;

                long maxHp = _character.TotalHp.Value;
                long newHp = _character.CurrentHp.Value + (long)Mathf.Ceil(amount);
                if (maxHp >= 0) newHp = Math.Min(newHp, maxHp);
                if (newHp < 0) newHp = 0;

                _character.CurrentHp.OnNext(newHp);
            }
        }
    }
}
