using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 획득 이벤트 페이로드
    /// </summary>
    public readonly struct ItemCollectedEventData
    {
        /// <summary>
        /// 획득한 아이템 UID입니다.
        /// </summary>
        public readonly int ItemUid;

        /// <summary>
        /// 기존 int 기반 구독자와의 호환을 위한 획득 수량입니다.
        /// long 범위를 초과하는 재화는 int 최댓값으로 보정됩니다.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// 실제 획득 수량입니다.
        /// </summary>
        public readonly long ItemCount;

        /// <summary>
        /// 아이템을 획득한 캐릭터 VID입니다.
        /// </summary>
        public readonly int? OwnerVid; // 누가 획득했는지 필요하면

        /// <summary>
        /// 월드 드랍을 생성한 상위 시스템의 런타임 출처 키입니다.
        /// </summary>
        public readonly string SourceKey;

        /// <summary>
        /// 상위 시스템이 현재 유효한 드랍을 식별하기 위한 런타임 토큰입니다.
        /// </summary>
        public readonly long RuntimeToken;

        /// <summary>
        /// 아이템 획득 이벤트가 생성된 실시간 시각입니다.
        /// </summary>
        public readonly double TimeRealtimeSinceStartup;

        /// <summary>
        /// 기존 int 수량 기반 아이템 획득 이벤트 데이터를 생성합니다.
        /// </summary>
        public ItemCollectedEventData(int itemUid, int count, int? ownerVid = null)
            : this(itemUid, (long)count, ownerVid)
        {
        }

        /// <summary>
        /// long 수량과 월드 드랍 런타임 식별 정보를 포함한 아이템 획득 이벤트 데이터를 생성합니다.
        /// </summary>
        public ItemCollectedEventData(
            int itemUid,
            long itemCount,
            int? ownerVid = null,
            string sourceKey = null,
            long runtimeToken = 0)
        {
            ItemUid = itemUid;
            ItemCount = itemCount;
            Count = itemCount >= int.MaxValue ? int.MaxValue :
                itemCount <= int.MinValue ? int.MinValue : (int)itemCount;
            OwnerVid = ownerVid;
            SourceKey = sourceKey;
            RuntimeToken = runtimeToken;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}
