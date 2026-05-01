using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 클리어 기록과 월드맵 노드 활성 상태를 저장하는 Core 세이브 데이터입니다.
    /// </summary>
    public sealed class MapProgressData : DefaultData, ISaveData
    {
        /// <summary>
        /// 클리어한 실제 게임 맵 기록입니다. key는 TableMap의 mapUid입니다.
        /// </summary>
        public Dictionary<int, MapClearRecord> ClearedMaps = new Dictionary<int, MapClearRecord>();

        /// <summary>
        /// 활성화된 월드맵 노드 기록입니다. key는 월드맵 그래프의 nodeId입니다.
        /// </summary>
        public Dictionary<string, bool> ActivatedWorldMapNodes = new Dictionary<string, bool>();

        /// <summary>
        /// 저장 컨테이너에서 맵 진행 데이터를 복원합니다.
        /// </summary>
        /// <param name="loader">테이블 로더입니다. 현재 맵 진행 데이터는 테이블을 직접 참조하지 않습니다.</param>
        /// <param name="saveDataContainer">로드된 Core 저장 데이터 컨테이너입니다.</param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            ClearedMaps.Clear();
            ActivatedWorldMapNodes.Clear();

            MapProgressData loadedData = saveDataContainer?.MapProgressData;
            if (loadedData == null)
            {
                return;
            }

            RestoreClearedMaps(loadedData.ClearedMaps);
            RestoreActivatedWorldMapNodes(loadedData.ActivatedWorldMapNodes);
        }

        /// <summary>
        /// 지정한 실제 게임 맵을 클리어 기록으로 저장합니다.
        /// </summary>
        /// <param name="mapUid">클리어한 TableMap UID입니다.</param>
        /// <returns>새 기록이 추가되거나 기존 기록이 갱신되면 true를 반환합니다.</returns>
        public bool ClearMap(int mapUid)
        {
            if (mapUid <= 0)
            {
                return false;
            }

            bool changed = AddOrUpdateClearRecord(mapUid);
            if (changed)
            {
                SaveDatas();
            }

            return changed;
        }

        /// <summary>
        /// 지정한 실제 게임 맵이 클리어되었는지 확인합니다.
        /// </summary>
        /// <param name="mapUid">확인할 TableMap UID입니다.</param>
        /// <returns>클리어 기록이 있으면 true를 반환합니다.</returns>
        public bool IsMapCleared(int mapUid)
        {
            return mapUid > 0 &&
                   ClearedMaps != null &&
                   ClearedMaps.ContainsKey(mapUid);
        }

        /// <summary>
        /// 지정한 월드맵 노드를 활성 상태로 저장합니다.
        /// </summary>
        /// <param name="nodeId">활성화할 월드맵 노드 ID입니다.</param>
        /// <returns>새 활성 기록이 추가되면 true를 반환합니다.</returns>
        public bool ActivateWorldMapNode(string nodeId)
        {
            nodeId = NormalizeNodeId(nodeId);
            if (string.IsNullOrEmpty(nodeId))
            {
                return false;
            }

            if (ActivatedWorldMapNodes == null)
            {
                ActivatedWorldMapNodes = new Dictionary<string, bool>();
            }

            if (ActivatedWorldMapNodes.TryGetValue(nodeId, out bool activated) && activated)
            {
                return false;
            }

            ActivatedWorldMapNodes[nodeId] = true;
            SaveDatas();
            return true;
        }

        /// <summary>
        /// 지정한 월드맵 노드가 저장 데이터에서 활성화되었는지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 월드맵 노드 ID입니다.</param>
        /// <returns>활성 기록이 true이면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeActivated(string nodeId)
        {
            nodeId = NormalizeNodeId(nodeId);
            return !string.IsNullOrEmpty(nodeId) &&
                   ActivatedWorldMapNodes != null &&
                   ActivatedWorldMapNodes.TryGetValue(nodeId, out bool activated) &&
                   activated;
        }

        /// <summary>
        /// 저장된 클리어 기록을 검증 가능한 값만 선별해 복원합니다.
        /// </summary>
        /// <param name="records">저장 파일에서 읽은 맵 클리어 기록입니다.</param>
        private void RestoreClearedMaps(Dictionary<int, MapClearRecord> records)
        {
            if (records == null)
            {
                return;
            }

            foreach (var pair in records)
            {
                if (pair.Key <= 0 || pair.Value == null)
                {
                    continue;
                }

                MapClearRecord record = pair.Value;
                record.MapUid = pair.Key;
                record.ClearCount = Math.Max(1, record.ClearCount);
                ClearedMaps[pair.Key] = record;
            }
        }

        /// <summary>
        /// 저장된 월드맵 노드 활성 기록을 true 값만 선별해 복원합니다.
        /// </summary>
        /// <param name="nodes">저장 파일에서 읽은 월드맵 노드 활성 기록입니다.</param>
        private void RestoreActivatedWorldMapNodes(Dictionary<string, bool> nodes)
        {
            if (nodes == null)
            {
                return;
            }

            foreach (var pair in nodes)
            {
                string nodeId = NormalizeNodeId(pair.Key);
                if (string.IsNullOrEmpty(nodeId) || !pair.Value)
                {
                    continue;
                }

                ActivatedWorldMapNodes[nodeId] = true;
            }
        }

        /// <summary>
        /// 맵 클리어 기록을 새로 만들거나 기존 기록의 클리어 횟수를 증가시킵니다.
        /// </summary>
        /// <param name="mapUid">클리어한 TableMap UID입니다.</param>
        /// <returns>기록이 변경되면 true를 반환합니다.</returns>
        private bool AddOrUpdateClearRecord(int mapUid)
        {
            if (ClearedMaps == null)
            {
                ClearedMaps = new Dictionary<int, MapClearRecord>();
            }

            string clearedAtUtc = DateTime.UtcNow.ToString("o");
            if (!ClearedMaps.TryGetValue(mapUid, out MapClearRecord record) || record == null)
            {
                ClearedMaps[mapUid] = new MapClearRecord
                {
                    MapUid = mapUid,
                    FirstClearedAtUtc = clearedAtUtc,
                    LastClearedAtUtc = clearedAtUtc,
                    ClearCount = 1,
                };
                return true;
            }

            record.MapUid = mapUid;
            if (string.IsNullOrEmpty(record.FirstClearedAtUtc))
            {
                record.FirstClearedAtUtc = clearedAtUtc;
            }

            record.LastClearedAtUtc = clearedAtUtc;
            record.ClearCount = Math.Max(0, record.ClearCount) + 1;
            return true;
        }

        /// <summary>
        /// 월드맵 nodeId 비교에 사용할 수 있도록 앞뒤 공백을 제거합니다.
        /// </summary>
        /// <param name="nodeId">정규화할 월드맵 노드 ID입니다.</param>
        /// <returns>정규화된 월드맵 노드 ID입니다.</returns>
        private static string NormalizeNodeId(string nodeId)
        {
            return string.IsNullOrWhiteSpace(nodeId) ? null : nodeId.Trim();
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }

    /// <summary>
    /// 실제 게임 맵 하나의 클리어 이력을 저장하는 레코드입니다.
    /// </summary>
    public sealed class MapClearRecord
    {
        /// <summary>클리어한 TableMap UID입니다.</summary>
        public int MapUid;

        /// <summary>최초 클리어 시각입니다. UTC ISO-8601 문자열로 저장합니다.</summary>
        public string FirstClearedAtUtc;

        /// <summary>마지막 클리어 시각입니다. UTC ISO-8601 문자열로 저장합니다.</summary>
        public string LastClearedAtUtc;

        /// <summary>누적 클리어 횟수입니다.</summary>
        public int ClearCount;
    }
}
