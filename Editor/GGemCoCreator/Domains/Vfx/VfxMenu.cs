// Packages/com.ggemco.core/Editor/GGemCoCreator/Domains/Vfx/Menu.Vfx.cs
#if UNITY_EDITOR && GGEMCO_USE_VFX
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    internal static class VfxMenu
    {
        [MenuItem(MenuPath.Vfx + "Default Vfx", false, MenuPath.PriorityTop)]
        private static void CreateDefault(MenuCommand cmd) => VfxFactory.CreateDefault(cmd);
    }
}
#endif