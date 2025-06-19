using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class DefaultTagName : MonoBehaviour
    {
        [Header("기본오브젝트")]
        [Tooltip("이름을 표시할 UI Text 오브젝트")]
        public TextMeshProUGUI textName;
        
        [Header("기본속성")]
        [Tooltip("폰트 크기")]
        public float fontSize = 24;
        [Tooltip("폰트 색상")]
        public Color color = Color.white;
        [Tooltip("네임 태그 위치. 아이템 이미지 가운데를 기준으로 계산하여 입력.")]
        public Vector3 diffTextPosition;

        protected virtual void ApplyTextEffect()
        {
            textName.color = color;
            textName.fontSize = fontSize;
        }
        /// <summary>
        /// 아이템 비활성에 맞춰서 이름도 비활성화 처리 
        /// </summary>
        private void OnDisable()
        {
            textName.gameObject.SetActive(false);
        }
        /// <summary>
        /// 아이템 활성에 맞춰서 이름도 활성화 처리 
        /// </summary>
        private void OnEnable()
        {
            textName.gameObject.SetActive(true);
        }
    }
}