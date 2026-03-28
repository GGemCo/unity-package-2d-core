using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneTimelineValidationUtility
    {
        public static bool ValidateEvent(CutsceneEvent cutsceneEvent, out string error)
        {
            error = null;

            if (cutsceneEvent.type == CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

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
