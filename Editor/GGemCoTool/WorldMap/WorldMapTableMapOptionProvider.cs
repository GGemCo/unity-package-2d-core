using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// TableMap 데이터를 검색 드롭다운 옵션으로 변환해 보관합니다.
    /// </summary>
    internal sealed class WorldMapTableMapOptionProvider
    {
        private readonly List<SearchableDropdownUtility.Option<int>> _options =
            new List<SearchableDropdownUtility.Option<int>>();

        /// <summary>최근 로드된 TableMap입니다.</summary>
        public TableMap TableMap { get; private set; }

        /// <summary>검색 드롭다운에 표시할 맵 옵션 목록입니다.</summary>
        public IReadOnlyList<SearchableDropdownUtility.Option<int>> Options => _options;

        /// <summary>
        /// TableMap을 다시 로드하고 검색 옵션 캐시를 재구성합니다.
        /// </summary>
        public void Reload()
        {
            TableMap = TableLoaderManager.LoadMapTable();
            RebuildOptions();
        }

        /// <summary>
        /// 지정한 맵 UID의 옵션 인덱스를 찾습니다.
        /// </summary>
        /// <param name="mapUid">찾을 TableMap UID입니다.</param>
        /// <returns>옵션 인덱스입니다. 없으면 -1입니다.</returns>
        public int FindIndexByUid(int mapUid)
        {
            if (mapUid <= 0)
            {
                return -1;
            }

            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i].Data == mapUid)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 맵 UID에 대응하는 표시 이름을 반환합니다.
        /// </summary>
        /// <param name="mapUid">TableMap UID입니다.</param>
        /// <returns>테이블 이름이 있으면 이름, 없으면 UID 문자열입니다.</returns>
        public string GetDisplayName(int mapUid)
        {
            StruckTableMap mapData = TableMap != null ? TableMap.GetDataByUid(mapUid) : null;
            if (mapData == null)
            {
                return "Map " + mapUid;
            }

            return string.IsNullOrWhiteSpace(mapData.Name) ? "Map " + mapUid : mapData.Name;
        }

        /// <summary>
        /// 현재 TableMap 기준으로 검색 옵션 목록을 다시 만듭니다.
        /// </summary>
        private void RebuildOptions()
        {
            _options.Clear();
            if (TableMap == null)
            {
                return;
            }

            Dictionary<int, StruckTableMap> mapDatas = TableMap.GetDatas();
            foreach (KeyValuePair<int, StruckTableMap> pair in mapDatas)
            {
                StruckTableMap info = pair.Value;
                if (info == null || info.Uid <= 0)
                {
                    continue;
                }

                _options.Add(new SearchableDropdownUtility.Option<int>(
                    info.Uid.ToString(),
                    info.Name,
                    info.Uid));
            }
        }
    }
}
