// Packages/com.ggemco.core/Editor/GGemCoCreator/Domains/Effect/Menu.Effect.cs
#if UNITY_EDITOR && GGEMCO_USE_EFFECT
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    internal static class EffectMenu
    {
        [MenuItem(MenuPath.Effect + "Default Effect", false, MenuPath.PriorityTop)]
        private static void CreateDefault(MenuCommand cmd) => EffectFactory.CreateDefault(cmd);
    }
}
#endif