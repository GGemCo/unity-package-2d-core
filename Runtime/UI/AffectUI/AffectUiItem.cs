using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI에서 어펙트(버프/디버프) 아이콘을 그리기 위한 최소 데이터.
    /// - Core는 '표시'만 담당하고, 실제 적용/만료/스택 규칙은 AffectComponent가 관리한다.
    /// </summary>
    [Serializable]
    public readonly struct AffectUiItem
    {
        public readonly int AffectUid;
        public readonly int Stacks;
        public readonly float RemainingTime;
        public readonly float TotalDuration;
        public readonly string IconKey;

        public AffectUiItem(int affectUid, int stacks, float remainingTime, float totalDuration, string iconKey)
        {
            AffectUid = affectUid;
            Stacks = stacks;
            RemainingTime = remainingTime;
            TotalDuration = totalDuration;
            IconKey = iconKey;
        }
    }
}
