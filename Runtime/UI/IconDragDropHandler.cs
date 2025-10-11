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

            var dropped = droppedIcon.GetComponent<UIIcon>();
            var target = targetIcon.GetComponent<UIIcon>();
            if (dropped == null || target == null || _dragDropStrategy == null)
            {
                GoBackToSlot(droppedIcon);
                return;
            }

            _dragDropStrategy.HandleDragInIcon(_window, dropped, target);
            GoBackToSlot(droppedIcon);
        }

        private void GoBackToSlot(GameObject droppedIcon)
        {
            if (droppedIcon == null) return;

            UIIcon icon = droppedIcon.GetComponent<UIIcon>();
            if (icon == null || icon.window == null) return;

            GameObject targetSlot = icon.window.slots[icon.slotIndex];
            droppedIcon.transform.SetParent(targetSlot.transform);
            droppedIcon.transform.position = icon.GetDragOriginalPosition();
            droppedIcon.transform.SetSiblingIndex(1);
        }
    }
}
