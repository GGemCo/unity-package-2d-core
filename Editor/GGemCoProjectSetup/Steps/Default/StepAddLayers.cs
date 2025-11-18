#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// ConfigLayer.GetValues()를 기준으로 사용자 레이어(8~31)에 추가합니다.
    /// - 이미 존재하는 레이어 이름은 스킵
    /// - 빈 슬롯이 없으면 경고 출력
    /// </summary>
    public sealed class StepAddLayers : SetupStepBase
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (objs == null || objs.Length == 0)
            {
                message = $"TagManager.asset을 찾을 수 없습니다: {TagManagerPath}";
                return false;
            }
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingLayers = new SettingLayers();
            settingLayers.AddLayers(ctx);
        }
    }
}
#endif
