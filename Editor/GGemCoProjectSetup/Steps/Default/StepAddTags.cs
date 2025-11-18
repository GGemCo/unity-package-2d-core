#if UNITY_EDITOR
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// ConfigTags.GetValues()를 기준으로 태그를 추가합니다.
    /// - 이미 존재하는 태그는 스킵
    /// - 결과는 EditorSetupLogger로 요약 출력
    /// </summary>
    public sealed class StepAddTags : SetupStepBase
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

            // ConfigTags가 비어 있어도 실행 자체는 가능하므로 통과
            message = null;
            return true;
        }

        public override void Execute(EditorSetupContext ctx)
        {
            var settingTags = new SettingTags();
            settingTags.AddTags(ctx);
        }
    }
}
#endif
