using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public interface IDragDropStrategy
    {
        void HandleDragInIcon(UIWindow window, UIIcon dropped, UIIcon target);
        // 선택적 메서드: 기본 구현 제공
        void HandleDragInWindow(UIWindow window, UIIcon dropped)
        {
            // 기본 구현: 아무 작업도 하지 않음
        }
        void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon, Vector3 originalPosition);
    }
}