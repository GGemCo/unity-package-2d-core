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
        /// <param name="error">검증 실패 시 오류 메시지입니다. 성공 시 <see langword="null"/>입니다.</param>
        /// <returns>유효한 이벤트면 <see langword="true"/>를 반환합니다.</returns>
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

            // CharacterTweenMove 이벤트는 대상 캐릭터 타입이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterTweenMove &&
                cutsceneEvent.characterTweenMove != null &&
                cutsceneEvent.characterTweenMove.characterType == CharacterConstants.Type.None)
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

            if (cutsceneEvent.type == CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                !IsFinite(cutsceneEvent.cameraChangeTarget.offset))
            {
                error = $"type: {cutsceneEvent.type} / Camera Change Target Offset에 NaN 또는 무한대를 사용할 수 없습니다.";
                Debug.LogError(error);
                return false;
            }

            // CharacterFade 이벤트는 RuntimeOverride 또는 Fixed 대상이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterFade &&
                cutsceneEvent.characterFade != null)
            {
                CharacterFadeData fadeData = cutsceneEvent.characterFade;
                if (!HasValidCharacterTarget(fadeData.target, fadeData.characterType))
                {
                    error = $"type: {cutsceneEvent.type} / CharacterFade 대상 캐릭터를 설정하지 않았습니다.";
                    Debug.LogError(error);
                    return false;
                }
            }

            // CharacterAirborne 이벤트는 RuntimeOverride 또는 Fixed 대상이 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterAirborne &&
                cutsceneEvent.characterAirborne != null)
            {
                CharacterAirborneData airborneData = cutsceneEvent.characterAirborne;
                if (!HasValidCharacterTarget(airborneData.target, airborneData.characterType))
                {
                    error = $"type: {cutsceneEvent.type} / CharacterAirborne 대상 캐릭터를 설정하지 않았습니다.";
                    Debug.LogError(error);
                    return false;
                }
            }

            // CharacterSpawn 이벤트는 Monster/Npc 타입과 유효한 uid가 반드시 필요합니다.
            if (cutsceneEvent.type == CutsceneEventType.CharacterSpawn &&
                cutsceneEvent.characterSpawn != null)
            {
                CharacterSpawnData spawnData = cutsceneEvent.characterSpawn;
                if (!IsValidCharacterSpawnType(spawnData.characterType))
                {
                    error = $"type: {cutsceneEvent.type} / CharacterSpawn 타입은 Monster 또는 Npc만 지원합니다.";
                    Debug.LogError(error);
                    return false;
                }

                if (spawnData.characterUid <= 0)
                {
                    error = $"type: {cutsceneEvent.type} / CharacterSpawn uid는 1 이상이어야 합니다.";
                    Debug.LogError(error);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 캐릭터 대상 참조가 유효한지 검사합니다.
        /// RuntimeOverride, Fixed, Legacy 대상 중 하나라도 설정되어 있으면 유효합니다.
        /// </summary>
        /// <param name="targetReference">타겟 대상 참조 데이터입니다.</param>
        /// <param name="legacyCharacterType">레거시 대상 타입 값입니다.</param>
        /// <returns>하나 이상의 대상 체계가 유효하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool HasValidCharacterTarget(
            CutsceneCharacterReference targetReference,
            CharacterConstants.Type legacyCharacterType)
        {
            bool hasRuntimeTarget = targetReference != null &&
                                    targetReference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride &&
                                    targetReference.runtimeTargetKey != CutsceneKeyCharacterTarget.None;
            bool hasFixedTarget = targetReference != null &&
                                  targetReference.sourceMode == CutsceneCharacterTargetSourceMode.Fixed &&
                                  targetReference.characterType != CharacterConstants.Type.None;
            bool hasLegacyTarget = legacyCharacterType != CharacterConstants.Type.None;
            return hasRuntimeTarget || hasFixedTarget || hasLegacyTarget;
        }

        /// <summary>
        /// CharacterSpawn에서 지원하는 캐릭터 타입인지 검사합니다.
        /// </summary>
        /// <param name="characterType">검사할 캐릭터 타입입니다.</param>
        /// <returns>Monster 또는 Npc면 <see langword="true"/>를 반환합니다.</returns>
        private static bool IsValidCharacterSpawnType(CharacterConstants.Type characterType)
        {
            return characterType == CharacterConstants.Type.Monster ||
                   characterType == CharacterConstants.Type.Npc;
        }

        /// <summary>
        /// Vector2의 두 좌표가 카메라 위치 계산에 사용할 수 있는 유한 값인지 확인합니다.
        /// </summary>
        /// <param name="value">검사할 좌표입니다.</param>
        /// <returns>두 좌표가 모두 NaN 또는 무한대가 아니면 <see langword="true"/>입니다.</returns>
        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsInfinity(value.y);
        }
    }
}
