#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 생성 메뉴 경로/우선순위 상수. 한 곳에서 일원화.
    /// </summary>
    internal static class MenuPath
    {
        // GGemCoTool 상단 메뉴의 Core 생성 도구 아래에 노출됩니다.
        public const string Root = GGemCoToolMenu.Core + GGemCoToolMenu.Development + "오브젝트 생성툴/";

        // 도메인별 경로
        public const string Trap = Root + "Trap/";
        public const string Projectile = Root + "Projectile/";
        public const string Effect = Root + "Effect/";

        /// <summary>
        /// Creator 하위 메뉴에서 가장 위에 배치할 항목의 우선순위입니다.
        /// </summary>
        /// <remarks>
        /// Core 개발툴 메뉴 기준값에 로컬 순서를 더해 Creator 하위 메뉴 순서를 고정합니다.
        /// </remarks>
        public const int PriorityTop = GGemCoToolMenuPriority.CoreDevelopment + 80;

        /// <summary>
        /// Creator 하위 메뉴에서 일반 항목에 적용할 기본 우선순위입니다.
        /// </summary>
        public const int PriorityNormal = GGemCoToolMenuPriority.CoreDevelopment + 100;

        /// <summary>
        /// 오브젝트 생성 허브 창을 엽니다.
        /// </summary>
        [MenuItem(Root + "Open Creator Hub", false, PriorityTop)]
        private static void OpenHub() => CreatorHubWindow.Open();
    }
}
#endif
