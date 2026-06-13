using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 같은 Encounter ID를 가진 몬스터를 등록하고 그룹 단위 활성화와 지원 어그로를 전달합니다.
    /// </summary>
    public static class MonsterEncounterRegistry
    {
        private static readonly Dictionary<int, List<MonsterEncounterMember>> MembersByEncounter = new();
        private static readonly List<MonsterEncounterMember> Snapshot = new();
        private static readonly List<MonsterEncounterMember> AssistCandidates = new();
        private static bool _isDispatchingAssist;

        /// <summary>
        /// Encounter 멤버를 그룹에 등록합니다.
        /// </summary>
        /// <param name="member">등록할 Encounter 멤버입니다.</param>
        public static void Register(MonsterEncounterMember member)
        {
            if (member == null || member.EncounterId <= 0)
            {
                return;
            }

            if (!MembersByEncounter.TryGetValue(member.EncounterId, out List<MonsterEncounterMember> members))
            {
                members = new List<MonsterEncounterMember>(8);
                MembersByEncounter.Add(member.EncounterId, members);
            }

            if (!members.Contains(member))
            {
                members.Add(member);
            }
        }

        /// <summary>
        /// Encounter 멤버를 현재 그룹에서 제거합니다.
        /// </summary>
        /// <param name="member">등록을 해제할 Encounter 멤버입니다.</param>
        public static void Unregister(MonsterEncounterMember member)
        {
            if (member == null || member.EncounterId <= 0 ||
                !MembersByEncounter.TryGetValue(member.EncounterId, out List<MonsterEncounterMember> members))
            {
                return;
            }

            members.Remove(member);
            PruneInvalidMembers(members);
            if (members.Count == 0)
            {
                MembersByEncounter.Remove(member.EncounterId);
            }
        }

        /// <summary>
        /// Encounter 볼륨에 진입한 대상을 그룹 전체에 전달합니다.
        /// </summary>
        /// <param name="encounterId">활성화할 Encounter 그룹 ID입니다.</param>
        /// <param name="target">그룹이 함께 교전할 대상입니다.</param>
        /// <param name="source">볼륨을 소유하거나 활성화를 시작한 멤버입니다.</param>
        /// <returns>Encounter Threat가 적용된 멤버 수입니다.</returns>
        public static int Activate(int encounterId, CharacterBase target, MonsterEncounterMember source = null)
        {
            if (encounterId <= 0 || target == null ||
                !MembersByEncounter.TryGetValue(encounterId, out List<MonsterEncounterMember> members))
            {
                return 0;
            }

            BuildSnapshot(members);
            int activatedCount = 0;

            // 활성화 볼륨의 소유자를 먼저 처리하여 첫 타겟 선택과 디버그 흐름을 안정적으로 유지합니다.
            if (source != null && Snapshot.Contains(source) && source.ReceiveEncounterActivation(target))
            {
                activatedCount++;
            }

            for (int i = 0; i < Snapshot.Count; i++)
            {
                MonsterEncounterMember member = Snapshot[i];
                if (member == source)
                {
                    continue;
                }

                if (member.ReceiveEncounterActivation(target))
                {
                    activatedCount++;
                }
            }

            Snapshot.Clear();
            return activatedCount;
        }

        /// <summary>
        /// Encounter 볼륨 이탈 정책이 활성화된 경우 그룹 전체의 Encounter Threat를 제거합니다.
        /// </summary>
        /// <param name="encounterId">비활성화할 Encounter 그룹 ID입니다.</param>
        /// <param name="target">Encounter Threat를 제거할 대상입니다.</param>
        /// <returns>Encounter Threat가 제거된 멤버 수입니다.</returns>
        public static int Deactivate(int encounterId, CharacterBase target)
        {
            if (encounterId <= 0 || target == null ||
                !MembersByEncounter.TryGetValue(encounterId, out List<MonsterEncounterMember> members))
            {
                return 0;
            }

            BuildSnapshot(members);
            int deactivatedCount = 0;
            for (int i = 0; i < Snapshot.Count; i++)
            {
                if (Snapshot[i].RemoveEncounterActivation(target))
                {
                    deactivatedCount++;
                }
            }

            Snapshot.Clear();
            return deactivatedCount;
        }

        /// <summary>
        /// 한 멤버가 새 전투 대상을 얻으면 같은 Encounter 그룹의 가까운 동료에게 지원 어그로를 전달합니다.
        /// </summary>
        /// <param name="source">지원 요청을 시작한 Encounter 멤버입니다.</param>
        /// <param name="target">동료가 함께 교전할 대상입니다.</param>
        /// <returns>지원 Encounter Threat가 적용된 동료 수입니다.</returns>
        public static int AlertAssistants(MonsterEncounterMember source, CharacterBase target)
        {
            if (_isDispatchingAssist || source == null || target == null || source.EncounterId <= 0 ||
                !MembersByEncounter.TryGetValue(source.EncounterId, out List<MonsterEncounterMember> members))
            {
                return 0;
            }

            _isDispatchingAssist = true;
            try
            {
                AssistCandidates.Clear();
                Vector3 sourcePosition = source.transform.position;
                float assistRadius = source.Profile.AssistRadius;
                float assistRadiusSqr = assistRadius > 0f ? assistRadius * assistRadius : float.PositiveInfinity;

                for (int i = 0; i < members.Count; i++)
                {
                    MonsterEncounterMember candidate = members[i];
                    if (candidate == null || candidate == source || !candidate.CanReceiveEncounterAlert)
                    {
                        continue;
                    }

                    if ((candidate.transform.position - sourcePosition).sqrMagnitude <= assistRadiusSqr)
                    {
                        AssistCandidates.Add(candidate);
                    }
                }

                AssistCandidates.Sort((left, right) =>
                {
                    float leftDistance = (left.transform.position - sourcePosition).sqrMagnitude;
                    float rightDistance = (right.transform.position - sourcePosition).sqrMagnitude;
                    return leftDistance.CompareTo(rightDistance);
                });

                int limit = source.Profile.MaxAssistCount > 0
                    ? Mathf.Min(source.Profile.MaxAssistCount, AssistCandidates.Count)
                    : AssistCandidates.Count;
                int activatedCount = 0;
                for (int i = 0; i < limit; i++)
                {
                    if (AssistCandidates[i].ReceiveEncounterActivation(target))
                    {
                        activatedCount++;
                    }
                }

                AssistCandidates.Clear();
                return activatedCount;
            }
            finally
            {
                _isDispatchingAssist = false;
            }
        }

        private static void BuildSnapshot(List<MonsterEncounterMember> members)
        {
            PruneInvalidMembers(members);
            Snapshot.Clear();
            Snapshot.AddRange(members);
        }

        private static void PruneInvalidMembers(List<MonsterEncounterMember> members)
        {
            for (int i = members.Count - 1; i >= 0; i--)
            {
                MonsterEncounterMember member = members[i];
                if (member == null || !member.CanRemainRegistered)
                {
                    members.RemoveAt(i);
                }
            }
        }
    }
}
