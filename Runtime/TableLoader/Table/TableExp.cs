using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 테이블 Structure
    /// </summary>
    public class StruckTableExp
    {
        public int Uid;
        public int Level;
        public long NeedExp;
    }	

    /// <summary>
    /// 경험치 테이블 
    /// </summary>
    public class TableExp : DefaultTable<StruckTableExp>
    {
        public override string Key => ConfigAddressableTable.Exp;
        private readonly Dictionary<int, long> _dataLevel = new Dictionary<int, long>();

        protected override void OnLoadedData(StruckTableExp data)
        {
            _dataLevel[data.Level] = data.NeedExp;
        }
        
        public long GetNeedExp(int level)
        {
            return _dataLevel.GetValueOrDefault(level, 0);
        }

        /// <summary>
        /// 마지막 레벨 가져오기 
        /// </summary>
        /// <returns></returns>
        public int GetLastLevel()
        {
            var datas = GetDatas();
            if (datas == null || datas.Count == 0)
                return -1; // 데이터가 없을 경우 예외 처리
        
            return _dataLevel.Keys.Max(); // 가장 큰 Level 찾기
        }

        protected override StruckTableExp BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableExp
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Level = MathHelper.ParseInt(data["Level"]),
                NeedExp = MathHelper.ParseInt(data["NeedExp"]),
            };
        }
    }
}