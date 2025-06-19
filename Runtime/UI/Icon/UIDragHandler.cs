using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private UIIcon icon;
        // 아이콘 이미지
        private Image image;
        // 드래그 하기전 위치
        private Vector3 originalPosition;
        // 드래그 가능 여부
        private bool isPossibleDrag = true;

        private void Awake()
        {
            icon = GetComponent<UIIcon>();
            image = GetComponent<Image>();
        }

        public void SetIsPossibleDrag(bool set) => isPossibleDrag = set;
        public bool GetIsPossibleDrag() => isPossibleDrag;
        /// <summary>
        /// 드래그 시작
        /// </summary>
        /// <param name="eventData"></param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isPossibleDrag) return;

            transform.SetParent(transform.root);
            image.raycastTarget = false;

            originalPosition = transform.position;
        }
        /// <summary>
        /// 드래그 중일때 
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isPossibleDrag) return;
            transform.position = Input.mousePosition;
        }
        /// <summary>
        /// 드래그 끝났을때
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isPossibleDrag) return;
            image.raycastTarget = true;

            GameObject droppedIcon = eventData.pointerDrag;
            UIIcon droppedUiIcon = droppedIcon.GetComponent<UIIcon>();
            GameObject targetIcon = eventData.pointerEnter;
            
            if (droppedIcon != null)
            {
                // 윈도우 밖에 드래그 앤 드랍했을때  
                if (targetIcon == null)
                {
                    droppedUiIcon.window.OnEndDragOutWindow(eventData, droppedIcon, targetIcon, originalPosition);
                    return;
                }
                UIIcon targetUiIcon = targetIcon.GetComponent<UIIcon>();
                if (targetUiIcon != null && targetUiIcon.window != null)
                {
                    targetUiIcon.window.OnEndDragInIcon(droppedIcon, targetIcon);
                    return;
                }
            }
            GameObject targetSlot = droppedUiIcon.window.slots[droppedUiIcon.slotIndex];
            droppedIcon.transform.SetParent(targetSlot.transform);
            droppedIcon.transform.position = originalPosition;
        }
        public Vector3 GetOriginalPosition() => originalPosition;
    }
}