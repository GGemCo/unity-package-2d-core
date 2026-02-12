namespace GGemCo2DCore
{
    /// <summary>
    /// 피격 처리 결과(할당 없이 값 타입으로 반환)
    /// </summary>
    public readonly struct HitReactionDecision
    {
        public readonly bool ConsumedStacks;
        public readonly int RemainingStacks;

        /// <summary>이번 히트로 인해 실제로 리액션을 발동해야 하는지</summary>
        public readonly bool ShouldReact;

        public readonly CharacterConstants.HitReactionType ReactionType;

        public HitReactionDecision(bool consumedStacks, int remainingStacks, bool shouldReact, CharacterConstants.HitReactionType reactionType)
        {
            ConsumedStacks = consumedStacks;
            RemainingStacks = remainingStacks;
            ShouldReact = shouldReact;
            ReactionType = reactionType;
        }

        public static HitReactionDecision NoReaction(int stacks)
            => new HitReactionDecision(false, stacks, false, CharacterConstants.HitReactionType.None);
    }
}