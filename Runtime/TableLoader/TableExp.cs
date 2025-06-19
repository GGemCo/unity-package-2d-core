using System.Linq;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 경험치 테이블 
    /// </summary>
    public class TableExp : DefaultTable
    {
        public long GetNeedExp(int level) => long.TryParse(GetDataColumn(level, "NeedExp"), out var v) ? v : 0;

        /// <summary>
        /// 마지막 레벨 가져오기 
        /// </summary>
        /// <returns></returns>
        public int GetLastLevel()
        {
            var datas = GetDatas();
            if (datas == null || datas.Count == 0)
                return -1; // 데이터가 없을 경우 예외 처리

            int maxUid = datas.Keys.Max(); // 가장 큰 Uid 찾기
            if (!datas[maxUid].TryGetValue("Level", out string levelStr)) return -1; // 변환 실패 시 예외 처리
            if (int.TryParse(levelStr, out int level))
                return level;

            return -1; // 변환 실패 시 예외 처리
        }
    }
}