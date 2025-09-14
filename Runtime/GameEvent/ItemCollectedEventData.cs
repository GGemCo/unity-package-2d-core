using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 획득 이벤트 페이로드
    /// </summary>
    public readonly struct ItemCollectedEventData
    {
        public readonly int ItemUid;
        public readonly int Count;
        public readonly int? OwnerVid; // 누가 획득했는지 필요하면
        public readonly double TimeRealtimeSinceStartup;

        public ItemCollectedEventData(int itemUid, int count, int? ownerVid = null)
        {
            ItemUid = itemUid;
            Count = count;
            OwnerVid = ownerVid;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}