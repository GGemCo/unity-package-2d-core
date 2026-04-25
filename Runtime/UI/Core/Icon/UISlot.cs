using UnityEngine;
using UnityEngine.UI;

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
    }
}
