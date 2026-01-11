using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public class DefaultSettingTool : DefaultEditorWindow
    {
        private readonly SettingGGemCo _settingGGemCo = new SettingGGemCo();
        private readonly SettingTags _settingTags = new SettingTags();
        private readonly SettingSortingLayers _settingSortingLayers = new SettingSortingLayers();
        private readonly SettingLayers _settingLayers = new SettingLayers();
        private readonly SettingResource _settingResource = new SettingResource();
        private readonly SettingDefaultScene _settingDefaultScene = new SettingDefaultScene();

        [MenuItem(ConfigEditor.NameToolSettingDefault, false, (int)ConfigEditor.ToolOrdering.DefaultSetting)]
        public static void ShowWindow()
        {
            GetWindow<DefaultSettingTool>("기본 셋팅하기");
        }

        private void OnGUI()
        {
            _settingGGemCo.OnGUI();
            EditorGUILayout.Space(10);
            _settingTags.OnGUI();
            EditorGUILayout.Space(10);
            _settingSortingLayers.OnGUI();
            EditorGUILayout.Space(10);
            _settingLayers.OnGUI();
            EditorGUILayout.Space(10);
            _settingResource.OnGUI();
            EditorGUILayout.Space(10);
            _settingDefaultScene.OnGUI();
            
            EditorGUILayout.Space(20);
        }
    }
}