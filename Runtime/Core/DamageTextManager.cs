using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기존 데미지 텍스트 요청 형식을 유지하기 위한 호환 데이터입니다.
    /// </summary>
    public class MetadataDamageText : UIFloatingTextRequest
    {
        public float Damage
        {
            get => NumericValue;
            set => NumericValue = value;
        }

        /// <summary>
        /// damage 숫자 대신 텍스트를 사용해야 할 때 사용합니다.
        /// </summary>
        public string SpecialDamageText
        {
            get => Text;
            set => Text = value;
        }

        public MetadataDamageText()
        {
            Type = UIFloatingTextType.Damage;
        }
    }

    /// <summary>
    /// 기존 DamageTextManager 이름을 유지하면서 범용 플로팅 텍스트 기능을 제공합니다.
    /// </summary>
    public class DamageTextManager : UIFloatingTextManager
    {
        public void ShowDamageText(MetadataDamageText metadataDamageText)
        {
            if (metadataDamageText == null)
            {
                return;
            }

            if (metadataDamageText.Type == UIFloatingTextType.None)
            {
                metadataDamageText.Type = UIFloatingTextType.Damage;
            }

            ShowFloatingText(metadataDamageText);
        }

        public void ShowFloatingText(Vector3 worldPosition, string text, Color color,
            UIFloatingTextType type = UIFloatingTextType.Info, int fontSize = 0)
        {
            ShowFloatingText(new UIFloatingTextRequest
            {
                WorldPosition = worldPosition,
                Text = text,
                Color = color,
                FontSize = fontSize,
                Type = type,
            });
        }
    }
}
