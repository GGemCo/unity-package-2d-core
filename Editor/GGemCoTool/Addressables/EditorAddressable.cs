using UnityEditor;

namespace GGemCo.Editor
{
    public class EditorAddressable : EditorWindow
    {
        private readonly SettingScriptableObject _settingScriptableObject = new SettingScriptableObject();
        private readonly SettingTable _settingTable = new SettingTable();
        private readonly SettingCharacter _settingCharacter = new SettingCharacter();
        private readonly SettingMap _settingMap = new SettingMap();

        [MenuItem(ConfigEditor.NameToolAddressableSetting, false, (int)ConfigEditor.ToolOrdering.SettingAddressable)]
        public static void ShowWindow()
        {
            GetWindow<EditorAddressable>("Addressable 셋팅하기");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            _settingScriptableObject.OnGUI();
            EditorGUILayout.Space(10);
            _settingTable.OnGUI();
            EditorGUILayout.Space(10);
            _settingMap.OnGUI();
            EditorGUILayout.Space(10);
            _settingCharacter.OnGUI();
        }
    }
}