using UnityEngine;

namespace GGemCo.Scripts.UI
{
    /// <summary>
    /// 윈도우 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public interface IDragDropStrategy
    {
        void HandleDragInIcon(UIWindow window, UIIcon dropped, UIIcon target);
        void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon, Vector3 originalPosition);
    }
}