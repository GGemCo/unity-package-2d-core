using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    public class IconDragDropHandler
    {
        private readonly UIWindow _window;
        private IDragDropStrategy _dragDropStrategy;

        public IconDragDropHandler(UIWindow window)
        {
            _window = window;
        }

        public void SetStrategy(IDragDropStrategy strategy)
        {
            _dragDropStrategy = strategy;
        }

        public void HandleDragOut(PointerEventData eventData, GameObject droppedIcon, GameObject targetIcon, Vector3 originalPosition)
        {
            if (droppedIcon == null) return;

            Vector3 worldPosition = _window.SceneGame.mainCamera.ScreenToWorldPoint(
                new Vector3(eventData.position.x, eventData.position.y, _window.SceneGame.mainCamera.nearClipPlane));

            _dragDropStrategy.HandleDragOut(_window, worldPosition, droppedIcon, targetIcon, originalPosition);
            GoBackToSlot(droppedIcon);
        }

        public void HandleDragInIcon(GameObject droppedIcon, GameObject targetIcon)
        {
            if (droppedIcon == null || targetIcon == null) return;

            var dropped = droppedIcon.GetComponentInParent<UIIcon>();
            var target = targetIcon.GetComponentInParent<UIIcon>();
            if (dropped == null || target == null || _dragDropStrategy == null)
            {
                GoBackToSlot(droppedIcon);
                return;
            }
            
            // 순서 중요. 먼저 되돌린다. 보임, 안보임 처리는 다음 함수에서 처리
            GoBackToSlot(droppedIcon);
            // 아니면 HandleDragInIcon 여기서 return 받고 처리
            _dragDropStrategy.HandleDragInIcon(_window, dropped, target);
        }

        public void HandleDragInWindow(GameObject droppedIcon)
        {
            if (droppedIcon == null) return;

            var dropped = droppedIcon.GetComponent<UIIcon>();
            if (dropped == null || _dragDropStrategy == null)
            {
                GoBackToSlot(droppedIcon);
                return;
            }

            GoBackToSlot(droppedIcon);
            _dragDropStrategy.HandleDragInWindow(_window, dropped);
        }

        private void GoBackToSlot(GameObject droppedIcon)
        {
            if (droppedIcon == null) return;

            UIIcon icon = droppedIcon.GetComponent<UIIcon>();
            if (icon == null || icon.window == null) return;

            UISlot targetSlot = icon.window.GetSlotByIndex(icon.slotIndex);
            droppedIcon.transform.SetParent(targetSlot.gameObject.transform);
            droppedIcon.transform.position = icon.GetDragOriginalPosition();
            droppedIcon.transform.SetSiblingIndex(1);
        }
    }
}
