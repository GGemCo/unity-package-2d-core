// Packages/com.ggemco.core/Editor/GGemCoCreator/Domains/Trap/Menu.Trap.cs
#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>Trap 생성 메뉴</summary>
    internal static class TrapMenu
    {
        /// <summary>
        /// Animator 기반 고정형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Fixed (Animator)", false, MenuPath.PriorityNormal)]
        private static void CreateFixed(MenuCommand cmd) => TrapFactory.CreateFixed(cmd);

        /// <summary>
        /// Trigger Collider가 포함된 Animator 기반 고정형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Fixed (Animator) + Trigger", false, MenuPath.PriorityNormal + 1)]
        private static void CreateFixedWithTrigger(MenuCommand cmd) => TrapFactory.CreateFixed(cmd, true);

        /// <summary>
        /// Trigger Collider가 포함된 타이머형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Timer (Animator) + Trigger", false, MenuPath.PriorityNormal + 2)]
        private static void CreateTimerWithTrigger(MenuCommand cmd) => TrapFactory.CreateTimer(cmd, true);

        /// <summary>
        /// 무한 반복형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Infinity (Animator)", false, MenuPath.PriorityNormal + 3)]
        private static void CreateInfinityWithTrigger(MenuCommand cmd) => TrapFactory.CreateInfinity(cmd);

        /// <summary>
        /// Animator 기반 이동형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Move (Animator)", false, MenuPath.PriorityNormal + 4)]
        private static void CreateMove(MenuCommand cmd) => TrapFactory.CreateMove(cmd);

        /// <summary>
        /// Trigger Collider가 포함된 Animator 기반 이동형 Trap을 생성합니다.
        /// </summary>
        /// <param name="cmd">Unity 메뉴 명령 컨텍스트입니다.</param>
        [MenuItem(MenuPath.Trap + "Trap Move (Animator) + Trigger", false, MenuPath.PriorityNormal + 5)]
        private static void CreateMoveWithTrigger(MenuCommand cmd) => TrapFactory.CreateMove(cmd, true);
    }
}
#endif
