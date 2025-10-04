#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 생성 메뉴 경로/우선순위 상수. 한 곳에서 일원화.
    /// </summary>
    internal static class MenuPath
    {
        // Hierarchy 우클릭/상단 메뉴(GameObject)에 모두 노출됩니다.
        public const string Root = "GameObject/GGemCo/Creator/";

        // 도메인별 경로
        public const string Trap = Root + "Trap/";
        public const string Projectile = Root + "Projectile/";
        public const string Effect = Root + "Effect/";

        // 우선순위(작을수록 위)
        public const int PriorityTop = 10;
        public const int PriorityNormal = 100;

        // 공용 유틸
        [MenuItem(Root + "Open Creator Hub", false, PriorityTop)]
        private static void OpenHub() => CreatorHubWindow.Open();
    }
}
#endif