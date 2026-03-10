using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 옵션 미리보기 계산 결과
    /// </summary>
    public sealed class StatPreviewResult
    {
        /// <summary>
        /// 고정값 증감
        /// </summary>
        public Dictionary<string, int> Flat { get; } = new(16);

        /// <summary>
        /// 퍼센트 증감
        /// </summary>
        public Dictionary<string, float> Percent { get; } = new(16);

        /// <summary>
        /// Affect UID 목록
        /// </summary>
        public List<int> AffectUids { get; } = new(8);

        /// <summary>
        /// 결과가 비어있는지 여부
        /// </summary>
        public bool IsEmpty => Flat.Count == 0 && Percent.Count == 0 && AffectUids.Count == 0;

        /// <summary>
        /// 초기화
        /// </summary>
        public void Clear()
        {
            Flat.Clear();
            Percent.Clear();
            AffectUids.Clear();
        }
    }
}