namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 물리 오버라이드 채널입니다.
    /// 우선순위 계산과 디버그 추적 시 어떤 시스템이 값을 소유하는지 구분하기 위해 사용합니다.
    /// </summary>
    public enum CharacterPhysicsOverrideChannel
    {
        Action = 0,
        Skill = 10,
        CrowdControl = 20,
        System = 30,
    }

    /// <summary>
    /// 캐릭터 물리 오버라이드 우선순위 기본값입니다.
    /// 숫자가 클수록 더 높은 우선순위를 가집니다.
    /// </summary>
    public static class CharacterPhysicsOverridePriority
    {
        public const int ActionJump = 40;
        public const int ActionDash = 45;
        public const int Skill = 60;
        public const int MotionSkill = 70;
        public const int MotionCrowdControl = 80;
        public const int System = 100;
    }
}
