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
        public Sprite ImageSprite;
        public Vector2 ImageSize;
        public int FontSize;
        public UIFloatingTextType Type = UIFloatingTextType.Info;
        public float MoveUpTime;
        public float MoveUpDistance;
        public float FadeOutTime;
        public float RandomXRange = -1f;
        public Easing.EaseType? EaseType;

        /// <summary>
        /// 플로팅 표시 요청이 스프라이트 이미지를 포함하는지 확인합니다.
        /// </summary>
        /// <returns>표시할 이미지 스프라이트가 있으면 <see langword="true"/>입니다.</returns>
        public bool HasImageSprite()
        {
            return ImageSprite != null;
        }

        /// <summary>
        /// 텍스트 기반 플로팅 표시로 사용할 문자열을 반환합니다.
        /// </summary>
        /// <returns>직접 지정한 텍스트가 있으면 해당 값을, 없으면 숫자 값을 문자열로 변환해 반환합니다.</returns>
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
