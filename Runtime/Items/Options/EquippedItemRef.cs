using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// Reference info for an equipped item.
    /// </summary>
    [Serializable]
    public sealed class EquippedItemRef
    {
        /// <summary>Item definition UID.</summary>
        public int ItemUid;

        /// <summary>
        /// Instance UID for rolled options.
        /// - 0: definition-only (legacy).
        /// - &gt;0: instance-based.
        /// </summary>
        public long InstanceId;

        /// <summary>
        /// Cached item definition row (not persisted).
        /// </summary>
        [NonSerialized]
        public StruckTableItem Definition;

        public EquippedItemRef(int itemUid, long instanceId, StruckTableItem definition)
        {
            ItemUid = itemUid;
            InstanceId = instanceId;
            Definition = definition;
        }
    }
}
