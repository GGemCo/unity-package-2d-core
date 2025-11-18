namespace GGemCo2DCoreEditor
{
    public class StepCreateLocalizationSetting : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingLocalization = new SettingLocalization();
            settingLocalization.CreateLocalizationSetting(ctx);
        }
    }
}