using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class CutsceneEvent
    {
        [HideInInspector] public float time;
        [HideInInspector] public float duration;
        [Tooltip("이벤트 타입")]
        public CutsceneEventType type;

        // 이 필드는 Prepare 단계에서 할당
        [JsonIgnore]
        public ICutsceneController Controller;

        // 타입별 필드 (Union 구조처럼 사용)
        public CameraMoveData cameraMove;
        public CameraZoomData cameraZoom;
        public CameraShakeData cameraShake;
        public CameraChangeTargetData cameraChangeTarget;

        public CharacterMoveData characterMove;
        public CharacterAnimationData characterAnimation;

        public DialogueBalloonData dialogueBalloon;
        public ScreenFadeData screenFade;
        public OverlayTextData overlayText;
        public CharacterWhiteOverlayData characterWhiteOverlay;
        public UiPanelData uiPanel;

        public CutsceneEvent()
        {
            EnsureDataForType(type);
        }

        public void EnsureDataForType()
        {
            EnsureDataForType(type);
        }

        public void EnsureDataForType(CutsceneEventType eventType)
        {
            type = eventType;

            switch (type)
            {
                case CutsceneEventType.CameraMove:
                    cameraMove ??= new CameraMoveData();
                    break;
                case CutsceneEventType.CameraZoom:
                    cameraZoom ??= new CameraZoomData();
                    break;
                case CutsceneEventType.CameraShake:
                    cameraShake ??= new CameraShakeData();
                    break;
                case CutsceneEventType.CameraChangeTarget:
                    cameraChangeTarget ??= new CameraChangeTargetData();
                    break;
                case CutsceneEventType.CharacterMove:
                    characterMove ??= new CharacterMoveData();
                    break;
                case CutsceneEventType.CharacterAnimation:
                    characterAnimation ??= new CharacterAnimationData();
                    break;
                case CutsceneEventType.DialogueBalloon:
                    dialogueBalloon ??= new DialogueBalloonData();
                    break;
                case CutsceneEventType.ScreenFade:
                    screenFade ??= new ScreenFadeData();
                    break;
                case CutsceneEventType.OverlayText:
                    overlayText ??= new OverlayTextData();
                    break;
                case CutsceneEventType.CharacterWhiteOverlay:
                    characterWhiteOverlay ??= new CharacterWhiteOverlayData();
                    break;
                case CutsceneEventType.UiPanel:
                    uiPanel ??= new UiPanelData();
                    break;
            }
        }

        public bool ShouldSerializeCameraMove() => type == CutsceneEventType.CameraMove && cameraMove != null;
        public bool ShouldSerializeCameraZoom() => type == CutsceneEventType.CameraZoom && cameraZoom != null;
        public bool ShouldSerializeCameraShake() => type == CutsceneEventType.CameraShake && cameraShake != null;
        public bool ShouldSerializeCameraChangeTarget() => type == CutsceneEventType.CameraChangeTarget && cameraChangeTarget != null;

        public bool ShouldSerializeCharacterMove() => type == CutsceneEventType.CharacterMove && characterMove != null;
        public bool ShouldSerializeCharacterAnimation() => type == CutsceneEventType.CharacterAnimation && characterAnimation != null;

        public bool ShouldSerializeDialogueBalloon() => type == CutsceneEventType.DialogueBalloon && dialogueBalloon != null;
        public bool ShouldSerializeScreenFade() => type == CutsceneEventType.ScreenFade && screenFade != null;
        public bool ShouldSerializeOverlayText() => type == CutsceneEventType.OverlayText && overlayText != null;
        public bool ShouldSerializeCharacterWhiteOverlay() => type == CutsceneEventType.CharacterWhiteOverlay && characterWhiteOverlay != null;
        public bool ShouldSerializeUiPanel() => type == CutsceneEventType.UiPanel && uiPanel != null;
    }

    [Serializable]
    public class CutsceneData
    {
        [Tooltip("전체 연출 길이 (초)")]
        public float duration;
        public List<CutsceneEvent> events = new();
    }
}
