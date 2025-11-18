using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 예제 RPG 설정으로 settings 스크립터블 오브젝트 설정
    /// </summary>
    public class StepSetSettingScriptableObject : SetupStepBase
    {
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// 프로젝트 내에서 특정 타입의 ScriptableObject 에셋을 하나 찾아 반환한다.
        /// 여러 개일 경우 첫 번째 것을 사용한다.
        /// </summary>
        private static T FindSettingsAsset<T>() where T : ScriptableObject
        {
            // 타입 이름으로 검색: "t:GGemCoSettings" 같은 형태
            string typeName = typeof(T).Name;
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}");
            if (guids == null || guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        public override void Execute(EditorSetupContext ctx)
        {
            // GGemCoSettings
            var ggemCoSettings = FindSettingsAsset<GGemCoSettings>();
            if (ggemCoSettings == null)
            {
                HelperLog.Warn($"[StepSetSettingScriptableObject] GGemCoSettings asset not found.", ctx);
            }
            else
            {
                ggemCoSettings.useSpine2d = true;
                ggemCoSettings.inputSystemType = InputSystemType.Both;

                var settingGGemCoInspector = ScriptableObject.CreateInstance<SettingGGemCoInspector>();
                settingGGemCoInspector.UpdateScriptingDefineSymbols(ggemCoSettings.useSpine2d);
                settingGGemCoInspector.SyncInputDefineSymbols(ggemCoSettings.inputSystemType);

                EditorUtility.SetDirty(ggemCoSettings);
            }
            
            // GGemCoPlayerSettings
            var playerSettings = FindSettingsAsset<GGemCoPlayerSettings>();
            if (playerSettings == null)
            {
                HelperLog.Warn($"[StepSetSettingScriptableObject] GGemCoPlayerSettings asset not found.", ctx);
            }
            else
            {
                playerSettings.facingDirection8 = CharacterConstants.FacingDirection8.Left;
                playerSettings.animationController = ConfigCommon.AnimationController.Spine;
                playerSettings.startScale = 0.3f;
                playerSettings.maxLevel = 10;
                playerSettings.size = new Vector2(128, 128);
                playerSettings.statHp = 10000;
                playerSettings.statMp = 10000;

                EditorUtility.SetDirty(playerSettings);
            }
            
            // GGemCoSaveSettings
            var saveSettings = FindSettingsAsset<GGemCoSaveSettings>();
            if (saveSettings == null)
            {
                HelperLog.Warn($"[StepSetSettingScriptableObject] GGemCoSaveSettings asset not found.", ctx);
            }
            else
            {
                saveSettings.useSaveData = true;

                EditorUtility.SetDirty(saveSettings);
            }
            
            // GGemCoMapSettings
            var mapSettings = FindSettingsAsset<GGemCoMapSettings>();
            if (mapSettings == null)
            {
                HelperLog.Warn($"[StepSetSettingScriptableObject] GGemCoMapSettings asset not found.", ctx);
            }
            else
            {
                mapSettings.tilemapGridCellSize = new Vector2(64, 64);
                mapSettings.startMapUid = 101;

                EditorUtility.SetDirty(mapSettings);
            }

            // 변경된 에셋 저장
            AssetDatabase.SaveAssets();

            HelperLog.Warn($"[StepSetSettingScriptableObject] Settings ScriptableObjects have been configured for sample RPG.", ctx);
        }
    }
}