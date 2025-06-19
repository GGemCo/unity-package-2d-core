using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 선택지 버튼
    /// </summary>
    public class UIButtonAnswer : MonoBehaviour
    {
        public TextMeshProUGUI buttonTitle;

        public float SetButtonTitle(string title)
        {
            if (buttonTitle == null) return 0;
            buttonTitle.text = title;
            return buttonTitle.preferredWidth;
        }

        public void ChangeWidth(float width)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(width, rectTransform.sizeDelta.y);
        }
    }
}