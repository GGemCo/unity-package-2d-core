#if UNITY_EDITOR

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 기본 씬(PreIntro/Loading/Intro/Game)을 생성하고 SceneCatalog와 Build Settings에 반영한다.
    /// - 이미 존재하면 생성 스킵, 카탈로그/빌드세팅만 동기화
    /// - 생성 경로는 기본값 제공 + 필요시 커스터마이즈 가능
    /// </summary>
    public sealed class StepCreateDefaultScenes : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingDefaultScene = new SettingDefaultScene();
            settingDefaultScene.CreateDefaultScene(ctx);
        }
    }
}
#endif