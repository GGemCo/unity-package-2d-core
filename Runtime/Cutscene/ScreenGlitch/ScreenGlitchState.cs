namespace GGemCo2DCore
{
    /// <summary>
    /// 렌더 패스가 한 프레임에 적용할 글리치 파라미터 묶음입니다.
    /// 컨트롤러는 컷신 시간에 맞춰 이 값을 갱신하고, 서비스는 렌더 패스에 현재 값을 제공합니다.
    /// </summary>
    public struct ScreenGlitchState
    {
        /// <summary>글리치 전체 강도입니다.</summary>
        public float Intensity;

        /// <summary>RGB 채널 분리 강도입니다.</summary>
        public float RgbSplit;

        /// <summary>가로 방향 줄 단위 흔들림 강도입니다.</summary>
        public float HorizontalJitter;

        /// <summary>세로 방향 순간 튐 강도입니다.</summary>
        public float VerticalJump;

        /// <summary>블록 노이즈 강도입니다.</summary>
        public float BlockNoise;

        /// <summary>스캔라인 강도입니다.</summary>
        public float ScanlineStrength;

        /// <summary>색상 흔들림 강도입니다.</summary>
        public float ColorDrift;

        /// <summary>노이즈 변화 속도입니다.</summary>
        public float NoiseSpeed;

        /// <summary>글리치 패턴 시드입니다.</summary>
        public float Seed;

        /// <summary>
        /// 효과가 실제로 렌더링될 만큼 활성 상태인지 확인합니다.
        /// </summary>
        /// <returns>화면에 적용할 강도가 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsActive()
        {
            return Intensity > 0.0001f;
        }
    }
}
