using System.Collections.Generic;

namespace GGemCo.Scripts
{

    [System.Serializable]
    public class QuestReward
    {
        public int experience;
        public int gold;
        public int silver;
        public List<RewardItem> items = new List<RewardItem>();
    }
    [System.Serializable]
    public class RewardItem
    {
        public int itemUid;
        public int amount;
    }
}