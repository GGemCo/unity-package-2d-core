using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 경직(Hit Stop) 적용 시 사용할 요청 데이터입니다.
    /// </summary>
    public readonly struct HitStopRequest
    {
        public readonly float DurationSeconds;
        public readonly bool PauseAnimation;
        public readonly bool FreezePhysics;
        public readonly int SourceSkillUid;

        /// <summary>
        /// 경직 요청 데이터를 생성합니다.
        /// </summary>
        public HitStopRequest(
            float durationSeconds,
            bool pauseAnimation = true,
            bool freezePhysics = true,
            int sourceSkillUid = 0)
        {
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            PauseAnimation = pauseAnimation;
            FreezePhysics = freezePhysics;
            SourceSkillUid = sourceSkillUid;
        }
    }
}
