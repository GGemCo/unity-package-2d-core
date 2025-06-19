
using UnityEngine;

namespace GGemCo.Scripts
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
        public enum ObjectiveType
        {
            None,
            TalkToNpc,
            KillMonster,
            CollectItem,
            ReachMap,
            ReachPosition,
            PlayCutscene,
        }
        public static string GetJsonFolderPath()
        {
            return Application.dataPath+ JsonFolderPath;
        }
    }
}