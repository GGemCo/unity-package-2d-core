using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 인스턴스(드랍/획득 후 고유하게 존재하는 아이템).
    /// </summary>
    [Serializable]
    public sealed class ItemInstanceData
    {
        public long InstanceId;
        public int ItemUid;
        public ItemRarity Rarity;

        /// <summary>
        /// 드랍 시 확정된 랜덤 옵션 결과.
        /// - AffixUid와 RolledValue를 저장한다.
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
