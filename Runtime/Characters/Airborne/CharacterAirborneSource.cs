using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 공중 상태로 간주되는 원인을 나타냅니다.
    /// 여러 시스템이 동시에 공중 상태를 요구할 수 있으므로 플래그로 조합해서 사용합니다.
    /// </summary>
    [Flags]
    public enum CharacterAirborneSource
    {
        /// <summary>
        /// 공중 상태 원인이 없습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 지면 Probe 결과 지상에 닿아 있지 않은 상태입니다.
        /// </summary>
        PhysicsProbe = 1 << 0,

        /// <summary>
        /// 점프 액션으로 인해 공중 상태로 취급됩니다.
        /// </summary>
        Jump = 1 << 1,

        /// <summary>
        /// Crowd Control로 인해 공중 상태로 취급됩니다.
        /// </summary>
        CrowdControl = 1 << 2,

        /// <summary>
        /// 스킬 이동 이벤트로 인해 공중 상태로 취급됩니다.
        /// </summary>
        SkillMotion = 1 << 3,

        /// <summary>
        /// 스킬 런지 이벤트로 인해 공중 상태로 취급됩니다.
        /// </summary>
        Lunge = 1 << 4,

        /// <summary>
        /// 컷신이나 연출 시스템으로 인해 공중 상태로 취급됩니다.
        /// </summary>
        Cutscene = 1 << 5,

        /// <summary>
        /// 외부 시스템에서 명시적으로 등록한 공중 상태입니다.
        /// </summary>
        External = 1 << 6,
    }
}
