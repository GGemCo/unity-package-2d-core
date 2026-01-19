using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core ↔ (옵션 패키지)Affect 연결을 위한 브리지.
    /// Affect 패키지가 존재하면 런타임에서 Provider를 등록하고,
    /// 없으면 Null Provider가 사용됩니다.
    /// </summary>
    public static class AffectBridge
    {
        private static IAffectDescriptionProvider _descriptionProvider = NullAffectDescriptionProvider.Instance;

        /// <summary>
        /// Affect 설명 Provider.
        /// - Affect 미설치: Null Provider
        /// - Affect 설치: Affect 측에서 런타임 등록
        /// </summary>
        public static IAffectDescriptionProvider DescriptionProvider
        {
            get => _descriptionProvider ?? NullAffectDescriptionProvider.Instance;
            set => _descriptionProvider = value ?? NullAffectDescriptionProvider.Instance;
        }

        /// <summary>
        /// 현재 Provider가 실제 구현인지(=Affect 설치/등록됨) 여부.
        /// </summary>
        public static bool HasProvider => !ReferenceEquals(DescriptionProvider, NullAffectDescriptionProvider.Instance);

        /// <summary>
        /// 외부 패키지에서 Provider를 안전하게 교체할 때 사용합니다.
        /// </summary>
        public static void SetProvider(IAffectDescriptionProvider provider)
        {
            DescriptionProvider = provider;
        }
    }
}