using UnityEngine;

namespace GGemCo.Scripts
{
    public static class ColorHelper
    {
        /// <summary>
        /// Hex를 Color로 리턴합니다.
        /// </summary>
        /// <param name="hex">Hex (#생략 가능)</param>
        /// <returns></returns>
        public static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", ""); // "#" 문자 제거
            if (hex.Length != 6)
            {
                Debug.LogError("유효하지 않은 Hex 값입니다.");
                return Color.white;
            }

            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color32(r, g, b, 255);
        }
    }
}