using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 유니티 6 버전 미만일경우,
    /// Localize String Event 가 사용되는 오브젝트에
    /// AddComponent 하여 Update String 에 사용 
    /// </summary>
    public class LocalizationTextProxy : MonoBehaviour
    {
        public TextMeshProUGUI target;

        public void SetText(string value)
        {
            if (target != null)
                target.text = value; // ✅ 여기서 직접 buttonText.text 할당 가능
        }
    }
}