using UnityEditor;

namespace GGemCo2DCoreEditor
{
    public static class TableEditorMenu
    {
        [MenuItem("GGemCo/Tools/Table Editor")]
        public static void Open()
        {
            TableEditorWindow.OpenWindow();
        }
    }
}
