namespace GGemCo2DCore
{
    /// <summary>
    /// Affect 패키지가 설치되지 않았을 때 사용되는 Null Provider.
    /// 항상 빈 문자열을 반환하여 Core 단독 동작을 보장합니다.
    /// </summary>
    internal sealed class NullAffectDescriptionProvider : IAffectDescriptionProvider
    {
        public static readonly NullAffectDescriptionProvider Instance = new NullAffectDescriptionProvider();

        private NullAffectDescriptionProvider() { }

        public string GetDescription(int affectUid) => string.Empty;

        public string GetDescriptionWithChancePrefix(int affectUid, float chancePercent) => string.Empty;
    }
}