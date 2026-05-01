using System.Collections.Generic;

namespace GGemCo2DCore
{

    [System.Serializable]
    public class QuestReward
    {
        public int experience;
        public int gold;
        public int silver;
        public List<RewardItem> items = new List<RewardItem>();
        public QuestRewardMapProgress mapProgress = new QuestRewardMapProgress();
    }

    /// <summary>
    /// 퀘스트 완료 보상으로 적용할 맵 진행 상태 변경 정보를 보관합니다.
    /// </summary>
    [System.Serializable]
    public class QuestRewardMapProgress
    {
        public int clearMapUid;
        public List<string> activateWorldMapNodeIds = new List<string>();
    }

    [System.Serializable]
    public class RewardItem
    {
        public int itemUid;
        public int amount;
    }
}
