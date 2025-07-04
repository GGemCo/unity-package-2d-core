using TMPro;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class LocalizedTextProxy : MonoBehaviour
    {
        public TextMeshProUGUI Target;

        public void SetText(string value)
        {
            if (Target != null)
                Target.text = value; // ✅ 여기서 직접 buttonText.text 할당 가능
        }
    }
}