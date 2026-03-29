using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CutsceneEvent의 유효성을 검사하는 검증 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// 이벤트 타입에 따라 필수 데이터가 올바르게 설정되었는지 확인합니다.
    /// 검증 실패 시 Unity 콘솔에 에러 로그를 출력합니다.
    /// </remarks>
    internal static class CutsceneTimelineValidationUtility
    {
        /// <summary>
        /// 단일 <see cref="CutsceneEvent"/>의 데이터 유효성을 검사합니다.
        /// </summary>
        /// <param name="cutsceneEvent">검증할 컷신 이벤트입니다.</param>
        /// <param name="error">검증 실패 시 오류 메시지입니다. 성공 시 null입니다.</param>
        /// <returns>유효한 이벤트이면 true, 그렇지 않으면 false를 반환합니다.</returns>
        /// <remarks>
        /// 현재 다음 조건을 검증합니다:
        /// - CharacterMove: 캐릭터 타입이 None이면 실패
        /// - CameraChangeTarget: 캐릭터 타입이 None이면 실패
        /// </remarks>
        public static bool ValidateEvent(CutsceneEvent cutsceneEvent, out string error)
        {
            error = null;

            // CharacterMove 이벤트에서 캐릭터 타입이 설정되지 않은 경우
            if (cutsceneEvent.type == CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            // CameraChangeTarget 이벤트에서 캐릭터 타입이 설정되지 않은 경우
            if (cutsceneEvent.type == CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                cutsceneEvent.cameraChangeTarget.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            return true;
        }
    }
}