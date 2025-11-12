using System.Collections.Generic;

namespace GGemCo2DCore
{
    public interface ITableParser
    {
        string Key { get; }
        void LoadData(string content);
    }
    public interface ITableParser<TRow> : ITableParser where TRow : class
    {
        /// <summary>uid로 강타입 행을 가져옵니다. 없으면 null.</summary>
        TRow GetDataByUid(int uid);

        /// <summary>uid로 강타입 행을 시도합니다.</summary>
        bool TryGetDataByUid(int uid, out TRow row);

        /// <summary>전체 데이터를 읽기 전용으로 노출(선택)</summary>
        IReadOnlyDictionary<int, TRow> GetAll();
    }
}