using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 웨이브 시나리오의 런타임 진행 상태를 관리합니다.
    /// </summary>
    public sealed class MapWaveScenarioRuntime
    {
        private readonly List<MapWaveGroupData> _orderedGroups;
        private readonly Dictionary<int, MapWaveGroupData> _groupByUid;
        private readonly Dictionary<int, MapWaveGroupRuntime> _activeGroups =
            new Dictionary<int, MapWaveGroupRuntime>();

        /// <summary>
        /// 시나리오 정의 데이터입니다.
        /// </summary>
        public MapWaveScenarioData Data { get; }

        /// <summary>
        /// 현재 활성화된 그룹 인스턴스 수입니다.
        /// </summary>
        public int ActiveGroupCount => _activeGroups.Count;

        /// <summary>
        /// 다음 그룹 전환 대기 코루틴 수입니다.
        /// </summary>
        public int PendingTransitionCount { get; private set; }

        /// <summary>
        /// 시나리오가 종료되었는지 여부입니다.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 웨이브 시나리오 런타임을 생성합니다.
        /// </summary>
        /// <param name="data">시나리오 정의 데이터입니다.</param>
        public MapWaveScenarioRuntime(MapWaveScenarioData data)
        {
            Data = data;
            _orderedGroups = BuildOrderedGroups(data);
            _groupByUid = BuildGroupLookup(_orderedGroups);
        }

        /// <summary>
        /// 첫 번째 실행 그룹을 반환합니다.
        /// </summary>
        /// <returns>시작 그룹 데이터입니다. 실행할 그룹이 없으면 null입니다.</returns>
        public MapWaveGroupData GetFirstGroup()
        {
            return _orderedGroups.Count > 0 ? _orderedGroups[0] : null;
        }

        /// <summary>
        /// 현재 그룹 다음에 실행할 그룹을 반환합니다.
        /// </summary>
        /// <param name="currentGroup">현재 실행 기준 그룹입니다.</param>
        /// <returns>다음 그룹 데이터입니다. 더 이상 실행할 그룹이 없으면 null입니다.</returns>
        public MapWaveGroupData GetNextGroup(MapWaveGroupData currentGroup)
        {
            if (currentGroup == null)
            {
                return null;
            }

            if (currentGroup.NextGroupUid > 0 &&
                _groupByUid.TryGetValue(currentGroup.NextGroupUid, out MapWaveGroupData explicitNext))
            {
                return explicitNext;
            }

            int currentIndex = _orderedGroups.IndexOf(currentGroup);
            int nextIndex = currentIndex + 1;
            return nextIndex >= 0 && nextIndex < _orderedGroups.Count
                ? _orderedGroups[nextIndex]
                : null;
        }

        /// <summary>
        /// 그룹 인스턴스를 활성 그룹 목록에 등록합니다.
        /// </summary>
        /// <param name="groupRuntime">등록할 그룹 런타임입니다.</param>
        public void AddActiveGroup(MapWaveGroupRuntime groupRuntime)
        {
            if (groupRuntime == null)
            {
                return;
            }

            _activeGroups[groupRuntime.InstanceId] = groupRuntime;
        }

        /// <summary>
        /// 그룹 인스턴스 ID로 활성 그룹을 조회합니다.
        /// </summary>
        /// <param name="instanceId">그룹 런타임 인스턴스 ID입니다.</param>
        /// <param name="groupRuntime">조회된 그룹 런타임입니다.</param>
        /// <returns>활성 그룹을 찾으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGetActiveGroup(int instanceId, out MapWaveGroupRuntime groupRuntime)
        {
            return _activeGroups.TryGetValue(instanceId, out groupRuntime);
        }

        /// <summary>
        /// 활성 그룹 목록에서 지정 그룹을 제거합니다.
        /// </summary>
        /// <param name="instanceId">제거할 그룹 런타임 인스턴스 ID입니다.</param>
        public void RemoveActiveGroup(int instanceId)
        {
            _activeGroups.Remove(instanceId);
        }

        /// <summary>
        /// 다음 그룹 전환 대기 수를 증가시킵니다.
        /// </summary>
        public void IncrementPendingTransition()
        {
            PendingTransitionCount++;
        }

        /// <summary>
        /// 다음 그룹 전환 대기 수를 감소시킵니다.
        /// </summary>
        public void DecrementPendingTransition()
        {
            if (PendingTransitionCount > 0)
            {
                PendingTransitionCount--;
            }
        }

        /// <summary>
        /// 시나리오를 완료 상태로 전환합니다.
        /// </summary>
        public void MarkCompleted()
        {
            IsCompleted = true;
        }

        /// <summary>
        /// 그룹 목록을 Order 기준으로 정렬합니다.
        /// </summary>
        /// <param name="scenarioData">정렬할 시나리오 데이터입니다.</param>
        /// <returns>실행 순서로 정렬된 그룹 목록입니다.</returns>
        private static List<MapWaveGroupData> BuildOrderedGroups(MapWaveScenarioData scenarioData)
        {
            List<MapWaveGroupData> result = scenarioData?.Groups != null
                ? new List<MapWaveGroupData>(scenarioData.Groups)
                : new List<MapWaveGroupData>();

            result.Sort(CompareGroups);
            return result;
        }

        /// <summary>
        /// 그룹 UID 조회 캐시를 생성합니다.
        /// </summary>
        /// <param name="groups">정렬된 그룹 목록입니다.</param>
        /// <returns>그룹 UID 기준 조회 캐시입니다.</returns>
        private static Dictionary<int, MapWaveGroupData> BuildGroupLookup(List<MapWaveGroupData> groups)
        {
            Dictionary<int, MapWaveGroupData> result = new Dictionary<int, MapWaveGroupData>();
            if (groups == null)
            {
                return result;
            }

            foreach (MapWaveGroupData group in groups)
            {
                if (group == null || group.GroupUid <= 0)
                {
                    continue;
                }

                result[group.GroupUid] = group;
            }

            return result;
        }

        /// <summary>
        /// 웨이브 그룹 실행 순서를 비교합니다.
        /// </summary>
        /// <param name="left">왼쪽 그룹 데이터입니다.</param>
        /// <param name="right">오른쪽 그룹 데이터입니다.</param>
        /// <returns>정렬 비교 결과입니다.</returns>
        private static int CompareGroups(MapWaveGroupData left, MapWaveGroupData right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int orderCompare = left.Order.CompareTo(right.Order);
            return orderCompare != 0
                ? orderCompare
                : left.GroupUid.CompareTo(right.GroupUid);
        }
    }
}
