using System.Collections.Generic;
using System.Linq;

namespace GGemCo.Scripts
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
            if (saveDataContainer?.SkillData != null)
            {
                QuickSlotDatas = new Dictionary<int, SaveDataIcon>(saveDataContainer.QuickSlotData.QuickSlotDatas);
            }
        }
        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowQuickSlot>(UIWindowManager.WindowUid.QuickSlot)?.maxCountIcon ?? 0;
        }
        
        /// <summary>
        /// 스킬 설정
        /// </summary>
        public void SetSkill(int slotIndex, int skillUid, int skillCount, int level, bool skillLearn = false)
        {
            if (skillUid <= 0) return;

            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, level, skillLearn);
            SaveDatas();
        }
        /// <summary>
        /// 스킬 삭제
        /// </summary>
        public void RemoveSkill(int slotIndex)
        {
            if (!QuickSlotDatas.ContainsKey(slotIndex)) return;
            QuickSlotDatas[slotIndex] = new SaveDataIcon(slotIndex, 0, 0);
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
                return new ResultCommon(ResultCommon.Type.Fail, $"스킬 정보가 없습니다.");
            }

            bool exist = QuickSlotDatas.Any(data => data.Value.Uid == skillUid);
            if (exist)
            {
                return new ResultCommon(ResultCommon.Type.Fail, $"이미 등록된 스킬입니다.");
            }
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1)
            {
                return new ResultCommon(ResultCommon.Type.Fail, "퀵슬롯에 공간이 부족합니다.");
            }

            controls.Add(new SaveDataIcon(emptyIndex, skillUid, skillCount, skillLevel, isLearn));

            SaveDatas();
            return new ResultCommon(ResultCommon.Type.Success, "", controls);
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

        public Dictionary<int, SaveDataIcon> GetAllDatas()
        {
            return QuickSlotDatas;
        }
    }
}