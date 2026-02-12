namespace GGemCo2DCore
{
    /// <summary>
    /// 한 번의 타격(히트) 정보.
    /// 공격/스킬/프로젝트 등 어느 경로든 이 페이로드로 “경직 스택 피해”를 전달합니다.
    /// </summary>
    public readonly struct HitPayload
    {
        /// <summary>이번 히트가 경직 무시 스택을 몇 개 깎는지(0이면 깎지 않음)</summary>
        public readonly int StaggerStackDamage;

        /// <summary>스택과 무관하게 강제 리액션(선택)</summary>
        public readonly bool ForceReaction;

        /// <summary>요구하는 리액션 타입</summary>
        public readonly CharacterConstants.HitReactionType ReactionType;

        /// <summary>연타/다단 히트 구분용(선택): 동일 공격 ID</summary>
        public readonly int AttackId;

        public HitPayload(int staggerStackDamage, CharacterConstants.HitReactionType reactionType, bool forceReaction = false, int attackId = 0)
        {
            StaggerStackDamage = staggerStackDamage;
            ReactionType = reactionType;
            ForceReaction = forceReaction;
            AttackId = attackId;
        }

        public static HitPayload None => new HitPayload(0, CharacterConstants.HitReactionType.None);
    }
}