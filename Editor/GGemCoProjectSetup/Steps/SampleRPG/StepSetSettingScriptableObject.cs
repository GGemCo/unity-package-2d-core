using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 샘플 RPG 프로젝트 기준의 기본 설정 값을 각종 Settings ScriptableObject 에셋에 적용하는 설정 스텝입니다.
    /// 대상 에셋이 존재하지 않으면 경고 로그를 남기고 해당 항목은 건너뜁니다.
    /// </summary>
    /// <remarks>
    /// NOTE:
    /// - 이 스텝은 프로젝트 내에 존재하는 Settings 에셋을 검색(FindSettingsAsset)하여 값을 덮어씁니다.
    /// - 값 변경 후 EditorUtility.SetDirty로 변경 표시를 하고, 마지막에 AssetDatabase.SaveAssets로 저장합니다.
    /// </remarks>
    public class StepSetSettingScriptableObject : SetupStepBase
    {
        /// <summary>
        /// 설정 적용 전에 사전 조건을 검증합니다.
        /// 현재 구현은 항상 통과하며, 필요 시 Settings 에셋 존재 여부 등을 여기서 확인할 수 있습니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        /// <param name="message">검증 실패 시 사용자에게 표시할 메시지</param>
        /// <returns>검증이 통과되면 true, 실패하면 false</returns>
        public override bool Validate(EditorSetupContext ctx, out string message)
        {
            message = null;
            return true;
        }

        /// <summary>
        /// 샘플 RPG 기준의 기본 설정 값을 Settings ScriptableObject 들에 적용하고 저장합니다.
        /// </summary>
        /// <param name="ctx">에디터 설정 전체에서 공유되는 컨텍스트 객체</param>
        public override void Execute(EditorSetupContext ctx)
        {
            // GGemCoSettings: Spine2D 사용 및 Input System define 동기화
            var ggemCoSettings = FindSettingsAsset<GGemCoSettings>();
            if (ggemCoSettings == null)
            {
                HelperLog.Warn($"[{nameof(StepSetSceneRequireObject)}] GGemCoSettings asset not found.", ctx);
            }
            else
            {
                // 샘플 RPG 기본값 적용
                ggemCoSettings.useSpine2d = true;
#if UNITY_6000_0_OR_NEWER
                ggemCoSettings.inputSystemType = InputSystemType.NewInputSystem;
#else
                ggemCoSettings.inputSystemType = InputSystemType.OldInputManager;
#endif

                // 스크립팅 디파인 심볼을 설정 값과 동기화
                var settingGGemCoInspector = ScriptableObject.CreateInstance<SettingGGemCoInspector>();
                settingGGemCoInspector.UpdateScriptingDefineSymbols(ggemCoSettings.useSpine2d);
                settingGGemCoInspector.SyncInputDefineSymbols(ggemCoSettings.inputSystemType);

                // 에셋 변경 표시(저장은 마지막 SaveAssets에서 수행)
                EditorUtility.SetDirty(ggemCoSettings);
            }

            // GGemCoPlayerSettings: 플레이어 초기 스탯/크기/애니메이션 컨트롤러 등 샘플값 적용
            var playerSettings = FindSettingsAsset<GGemCoPlayerSettings>();
            if (playerSettings == null)
            {
                HelperLog.Warn($"[{nameof(StepSetSceneRequireObject)}] GGemCoPlayerSettings asset not found.", ctx);
            }
            else
            {
                playerSettings.facingDirection8 = CharacterConstants.FacingDirection8.Left;
                playerSettings.animationController = ConfigCommon.AnimationController.Spine;
                playerSettings.startScale = 0.3f;
                playerSettings.maxLevel = 10;
                playerSettings.size = new Vector2(128, 128);
                playerSettings.stats.hp = 10000;
                playerSettings.stats.mp = 10000;

                if (playerSettings.elementGaugeRules == null || playerSettings.elementGaugeRules.Count == 0)
                {
                    playerSettings.elementGaugeRules = ElementGaugeRuleDefinition.CreateDefaultPlayerRules();
                }

                EditorUtility.SetDirty(playerSettings);
            }

            // GGemCoSaveSettings: 저장 데이터 사용 여부 기본값 적용
            var saveSettings = FindSettingsAsset<GGemCoSaveSettings>();
            if (saveSettings == null)
            {
                HelperLog.Warn($"[{nameof(StepSetSceneRequireObject)}] GGemCoSaveSettings asset not found.", ctx);
            }
            else
            {
                saveSettings.useSaveData = true;

                EditorUtility.SetDirty(saveSettings);
            }

            // GGemCoMapSettings: 타일맵 그리드/시작 맵 UID 등 기본값 적용
            var mapSettings = FindSettingsAsset<GGemCoMapSettings>();
            if (mapSettings == null)
            {
                HelperLog.Warn($"[{nameof(StepSetSceneRequireObject)}] GGemCoMapSettings asset not found.", ctx);
            }
            else
            {
                mapSettings.tilemapGridCellSize = new Vector2(64, 64);
                mapSettings.startMapUid = 101;

                EditorUtility.SetDirty(mapSettings);
            }

            HelperLog.Info($"[{nameof(StepSetSceneRequireObject)}] Settings ScriptableObjects have been configured for sample RPG.", ctx);
        }
    }
}
