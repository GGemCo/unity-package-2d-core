namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 애니메이션 재생 요청.
    /// Spine/Sprite 구현 차이를 숨기기 위한 DTO.
    /// </summary>
    public readonly struct SkillAnimationRequest
    {
        /// <summary>스킬 UID(테이블 기준).</summary>
        public readonly int SkillUid;

        /// <summary>재생 단계.</summary>
        public readonly SkillAnimationPhase Phase;

        /// <summary>루프 재생 여부.</summary>
        public readonly bool Loop;

        /// <summary>애니메이션 재생 속도(1 = 기본).</summary>
        public readonly float TimeScale;

        /// <summary>
        /// 특정 구현에서 사용할 애니메이션 이름 오버라이드.
        /// 값이 비어있지 않으면 네이밍 규칙보다 우선한다.
        /// </summary>
        public readonly string OverrideAnimationName;

        public SkillAnimationRequest(
            int skillUid,
            SkillAnimationPhase phase,
            bool loop = false,
            float timeScale = 1f,
            string overrideAnimationName = null)
        {
            SkillUid = skillUid;
            Phase = phase;
            Loop = loop;
            TimeScale = timeScale;
            OverrideAnimationName = overrideAnimationName;
        }
    }
}
