using TMPro;

namespace GGemCo2DCore
{
    /// <summary>
    /// TextMeshProUGUI의 Alignment를 설정하는 유틸리티 클래스
    /// TextMeshProHelper.SetAlignment(
    ///     textMeshPro,
    ///     TextMeshProHelper.HorizontalAlignment.Center,
    ///     TextMeshProHelper.VerticalAlignment.Middle
    /// );
    /// </summary>
    public static class TextMeshProHelper
    {
        /// <summary>
        /// 수평 정렬 기준
        /// </summary>
        public enum HorizontalAlignment
        {
            Left,
            Center,
            Right,
            Justified,
        }

        /// <summary>
        /// 수직 정렬 기준
        /// </summary>
        public enum VerticalAlignment
        {
            Top,
            Middle,
            Bottom,
            Baseline,
            Capline
        }

        /// <summary>
        /// 정렬 옵션 설정
        /// </summary>
        /// <param name="text">정렬을 설정할 TextMeshProUGUI 컴포넌트</param>
        /// <param name="horizontal">수평 정렬</param>
        /// <param name="vertical">수직 정렬</param>
        public static void SetAlignment(TextMeshProUGUI text, HorizontalAlignment horizontal, VerticalAlignment vertical)
        {
            TextAlignmentOptions alignment = GetAlignmentOptions(horizontal, vertical);
            text.alignment = alignment;
        }

        /// <summary>
        /// 정렬 옵션 조합 반환
        /// </summary>
        private static TextAlignmentOptions GetAlignmentOptions(HorizontalAlignment h, VerticalAlignment v)
        {
            return (v, h) switch
            {
                (VerticalAlignment.Top, HorizontalAlignment.Left) => TextAlignmentOptions.TopLeft,
                (VerticalAlignment.Top, HorizontalAlignment.Center) => TextAlignmentOptions.Top,
                (VerticalAlignment.Top, HorizontalAlignment.Right) => TextAlignmentOptions.TopRight,
                (VerticalAlignment.Top, HorizontalAlignment.Justified) => TextAlignmentOptions.TopJustified,

                (VerticalAlignment.Middle, HorizontalAlignment.Left) => TextAlignmentOptions.Left,
                (VerticalAlignment.Middle, HorizontalAlignment.Center) => TextAlignmentOptions.Center,
                (VerticalAlignment.Middle, HorizontalAlignment.Right) => TextAlignmentOptions.Right,
                (VerticalAlignment.Middle, HorizontalAlignment.Justified) => TextAlignmentOptions.Justified,

                (VerticalAlignment.Bottom, HorizontalAlignment.Left) => TextAlignmentOptions.BottomLeft,
                (VerticalAlignment.Bottom, HorizontalAlignment.Center) => TextAlignmentOptions.Bottom,
                (VerticalAlignment.Bottom, HorizontalAlignment.Right) => TextAlignmentOptions.BottomRight,
                (VerticalAlignment.Bottom, HorizontalAlignment.Justified) => TextAlignmentOptions.BottomJustified,

                (VerticalAlignment.Baseline, HorizontalAlignment.Left) => TextAlignmentOptions.BaselineLeft,
                (VerticalAlignment.Baseline, HorizontalAlignment.Center) => TextAlignmentOptions.Baseline,
                (VerticalAlignment.Baseline, HorizontalAlignment.Right) => TextAlignmentOptions.BaselineRight,
                (VerticalAlignment.Baseline, HorizontalAlignment.Justified) => TextAlignmentOptions.BaselineJustified,

                (VerticalAlignment.Capline, HorizontalAlignment.Left) => TextAlignmentOptions.CaplineLeft,
                (VerticalAlignment.Capline, HorizontalAlignment.Center) => TextAlignmentOptions.Capline,
                (VerticalAlignment.Capline, HorizontalAlignment.Right) => TextAlignmentOptions.CaplineRight,
                (VerticalAlignment.Capline, HorizontalAlignment.Justified) => TextAlignmentOptions.CaplineJustified,

                _ => TextAlignmentOptions.Center
            };
        }
    }
}
