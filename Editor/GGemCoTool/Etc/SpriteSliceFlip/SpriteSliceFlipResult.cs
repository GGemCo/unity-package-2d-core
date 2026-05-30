#if UNITY_EDITOR
namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Slice 좌우 반전 처리 결과입니다.
    /// </summary>
    internal readonly struct SpriteSliceFlipResult
    {
        /// <summary>
        /// 생성된 PNG Atlas의 프로젝트 상대 경로입니다.
        /// </summary>
        public readonly string AssetPath;

        /// <summary>
        /// 처리된 Sub Sprite 개수입니다.
        /// </summary>
        public readonly int ProcessedSpriteCount;

        /// <summary>
        /// 투명 영역으로 판단되어 건너뛴 Sub Sprite 개수입니다.
        /// </summary>
        public readonly int SkippedTransparentSpriteCount;

        /// <summary>
        /// 처리 결과를 초기화합니다.
        /// </summary>
        /// <param name="assetPath">생성된 PNG Atlas의 프로젝트 상대 경로입니다.</param>
        /// <param name="processedSpriteCount">처리된 Sub Sprite 개수입니다.</param>
        /// <param name="skippedTransparentSpriteCount">투명 영역으로 건너뛴 Sub Sprite 개수입니다.</param>
        public SpriteSliceFlipResult(string assetPath, int processedSpriteCount, int skippedTransparentSpriteCount)
        {
            AssetPath = assetPath;
            ProcessedSpriteCount = processedSpriteCount;
            SkippedTransparentSpriteCount = skippedTransparentSpriteCount;
        }
    }
}
#endif
