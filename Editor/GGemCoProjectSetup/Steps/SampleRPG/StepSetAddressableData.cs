using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class StepSetAddressableData : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string msg)
        {
            msg = null;
            return true;
        }
        public override void Execute(EditorSetupContext ctx)
        {
            var addressableEditor = ctx.addressableEditor;
            
            // settings 스크립터블 오브젝트, 테이블은 StepSetDefaultAddressableData 클래스에서 처리
            
            var settingCharacters = new SettingCharacters(addressableEditor);
            settingCharacters.Setup(ctx);
            
            var settingMap = new SettingMap(addressableEditor);
            settingMap.Setup(ctx);
            
            var settingEffect = new SettingEffect(addressableEditor);
            settingEffect.Setup(ctx);
            
            var settingItem = new SettingItem(addressableEditor);
            settingItem.Setup(ctx);
            
            var settingDialogue = new SettingDialogue(addressableEditor);
            settingDialogue.Setup(ctx);
            
            var settingQuest = new SettingQuest(addressableEditor);
            settingQuest.Setup(ctx);
            
            var settingCutscene = new SettingCutscene(addressableEditor);
            settingCutscene.Setup(ctx);
            
            var settingSkill = new SettingSkill(addressableEditor);
            settingSkill.Setup(ctx);
            
            var settingSound = new SettingSound(addressableEditor);
            settingSound.Setup(ctx);
        }
    }
}