using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어와 현재 교전 중인 몬스터 목록을 관리하고 플레이어 전투 상태를 동기화합니다.
    /// </summary>
    /// <remarks>
    /// 몬스터별 전투 시작/종료 이벤트를 등록 방식으로 수집하여,
    /// 일부 몬스터가 사망하거나 이탈해도 다른 교전 대상이 남아 있으면 전투 상태를 유지합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatEngagementTracker : MonoBehaviour
    {
        private readonly Dictionary<int, Monster> _engagedMonsters = new();
        private readonly List<int> _invalidMonsterIds = new();

        private Player _owner;

        /// <summary>
        /// 전투 참여 목록이 변경된 뒤 호출됩니다.
        /// </summary>
        public event Action EngagementsChanged;

        /// <summary>
        /// 현재 유효한 전투 참여 몬스터 수를 반환합니다.
        /// </summary>
        public int EngagedCount
        {
            get
            {
                PruneInvalidEngagements();
                return _engagedMonsters.Count;
            }
        }

        /// <summary>
        /// 현재 하나 이상의 유효한 몬스터와 교전 중인지 여부를 반환합니다.
        /// </summary>
        public bool HasEngagements => EngagedCount > 0;

        /// <summary>
        /// 컴포넌트 생성 시 같은 게임 오브젝트의 플레이어를 기본 소유자로 연결합니다.
        /// </summary>
        private void Awake()
        {
            Initialize(GetComponent<Player>());
        }

        /// <summary>
        /// 전투 상태를 동기화할 플레이어를 설정합니다.
        /// </summary>
        /// <param name="owner">전투 참여 목록을 소유한 플레이어입니다.</param>
        public void Initialize(Player owner)
        {
            if (owner == null)
            {
                return;
            }

            _owner = owner;
            SynchronizeOwnerBattleStatus();
        }

        /// <summary>
        /// 플레이어와 교전을 시작한 몬스터를 참여 목록에 등록합니다.
        /// </summary>
        /// <param name="monster">등록할 몬스터입니다.</param>
        /// <returns>새로운 몬스터가 등록되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool Register(Monster monster)
        {
            if (!IsValidEngagement(monster) || (_owner != null && _owner.IsStatusDead()))
            {
                return false;
            }

            int instanceId = monster.GetInstanceID();
            if (_engagedMonsters.TryGetValue(instanceId, out Monster registeredMonster) &&
                registeredMonster == monster)
            {
                return false;
            }

            _engagedMonsters[instanceId] = monster;
            NotifyEngagementsChanged();
            return true;
        }

        /// <summary>
        /// 지정한 몬스터를 플레이어의 전투 참여 목록에서 해제합니다.
        /// </summary>
        /// <param name="monster">해제할 몬스터입니다.</param>
        /// <returns>등록된 몬스터가 실제로 해제되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool Unregister(Monster monster)
        {
            if (monster == null)
            {
                return false;
            }

            if (!_engagedMonsters.Remove(monster.GetInstanceID()))
            {
                return false;
            }

            NotifyEngagementsChanged();
            return true;
        }

        /// <summary>
        /// 지정한 몬스터가 현재 전투 참여 목록에 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="monster">확인할 몬스터입니다.</param>
        /// <returns>유효한 교전 대상으로 등록되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool Contains(Monster monster)
        {
            if (monster == null)
            {
                return false;
            }

            PruneInvalidEngagements();
            return _engagedMonsters.TryGetValue(monster.GetInstanceID(), out Monster registeredMonster) &&
                   registeredMonster == monster;
        }

        /// <summary>
        /// 현재 플레이어의 모든 전투 참여 몬스터를 해제합니다.
        /// </summary>
        public void Clear()
        {
            if (_engagedMonsters.Count == 0)
            {
                SynchronizeOwnerBattleStatus();
                return;
            }

            _engagedMonsters.Clear();
            NotifyEngagementsChanged();
        }

        /// <summary>
        /// 현재 참여 목록에서 기준 위치와 가장 가까운 유효 몬스터를 찾습니다.
        /// </summary>
        /// <param name="origin">거리 계산의 기준 월드 좌표입니다.</param>
        /// <param name="monster">찾은 가장 가까운 몬스터입니다.</param>
        /// <returns>유효한 몬스터를 찾았으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGetNearestEngagedMonster(Vector3 origin, out Monster monster)
        {
            PruneInvalidEngagements();

            monster = null;
            float nearestSqrDistance = float.PositiveInfinity;

            foreach (KeyValuePair<int, Monster> pair in _engagedMonsters)
            {
                Monster candidate = pair.Value;
                if (!IsValidEngagement(candidate))
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                monster = candidate;
            }

            return monster != null;
        }

        /// <summary>
        /// 현재 유효한 전투 참여 몬스터 목록을 호출자가 제공한 리스트에 복사합니다.
        /// </summary>
        /// <param name="results">복사 결과를 받을 리스트입니다. 호출 시 기존 내용은 제거됩니다.</param>
        /// <returns>복사된 유효 몬스터 수입니다.</returns>
        /// <remarks>
        /// 전역 전투 HUD처럼 교전 목록을 순회해야 하는 시스템에서 내부 Dictionary를 직접 노출하지 않기 위해 사용합니다.
        /// 호출자가 리스트를 재사용하면 전투 중 반복 할당 없이 후보를 평가할 수 있습니다.
        /// </remarks>
        public int CopyEngagedMonsters(List<Monster> results)
        {
            if (results == null)
            {
                return 0;
            }

            PruneInvalidEngagements();
            results.Clear();
            foreach (KeyValuePair<int, Monster> pair in _engagedMonsters)
            {
                Monster monster = pair.Value;
                if (IsValidEngagement(monster))
                {
                    results.Add(monster);
                }
            }

            return results.Count;
        }

        /// <summary>
        /// 사망, 비활성화 또는 제거된 몬스터를 참여 목록에서 정리합니다.
        /// </summary>
        private void PruneInvalidEngagements()
        {
            if (_engagedMonsters.Count == 0)
            {
                return;
            }

            _invalidMonsterIds.Clear();
            foreach (KeyValuePair<int, Monster> pair in _engagedMonsters)
            {
                if (!IsValidEngagement(pair.Value))
                {
                    _invalidMonsterIds.Add(pair.Key);
                }
            }

            if (_invalidMonsterIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _invalidMonsterIds.Count; i++)
            {
                _engagedMonsters.Remove(_invalidMonsterIds[i]);
            }

            _invalidMonsterIds.Clear();
            NotifyEngagementsChanged();
        }

        /// <summary>
        /// 지정한 몬스터를 전투 참여 대상으로 유지할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="monster">검증할 몬스터입니다.</param>
        /// <returns>활성 상태이며 살아 있는 몬스터이면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsValidEngagement(Monster monster)
        {
            return monster != null &&
                   monster.gameObject.activeInHierarchy &&
                   !monster.IsStatusDead();
        }

        /// <summary>
        /// 플레이어 전투 상태를 현재 참여 수에 맞춰 갱신하고 변경 이벤트를 발행합니다.
        /// </summary>
        private void NotifyEngagementsChanged()
        {
            SynchronizeOwnerBattleStatus();
            EngagementsChanged?.Invoke();
        }

        /// <summary>
        /// 참여 중인 몬스터가 하나 이상이면 플레이어를 전투 상태로, 없으면 비전투 상태로 동기화합니다.
        /// </summary>
        private void SynchronizeOwnerBattleStatus()
        {
            if (_owner == null)
            {
                return;
            }

            if (_engagedMonsters.Count > 0 && !_owner.IsStatusDead())
            {
                _owner.SetBattleStatusInBattle();
                return;
            }

            _owner.SetBattleStatusNone();
        }
    }
}
