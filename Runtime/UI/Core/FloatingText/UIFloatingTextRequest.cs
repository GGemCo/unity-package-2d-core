using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플로팅 텍스트 출력 요청 데이터입니다.
    /// </summary>
    public class UIFloatingTextRequest
    {
        public Vector3 WorldPosition;
        public float NumericValue;
        public string Text = string.Empty;
        public Color Color = Color.white;
        public int FontSize;
        public UIFloatingTextType Type = UIFloatingTextType.Info;
        public float MoveUpTime;
        public float MoveUpDistance;
        public float FadeOutTime;
        public float RandomXRange = -1f;
        public Easing.EaseType? EaseType;

        public virtual string ResolveDisplayText()
        {
            if (!string.IsNullOrEmpty(Text))
            {
                return Text;
            }

            return NumericValue.ToString();
        }
    }
}
