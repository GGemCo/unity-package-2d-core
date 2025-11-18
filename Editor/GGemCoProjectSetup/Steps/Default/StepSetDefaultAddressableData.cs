using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class StepSetDefaultAddressableData : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            msg = null;
            return true;
        }
        public override void Execute(EditorSetupContext ctx)
        {
            var addressableEditor = ScriptableObject.CreateInstance<AddressableEditor>();
            // settings 스크립터블 오브젝트
            var settingScriptableObject = new SettingScriptableObject(addressableEditor);
            settingScriptableObject.Setup(ctx);
            
            // 테이블
            var settingTable = new SettingTable(addressableEditor);
            settingTable.ClearGroup(ctx);
            settingTable.Setup(ctx);
        }
    }
}