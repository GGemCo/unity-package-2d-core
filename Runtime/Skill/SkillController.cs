using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 캐릭터가 가지고 있는 스킬 컨트롤러
    /// </summary>
    public class SkillController
    {
        private CharacterBase character;
        private TableSkill tableSkill;
        private TableEffect tableEffect;
        public void Initialize(CharacterBase characterBase)
        {
            character = characterBase;
            tableSkill = TableLoaderManager.Instance.TableSkill;
            tableEffect = TableLoaderManager.Instance.TableEffect;
        }

        /// <summary>
        /// 이펙트가 날아가는 스킬일 경우, 날아가는 속도 0 이면 바로 Target 에 적용된다
        /// </summary>
        /// <param name="skillUid"></param>
        /// <param name="skillLevel"></param>
        public void MakeSkill(int skillUid, int skillLevel)
        {
            var info = tableSkill.GetDataByUidLevel(skillUid, skillLevel);
            if (info == null)
            {
                GcLogger.LogError("스킬 테이블에 없는 스킬 입니다. skill Uid:"+skillUid+", Level:"+skillLevel);
                return;
            }

            GameObject skill = new GameObject("skill" + info.Uid);
            DefaultSkill defaultSkill = skill.AddComponent<DefaultSkill>();
            defaultSkill.Initialize(character, skillUid, skillLevel);
        }
    }
}