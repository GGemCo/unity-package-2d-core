#if UNITY_EDITOR
using UnityEditor;


namespace GGemCo.Scripts
{
    public static class UnityEditorHelper
    {
        private static bool _isExitingPlayMode = false;

        [InitializeOnLoadMethod]
        static void Setup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    _isExitingPlayMode = true;
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    _isExitingPlayMode = false;
                }
            };
        }
        public static bool GetIsExitingPlayMode() => _isExitingPlayMode;
    }
}
#endif