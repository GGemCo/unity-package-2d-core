using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 윈도우 아이콘 생성 하기
    /// </summary>
    public interface ISlotIconBuildStrategy
    {
        void BuildSlotsAndIcons(UIWindow window, GridLayoutGroup container, int maxCount, IconConstants.Type iconType, Vector2 slotSize, Vector2 iconSize, GameObject[] slots, GameObject[] icons);
    }
    
}