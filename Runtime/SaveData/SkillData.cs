using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 스킬 UI 에 스킬별 레벨 정보 저장
    /// </summary>
    public class SkillData : DefaultData, ISaveData
    {
        // public 으로 해야 json 으로 저장된다. 
        public Dictionary<int, SaveDataIcon> SkillDatas = new();
        
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            SkillDatas.Clear();
            if (saveDataContainer?.SkillData != null)
            {
                SkillDatas = new Dictionary<int, SaveDataIcon>(saveDataContainer.SkillData.SkillDatas);
            }
        }
        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowSkill>(UIWindowManager.WindowUid.Skill)?.maxCountIcon ?? 0;
        }
        /// <summary>
        /// 스킬 레벨업
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="skillUid"></param>
        /// <param name="skillCount"></param>
        /// <param name="skillLevel"></param>
        /// <param name="skillLearn"></param>
        public ResultCommon SetSkillLevelUp(int slotIndex, int skillUid, int skillCount, int skillLevel, bool skillLearn)
        {
            if (skillUid <= 0)
            {
                return new ResultCommon(ResultCommon.Type.Fail, $"스킬 정보가 없습니다.");
            }

            if (!SkillDatas.ContainsKey(slotIndex))
            {
                return new ResultCommon(ResultCommon.Type.Fail, $"아직 스킬을 배우지 않았습니다.");
            }
            List<SaveDataIcon> controls = new List<SaveDataIcon> { new (slotIndex, skillUid, skillCount, skillLevel, skillLearn) };

            SaveDatas();
            return new ResultCommon(ResultCommon.Type.Success, "", controls);
        }
        /// <summary>
        /// 스킬 설정
        /// </summary>
        public void SetSkill(int slotIndex, int skillUid, int skillCount, int skillLevel, bool skillLearn)
        {
            if (skillUid <= 0) return;

            SkillDatas[slotIndex] = new SaveDataIcon(slotIndex, skillUid, skillCount, skillLevel, skillLearn);
            SaveDatas();
        }
        /// <summary>
        /// 스킬 레벨 설정
        /// </summary>
        public ResultCommon SetSkillLearn(int slotIndex, int skillUid, int skillCount, int skillLevel, bool skillLearn)
        {
            if (skillUid <= 0)
            {
                return new ResultCommon(ResultCommon.Type.Fail, $"스킬 정보가 없습니다.");
            }

            List<SaveDataIcon> controls = new List<SaveDataIcon> { new (slotIndex, skillUid, skillCount, skillLevel, skillLearn) };
            SaveDatas();
            return new ResultCommon(ResultCommon.Type.Success, "", controls);
        }
        /// <summary>
        /// 모든 스킬 목록 가져오기
        /// </summary>
        public Dictionary<int, SaveDataIcon> GetAllDatas()
        {
            return SkillDatas;
        }

        public SaveDataIcon GetData(int slotIndex)
        {
            return SkillDatas.GetValueOrDefault(slotIndex);
        }
    }
}