using UnityEditor;

namespace GGemCo.Editor
{
    public class DefaultSettinsTool : EditorWindow
    {
        private readonly SettingGGemCo settingGGemCo = new SettingGGemCo();
        private readonly SettingTags settingTags = new SettingTags();
        private readonly SettingSortingLayers settingSortingLayers = new SettingSortingLayers();
        private readonly SettingLayers settingLayers = new SettingLayers();
        private readonly SettingAddressable settingAddressable = new SettingAddressable();

        [MenuItem(ConfigEditor.NameToolDefaultSetting, false, 1)]
        public static void ShowWindow()
        {
            GetWindow<DefaultSettinsTool>("기본 셋팅하기");
        }

        private void OnGUI()
        {
            settingGGemCo.OnGUI();
            EditorGUILayout.Space(10);
            settingTags.OnGUI();
            EditorGUILayout.Space(10);
            settingSortingLayers.OnGUI();
            EditorGUILayout.Space(10);
            settingLayers.OnGUI();
            EditorGUILayout.Space(10);
            settingAddressable.OnGUI();
        }
    }
}