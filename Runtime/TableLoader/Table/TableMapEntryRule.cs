using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 입장 요청을 다른 맵으로 라우팅하기 위한 테이블 행입니다.
    /// </summary>
    public class StruckTableMapEntryRule : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public bool Enabled;
        public int Priority;
        public int RequestMapUid;
        public int ConditionLicenseUid;
        public MapEntryRuleConstants.CompareType CompareType;
        public string CompareValue;
        public int TargetMapUid;
        public string Memo;
    }

    /// <summary>
    /// map_entry_rule 테이블을 로드하고 요청 맵 기준으로 규칙을 조회합니다.
    /// </summary>
    public class TableMapEntryRule : DefaultTable<StruckTableMapEntryRule>
    {
        private readonly Dictionary<int, List<StruckTableMapEntryRule>> _rulesByRequestMapUid =
            new Dictionary<int, List<StruckTableMapEntryRule>>();

        public override string Key => ConfigAddressableTable.MapEntryRule;

        /// <summary>
        /// 테이블 재로드 전에 요청 맵 기준 규칙 캐시를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _rulesByRequestMapUid.Clear();
        }

        /// <summary>
        /// 로드된 규칙을 요청 맵 UID 기준 캐시에 등록합니다.
        /// </summary>
        /// <param name="row">로드가 완료된 맵 입장 규칙 행입니다.</param>
        protected override void OnLoadedData(StruckTableMapEntryRule row)
        {
            if (row == null || !row.Enabled || row.RequestMapUid <= 0 || row.TargetMapUid <= 0)
            {
                return;
            }

            if (!_rulesByRequestMapUid.TryGetValue(row.RequestMapUid, out List<StruckTableMapEntryRule> rules))
            {
                rules = new List<StruckTableMapEntryRule>();
                _rulesByRequestMapUid[row.RequestMapUid] = rules;
            }

            rules.Add(row);
            rules.Sort(CompareRulePriority);
        }

        /// <summary>
        /// 테이블 행 데이터를 강타입 맵 입장 규칙으로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명과 값을 담은 테이블 행 사전입니다.</param>
        /// <returns>변환된 맵 입장 규칙 행입니다.</returns>
        protected override StruckTableMapEntryRule BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableMapEntryRule
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                Enabled = reader.BoolYN("Enabled"),
                Priority = reader.Int("Priority"),
                RequestMapUid = reader.Int("RequestMapUid"),
                ConditionLicenseUid = reader.Int("ConditionLicenseUid"),
                CompareType = reader.Enum<MapEntryRuleConstants.CompareType>("CompareType"),
                CompareValue = reader.String("CompareValue"),
                TargetMapUid = reader.Int("TargetMapUid"),
                Memo = reader.String("Memo"),
            };
        }

        /// <summary>
        /// 요청 맵 UID에 해당하는 활성 규칙 목록을 반환합니다.
        /// </summary>
        /// <param name="requestMapUid">플레이어가 원래 입장하려던 맵 UID입니다.</param>
        /// <returns>우선순위 순서로 정렬된 규칙 목록입니다.</returns>
        public IReadOnlyList<StruckTableMapEntryRule> GetRulesByRequestMapUid(int requestMapUid)
        {
            return requestMapUid > 0 && _rulesByRequestMapUid.TryGetValue(requestMapUid, out List<StruckTableMapEntryRule> rules)
                ? rules
                : Array.Empty<StruckTableMapEntryRule>();
        }

        /// <summary>
        /// 낮은 Priority가 먼저 오도록 규칙 정렬 순서를 계산합니다.
        /// </summary>
        /// <param name="left">왼쪽 비교 대상 규칙입니다.</param>
        /// <param name="right">오른쪽 비교 대상 규칙입니다.</param>
        /// <returns>정렬 비교 결과입니다.</returns>
        private static int CompareRulePriority(StruckTableMapEntryRule left, StruckTableMapEntryRule right)
        {
            int priorityCompare = left.Priority.CompareTo(right.Priority);
            return priorityCompare != 0 ? priorityCompare : left.Uid.CompareTo(right.Uid);
        }

    }
}
