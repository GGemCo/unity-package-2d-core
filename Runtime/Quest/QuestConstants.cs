
using UnityEngine;

namespace GGemCo2DCore
{
    public static class QuestConstants
    {
        public const string JsonFolderName = "Quests/";
        public const string JsonFolderPath = "/Resources/"+JsonFolderName;
        public enum Type
        {
            None,
            Main,
            Sub
        }
        public enum Status
        {
            None,
            Ready,
            InProgress,
            Complete, // 보상 받기 전
            End // 보상 받은 후
        }

        /// <summary>
        /// 퀘스트가 시작되는 조건을 정의합니다.
        /// </summary>
        public enum TriggerType
        {
            None,
            TalkToNpc,
            EnterMap
        }

        public enum ObjectiveType
        {
            None = 0,
            TalkToNpc = 1,
            KillMonster = 2,
            CollectItem = 3,
            ReachMap = 4,
            ReachPosition = 5,
            PlayCutscene = 6,
            KillMonsterInMap = 7,
        }
        public static string GetJsonFolderPath()
        {
            return Application.dataPath+ JsonFolderPath;
        }
    }
}
