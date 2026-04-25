namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리 아이템을 퀵슬롯에 "복사 등록"하는 전략입니다.
    /// 퀵슬롯은 원본 인벤토리 아이템을 소비하지 않고 참조 정보만 저장합니다.
    /// </summary>
    public class DragDropStrategyQuickSlotFromInventory : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            if (window == null || droppedUIIcon == null || targetUIIcon == null)
                return;

            if (droppedUIIcon.uid <= 0)
                return;

            // 퀵슬롯 등록은 swap 이 아니라 "대상 슬롯 덮어쓰기"이므로
            // 공통 1차 검증(target slot acceptance)만 통과하면 그대로 저장합니다.
            window.SetIconCount(
                targetUIIcon.slotIndex,
                droppedUIIcon.uid,
                droppedUIIcon.GetCount(),
                droppedUIIcon.GetLevel(),
                droppedUIIcon.IsLearn(),
                droppedUIIcon.instanceId,
                droppedUIIcon.GetIconType());
        }

        public void HandleDragOut(UIWindow window, UnityEngine.Vector3 worldPosition, UnityEngine.GameObject droppedIcon,
            UnityEngine.GameObject targetIcon, UnityEngine.Vector3 originalPosition)
        {
        }
    }
}
