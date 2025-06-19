using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CutsceneEvent
    {
        // [Tooltip("시작 시간")]
        [HideInInspector] public float time;
        // [Tooltip("클립 플레이 시간")]
        [HideInInspector] public float duration;
        [Tooltip("이벤트 타입")]
        public CutsceneEventType type;
        // 이 필드는 Prepare 단계에서 할당
        [JsonIgnore] // JSON 역직렬화 시 무시
        public ICutsceneController Controller;

        // 타입별 필드 (Union 구조처럼 사용)
        public CameraMoveData cameraMove;
        public CameraZoomData cameraZoom;
        public CameraShakeData cameraShake;
        public CameraChangeTargetData cameraChangeTarget;
        
        public CharacterMoveData characterMove;
        public CharacterAnimationData characterAnimation;
        
        public DialogueBalloonData dialogueBalloon;

        public bool ShouldSerializeCameraMove() => type == CutsceneEventType.CameraMove && cameraMove != null;
        public bool ShouldSerializeCameraZoom() => type == CutsceneEventType.CameraZoom && cameraZoom != null;
        public bool ShouldSerializeCameraShake() => type == CutsceneEventType.CameraShake && cameraShake != null;
        public bool ShouldSerializeCameraChangeTargetData() => type == CutsceneEventType.CameraChangeTarget && cameraChangeTarget != null;
        
        public bool ShouldSerializeCharacterMoveData() => type == CutsceneEventType.CharacterMove && characterMove != null;
        public bool ShouldSerializeCharacterAnimationData() => type == CutsceneEventType.CharacterAnimation && characterAnimation != null;
        
        public bool ShouldSerializeDialogueBalloonData() => type == CutsceneEventType.DialogueBalloon && dialogueBalloon != null;
    }
    
    [Serializable]
    public class CutsceneData
    {
        [Tooltip("전체 연출 길이 (초)")]
        public float duration;
        public List<CutsceneEvent> events = new();
    }
}
