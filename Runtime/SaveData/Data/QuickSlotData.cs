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
        
        public void SetIcon(int slotIndex, IconConstants.Type getIconType, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            if (getIconType == IconConstants.Type.Skill)
            {
                SetSkill(slotIndex, iconUid, iconCount, iconLevel, isLearned);
            }
            else if (getIconType == IconConstants.Type.SkillPassive)
            {
                SetSkillPassive(slotIndex, iconUid, iconCount, iconLevel, isLearned);
            }
            else if (getIconType == IconConstants.Type.Item)
            {
                SetItem(slotIndex, iconUid, iconCount);
            }
        }
        /// <summary>
        /// 스킬 설정
        /// </summary>
        private void SetSkill(int slotIndex, int skillUid, int skillCount, int level, bool skillLearn = false)
        {
            if (skillUid <= 0) return;

            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, level, skillLearn, 0, (int)IconConstants.Type.Skill);
            SaveDatas();
        }
        private bool SetSkillPassive(int slotIndex, int skillUid, int skillCount, int level, bool skillLearn = false)
        {
            if (skillUid <= 0) return false;
            
            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, level, skillLearn, 0, (int)IconConstants.Type.SkillPassive);
            SaveDatas();
            return true;
        }
        
        /// <summary>
        /// 아이템 설정
        /// </summary>
        private void SetItem(int slotIndex, int itemUid, int itemCount, long instanceId = 0)
        {
            if (itemUid <= 0) return;
            if (itemCount <= 0)
            {
                Remove(slotIndex);
                return;
            }

            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, itemUid, itemCount, 0, false, instanceId, (int)IconConstants.Type.Item);
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

        private bool GetSkill(int iconUid)
        {
            return QuickSlotDatas.Any(data => data.Value.Uid == iconUid && data.Value.IconType == (int)IconConstants.Type.Skill);
        }
        private bool GetSkillPassive(int iconUid)
        {
            return QuickSlotDatas.Any(data => data.Value.Uid == iconUid && data.Value.IconType == (int)IconConstants.Type.SkillPassive);
        }


        public int CheckSkill(int iconUid)
        {
            if (!GetSkill(iconUid)) return -1;
            int slotIndex = QuickSlotDatas.FirstOrDefault(data => data.Value.Uid == iconUid && data.Value.IconType == (int)IconConstants.Type.Skill).Key;
            Remove(slotIndex);
            return slotIndex;
        }
        
        public int CheckSkillPassive(int iconUid)
        {
            if (!GetSkillPassive(iconUid)) return -1;
            int slotIndex = QuickSlotDatas.FirstOrDefault(data => data.Value.Uid == iconUid && data.Value.IconType == (int)IconConstants.Type.SkillPassive).Key;
            Remove(slotIndex);
            return slotIndex;
        }

        public Dictionary<int, int> GetAllSkillPassive()
        {
            return QuickSlotDatas.Where(data => data.Value.IconType == (int)IconConstants.Type.SkillPassive).ToDictionary(data => data.Value.Uid, data => data.Value.Level);
        }
    }
}