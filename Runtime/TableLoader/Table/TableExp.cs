using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 경험치 테이블 Structure
    /// </summary>
    public class StruckTableExp
    {
        public int Uid;
        public int Level;
        public long NeedExp;
        public long NeedStatPointGold;
    }	

    /// <summary>
    /// 경험치 테이블 
    /// </summary>
    public class TableExp : DefaultTable<StruckTableExp>
    {
        public override string Key => ConfigAddressableTable.Exp;
        private readonly Dictionary<int, long> _dataLevel = new Dictionary<int, long>();
        private readonly Dictionary<int, long> _dataNeedStatPointGoldByLevel = new Dictionary<int, long>();

        protected override void OnLoadedData(StruckTableExp data)
        {
            _dataLevel[data.Level] = data.NeedExp;
            _dataNeedStatPointGoldByLevel[data.Level] = data.NeedStatPointGold;
        }
        
        public long GetNeedExp(int level)
        {
            return _dataLevel.GetValueOrDefault(level, 0);
        }

        /// <summary>
        /// 특정 레벨 달성에 필요한 스탯 포인트 투자 골드 비용을 반환합니다.
        /// - 예) 현재 레벨이 10이고, 포인트 1개 투자 시 레벨 11이 된다면 level=11 비용을 조회합니다.
        /// - 값이 없으면 0을 반환하며, 상위 로직에서 fallback 정책을 사용할 수 있습니다.
        /// </summary>
        public long GetNeedStatPointGold(int level)
        {
            return _dataNeedStatPointGoldByLevel.GetValueOrDefault(level, 0);
        }

        /// <summary>
        /// 마지막 레벨 가져오기 
        /// </summary>
        public int GetLastLevel()
        {
            var datas = GetDatas();
            if (datas == null || datas.Count == 0)
                return -1; // 데이터가 없을 경우 예외 처리
        
            return _dataLevel.Keys.Max(); // 가장 큰 Level 찾기
        }

        protected override StruckTableExp BuildRow(Dictionary<string, string> data)
        {
            data.TryGetValue("Uid", out string uid);
            data.TryGetValue("Level", out string level);
            data.TryGetValue("NeedExp", out string needExp);
            data.TryGetValue("NeedStatPointGold", out string needStatPointGold);

            return new StruckTableExp
            {
                Uid = MathHelper.ParseInt(uid),
                Level = MathHelper.ParseInt(level),
                NeedExp = MathHelper.ParseLong(needExp),
                NeedStatPointGold = MathHelper.ParseLong(needStatPointGold),
            };
        }
    }
}
