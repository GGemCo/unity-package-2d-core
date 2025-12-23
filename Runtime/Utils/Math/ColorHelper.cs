using UnityEngine;

namespace GGemCo2DCore
{
    public static class ColorHelper
    {
        /// <summary>
        /// Hex 문자열을 Color로 파싱합니다.
        /// 지원: RGB, RGBA, #RGB, #RGBA, #RRGGBB, #RRGGBBAA
        /// </summary>
        public static bool TryParseHex(string hex, out Color color)
        {
            color = default;

            if (string.IsNullOrWhiteSpace(hex))
                return false;

            // Unity 기본 파서 사용 (내부적으로 다양한 포맷 지원)
            return ColorUtility.TryParseHtmlString(hex, out color);
        }

        public static Color HexToColor(string hex, Color fallback)
            => TryParseHex(hex, out var c) ? c : fallback;

        public static string RGBAToHex(Color color)
            => $"#{ColorUtility.ToHtmlStringRGBA(color)}";

        public static string RGBToHex(Color color)
            => $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }
}