#if UNITY_EDITOR

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// ConfigLayer.GetValues()를 기준으로 사용자 레이어(8~31)에 추가합니다.
    /// - 이미 존재하는 레이어 이름은 스킵
    /// - 빈 슬롯이 없으면 경고 출력
    /// </summary>
    public sealed class StepCreateSettingScriptableObject : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingScriptableObject = new SettingGGemCo();
            settingScriptableObject.CreateSettings(ctx);
        }
    }
}
#endif
