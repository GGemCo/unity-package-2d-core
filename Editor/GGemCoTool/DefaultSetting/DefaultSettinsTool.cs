using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class DefaultSettinsTool : DefaultEditorWindow
    {
        private readonly SettingGGemCo _settingGGemCo = new SettingGGemCo();
        private readonly SettingTags _settingTags = new SettingTags();
        private readonly SettingSortingLayers _settingSortingLayers = new SettingSortingLayers();
        private readonly SettingLayers _settingLayers = new SettingLayers();

        [MenuItem(ConfigEditor.NameToolSettingDefault, false, (int)ConfigEditor.ToolOrdering.DefaultSetting)]
        public static void ShowWindow()
        {
            GetWindow<DefaultSettinsTool>("기본 셋팅하기");
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
            Common.OnGUITitle("Scene 추가하기");
            if (GUILayout.Button("Scene 추가하기"))
            {
                string path = $"Assets/GGemCo/Scenes/{ConfigDefine.SceneNameIntro}.unity";
                AddSceneToBuildSettings(path);
                path = $"Assets/GGemCo/Scenes/{ConfigDefine.SceneNameLoading}.unity";
                AddSceneToBuildSettings(path);
                path = $"Assets/GGemCo/Scenes/{ConfigDefine.SceneNameGame}.unity";
                AddSceneToBuildSettings(path);
            }
        }
    }
}