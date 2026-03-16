using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯에 들어간 스킬 정보 관리
    /// </summary>
    public class QuickSlotSimulationData : ItemStorageData
    {
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public override void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            base.Initialize(loader, saveDataContainer);
            ItemCounts.Clear();
            if (saveDataContainer?.QuickSlotSimulationData != null)
            {
                ItemCounts = new Dictionary<int, SaveDataIcon>(saveDataContainer.QuickSlotSimulationData.ItemCounts);
            }
        }
        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowQuickSlotSimulation>(UIWindowConstants.WindowUid.QuickSlotSimulation)?.maxCountIcon ?? 0;
        }
    }
}