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
        private const string CheatToolsEnabledKey = ConfigDefine.NameSDK + ".BuildProfile.CheatToolsEnabled";

        /// <summary>
        /// 현재 에디터에서 선택한 빌드 모드를 가져오거나 저장합니다.
        /// </summary>
        public static GGemCoBuildMode CurrentMode
        {
            get => (GGemCoBuildMode)EditorPrefs.GetInt(BuildModeKey, (int)GGemCoBuildMode.Development);
            set => EditorPrefs.SetInt(BuildModeKey, (int)value);
        }

        /// <summary>
        /// 치트 도구 컴파일 심볼을 사용자가 명시적으로 활성화했는지 여부를 가져오거나 저장합니다.
        /// 모드 전환 중에는 이 값을 기준으로 심볼을 자동 변경하지 않으며, Release 빌드 준비 단계에서만 제거합니다.
        /// </summary>
        public static bool CheatToolsEnabled
        {
            get => EditorPrefs.GetBool(CheatToolsEnabledKey, false);
            set => EditorPrefs.SetBool(CheatToolsEnabledKey, value);
        }
    }
}
