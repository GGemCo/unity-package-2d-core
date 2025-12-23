#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCore
{
    public static class UnityEditorHelper
    {
        private static bool _isExitingPlayMode;

        public static bool IsExitingPlayMode => _isExitingPlayMode;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            _isExitingPlayMode = state == PlayModeStateChange.ExitingPlayMode;
        }
    }
}
#endif