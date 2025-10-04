// Packages/com.ggemco.core/Editor/GGemCoCreator/Domains/Projectile/Menu.Projectile.cs
#if UNITY_EDITOR && GGEMCO_USE_EFFECT
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    internal static class ProjectileMenu
    {
        [MenuItem(MenuPath.Projectile + "Default Projectile", false, MenuPath.PriorityTop)]
        private static void CreateDefault(MenuCommand cmd) => ProjectileFactory.CreateDefault(cmd);
    }
}
#endif