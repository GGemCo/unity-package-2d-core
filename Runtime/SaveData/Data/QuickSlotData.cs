using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯에 들어간 스킬 정보 관리
    /// </summary>
    public class QuickSlotData : DefaultData, ISaveData
    {
        // public 으로 해야 json 으로 저장된다. 
        public Dictionary<int, SaveDataIcon> QuickSlotDatas = new();
        
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            QuickSlotDatas.Clear();
            if (saveDataContainer?.QuickSlotData != null)
            {
                QuickSlotDatas = new Dictionary<int, SaveDataIcon>(saveDataContainer.QuickSlotData.QuickSlotDatas);
            }
        }
        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowQuickSlot>(UIWindowConstants.WindowUid.QuickSlot)?.maxCountIcon ?? 0;
        }
        
        /// <summary>
        /// 스킬 설정
        /// </summary>
        public void SetSkill(int slotIndex, int skillUid, int skillCount, int level, bool skillLearn = false)
        {
            if (skillUid <= 0) return;

            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, level, skillLearn, 0, (int)QuickSlotContentKind.Skill);
            SaveDatas();
        }
        
        /// <summary>
        /// 아이템 설정
        /// </summary>
        public void SetItem(int slotIndex, int itemUid, int itemCount, long instanceId = 0)
        {
            if (itemUid <= 0) return;
            if (itemCount <= 0)
            {
                Remove(slotIndex);
                return;
            }

            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, itemUid, itemCount, 0, false, instanceId, (int)QuickSlotContentKind.Item);
            SaveDatas();
        }

        /// <summary>
        /// 스킬 삭제
        /// </summary>
        public void Remove(int slotIndex)
        {
            if (!QuickSlotDatas.ContainsKey(slotIndex)) return;
            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, 0);
            SaveDatas();
        }
        /// <summary>
        /// 스킬 삭제(호환용)
        /// </summary>
        public void RemoveSkill(int slotIndex)
        {
            Remove(slotIndex);
        }
        /// <summary>
        /// 스킬 추가.
        /// </summary>
        public ResultCommon AddSkill(int skillUid, int skillCount, int skillLevel, bool isLearn)
        {
            // todo. 정리 필요
            return ResultCommon.Fail($"QuickSlot_NoSkillInfo");//스킬 정보가 없습니다.
        }
        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        private int FindEmptySlot()
        {
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (!QuickSlotDatas.ContainsKey(i) || QuickSlotDatas[i].Uid <= 0 || QuickSlotDatas[i].Count <= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        
        public bool TryGetEntry(int slotIndex, out SaveDataIcon entry)
        {
            if (QuickSlotDatas != null && QuickSlotDatas.TryGetValue(slotIndex, out entry))
                return true;

            entry = null;
            return false;
        }

        public Dictionary<int, SaveDataIcon> GetAllDatas()
        {
            return QuickSlotDatas;
        }
    }
}