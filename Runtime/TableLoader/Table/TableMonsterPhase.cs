using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 페이즈 테이블의 1행 데이터입니다.
    /// </summary>
    public sealed class StruckTableMonsterPhase
    {
        /// <summary>행 UID입니다.</summary>
        public int Uid;
        /// <summary>대상 몬스터 UID입니다.</summary>
        public int MonsterUid;
        /// <summary>페이즈 순번(1부터 시작)입니다.</summary>
        public int PhaseIndex;
        /// <summary>디자이너 메모입니다.</summary>
        public string Memo;
        /// <summary>해당 페이즈에서 사용할 BT 파일명입니다.</summary>
        public string BtFileName;
        /// <summary>해당 페이즈 시작 HP 비율(0~1)입니다. EndHpFixed가 0 이하일 때 사용합니다.</summary>
        public float EndHpPercent;
        /// <summary>해당 페이즈 시작 고정 HP입니다. 0 이하면 EndHpPercent 정책을 사용합니다.</summary>
        public int EndHpFixed;
        /// <summary>해당 페이즈가 종료될 때 시작할 전환 컷신 UID입니다. 0이면 미사용입니다.</summary>
        public int TransitionCutsceneUid;
        /// <summary>해당 페이즈가 시작될 때 재생할 컷신 UID입니다. 0이면 미사용입니다.</summary>
        public int PhaseStartCutsceneUid;
        /// <summary>BT 교체 시 상태 보존 모드 문자열입니다.</summary>
        public string TreeSwitchMode;
    }

    /// <summary>
    /// 몬스터 페이즈 테이블입니다.
    /// </summary>
    public sealed class TableMonsterPhase : DefaultTable<StruckTableMonsterPhase>
    {
        /// <summary>
        /// 몬스터 UID별 페이즈 목록 인덱스입니다.
        /// </summary>
        private readonly Dictionary<int, List<StruckTableMonsterPhase>> _phaseByMonsterUid =
            new Dictionary<int, List<StruckTableMonsterPhase>>();

        /// <inheritdoc />
        public override string Key => ConfigAddressableTable.MonsterPhase;

        /// <summary>
        /// 로드 시작 전에 보조 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _phaseByMonsterUid.Clear();
        }

        /// <summary>
        /// 로드된 행을 몬스터 UID 인덱스에 누적합니다.
        /// </summary>
        /// <param name="data">방금 로드된 행 데이터입니다.</param>
        protected override void OnLoadedData(StruckTableMonsterPhase data)
        {
            if (data == null || data.MonsterUid <= 0)
                return;

            if (!_phaseByMonsterUid.TryGetValue(data.MonsterUid, out List<StruckTableMonsterPhase> list))
            {
                list = new List<StruckTableMonsterPhase>();
                _phaseByMonsterUid[data.MonsterUid] = list;
            }

            list.Add(data);
            list.Sort((a, b) => a.PhaseIndex.CompareTo(b.PhaseIndex));
        }

        /// <summary>
        /// 헤더/값 사전으로부터 몬스터 페이즈 행을 생성합니다.
        /// </summary>
        /// <param name="data">헤더명 기반 원시 값 사전입니다.</param>
        /// <returns>파싱된 페이즈 행입니다.</returns>
        protected override StruckTableMonsterPhase BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableMonsterPhase
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                MonsterUid = MathHelper.ParseInt(data["MonsterUid"]),
                PhaseIndex = MathHelper.ParseInt(data["PhaseIndex"]),
                Memo = data.TryGetValue("Memo", out string memo) ? memo : string.Empty,
                BtFileName = data.TryGetValue("BtFileName", out string btFileName) ? btFileName : string.Empty,
                EndHpPercent = data.TryGetValue("EndHpPercent", out string endHpPercent)
                    ? MathHelper.ParseFloat(endHpPercent)
                    : 0f,
                EndHpFixed = data.TryGetValue("EndHpFixed", out string endHpFixed)
                    ? MathHelper.ParseInt(endHpFixed)
                    : 0,
                TransitionCutsceneUid = ParseOptionalCutsceneUid(data, "TransitionCutsceneUid"),
                PhaseStartCutsceneUid = ParseOptionalCutsceneUid(data, "PhaseStartCutsceneUid"),
                TreeSwitchMode = data.TryGetValue("TreeSwitchMode", out string treeSwitchMode)
                    ? treeSwitchMode
                    : string.Empty,
            };
        }

        /// <summary>
        /// 선택 컷신 UID 컬럼을 정수로 파싱합니다.
        /// </summary>
        /// <param name="data">헤더명 기반 원시 값 사전입니다.</param>
        /// <param name="headerName">조회할 컷신 UID 컬럼명입니다.</param>
        /// <returns>컬럼이 없거나 파싱에 실패하면 0을 반환합니다.</returns>
        private static int ParseOptionalCutsceneUid(IReadOnlyDictionary<string, string> data, string headerName)
        {
            if (data == null || string.IsNullOrWhiteSpace(headerName))
                return 0;

            return data.TryGetValue(headerName, out string rawUid)
                ? MathHelper.ParseInt(rawUid)
                : 0;
        }

        /// <summary>
        /// 몬스터 UID로 페이즈 목록을 조회합니다.
        /// </summary>
        /// <param name="monsterUid">조회할 몬스터 UID입니다.</param>
        /// <returns>해당 몬스터의 페이즈 목록입니다. 없으면 빈 목록입니다.</returns>
        public IReadOnlyList<StruckTableMonsterPhase> GetDataByMonsterUid(int monsterUid)
        {
            return _phaseByMonsterUid.TryGetValue(monsterUid, out List<StruckTableMonsterPhase> list)
                ? list
                : System.Array.Empty<StruckTableMonsterPhase>();
        }

        /// <summary>
        /// 몬스터 UID로 페이즈 목록 조회를 시도합니다.
        /// </summary>
        /// <param name="monsterUid">조회할 몬스터 UID입니다.</param>
        /// <param name="rows">조회된 페이즈 목록입니다.</param>
        /// <returns>조회 성공 시 true를 반환합니다.</returns>
        public bool TryGetDataByMonsterUid(int monsterUid, out IReadOnlyList<StruckTableMonsterPhase> rows)
        {
            if (_phaseByMonsterUid.TryGetValue(monsterUid, out List<StruckTableMonsterPhase> list))
            {
                rows = list;
                return true;
            }

            rows = System.Array.Empty<StruckTableMonsterPhase>();
            return false;
        }
    }
}
