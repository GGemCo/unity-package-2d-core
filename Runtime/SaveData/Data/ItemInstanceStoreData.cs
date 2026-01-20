using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 인스턴스(랜덤 옵션 등) 저장 데이터.
    /// </summary>
    /// <remarks>
    /// - Inventory/Equip/Storage의 <see cref="SaveDataIcon.InstanceId"/>가 이 데이터의 <see cref="ItemInstanceInfo.InstanceId"/>를 참조한다.
    /// - SaveDataManager의 SaveDataContainer에 포함되어 저장/복원된다.
    /// </remarks>
    [Serializable]
    public sealed class ItemInstanceStoreData
    {
        public long NextId = 1;
        public List<ItemInstanceInfo> Items = new();
    }

    /// <summary>
    /// 드랍/획득 후 고유하게 존재하는 아이템 인스턴스 정보.
    /// </summary>
    [Serializable]
    public sealed class ItemInstanceInfo
    {
        public long InstanceId;
        public int ItemUid;
        public ItemConstants.Class Rarity;

        /// <summary>
        /// 드랍 시 확정된 랜덤 옵션 결과.
        /// </summary>
        public List<ItemAffixRoll> RolledAffixes = new();
    }

    /// <summary>
    /// 랜덤 옵션 1개 롤 결과.
    /// </summary>
    [Serializable]
    public struct ItemAffixRoll
    {
        public int AffixUid;
        public float RolledValue;

        public ItemAffixRoll(int affixUid, float rolledValue)
        {
            AffixUid = affixUid;
            RolledValue = rolledValue;
        }
    }
}
