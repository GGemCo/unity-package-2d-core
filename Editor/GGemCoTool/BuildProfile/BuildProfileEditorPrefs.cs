using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터에서 선택한 GGemCo 빌드 프로파일 값을 EditorPrefs에 저장합니다.
    /// </summary>
    public static class BuildProfileEditorPrefs
    {
        private const string BuildModeKey = ConfigDefine.NameSDK + ".BuildProfile.Mode";

        /// <summary>
        /// 현재 에디터에서 선택한 빌드 모드를 가져오거나 저장합니다.
        /// </summary>
        public static GGemCoBuildMode CurrentMode
        {
            get => (GGemCoBuildMode)EditorPrefs.GetInt(BuildModeKey, (int)GGemCoBuildMode.Development);
            set => EditorPrefs.SetInt(BuildModeKey, (int)value);
        }
    }
}
