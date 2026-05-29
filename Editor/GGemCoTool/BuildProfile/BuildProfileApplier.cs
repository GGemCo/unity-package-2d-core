using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터 Build Profile 선택값을 Settings 프로파일과 Unity 빌드 옵션에 반영합니다.
    /// </summary>
    public static class BuildProfileApplier
    {
        /// <summary>
        /// 지정한 빌드 모드를 현재 에디터 환경에 적용합니다.
        /// 모드 전환은 Settings 프로파일과 Unity Development Build 옵션만 변경하며,
        /// Scripting Define Symbol은 재컴파일 비용을 줄이기 위해 자동으로 변경하지 않습니다.
        /// </summary>
        /// <param name="mode">적용할 빌드 모드입니다.</param>
        public static void Apply(GGemCoBuildMode mode)
        {
            BuildProfileEditorPrefs.CurrentMode = mode;
            SettingsProfileEditorPrefs.CurrentProfile = ResolveSettingsProfile(mode);

            // Unity의 실제 Player 빌드 옵션도 현재 프로파일과 같은 방향으로 맞춥니다.
            // ReleaseSimulation은 에디터 테스트용이므로 실제 빌드 옵션은 Release와 동일하게 둡니다.
            EditorUserBuildSettings.development = mode == GGemCoBuildMode.Development;
        }

        /// <summary>
        /// 치트 도구 컴파일 심볼 사용 여부를 명시적으로 변경합니다.
        /// Development와 Release Simulation을 반복 전환할 때 재컴파일이 발생하지 않도록,
        /// 이 함수는 모드 전환 중 자동 호출하지 않고 사용자가 버튼을 눌렀을 때만 호출합니다.
        /// </summary>
        /// <param name="enabled">치트 도구 코드를 컴파일에 포함하려면 true입니다.</param>
        /// <returns>현재 빌드 타겟의 실제 심볼 목록이 변경되었으면 true입니다.</returns>
        public static bool SetCheatToolsEnabled(bool enabled)
        {
            BuildProfileEditorPrefs.CheatToolsEnabled = enabled;
            return BuildProfileScriptingDefineUtility.SetCheatToolsEnabledForActiveTarget(enabled);
        }

        /// <summary>
        /// 실제 Release 빌드 후보 상태를 준비합니다.
        /// 서비스용 Settings와 Release 빌드 옵션을 적용하고, 릴리즈 빌드에서 금지되는 치트 도구 컴파일 심볼을 제거합니다.
        /// </summary>
        public static void PrepareReleaseBuild()
        {
            BuildProfileEditorPrefs.CurrentMode = GGemCoBuildMode.Release;
            SettingsProfileEditorPrefs.CurrentProfile = SettingsProfileKind.Service;
            EditorUserBuildSettings.development = false;
            SetCheatToolsEnabled(false);
        }

        /// <summary>
        /// 빌드 모드에 대응되는 Settings 프로파일을 반환합니다.
        /// </summary>
        /// <param name="mode">빌드 모드입니다.</param>
        /// <returns>해당 모드에서 사용할 Settings 프로파일입니다.</returns>
        public static SettingsProfileKind ResolveSettingsProfile(GGemCoBuildMode mode)
        {
            return mode == GGemCoBuildMode.Development
                ? SettingsProfileKind.Development
                : SettingsProfileKind.Service;
        }
    }
}
