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
