using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 클리어, 월드맵 노드 표시, 월드맵 노드 활성 처리를 담당하는 Core 진행 컨트롤러입니다.
    /// </summary>
    public sealed class MapProgressController
    {
        private readonly SaveDataManager _saveDataManager;

        /// <summary>
        /// 저장 매니저를 기준으로 맵 진행 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="saveDataManager">Core 저장 매니저입니다.</param>
        public MapProgressController(SaveDataManager saveDataManager)
        {
            _saveDataManager = saveDataManager;
        }

        /// <summary>
        /// 실제 게임 맵 클리어를 기록하고, 함께 전달된 월드맵 노드를 표시 또는 활성화합니다.
        /// </summary>
        /// <param name="mapUid">클리어한 TableMap UID입니다.</param>
        /// <param name="activateWorldMapNodeIds">클리어 보상으로 활성화할 월드맵 nodeId 목록입니다.</param>
        /// <param name="visibleWorldMapNodeIds">표시만 켤 월드맵 nodeId 목록입니다.</param>
        /// <returns>맵 클리어, 노드 표시, 노드 활성 상태 중 하나라도 변경되면 true를 반환합니다.</returns>
        public bool ClearMap(
            int mapUid,
            IEnumerable<string> activateWorldMapNodeIds = null,
            IEnumerable<string> visibleWorldMapNodeIds = null)
        {
            MapProgressData progressData = GetProgressData();
            if (progressData == null)
            {
                return false;
            }

            bool changed = progressData.ClearMap(mapUid);
            if (activateWorldMapNodeIds != null)
            {
                foreach (string nodeId in activateWorldMapNodeIds)
                {
                    changed |= progressData.ActivateWorldMapNode(nodeId);
                }
            }

            if (visibleWorldMapNodeIds != null)
            {
                foreach (string nodeId in visibleWorldMapNodeIds)
                {
                    changed |= progressData.SetWorldMapNodeVisible(nodeId);
                }
            }

            RefreshWorldMapWindow();
            return changed;
        }

        /// <summary>
        /// 실제 게임 맵 목록을 클리어로 기록하고, 함께 전달된 월드맵 노드를 표시 또는 활성화합니다.
        /// </summary>
        /// <param name="mapUids">클리어한 TableMap UID 목록입니다.</param>
        /// <param name="activateWorldMapNodeIds">클리어 보상으로 활성화할 월드맵 nodeId 목록입니다.</param>
        /// <param name="visibleWorldMapNodeIds">표시만 켤 월드맵 nodeId 목록입니다.</param>
        /// <returns>맵 클리어, 노드 표시, 노드 활성 상태 중 하나라도 변경되면 true를 반환합니다.</returns>
        public bool ClearMaps(
            IEnumerable<int> mapUids,
            IEnumerable<string> activateWorldMapNodeIds = null,
            IEnumerable<string> visibleWorldMapNodeIds = null)
        {
            MapProgressData progressData = GetProgressData();
            if (progressData == null)
            {
                return false;
            }

            bool changed = progressData.ClearMaps(mapUids);
            if (activateWorldMapNodeIds != null)
            {
                foreach (string nodeId in activateWorldMapNodeIds)
                {
                    changed |= progressData.ActivateWorldMapNode(nodeId);
                }
            }

            if (visibleWorldMapNodeIds != null)
            {
                foreach (string nodeId in visibleWorldMapNodeIds)
                {
                    changed |= progressData.SetWorldMapNodeVisible(nodeId);
                }
            }

            RefreshWorldMapWindow();
            return changed;
        }

        /// <summary>
        /// 지정한 실제 게임 맵이 클리어되었는지 확인합니다.
        /// </summary>
        /// <param name="mapUid">확인할 TableMap UID입니다.</param>
        /// <returns>클리어 기록이 있으면 true를 반환합니다.</returns>
        public bool IsMapCleared(int mapUid)
        {
            MapProgressData progressData = GetProgressData();
            return progressData != null && progressData.IsMapCleared(mapUid);
        }

        /// <summary>
        /// 지정한 월드맵 노드를 활성화하고 월드맵 UI가 열려 있으면 상태를 갱신합니다.
        /// </summary>
        /// <param name="nodeId">활성화할 월드맵 nodeId입니다.</param>
        /// <returns>새 활성 기록이 추가되면 true를 반환합니다.</returns>
        public bool ActivateWorldMapNode(string nodeId)
        {
            MapProgressData progressData = GetProgressData();
            if (progressData == null)
            {
                return false;
            }

            bool changed = progressData.ActivateWorldMapNode(nodeId);
            if (changed)
            {
                RefreshWorldMapWindow();
            }

            return changed;
        }

        /// <summary>
        /// 지정한 월드맵 노드를 표시 상태로 저장하고, 월드맵 UI가 열려 있으면 즉시 갱신합니다.
        /// 활성화 상태는 변경하지 않으므로 비활성 노드는 비활성 표시를 유지합니다.
        /// </summary>
        /// <param name="nodeId">표시할 월드맵 nodeId입니다.</param>
        /// <returns>표시 기록이 새로 추가되면 true를 반환합니다.</returns>
        public bool SetWorldMapNodeVisible(string nodeId)
        {
            MapProgressData progressData = GetProgressData();
            if (progressData == null)
            {
                return false;
            }

            bool changed = progressData.SetWorldMapNodeVisible(nodeId);
            if (changed)
            {
                RefreshWorldMapWindow();
            }

            return changed;
        }

        /// <summary>
        /// 지정한 월드맵 노드가 저장 데이터에서 활성화되었는지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 월드맵 nodeId입니다.</param>
        /// <returns>활성 기록이 있으면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeActivated(string nodeId)
        {
            MapProgressData progressData = GetProgressData();
            return progressData != null && progressData.IsWorldMapNodeActivated(nodeId);
        }

        /// <summary>
        /// 지정한 월드맵 노드가 저장 데이터에서 표시 상태인지 확인합니다.
        /// </summary>
        /// <param name="nodeId">확인할 월드맵 nodeId입니다.</param>
        /// <returns>표시 기록이 저장되어 있으면 true를 반환합니다.</returns>
        public bool IsWorldMapNodeVisible(string nodeId)
        {
            MapProgressData progressData = GetProgressData();
            return progressData != null && progressData.IsWorldMapNodeVisible(nodeId);
        }

        /// <summary>
        /// Core 저장 매니저에서 맵 진행 저장 데이터를 가져옵니다.
        /// </summary>
        /// <returns>맵 진행 저장 데이터입니다.</returns>
        private MapProgressData GetProgressData()
        {
            return _saveDataManager != null ? _saveDataManager.MapProgress : null;
        }

        /// <summary>
        /// 월드맵 창이 생성되어 있으면 저장된 진행 상태를 UI에 즉시 반영합니다.
        /// </summary>
        private static void RefreshWorldMapWindow()
        {
            UIWindowWorldMap worldMapWindow = SceneGame.Instance?.uIWindowManager
                ?.GetUIWindowByUid<UIWindowWorldMap>(UIWindowConstants.WindowUid.WorldMap);
            worldMapWindow?.RefreshWorldMapProgressStates();
        }
    }
}
