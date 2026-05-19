using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="CutsceneEvent"/>의 필수 데이터 유효성을 검증하는 유틸리티입니다.
    /// </summary>
    internal static class CutsceneTimelineValidationUtility
    {
        /// <summary>
        /// 단일 컷신 이벤트의 필수 설정값을 검증합니다.
        /// </summary>
        /// <param name="cutsceneEvent">검증할 컷신 이벤트입니다.</param>
        /// <param name="error">검증 실패 시 에러 메시지입니다. 성공 시 <see langword="null"/>입니다.</param>
        /// <returns>유효한 이벤트이면 <see langword="true"/>를 반환합니다.</returns>
        public static bool ValidateEvent(CutsceneEvent cutsceneEvent, out string error)
        {
            error = null;

            if (cutsceneEvent == null)
            {
                error = "CutsceneEvent가 null입니다.";
                Debug.LogError(error);
                return false;
            }

            // CharacterMove 이벤트는 대상 캐릭터 타입이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 설정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            // CameraChangeTarget 이벤트는 대상 캐릭터 타입이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                cutsceneEvent.cameraChangeTarget.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 설정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            // CharacterFade 이벤트는 RuntimeOverride 또는 Fixed 대상이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterFade &&
                cutsceneEvent.characterFade != null)
            {
                var fadeData = cutsceneEvent.characterFade;
                bool hasRuntimeTarget = fadeData.target != null &&
                                        fadeData.target.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride &&
                                        fadeData.target.runtimeTargetKey != CutsceneKeyCharacterTarget.None;
                bool hasFixedTarget = fadeData.target != null &&
                                      fadeData.target.sourceMode == CutsceneCharacterTargetSourceMode.Fixed &&
                                      fadeData.target.characterType != CharacterConstants.Type.None;
                bool hasLegacyTarget = fadeData.characterType != CharacterConstants.Type.None;

                if (!hasRuntimeTarget && !hasFixedTarget && !hasLegacyTarget)
                {
                    error = $"type: {cutsceneEvent.type} / CharacterFade 대상 캐릭터를 설정하지 않았습니다.";
                    Debug.LogError(error);
                    return false;
                }
            }

            return true;
        }
    }
}
