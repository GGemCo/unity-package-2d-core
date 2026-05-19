using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 실행되는 단일 이벤트 데이터를 정의합니다.
    /// 이벤트 타입에 따라 하나의 데이터 필드만 유효하게 유지하는 Union 형태로 동작합니다.
    /// </summary>
    [Serializable]
    public class CutsceneEvent
    {
        /// <summary>
        /// 이벤트 시작 시간 (초)입니다.
        /// </summary>
        [HideInInspector] public float time;

        /// <summary>
        /// 이벤트 지속 시간 (초)입니다.
        /// </summary>
        [HideInInspector] public float duration;

        /// <summary>
        /// 이벤트 타입입니다.
        /// </summary>
        [Tooltip("이벤트 타입")]
        public CutsceneEventType type;

        /// <summary>
        /// 이벤트 실행을 담당하는 컨트롤러입니다.
        /// 런타임에만 사용되며 JSON 직렬화에서는 제외됩니다.
        /// </summary>
        [JsonIgnore]
        public ICutsceneController Controller;

        /// <summary>카메라 이동 데이터입니다.</summary>
        public CameraMoveData cameraMove;

        /// <summary>카메라 줌 데이터입니다.</summary>
        public CameraZoomData cameraZoom;

        /// <summary>카메라 흔들림 데이터입니다.</summary>
        public CameraShakeData cameraShake;

        /// <summary>카메라 대상 변경 데이터입니다.</summary>
        public CameraChangeTargetData cameraChangeTarget;

        /// <summary>캐릭터 이동 데이터입니다.</summary>
        public CharacterMoveData characterMove;

        /// <summary>캐릭터 애니메이션 데이터입니다.</summary>
        public CharacterAnimationData characterAnimation;

        /// <summary>캐릭터 애니메이션 속도 데이터입니다.</summary>
        public CharacterAnimationTimeScaleData characterAnimationTimeScale;

        /// <summary>대사 말풍선 데이터입니다.</summary>
        public DialogueBalloonData dialogueBalloon;

        /// <summary>화면 페이드 데이터입니다.</summary>
        public ScreenFadeData screenFade;

        /// <summary>오버레이 텍스트 데이터입니다.</summary>
        public OverlayTextData overlayText;

        /// <summary>캐릭터 흰색 오버레이 데이터입니다.</summary>
        public CharacterWhiteOverlayData characterWhiteOverlay;

        /// <summary>UI 패널 제어 데이터입니다.</summary>
        public UiPanelData uiPanel;

        /// <summary>UI 윈도우 표시 제어 데이터입니다.</summary>
        public UiWindowVisibilityData uiWindowVisibility;

        /// <summary>시간 배율(Time Scale) 데이터입니다.</summary>
        public TimeScaleData timeScale;

        /// <summary>월드 오브젝트 표시 상태 제어 데이터입니다.</summary>
        public WorldObjectVisibilityData worldObjectVisibility;

        /// <summary>캐릭터 조작 잠금 제어 데이터입니다.</summary>
        public CharacterControlLockData characterControlLock;

        /// <summary>화면 글리치 효과 데이터입니다.</summary>
        public ScreenGlitchData screenGlitch;

        /// <summary>캐릭터 페이드 효과 데이터입니다.</summary>
        public CharacterFadeData characterFade;

        /// <summary>
        /// 기본 생성자입니다.
        /// 현재 타입에 맞는 데이터만 유효하도록 초기화합니다.
        /// </summary>
        public CutsceneEvent()
        {
            EnsureDataForType(type);
        }

        /// <summary>
        /// 현재 설정된 타입에 맞는 데이터만 유효하도록 정리합니다.
        /// </summary>
        public void EnsureDataForType()
        {
            EnsureDataForType(type);
        }

        /// <summary>
        /// 지정된 이벤트 타입으로 전환하고, 해당 타입의 데이터만 유지하도록 정리합니다.
        /// 선택되지 않은 타입의 데이터는 모두 null로 초기화됩니다.
        /// </summary>
        /// <param name="eventType">유효하게 유지할 이벤트 타입입니다.</param>
        public void EnsureDataForType(CutsceneEventType eventType)
        {
            type = eventType;

            ClearUnusedData();

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

                case CutsceneEventType.DialogueBalloon:
                    dialogueBalloon ??= new DialogueBalloonData();
                    break;

                case CutsceneEventType.CharacterAnimation:
                    characterAnimation ??= new CharacterAnimationData();
                    break;

                case CutsceneEventType.CharacterAnimationTimeScale:
                    characterAnimationTimeScale ??= new CharacterAnimationTimeScaleData();
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

                case CutsceneEventType.UiWindowVisibility:
                    uiWindowVisibility ??= new UiWindowVisibilityData();
                    break;

                case CutsceneEventType.TimeScale:
                    timeScale ??= new TimeScaleData();
                    break;

                case CutsceneEventType.WorldObjectVisibility:
                    worldObjectVisibility ??= new WorldObjectVisibilityData();
                    break;

                case CutsceneEventType.CharacterControlLock:
                    characterControlLock ??= new CharacterControlLockData();
                    break;

                case CutsceneEventType.ScreenGlitch:
                    screenGlitch ??= new ScreenGlitchData();
                    break;

                case CutsceneEventType.CharacterFade:
                    characterFade ??= new CharacterFadeData();
                    break;
            }
        }

        /// <summary>
        /// 현재 선택된 타입을 제외한 모든 이벤트 데이터를 초기화합니다.
        /// 메모리 상에서도 하나의 타입 데이터만 유지되도록 보장합니다.
        /// </summary>
        private void ClearUnusedData()
        {
            if (type != CutsceneEventType.CameraMove)
            {
                cameraMove = null;
            }

            if (type != CutsceneEventType.CameraZoom)
            {
                cameraZoom = null;
            }

            if (type != CutsceneEventType.CameraShake)
            {
                cameraShake = null;
            }

            if (type != CutsceneEventType.CameraChangeTarget)
            {
                cameraChangeTarget = null;
            }

            if (type != CutsceneEventType.CharacterMove)
            {
                characterMove = null;
            }

            if (type != CutsceneEventType.DialogueBalloon)
            {
                dialogueBalloon = null;
            }

            if (type != CutsceneEventType.CharacterAnimation)
            {
                characterAnimation = null;
            }

            if (type != CutsceneEventType.CharacterAnimationTimeScale)
            {
                characterAnimationTimeScale = null;
            }

            if (type != CutsceneEventType.ScreenFade)
            {
                screenFade = null;
            }

            if (type != CutsceneEventType.OverlayText)
            {
                overlayText = null;
            }

            if (type != CutsceneEventType.CharacterWhiteOverlay)
            {
                characterWhiteOverlay = null;
            }

            if (type != CutsceneEventType.UiPanel)
            {
                uiPanel = null;
            }

            if (type != CutsceneEventType.UiWindowVisibility)
            {
                uiWindowVisibility = null;
            }

            if (type != CutsceneEventType.TimeScale)
            {
                timeScale = null;
            }

            if (type != CutsceneEventType.WorldObjectVisibility)
            {
                worldObjectVisibility = null;
            }

            if (type != CutsceneEventType.CharacterControlLock)
            {
                characterControlLock = null;
            }

            if (type != CutsceneEventType.ScreenGlitch)
            {
                screenGlitch = null;
            }

            if (type != CutsceneEventType.CharacterFade)
            {
                characterFade = null;
            }
        }

        /// <summary>CameraMove 타입일 때만 cameraMove를 직렬화합니다.</summary>
        public bool ShouldSerializeCameraMove() => type == CutsceneEventType.CameraMove && cameraMove != null;

        /// <summary>CameraZoom 타입일 때만 cameraZoom을 직렬화합니다.</summary>
        public bool ShouldSerializeCameraZoom() => type == CutsceneEventType.CameraZoom && cameraZoom != null;

        /// <summary>CameraShake 타입일 때만 cameraShake를 직렬화합니다.</summary>
        public bool ShouldSerializeCameraShake() => type == CutsceneEventType.CameraShake && cameraShake != null;

        /// <summary>CameraChangeTarget 타입일 때만 cameraChangeTarget을 직렬화합니다.</summary>
        public bool ShouldSerializeCameraChangeTarget() => type == CutsceneEventType.CameraChangeTarget && cameraChangeTarget != null;

        /// <summary>CharacterMove 타입일 때만 characterMove를 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterMove() => type == CutsceneEventType.CharacterMove && characterMove != null;

        /// <summary>CharacterAnimation 타입일 때만 characterAnimation을 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterAnimation() => type == CutsceneEventType.CharacterAnimation && characterAnimation != null;

        /// <summary>CharacterAnimationTimeScale 타입일 때만 characterAnimationTimeScale을 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterAnimationTimeScale() => type == CutsceneEventType.CharacterAnimationTimeScale && characterAnimationTimeScale != null;

        /// <summary>DialogueBalloon 타입일 때만 dialogueBalloon을 직렬화합니다.</summary>
        public bool ShouldSerializeDialogueBalloon() => type == CutsceneEventType.DialogueBalloon && dialogueBalloon != null;

        /// <summary>ScreenFade 타입일 때만 screenFade를 직렬화합니다.</summary>
        public bool ShouldSerializeScreenFade() => type == CutsceneEventType.ScreenFade && screenFade != null;

        /// <summary>OverlayText 타입일 때만 overlayText를 직렬화합니다.</summary>
        public bool ShouldSerializeOverlayText() => type == CutsceneEventType.OverlayText && overlayText != null;

        /// <summary>CharacterWhiteOverlay 타입일 때만 characterWhiteOverlay를 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterWhiteOverlay() => type == CutsceneEventType.CharacterWhiteOverlay && characterWhiteOverlay != null;

        /// <summary>UiPanel 타입일 때만 uiPanel을 직렬화합니다.</summary>
        public bool ShouldSerializeUiPanel() => type == CutsceneEventType.UiPanel && uiPanel != null;

        /// <summary>UiWindowVisibility 타입일 때만 uiWindowVisibility를 직렬화합니다.</summary>
        public bool ShouldSerializeUiWindowVisibility() => type == CutsceneEventType.UiWindowVisibility && uiWindowVisibility != null;

        /// <summary>TimeScale 타입일 때만 timeScale을 직렬화합니다.</summary>
        public bool ShouldSerializeTimeScale() => type == CutsceneEventType.TimeScale && timeScale != null;

        /// <summary>WorldObjectVisibility 타입일 때만 worldObjectVisibility를 직렬화합니다.</summary>
        public bool ShouldSerializeWorldObjectVisibility() =>
            type == CutsceneEventType.WorldObjectVisibility && worldObjectVisibility != null;

        /// <summary>CharacterControlLock 타입일 때만 characterControlLock을 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterControlLock() =>
            type == CutsceneEventType.CharacterControlLock && characterControlLock != null;

        /// <summary>ScreenGlitch 타입일 때만 screenGlitch를 직렬화합니다.</summary>
        public bool ShouldSerializeScreenGlitch() =>
            type == CutsceneEventType.ScreenGlitch && screenGlitch != null;

        /// <summary>CharacterFade 타입일 때만 characterFade를 직렬화합니다.</summary>
        public bool ShouldSerializeCharacterFade() =>
            type == CutsceneEventType.CharacterFade && characterFade != null;
    }

    /// <summary>
    /// 하나의 컷신을 구성하는 전체 데이터입니다.
    /// </summary>
    [Serializable]
    public class CutsceneData
    {
        /// <summary>
        /// 컷신 전체 길이 (초)입니다.
        /// </summary>
        [Tooltip("전체 연출 길이 (초)")]
        public float duration;

        /// <summary>
        /// 컷신에 포함된 이벤트 목록입니다.
        /// </summary>
        public List<CutsceneEvent> events = new();
    }
}
