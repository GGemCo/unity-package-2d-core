namespace GGemCo2DCore
{
    public interface ISkillCancelableDriver : IMonsterSkillDriver
    {
        /// <summary>현재 실행 중인 스킬을 취소하도록 요청한다.</summary>
        /// <returns>취소 요청이 처리되었으면 true.</returns>
        bool RequestCancelSkill(SkillCancelReason reason);
    }

    public enum SkillCancelReason
    {
        UserInput,
        HitStun,
        Knockback,
        Stun,
        Death,
        StateChanged,
        ForcedBySystem
    }
}