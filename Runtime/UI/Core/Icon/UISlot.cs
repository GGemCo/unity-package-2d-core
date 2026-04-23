using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이콘 슬롯
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        public bool isFiltering;
        public bool useCanvasGroup;

        public UIWindow window;
        public UIWindowConstants.WindowUid windowUid;
        public int index;
        private CanvasGroup _canvasGroup;
        public CanvasGroup CanvasGroup => _canvasGroup;

        private RectTransform _rectTransform;
        private Image _imageSlot;

        /// <summary>
        /// prefab 생성 후 호출되는 함수
        /// </summary>
        /// <param name="pwindow"></param>
        /// <param name="pwindowUid"></param>
        /// <param name="pindex"></param>
        /// <param name="slotSize"></param>
        public void Initialize(UIWindow pwindow, UIWindowConstants.WindowUid pwindowUid, int pindex, Vector2 slotSize)
        {
            window = pwindow;
            windowUid = pwindowUid;
            index = pindex;
            
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
    }
}
