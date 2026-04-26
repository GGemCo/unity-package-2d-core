using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 슬롯
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        [Header(UIWindowConstants.TitleHeaderCommon)]
        [Tooltip("필터링 된 슬롯인지 여부")]
        public bool isFiltering;
        [Tooltip("Canvas Group 컴포넌트 사용 여부")]
        public bool useCanvasGroup;
        
        [Header("색상")]
        [Tooltip("일반 상태 색상")]
        [SerializeField] private Color colorNormal = Color.white;
        [Tooltip("선택되었을 때 색상")]
        [SerializeField] private Color colorSelected = Color.blue;
        [Tooltip("현재 선택 문맥에서 이 슬롯의 아이템이 장착 중임을 표시할 이미지")]
        [SerializeField] private Image imageEquipped;
        [Tooltip("현재 선택 문맥에서 이 슬롯의 아이템이 장착 중임을 표시할 텍스트")]
        [SerializeField] private TextMeshProUGUI textEquipped;

        private UIWindow _window;
        private UIWindowConstants.WindowUid _windowUid;
        public int Index { get; private set; }

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Image _imageSlot;
        private bool _isSelected;

        /// <summary>
        /// prefab 생성 후 호출되는 함수
        /// </summary>
        /// <param name="window"></param>
        /// <param name="windowUid"></param>
        /// <param name="slotIndex"></param>
        /// <param name="slotSize"></param>
        public void Initialize(UIWindow window, UIWindowConstants.WindowUid windowUid, int slotIndex, Vector2 slotSize)
        {
            _window = window;
            _windowUid = windowUid;
            Index = slotIndex;
            
            if (_imageSlot == null)
                _imageSlot = GetComponent<Image>();
            
            _rectTransform = GetComponent<RectTransform>();
            if (useCanvasGroup)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            ChangeSlotImageSize(slotSize);
            SetEquippedState(false);
        }
        /// <summary>
        /// 슬롯 이미지 사이즈 변경하기
        /// </summary>
        /// <param name="size"></param>
        private void ChangeSlotImageSize(Vector2 size)
        {
            _rectTransform.sizeDelta = size;
        }

        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }
        public void SetAlpha(float alpha)
        {
            if (useCanvasGroup) _canvasGroup.alpha = alpha;
        }

        public void SetColor(Color color)
        {
            if (!_imageSlot) return;
            _imageSlot.color = color;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            SetColor(selected ? colorSelected : colorNormal);
        }

        /// <summary>
        /// 이 슬롯에 있는 아이템이 현재 선택 문맥에서 이미 사용 중인지 표시합니다.
        /// 아이콘 잠금과 달리 슬롯의 입력 가능 상태는 바꾸지 않고 표시 오브젝트만 토글합니다.
        /// </summary>
        public void SetEquippedState(bool equipped)
        {
            if (imageEquipped != null)
            {
                imageEquipped.gameObject.SetActive(equipped);
            }

            if (textEquipped != null)
            {
                textEquipped.gameObject.SetActive(equipped);
            }
        }
    }
}
