using UnityEngine;
using UnityEngine.EventSystems;

namespace GGemCo.Scripts.UI
{
    public class IconDragDropHandler
    {
        private readonly UIWindow window;
        private readonly Camera mainCamera;
        private IDragDropStrategy dragDropStrategy;

        public IconDragDropHandler(UIWindow window)
        {
            this.window = window;
            if (SceneGame.Instance == null) return;
            mainCamera = SceneGame.Instance.mainCamera;
        }

        public void SetStrategy(IDragDropStrategy strategy)
        {
            dragDropStrategy = strategy;
        }

        public void HandleDragOut(PointerEventData eventData, GameObject droppedIcon, GameObject targetIcon, Vector3 originalPosition)
        {
            if (droppedIcon == null) return;

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
                new Vector3(eventData.position.x, eventData.position.y, mainCamera.nearClipPlane));

            dragDropStrategy.HandleDragOut(window, worldPosition, droppedIcon, targetIcon, originalPosition);
            GoBackToSlot(droppedIcon);
        }

        public void HandleDragInIcon(GameObject droppedIcon, GameObject targetIcon)
        {
            if (droppedIcon == null || targetIcon == null) return;

            var dropped = droppedIcon.GetComponent<UIIcon>();
            var target = targetIcon.GetComponent<UIIcon>();
            if (dropped == null || target == null || dragDropStrategy == null)
            {
                GoBackToSlot(droppedIcon);
                return;
            }

            dragDropStrategy.HandleDragInIcon(window, dropped, target);
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
