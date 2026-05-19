using System.Collections.Generic;

namespace GGemCo2DCore
{
    [System.Serializable]
    public class Quest
    {
        public int uid;
        public string title;
        public List<QuestStep> steps = new List<QuestStep>();
        public QuestReward reward = new QuestReward();
    }

    [System.Serializable]
    public class QuestStep
    {
        public QuestConstants.ObjectiveType objectiveType;
        
        public int mapUid;
        public int targetUid;
        public int cutsceneUid;
        public Vec2 position;
        public int count;
        public int dialogueUid;

        /// <summary>
        /// <para><see cref="QuestConstants.ObjectiveType.PlayCutscene"/> 단계에서 사용할 컷신 UID를 반환합니다.</para>
        /// <para>신규 필드(<see cref="cutsceneUid"/>)가 비어 있는 기존 데이터는 <see cref="targetUid"/>를 폴백으로 사용합니다.</para>
        /// </summary>
        /// <returns>재생 대상 컷신 UID입니다. 유효한 값이 없으면 0을 반환합니다.</returns>
        public int GetPlayCutsceneUid()
        {
            return cutsceneUid > 0 ? cutsceneUid : targetUid;
        }
    }
}
