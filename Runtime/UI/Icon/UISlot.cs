using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이콘 슬롯
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        private UIWindow window;
        private UIWindowManager.WindowUid windowUid;
        private int index;

        private RectTransform rectTransform;

        /// <summary>
        /// prefab 생성 후 호출되는 함수
        /// </summary>
        /// <param name="pwindow"></param>
        /// <param name="pwindowUid"></param>
        /// <param name="pindex"></param>
        /// <param name="slotSize"></param>
        public void Initialize(UIWindow pwindow, UIWindowManager.WindowUid pwindowUid, int pindex, Vector2 slotSize)
        {
            window = pwindow;
            windowUid = pwindowUid;
            index = pindex;
            
            rectTransform = GetComponent<RectTransform>();
            ChangeSlotImageSize(slotSize);
        }
        /// <summary>
        /// 슬롯 이미지 사이즈 변경하기
        /// </summary>
        /// <param name="size"></param>
        public void ChangeSlotImageSize(Vector2 size)
        {
            rectTransform.sizeDelta = size;
        }

        public void SetPosition(Vector3 position)
        {
            transform.localPosition = position;
        }
    }
}
