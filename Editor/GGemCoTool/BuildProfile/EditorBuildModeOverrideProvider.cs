using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 에디터에서 선택한 Build Profile을 Core Runtime의 빌드 모드 게이트에 제공하는 공급자입니다.
    /// </summary>
    public sealed class EditorBuildModeOverrideProvider : IBuildModeOverrideProvider
    {
        /// <summary>
        /// EditorPrefs에 저장된 현재 빌드 모드를 반환합니다.
        /// </summary>
        /// <param name="mode">현재 선택된 빌드 모드입니다.</param>
        /// <returns>에디터 설정에서 값을 정상적으로 읽었으면 true입니다.</returns>
        public bool TryGetMode(out GGemCoBuildMode mode)
        {
            mode = BuildProfileEditorPrefs.CurrentMode;
            return true;
        }
    }
}
