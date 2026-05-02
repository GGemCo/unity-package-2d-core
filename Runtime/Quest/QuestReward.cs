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
        public List<QuestRewardLicense> licenses = new List<QuestRewardLicense>();
    }

    /// <summary>
    /// 퀘스트 완료 보상으로 적용할 맵 진행 상태 변경 정보를 보관합니다.
    /// </summary>
    [System.Serializable]
    public class QuestRewardMapProgress
    {
        /// <summary>퀘스트 완료 시 클리어 처리할 실제 게임 맵 UID입니다.</summary>
        public int clearMapUid;

        /// <summary>
        /// 퀘스트 완료 시 월드맵에서 표시만 켤 노드 ID 목록입니다.
        /// 노드의 비활성 상태는 유지합니다.
        /// </summary>
        public List<string> visibleWorldMapNodeIds = new List<string>();

        /// <summary>
        /// 퀘스트 완료 시 월드맵에서 표시하고 활성화할 노드 ID 목록입니다.
        /// 비활성 상태도 함께 해제합니다.
        /// </summary>
        public List<string> activateWorldMapNodeIds = new List<string>();
    }

    [System.Serializable]
    public class RewardItem
    {
        public int itemUid;
        public int amount;
    }

    /// <summary>
    /// 퀘스트 완료 보상으로 설정할 라이센스 값을 보관합니다.
    /// </summary>
    [System.Serializable]
    public class QuestRewardLicense
    {
        public int licenseUid;
        public string value = LicenseConstants.TrueValue;
    }
}
