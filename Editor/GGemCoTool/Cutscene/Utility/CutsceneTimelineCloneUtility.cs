using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneTimelineCloneUtility
    {
        public static CutsceneEvent CloneEvent(CutsceneEvent source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new CutsceneEvent
            {
                time = source.time,
                duration = source.duration,
                type = source.type,
                cameraMove = source.type == CutsceneEventType.CameraMove ? CloneCameraMoveData(source.cameraMove) : null,
                cameraZoom = source.type == CutsceneEventType.CameraZoom ? CloneCameraZoomData(source.cameraZoom) : null,
                cameraShake = source.type == CutsceneEventType.CameraShake ? CloneCameraShakeData(source.cameraShake) : null,
                cameraChangeTarget = source.type == CutsceneEventType.CameraChangeTarget ? CloneCameraChangeTargetData(source.cameraChangeTarget) : null,
                characterMove = source.type == CutsceneEventType.CharacterMove ? CloneCharacterMoveData(source.characterMove) : null,
                characterAnimation = source.type == CutsceneEventType.CharacterAnimation ? CloneCharacterAnimationData(source.characterAnimation) : null,
                characterAnimationTimeScale = source.type == CutsceneEventType.CharacterAnimationTimeScale ? CloneCharacterAnimationTimeScaleData(source.characterAnimationTimeScale) : null,
                dialogueBalloon = source.type == CutsceneEventType.DialogueBalloon ? CloneDialogueBalloonData(source.dialogueBalloon) : null,
                screenFade = source.type == CutsceneEventType.ScreenFade ? CloneScreenFadeData(source.screenFade) : null,
                overlayText = source.type == CutsceneEventType.OverlayText ? CloneOverlayTextData(source.overlayText) : null,
                characterWhiteOverlay = source.type == CutsceneEventType.CharacterWhiteOverlay ? CloneCharacterWhiteOverlayData(source.characterWhiteOverlay) : null,
                uiPanel = source.type == CutsceneEventType.UiPanel ? CloneUiPanelData(source.uiPanel) : null,
                uiWindowVisibility = source.type == CutsceneEventType.UiWindowVisibility ? CloneUiWindowVisibilityData(source.uiWindowVisibility) : null,
                timeScale = source.type == CutsceneEventType.TimeScale ? CloneTimeScaleData(source.timeScale) : null,
            };

            clone.EnsureDataForType();
            return clone;
        }

        private static CameraMoveData CloneCameraMoveData(CameraMoveData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraMoveData
            {
                startPosition = source.startPosition,
                endPosition = source.endPosition,
                endTargetPlayer = source.endTargetPlayer,
                easing = source.easing,
            };
        }

        private static CameraZoomData CloneCameraZoomData(CameraZoomData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraZoomData
            {
                startSize = source.startSize,
                endSize = source.endSize,
                easing = source.easing,
            };
        }

        private static CameraShakeData CloneCameraShakeData(CameraShakeData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraShakeData
            {
                duration = source.duration,
                shakeIntensity = source.shakeIntensity,
                leftStrength = source.leftStrength,
                rightStrength = source.rightStrength,
                downStrength = source.downStrength,
                upStrength = source.upStrength,
                repeatCount = source.repeatCount,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static CameraChangeTargetData CloneCameraChangeTargetData(CameraChangeTargetData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CameraChangeTargetData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
            };
        }

        private static CharacterMoveData CloneCharacterMoveData(CharacterMoveData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterMoveData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                characterMoveSpeed = source.characterMoveSpeed,
                startPosition = source.startPosition,
                endPosition = source.endPosition,
            };
        }

        private static CharacterAnimationData CloneCharacterAnimationData(CharacterAnimationData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterAnimationData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                spawnPosition = source.spawnPosition,
                isFlip = source.isFlip,
                animationName = source.animationName,
                animationLoop = source.animationLoop,
                animationTimeScale = source.animationTimeScale,
            };
        }

        private static CharacterAnimationTimeScaleData CloneCharacterAnimationTimeScaleData(CharacterAnimationTimeScaleData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterAnimationTimeScaleData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
                actionMode = source.actionMode,
                fromScale = source.fromScale,
                toScale = source.toScale,
                restoreScale = source.restoreScale,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
                captureOriginalOnTrigger = source.captureOriginalOnTrigger,
                useCapturedScaleForRestore = source.useCapturedScaleForRestore,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
            };
        }

        private static DialogueBalloonData CloneDialogueBalloonData(DialogueBalloonData source)
        {
            if (source == null)
            {
                return null;
            }

            return new DialogueBalloonData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                message = source.message,
                fontSize = source.fontSize,
            };
        }

        private static ScreenFadeData CloneScreenFadeData(ScreenFadeData source)
        {
            if (source == null)
            {
                return null;
            }

            return new ScreenFadeData
            {
                color = source.color,
                fromAlpha = source.fromAlpha,
                toAlpha = source.toAlpha,
                holdFinalState = source.holdFinalState,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
                renderMode = source.renderMode,
                sortingLayerName = source.sortingLayerName,
                orderInLayer = source.orderInLayer,
                planeDistance = source.planeDistance,
            };
        }

        private static OverlayTextData CloneOverlayTextData(OverlayTextData source)
        {
            if (source == null)
            {
                return null;
            }

            return new OverlayTextData
            {
                sourceMode = source.sourceMode,
                text = source.text,
                runtimeTextKey = source.runtimeTextKey,
                anchoredPosition = source.anchoredPosition,
                sizeDelta = source.sizeDelta,
                fontSize = source.fontSize,
                textColor = source.textColor,
                maxAlpha = source.maxAlpha,
                fadeIn = source.fadeIn,
                fadeOut = source.fadeOut,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static CharacterWhiteOverlayData CloneCharacterWhiteOverlayData(CharacterWhiteOverlayData source)
        {
            if (source == null)
            {
                return null;
            }

            return new CharacterWhiteOverlayData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
                color = source.color,
                fromStrength = source.fromStrength,
                toStrength = source.toStrength,
                restoreOnStop = source.restoreOnStop,
                refreshTargetsOnTrigger = source.refreshTargetsOnTrigger,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
            };
        }

        private static UiPanelData CloneUiPanelData(UiPanelData source)
        {
            if (source == null)
            {
                return null;
            }

            return new UiPanelData
            {
                panelId = source.panelId,
                createIfMissing = source.createIfMissing,
                destroyOnStop = source.destroyOnStop,
                hideOnStop = source.hideOnStop,
                anchorMin = source.anchorMin,
                anchorMax = source.anchorMax,
                pivot = source.pivot,
                fromAnchoredPosition = source.fromAnchoredPosition,
                toAnchoredPosition = source.toAnchoredPosition,
                fromSizeDelta = source.fromSizeDelta,
                toSizeDelta = source.toSizeDelta,
                siblingIndex = source.siblingIndex,
                fromColor = source.fromColor,
                toColor = source.toColor,
                fromAlpha = source.fromAlpha,
                toAlpha = source.toAlpha,
                raycastTarget = source.raycastTarget,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
            };
        }

        private static UiWindowVisibilityData CloneUiWindowVisibilityData(UiWindowVisibilityData source)
        {
            if (source == null)
            {
                return null;
            }

            return new UiWindowVisibilityData
            {
                mode = source.mode,
                targetWindows = source.targetWindows != null ? new List<UIWindowConstants.WindowUid>(source.targetWindows) : new List<UIWindowConstants.WindowUid>(),
                exceptWindows = source.exceptWindows != null ? new List<UIWindowConstants.WindowUid>(source.exceptWindows) : new List<UIWindowConstants.WindowUid>(),
                show = source.show,
                restoreOnStop = source.restoreOnStop,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
            };
        }

        private static TimeScaleData CloneTimeScaleData(TimeScaleData source)
        {
            if (source == null)
            {
                return null;
            }

            return new TimeScaleData
            {
                actionMode = source.actionMode,
                fromScale = source.fromScale,
                toScale = source.toScale,
                restoreScale = source.restoreScale,
                easing = source.easing,
                useUnscaledTime = source.useUnscaledTime,
                timelineMode = source.timelineMode,
                useCapturedScaleForRestore = source.useCapturedScaleForRestore,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
                affectFixedDeltaTime = source.affectFixedDeltaTime,
                minimumScaleForFixedDeltaTime = source.minimumScaleForFixedDeltaTime,
            };
        }
    }
}
