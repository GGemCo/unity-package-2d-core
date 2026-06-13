using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터가 인식한 대상별 Threat를 누적하고 현재 전투 타겟을 선택합니다.
    /// </summary>
    /// <remarks>
    /// 감지, 패트롤, 피격 원인을 대상별로 독립 보관하므로 한 원인이 해제되어도
    /// 다른 원인으로 남은 Threat가 있으면 전투 관계와 현재 타겟을 유지합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MonsterThreatController : MonoBehaviour, IMonsterPoolLifecycle
    {
        private const float ThreatEpsilon = 0.0001f;
        private const float MaintenanceIntervalSeconds = 0.2f;

        private sealed class ThreatEntry
        {
            public int InstanceId;
            public CharacterBase Target;
            public float DetectionThreat;
            public float PatrolThreat;
            public float DamageThreat;
            public float ExternalThreat;
            public float LastUpdatedTime;
            public Vector3 LastKnownPosition;

            public float TotalThreat =>
                DetectionThreat +
                PatrolThreat +
                DamageThreat +
                ExternalThreat;

            public MonsterThreatSource Sources
            {
                get
                {
                    MonsterThreatSource sources = MonsterThreatSource.None;
                    if (DetectionThreat > ThreatEpsilon) sources |= MonsterThreatSource.DetectionRange;
                    if (PatrolThreat > ThreatEpsilon) sources |= MonsterThreatSource.Patrol;
                    if (DamageThreat > ThreatEpsilon) sources |= MonsterThreatSource.Damage;
                    if (ExternalThreat > ThreatEpsilon) sources |= MonsterThreatSource.External;
                    return sources;
                }
            }
        }

        private readonly Dictionary<int, ThreatEntry> _entries = new();
        private readonly List<int> _invalidEntryIds = new();
        private readonly List<CharacterBase> _removedTargets = new();

        private Monster _owner;
        private MonsterThreatProfile _profile;
        private CharacterBase _currentTarget;
        private CharacterBase _forcedTarget;
        private float _forcedTargetExpireTime;
        private float _nextMaintenanceTime;

        /// <summary>Threat 목록에 새로운 대상이 등록된 직후 호출됩니다.</summary>
        public event Action<CharacterBase> ThreatTargetRegistered;

        /// <summary>Threat 목록에서 대상이 완전히 제거된 직후 호출됩니다.</summary>
        public event Action<CharacterBase> ThreatTargetUnregistered;

        /// <summary>최종 선택된 현재 전투 타겟이 변경된 직후 호출됩니다.</summary>
        public event Action<CharacterBase, CharacterBase> CurrentTargetChanged;

        /// <summary>현재 선택된 전투 타겟입니다.</summary>
        public CharacterBase CurrentTarget => _currentTarget;

        /// <summary>현재 기억 중인 유효 Threat 대상 수입니다.</summary>
        public int TargetCount
        {
            get
            {
                PruneInvalidEntries();
                return _entries.Count;
            }
        }

        /// <summary>하나 이상의 유효한 Threat 대상이 있는지 여부입니다.</summary>
        public bool HasTargets => TargetCount > 0;

        /// <summary>
        /// Threat를 소유할 몬스터를 연결하고 런타임 상태를 초기화합니다.
        /// </summary>
        /// <param name="owner">이 컨트롤러를 소유한 몬스터입니다.</param>
        public void Initialize(Monster owner)
        {
            if (_owner != null && _owner != owner)
            {
                ClearAllThreats();
            }

            _owner = owner;
            if (_profile.MaxThreatTargets <= 0)
            {
                _profile = MonsterThreatProfile.Create(null);
            }

            _nextMaintenanceTime = Time.time + ResolveInitialMaintenanceDelay();
        }

        /// <summary>
        /// 테이블에서 정규화한 Threat 정책을 적용하고 최대 대상 수를 다시 맞춥니다.
        /// </summary>
        /// <param name="profile">적용할 Threat 프로필입니다.</param>
        public void Configure(MonsterThreatProfile profile)
        {
            _profile = profile;
            TrimEntriesToCapacity();
            RefreshCurrentTarget();
        }

        /// <summary>
        /// 감지 또는 패트롤처럼 범위 안에 있는 동안 유지되는 Threat 원인을 설정합니다.
        /// </summary>
        /// <param name="target">Threat 대상으로 등록할 캐릭터입니다.</param>
        /// <param name="source">감지 범위 또는 패트롤 원인입니다.</param>
        /// <param name="isActive">원인을 활성화할지 제거할지 여부입니다.</param>
        /// <param name="threatValue">활성화 시 유지할 Threat 값입니다.</param>
        /// <returns>Threat 목록 또는 점수가 실제로 변경되었으면 <see langword="true"/>입니다.</returns>
        public bool SetPresenceThreat(
            CharacterBase target,
            MonsterThreatSource source,
            bool isActive,
            float threatValue)
        {
            if (source != MonsterThreatSource.DetectionRange && source != MonsterThreatSource.Patrol)
            {
                return false;
            }

            if (!isActive)
            {
                return RemovePresenceThreat(target, source);
            }

            if (!IsValidTarget(target))
            {
                return false;
            }

            ThreatEntry entry = GetOrCreateEntry(target);
            if (entry == null)
            {
                return false;
            }

            float normalizedThreat = Mathf.Max(ThreatEpsilon, threatValue);
            float previousThreat = source == MonsterThreatSource.DetectionRange
                ? entry.DetectionThreat
                : entry.PatrolThreat;

            if (Mathf.Approximately(previousThreat, normalizedThreat))
            {
                RefreshEntryObservation(entry);
                return false;
            }

            if (source == MonsterThreatSource.DetectionRange)
            {
                entry.DetectionThreat = normalizedThreat;
            }
            else
            {
                entry.PatrolThreat = normalizedThreat;
            }

            RefreshEntryObservation(entry);
            RefreshCurrentTarget();
            return true;
        }

        /// <summary>
        /// 확정 피해량을 프로필 기준 Threat로 변환하여 대상에게 누적합니다.
        /// </summary>
        /// <param name="target">피해를 발생시킨 캐릭터입니다.</param>
        /// <param name="confirmedDamage">방어력 적용 후 확정된 피해량입니다.</param>
        /// <returns>Threat가 누적되었으면 <see langword="true"/>입니다.</returns>
        public bool AddDamageThreat(CharacterBase target, long confirmedDamage)
        {
            return AddThreat(
                target,
                _profile.CalculateDamageThreat(confirmedDamage),
                MonsterThreatSource.Damage);
        }

        /// <summary>
        /// 외부 시스템 또는 피해 처리에서 지정한 Threat를 대상에게 누적합니다.
        /// </summary>
        /// <param name="target">Threat 대상 캐릭터입니다.</param>
        /// <param name="amount">누적할 0보다 큰 Threat 값입니다.</param>
        /// <param name="source">피해 또는 외부 원인입니다.</param>
        /// <returns>Threat가 누적되었으면 <see langword="true"/>입니다.</returns>
        public bool AddThreat(
            CharacterBase target,
            float amount,
            MonsterThreatSource source = MonsterThreatSource.External)
        {
            if (!IsValidTarget(target) || amount <= 0f)
            {
                return false;
            }

            if (source != MonsterThreatSource.Damage && source != MonsterThreatSource.External)
            {
                return false;
            }

            ThreatEntry entry = GetOrCreateEntry(target);
            if (entry == null)
            {
                return false;
            }

            if (source == MonsterThreatSource.Damage)
            {
                entry.DamageThreat += amount;
            }
            else
            {
                entry.ExternalThreat += amount;
            }

            RefreshEntryObservation(entry);
            RefreshCurrentTarget();
            return true;
        }

        /// <summary>
        /// 도발 또는 보스 패턴처럼 지정한 대상을 일정 시간 동안 최우선 타겟으로 고정합니다.
        /// </summary>
        /// <param name="target">강제로 선택할 캐릭터입니다.</param>
        /// <param name="durationSeconds">고정 시간입니다. 0 이하면 명시적으로 해제할 때까지 유지합니다.</param>
        /// <returns>강제 타겟을 적용했으면 <see langword="true"/>입니다.</returns>
        public bool ForceTarget(CharacterBase target, float durationSeconds)
        {
            if (!IsValidTarget(target))
            {
                return false;
            }

            ThreatEntry entry = GetOrCreateEntry(target);
            if (entry == null)
            {
                return false;
            }

            if (entry.TotalThreat <= ThreatEpsilon)
            {
                entry.ExternalThreat = Mathf.Max(ThreatEpsilon, _profile.MinimumDamageThreat);
            }

            RefreshEntryObservation(entry);
            _forcedTarget = target;
            _forcedTargetExpireTime = durationSeconds > 0f
                ? Time.time + durationSeconds
                : float.PositiveInfinity;
            RefreshCurrentTarget();
            return true;
        }

        /// <summary>
        /// 현재 적용 중인 강제 타겟 정책을 해제하고 일반 Threat 순위로 다시 선택합니다.
        /// </summary>
        public void ClearForcedTarget()
        {
            if (_forcedTarget == null)
            {
                return;
            }

            _forcedTarget = null;
            _forcedTargetExpireTime = 0f;
            RefreshCurrentTarget();
        }

        /// <summary>
        /// 지정한 대상의 모든 Threat 원인을 제거합니다.
        /// </summary>
        /// <param name="target">제거할 Threat 대상입니다.</param>
        /// <returns>대상이 실제로 제거되었으면 <see langword="true"/>입니다.</returns>
        public bool ClearThreat(CharacterBase target)
        {
            if (target == null || !_entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry))
            {
                return false;
            }

            RemoveEntry(entry);
            RefreshCurrentTarget();
            return true;
        }

        /// <summary>
        /// 모든 Threat 대상과 강제 타겟을 제거합니다.
        /// </summary>
        public void ClearAllThreats()
        {
            if (_entries.Count == 0)
            {
                _forcedTarget = null;
                _forcedTargetExpireTime = 0f;
                SetCurrentTarget(null);
                return;
            }

            _removedTargets.Clear();
            foreach (KeyValuePair<int, ThreatEntry> pair in _entries)
            {
                CharacterBase target = pair.Value.Target;
                if (target != null)
                {
                    _removedTargets.Add(target);
                }
            }

            _entries.Clear();
            _forcedTarget = null;
            _forcedTargetExpireTime = 0f;
            SetCurrentTarget(null);

            for (int i = 0; i < _removedTargets.Count; i++)
            {
                ThreatTargetUnregistered?.Invoke(_removedTargets[i]);
            }

            _removedTargets.Clear();
        }

        /// <summary>
        /// 현재 선택된 전투 타겟을 반환합니다.
        /// </summary>
        /// <param name="target">선택된 타겟입니다.</param>
        /// <returns>유효한 현재 타겟이 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetCurrentTarget(out CharacterBase target)
        {
            PruneInvalidEntries();
            RefreshCurrentTarget();
            target = _currentTarget;
            return target != null;
        }

        /// <summary>
        /// 지정한 대상이 현재 Threat 목록에 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 캐릭터입니다.</param>
        /// <returns>유효한 Threat 항목이 있으면 <see langword="true"/>입니다.</returns>
        public bool ContainsTarget(CharacterBase target)
        {
            if (target == null)
            {
                return false;
            }

            PruneInvalidEntries();
            return _entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry) &&
                   entry.Target == target &&
                   IsValidEntry(entry);
        }

        /// <summary>
        /// 지정한 대상에게 특정 원인의 Threat가 남아 있는지 확인합니다.
        /// </summary>
        /// <param name="target">확인할 캐릭터입니다.</param>
        /// <param name="source">확인할 단일 Threat 원인입니다.</param>
        /// <returns>해당 원인의 Threat가 0보다 크면 <see langword="true"/>입니다.</returns>
        public bool HasThreatSource(CharacterBase target, MonsterThreatSource source)
        {
            if (target == null)
            {
                return false;
            }

            PruneInvalidEntries();
            return _entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry) &&
                   entry.Target == target &&
                   (entry.Sources & source) != 0;
        }

        /// <summary>
        /// 지정한 대상에게 현재 누적된 총 Threat를 조회합니다.
        /// </summary>
        /// <param name="target">조회할 캐릭터입니다.</param>
        /// <param name="threat">누적된 총 Threat입니다.</param>
        /// <returns>대상이 Threat 목록에 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetThreat(CharacterBase target, out float threat)
        {
            threat = 0f;
            if (target == null)
            {
                return false;
            }

            PruneInvalidEntries();
            if (!_entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry) || entry.Target != target)
            {
                return false;
            }

            threat = entry.TotalThreat;
            return threat > ThreatEpsilon;
        }

        /// <summary>
        /// 현재 Threat 목록을 다시 평가해 최종 전투 타겟을 갱신합니다.
        /// </summary>
        /// <returns>유효한 현재 타겟을 선택했으면 <see langword="true"/>입니다.</returns>
        public bool RefreshCurrentTarget()
        {
            ExpireForcedTargetIfNeeded();
            PruneInvalidEntries();

            CharacterBase selectedTarget = SelectBestTarget();
            SetCurrentTarget(selectedTarget);
            return selectedTarget != null;
        }

        /// <summary>
        /// 낮은 주기로 삭제 대상과 강제 타겟 만료를 정리합니다.
        /// </summary>
        private void Update()
        {
            if (Time.time < _nextMaintenanceTime)
            {
                return;
            }

            _nextMaintenanceTime = Time.time + MaintenanceIntervalSeconds;
            RefreshCurrentTarget();
        }

        /// <summary>
        /// 감지 또는 패트롤 원인만 제거하고 다른 원인의 Threat는 유지합니다.
        /// </summary>
        private bool RemovePresenceThreat(CharacterBase target, MonsterThreatSource source)
        {
            if (target == null || !_entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry))
            {
                return false;
            }

            bool changed;
            if (source == MonsterThreatSource.DetectionRange)
            {
                changed = entry.DetectionThreat > ThreatEpsilon;
                entry.DetectionThreat = 0f;
            }
            else
            {
                changed = entry.PatrolThreat > ThreatEpsilon;
                entry.PatrolThreat = 0f;
            }

            if (!changed)
            {
                return false;
            }

            if (entry.TotalThreat <= ThreatEpsilon)
            {
                RemoveEntry(entry);
            }
            else
            {
                RefreshEntryObservation(entry);
            }

            RefreshCurrentTarget();
            return true;
        }

        /// <summary>
        /// 대상에 대응하는 Threat 항목을 찾거나 새로 생성합니다.
        /// </summary>
        private ThreatEntry GetOrCreateEntry(CharacterBase target)
        {
            int instanceId = target.GetInstanceID();
            if (_entries.TryGetValue(instanceId, out ThreatEntry existingEntry) && existingEntry.Target == target)
            {
                return existingEntry;
            }

            EnsureCapacityForNewEntry();
            if (_entries.Count >= Mathf.Max(1, _profile.MaxThreatTargets))
            {
                return null;
            }

            ThreatEntry entry = new ThreatEntry
            {
                InstanceId = instanceId,
                Target = target,
                LastUpdatedTime = Time.time,
                LastKnownPosition = target.transform.position,
            };
            _entries[instanceId] = entry;
            ThreatTargetRegistered?.Invoke(target);
            return entry;
        }

        /// <summary>
        /// 최대 대상 수를 넘은 경우 현재/강제 타겟을 제외한 가장 낮은 Threat 항목을 제거합니다.
        /// </summary>
        private void EnsureCapacityForNewEntry()
        {
            int capacity = Mathf.Max(1, _profile.MaxThreatTargets);
            if (_entries.Count < capacity)
            {
                return;
            }

            ThreatEntry lowestEntry = null;
            foreach (KeyValuePair<int, ThreatEntry> pair in _entries)
            {
                ThreatEntry candidate = pair.Value;
                if (candidate.Target == _currentTarget || candidate.Target == _forcedTarget)
                {
                    continue;
                }

                if (lowestEntry == null || candidate.TotalThreat < lowestEntry.TotalThreat)
                {
                    lowestEntry = candidate;
                }
            }

            if (lowestEntry != null)
            {
                RemoveEntry(lowestEntry);
            }
        }

        /// <summary>
        /// 프로필 최대 대상 수에 맞춰 낮은 Threat 항목부터 제거합니다.
        /// </summary>
        private void TrimEntriesToCapacity()
        {
            int capacity = Mathf.Max(1, _profile.MaxThreatTargets);
            while (_entries.Count > capacity)
            {
                int countBefore = _entries.Count;
                EnsureCapacityForNewEntry();
                if (_entries.Count == countBefore)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 일반 Threat 순위와 현재 타겟 유지 비율을 적용해 최종 후보를 선택합니다.
        /// </summary>
        private CharacterBase SelectBestTarget()
        {
            if (IsValidForcedTarget())
            {
                return _forcedTarget;
            }

            ThreatEntry currentEntry = TryGetEntry(_currentTarget);
            ThreatEntry bestEntry = null;
            float bestDistanceSqr = float.PositiveInfinity;
            Vector3 ownerPosition = _owner != null ? _owner.transform.position : transform.position;

            foreach (KeyValuePair<int, ThreatEntry> pair in _entries)
            {
                ThreatEntry candidate = pair.Value;
                if (!IsValidEntry(candidate))
                {
                    continue;
                }

                float candidateDistanceSqr = (candidate.Target.transform.position - ownerPosition).sqrMagnitude;
                if (bestEntry == null || candidate.TotalThreat > bestEntry.TotalThreat + ThreatEpsilon)
                {
                    bestEntry = candidate;
                    bestDistanceSqr = candidateDistanceSqr;
                    continue;
                }

                if (Mathf.Abs(candidate.TotalThreat - bestEntry.TotalThreat) <= ThreatEpsilon &&
                    candidateDistanceSqr < bestDistanceSqr)
                {
                    bestEntry = candidate;
                    bestDistanceSqr = candidateDistanceSqr;
                }
            }

            if (bestEntry == null)
            {
                return null;
            }

            if (currentEntry == null || bestEntry == currentEntry)
            {
                return bestEntry.Target;
            }

            float switchThreshold = currentEntry.TotalThreat * Mathf.Max(1f, _profile.TargetSwitchThreatRatio);
            return bestEntry.TotalThreat + ThreatEpsilon >= switchThreshold
                ? bestEntry.Target
                : currentEntry.Target;
        }

        /// <summary>
        /// 지정한 캐릭터의 Threat 항목을 반환합니다.
        /// </summary>
        private ThreatEntry TryGetEntry(CharacterBase target)
        {
            if (target == null)
            {
                return null;
            }

            return _entries.TryGetValue(target.GetInstanceID(), out ThreatEntry entry) && entry.Target == target
                ? entry
                : null;
        }

        /// <summary>
        /// 현재 전투 타겟을 변경하고 구독자에게 이전/신규 대상을 전달합니다.
        /// </summary>
        private void SetCurrentTarget(CharacterBase target)
        {
            if (_currentTarget == target)
            {
                return;
            }

            CharacterBase previousTarget = _currentTarget;
            _currentTarget = target;
            CurrentTargetChanged?.Invoke(previousTarget, target);
        }

        /// <summary>
        /// Threat 항목을 완전히 제거하고 등록 해제 이벤트를 발행합니다.
        /// </summary>
        private void RemoveEntry(ThreatEntry entry)
        {
            if (entry == null || !_entries.Remove(entry.InstanceId))
            {
                return;
            }

            CharacterBase target = entry.Target;
            if (_forcedTarget == target)
            {
                _forcedTarget = null;
                _forcedTargetExpireTime = 0f;
            }

            if (target != null)
            {
                ThreatTargetUnregistered?.Invoke(target);
            }
        }

        /// <summary>
        /// 사망, 비활성화 또는 제거된 캐릭터의 Threat 항목을 정리합니다.
        /// </summary>
        private void PruneInvalidEntries()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            _invalidEntryIds.Clear();
            foreach (KeyValuePair<int, ThreatEntry> pair in _entries)
            {
                if (!IsValidEntry(pair.Value))
                {
                    _invalidEntryIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _invalidEntryIds.Count; i++)
            {
                int instanceId = _invalidEntryIds[i];
                if (_entries.TryGetValue(instanceId, out ThreatEntry entry))
                {
                    RemoveEntry(entry);
                }
            }

            _invalidEntryIds.Clear();
        }

        /// <summary>
        /// 강제 타겟의 시간이 만료되었거나 대상이 무효해졌으면 일반 선택 정책으로 복귀합니다.
        /// </summary>
        private void ExpireForcedTargetIfNeeded()
        {
            if (_forcedTarget == null)
            {
                return;
            }

            if (!IsValidTarget(_forcedTarget) || Time.time >= _forcedTargetExpireTime)
            {
                _forcedTarget = null;
                _forcedTargetExpireTime = 0f;
            }
        }

        /// <summary>
        /// 현재 강제 타겟이 유효하고 유지 시간 안에 있는지 확인합니다.
        /// </summary>
        private bool IsValidForcedTarget()
        {
            return _forcedTarget != null &&
                   IsValidTarget(_forcedTarget) &&
                   Time.time < _forcedTargetExpireTime &&
                   TryGetEntry(_forcedTarget) != null;
        }

        /// <summary>
        /// Threat 항목의 마지막 관측 시간과 위치를 갱신합니다.
        /// </summary>
        private static void RefreshEntryObservation(ThreatEntry entry)
        {
            if (entry == null || entry.Target == null)
            {
                return;
            }

            entry.LastUpdatedTime = Time.time;
            entry.LastKnownPosition = entry.Target.transform.position;
        }

        /// <summary>
        /// Threat 항목과 대상이 현재 전투 후보로 유효한지 확인합니다.
        /// </summary>
        private bool IsValidEntry(ThreatEntry entry)
        {
            return entry != null && entry.TotalThreat > ThreatEpsilon && IsValidTarget(entry.Target);
        }

        /// <summary>
        /// 몬스터 자신이 아니며 활성 상태로 살아 있는 캐릭터인지 확인합니다.
        /// </summary>
        private bool IsValidTarget(CharacterBase target)
        {
            return target != null &&
                   target != _owner &&
                   target.gameObject.activeInHierarchy &&
                   !target.IsStatusDead();
        }

        /// <summary>
        /// 다수 몬스터의 유지보수 검사가 같은 프레임에 집중되지 않도록 첫 실행 시간을 분산합니다.
        /// </summary>
        private float ResolveInitialMaintenanceDelay()
        {
            int phase = Mathf.Abs(GetInstanceID() % 1000);
            return phase / 1000f * MaintenanceIntervalSeconds;
        }

        /// <summary>
        /// 풀에서 다시 대여될 때 이전 Threat 상태를 초기화합니다.
        /// </summary>
        /// <param name="owner">대여된 몬스터입니다.</param>
        public void OnPoolRent(Monster owner)
        {
            _owner = owner;
            ClearAllThreats();
            _nextMaintenanceTime = Time.time + ResolveInitialMaintenanceDelay();
        }

        /// <summary>
        /// 풀로 반환되기 전에 모든 대상의 전투 참여 관계를 정리합니다.
        /// </summary>
        /// <param name="owner">반환되는 몬스터입니다.</param>
        public void OnPoolReturn(Monster owner)
        {
            ClearAllThreats();
        }
    }
}
