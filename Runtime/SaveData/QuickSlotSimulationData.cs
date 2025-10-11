using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯에 들어간 스킬 정보 관리
    /// </summary>
    public class QuickSlotSimulationData : DefaultData, ISaveData
    {
        // public 으로 해야 json 으로 저장된다. 
        public Dictionary<int, SaveDataIcon> quickSlotSimulationDatas = new();
        
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            quickSlotSimulationDatas.Clear();
            if (saveDataContainer?.QuickSlotSimulationData != null)
            {
                quickSlotSimulationDatas = new Dictionary<int, SaveDataIcon>(saveDataContainer.QuickSlotSimulationData.quickSlotSimulationDatas);
            }
        }
        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowQuickSlotSimulation>(UIWindowConstants.WindowUid.QuickSlotSimulation)?.maxCountIcon ?? 0;
        }
        
        /// <summary>
        /// 스킬 설정
        /// </summary>
        public void SetSkill(int slotIndex, int skillUid, int skillCount, int level, bool skillLearn = false)
        {
            if (skillUid <= 0) return;

            quickSlotSimulationDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, level, skillLearn);
            SaveDatas();
        }
        /// <summary>
        /// 스킬 삭제
        /// </summary>
        public void RemoveSkill(int slotIndex)
        {
            if (!quickSlotSimulationDatas.ContainsKey(slotIndex)) return;
            quickSlotSimulationDatas[slotIndex] = new SaveDataIcon(slotIndex, 0);
            SaveDatas();
        }
        /// <summary>
        /// 스킬 추가.
        /// </summary>
        public ResultCommon AddSkill(int skillUid, int skillCount, int skillLevel, bool isLearn)
        {
            var info = TableLoaderManager.Instance.TableSkill.GetDataByUidLevel(skillUid, skillLevel);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail($"QuickSlot_NoSkillInfo");//스킬 정보가 없습니다.
            }

            bool exist = quickSlotSimulationDatas.Any(data => data.Value.Uid == skillUid);
            if (exist)
            {
                return ResultCommon.Fail($"QuickSlot_SkillAlreadyAssigned");//이미 등록된 스킬입니다.
            }
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1)
            {
                return ResultCommon.Fail("QuickSlot_NotEnoughSpace");//퀵슬롯에 공간이 부족합니다.
            }

            controls.Add(new SaveDataIcon(emptyIndex, skillUid, skillCount, skillLevel, isLearn));

            SaveDatas();
            return ResultCommon.SuccessWithIcons(controls);
        }

        public ResultCommon AddItem(int itemUid, int itemCount, int itemLevel)
        {
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail($"Slot_ItemNotFound");//아이템 정보가 없습니다.
            }

            bool exist = quickSlotSimulationDatas.Any(data => data.Value.Uid == itemUid);
            if (exist)
            {
                return ResultCommon.Fail($"QuickSlot_ItemAlreadyAssigned");//이미 등록된 아이템입니다.
            }
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1)
            {
                return ResultCommon.Fail("QuickSlot_NotEnoughSpace");//퀵슬롯에 공간이 부족합니다.
            }

            controls.Add(new SaveDataIcon(emptyIndex, itemUid, itemCount, itemLevel));

            SaveDatas();
            return ResultCommon.SuccessWithIcons(controls);
        }
        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (!quickSlotSimulationDatas.ContainsKey(i) || quickSlotSimulationDatas[i].Uid <= 0 || quickSlotSimulationDatas[i].Count <= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public Dictionary<int, SaveDataIcon> GetAllDatas()
        {
            return quickSlotSimulationDatas;
        }
    }
}