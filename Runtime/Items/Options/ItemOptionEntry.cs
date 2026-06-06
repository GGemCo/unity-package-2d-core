using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 옵션 1개를 표현하는 공용 DTO.
    /// </summary>
    [Serializable]
    public struct ItemOptionEntry
    {
        /// <summary>옵션의 도메인(Stat/State/DamageType/Affect).</summary>
        public ItemOptionKind Kind;

        /// <summary>
        /// 대상 ID.
        /// - Kind=Stat   : BASE_* 또는 STAT_* (TableStat.Id)
        /// - Kind=State  : STATE_* (TableState.Id)
        /// - Kind=DamageType: DT_* (TableDamageType.Id)
        /// - Kind=Affect : AffectUid 또는 AffectId(정책에 맞게 사용)
        /// </summary>
        public string TargetId;

        /// <summary>적용 방식(기존 SuffixType 재사용).</summary>
        public ConfigCommon.SuffixType Op;

        /// <summary>값.</summary>
        public float Value;

        /// <summary>확률(선택). 0이면 항상 적용으로 간주.</summary>
        public int Chance;

        /// <summary>지속 시간(선택). 0이면 즉시/영구 정책에 따름.</summary>
        public float Duration;

        public bool IsValid => !string.IsNullOrEmpty(TargetId);

        public ItemOptionEntry(ItemOptionKind kind, string targetId, ConfigCommon.SuffixType op, float value,
            int chance = 0, float duration = 0)
        {
            Kind = kind;
            TargetId = targetId;
            Op = op;
            Value = value;
            Chance = chance;
            Duration = duration;
        }
    }
}
