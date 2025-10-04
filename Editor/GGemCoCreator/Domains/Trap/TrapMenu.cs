// Packages/com.ggemco.core/Editor/GGemCoCreator/Domains/Trap/Menu.Trap.cs
#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>Trap 생성 메뉴</summary>
    internal static class TrapMenu
    {
        [MenuItem(MenuPath.Trap + "Trap Fixed (Animator)", false, 100)]
        private static void CreateFixed(MenuCommand cmd) => TrapFactory.CreateFixed(cmd);
        
        [MenuItem(MenuPath.Trap + "Trap Fixed (Animator) + Trigger", false, 101)]
        private static void CreateFixedWithTrigger(MenuCommand cmd) => TrapFactory.CreateFixed(cmd, true);

        [MenuItem(MenuPath.Trap + "Trap Timer (Animator) + Trigger", false, 102)]
        private static void CreateTimerWithTrigger(MenuCommand cmd) => TrapFactory.CreateTimer(cmd, true);

        [MenuItem(MenuPath.Trap + "Trap Infinity (Animator)", false, 103)]
        private static void CreateInfinityWithTrigger(MenuCommand cmd) => TrapFactory.CreateInfinity(cmd);

        [MenuItem(MenuPath.Trap + "Trap Move (Animator)", false, 104)]
        private static void CreateMove(MenuCommand cmd) => TrapFactory.CreateMove(cmd);
        [MenuItem(MenuPath.Trap + "Trap Move (Animator) + Trigger", false, 105)]
        private static void CreateMoveWithTrigger(MenuCommand cmd) => TrapFactory.CreateMove(cmd, true);
    }
}
#endif