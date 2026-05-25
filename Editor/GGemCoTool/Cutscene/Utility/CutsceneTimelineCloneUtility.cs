using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="CutsceneEvent"/> 및 내부 Payload 데이터를 이벤트 타입에 따라 깊은 복사(Deep Copy)하는 유틸리티 클래스입니다.
    /// </summary>
    /// <remarks>
    /// - 이벤트의 타입에 맞는 Payload만 선택적으로 복제합니다.
    /// - 복제 이후 <c>EnsureDataForType()</c>를 호출하여 누락된 필드를 안전하게 초기화합니다.
    /// - 참조 타입 필드는 새로운 인스턴스로 생성하여 원본과의 참조 공유를 방지합니다.
    /// </remarks>
    internal static class CutsceneTimelineCloneUtility
    {
        /// <summary>
        /// <see cref="CutsceneEvent"/> 인스턴스를 깊은 복사하여 새로운 이벤트 객체를 생성합니다.
        /// </summary>
        /// <param name="source">복제할 원본 이벤트입니다. null일 경우 null을 반환합니다.</param>
        /// <returns>복제된 새로운 이벤트 객체 또는 null입니다.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// 내부 Payload 복제 과정에서 타입과 데이터가 일치하지 않는 경우 발생할 수 있습니다.
        /// </exception>
        public static CutsceneEvent CloneEvent(CutsceneEvent source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new CutsceneEvent
            {
                eventGuid = source.eventGuid,
                time = source.time,
                duration = source.duration,
                type = source.type
            };
            clone.EnsureEventGuid();

            // 타입에 맞는 payload만 복제
            ClonePayloadByType(source, clone);

            // 안전 보정 (누락 필드 초기화)
            clone.EnsureDataForType();

            return clone;
        }

        /// <summary>
        /// 이벤트 타입에 따라 적절한 Payload를 선택적으로 복제하여 대상 이벤트에 설정합니다.
        /// </summary>
        /// <param name="source">원본 이벤트입니다.</param>
        /// <param name="target">복제 대상 이벤트입니다.</param>
        /// <remarks>
        /// 각 타입별 Payload는 서로 독립적인 구조를 가지므로, 해당 타입에 맞는 데이터만 복제됩니다.
        /// </remarks>
        private static void ClonePayloadByType(CutsceneEvent source, CutsceneEvent target)
        {
            switch (source.type)
            {
                case CutsceneEventType.CameraMove:
                    target.cameraMove = CloneCameraMoveData(source.cameraMove);
                    break;

                case CutsceneEventType.CameraZoom:
                    target.cameraZoom = CloneCameraZoomData(source.cameraZoom);
                    break;

                case CutsceneEventType.CameraShake:
                    target.cameraShake = CloneCameraShakeData(source.cameraShake);
                    break;

                case CutsceneEventType.CameraChangeTarget:
                    target.cameraChangeTarget = CloneCameraChangeTargetData(source.cameraChangeTarget);
                    break;

                case CutsceneEventType.CharacterMove:
                    target.characterMove = CloneCharacterMoveData(source.characterMove);
                    break;

                case CutsceneEventType.CharacterTweenMove:
                    target.characterTweenMove = CloneCharacterTweenMoveData(source.characterTweenMove);
                    break;

                case CutsceneEventType.CharacterAnimation:
                    target.characterAnimation = CloneCharacterAnimationData(source.characterAnimation);
                    break;

                case CutsceneEventType.CharacterAnimationTimeScale:
                    target.characterAnimationTimeScale = CloneCharacterAnimationTimeScaleData(source.characterAnimationTimeScale);
                    break;

                case CutsceneEventType.DialogueBalloon:
                    target.dialogueBalloon = CloneDialogueBalloonData(source.dialogueBalloon);
                    break;

                case CutsceneEventType.ScreenFade:
                    target.screenFade = CloneScreenFadeData(source.screenFade);
                    break;

                case CutsceneEventType.OverlayText:
                    target.overlayText = CloneOverlayTextData(source.overlayText);
                    break;

                case CutsceneEventType.CharacterWhiteOverlay:
                    target.characterWhiteOverlay = CloneCharacterWhiteOverlayData(source.characterWhiteOverlay);
                    break;

                case CutsceneEventType.UiPanel:
                    target.uiPanel = CloneUiPanelData(source.uiPanel);
                    break;

                case CutsceneEventType.UiWindowVisibility:
                    target.uiWindowVisibility = CloneUiWindowVisibilityData(source.uiWindowVisibility);
                    break;

                case CutsceneEventType.TimeScale:
                    target.timeScale = CloneTimeScaleData(source.timeScale);
                    break;

                case CutsceneEventType.WorldObjectVisibility:
                    target.worldObjectVisibility = CloneWorldObjectVisibilityData(source.worldObjectVisibility);
                    break;

                case CutsceneEventType.CharacterControlLock:
                    target.characterControlLock = CloneCharacterControlLockData(source.characterControlLock);
                    break;

                case CutsceneEventType.ScreenGlitch:
                    target.screenGlitch = CloneScreenGlitchData(source.screenGlitch);
                    break;

                case CutsceneEventType.CharacterFade:
                    target.characterFade = CloneCharacterFadeData(source.characterFade);
                    break;

                case CutsceneEventType.CharacterAirborne:
                    target.characterAirborne = CloneCharacterAirborneData(source.characterAirborne);
                    break;

                case CutsceneEventType.CharacterSpawn:
                    target.characterSpawn = CloneCharacterSpawnData(source.characterSpawn);
                    break;

                case CutsceneEventType.DialogueWindow:
                    target.dialogueWindow = CloneDialogueWindowData(source.dialogueWindow);
                    break;
            }
        }

        /// <summary>
        /// <see cref="CameraMoveData"/>를 깊은 복사합니다.
        /// </summary>
        private static CameraMoveData CloneCameraMoveData(CameraMoveData source)
        {
            return source == null ? null : new CameraMoveData
            {
                startPosition = source.startPosition,
                endPosition = source.endPosition,
                endTargetPlayer = source.endTargetPlayer,
                easing = source.easing,
            };
        }

        /// <summary>
        /// <see cref="CameraZoomData"/>를 깊은 복사합니다.
        /// </summary>
        private static CameraZoomData CloneCameraZoomData(CameraZoomData source)
        {
            return source == null ? null : new CameraZoomData
            {
                startSize = source.startSize,
                endSize = source.endSize,
                easing = source.easing,
            };
        }

        /// <summary>
        /// <see cref="CameraShakeData"/>를 깊은 복사합니다.
        /// </summary>
        private static CameraShakeData CloneCameraShakeData(CameraShakeData source)
        {
            return source == null ? null : new CameraShakeData
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

        /// <summary>
        /// <see cref="CameraChangeTargetData"/>를 깊은 복사합니다.
        /// </summary>
        private static CameraChangeTargetData CloneCameraChangeTargetData(CameraChangeTargetData source)
        {
            return source == null ? null : new CameraChangeTargetData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
            };
        }

        /// <summary>
        /// <see cref="CharacterMoveData"/>를 깊은 복사합니다.
        /// </summary>
        private static CharacterMoveData CloneCharacterMoveData(CharacterMoveData source)
        {
            return source == null ? null : new CharacterMoveData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                characterMoveSpeed = source.characterMoveSpeed,
                moveMode = source.moveMode,
                startPosition = source.startPosition,
                endPosition = source.endPosition,
                relativeDirection = source.relativeDirection,
                relativeDistance = source.relativeDistance,
                relativeOffset = source.relativeOffset,
                facingMode = source.facingMode,
                explicitFacing = source.explicitFacing,
            };
        }

        /// <summary>
        /// <see cref="CharacterTweenMoveData"/>를 깊은 복사합니다.
        /// </summary>
        private static CharacterTweenMoveData CloneCharacterTweenMoveData(CharacterTweenMoveData source)
        {
            return source == null ? null : new CharacterTweenMoveData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                moveMode = source.moveMode,
                easing = source.easing,
                startPosition = source.startPosition,
                endPosition = source.endPosition,
                relativeDirection = source.relativeDirection,
                relativeDistance = source.relativeDistance,
                relativeOffset = source.relativeOffset,
                facingMode = source.facingMode,
                explicitFacing = source.explicitFacing,
            };
        }

        /// <summary>
        /// <see cref="CharacterAnimationData"/>를 깊은 복사합니다.
        /// </summary>
        private static CharacterAnimationData CloneCharacterAnimationData(CharacterAnimationData source)
        {
            return source == null ? null : new CharacterAnimationData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                spawnPosition = source.spawnPosition,
                facingMode = source.facingMode,
                explicitFacing = source.explicitFacing,
                animationName = source.animationName,
                animationLoop = source.animationLoop,
                animationTimeScale = source.animationTimeScale,
            };
        }

        /// <summary>
        /// 캐릭터 애니메이션의 재생 속도를 제어하는 데이터를 깊은 복사합니다.
        /// </summary>
        private static CharacterAnimationTimeScaleData CloneCharacterAnimationTimeScaleData(CharacterAnimationTimeScaleData source)
        {
            return source == null ? null : new CharacterAnimationTimeScaleData
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

        /// <summary>
        /// 대사 말풍선 데이터를 깊은 복사합니다.
        /// </summary>
        private static DialogueBalloonData CloneDialogueBalloonData(DialogueBalloonData source)
        {
            return source == null ? null : new DialogueBalloonData
            {
                isFollowTarget = source.isFollowTarget,
                characterType = source.characterType,
                characterUid = source.characterUid,
                message = source.message,
                messageTable = source.messageTable,
                messageKey = source.messageKey,
                fontSize = source.fontSize,
                useTypewriter = source.useTypewriter,
                typewriterCharactersPerSecond = source.typewriterCharactersPerSecond,
                waitForUserInput = source.waitForUserInput,
                advancePolicy = source.advancePolicy,
                useTalkLoopAnimation = source.useTalkLoopAnimation,
                talkLoopAnimationName = source.talkLoopAnimationName,
                talkLoopAnimationTarget = CloneCharacterReference(source.talkLoopAnimationTarget),
                talkLoopAnimationTimeScale = source.talkLoopAnimationTimeScale,
                restoreTalkLoopAnimationOnStop = source.restoreTalkLoopAnimationOnStop,
                thumbnailPositionType = source.thumbnailPositionType,
                thumbnailImage = source.thumbnailImage,
                offsetImageThumbnailCharacter = source.offsetImageThumbnailCharacter,
                offsetImageThumbnailCharacterLeft = source.offsetImageThumbnailCharacterLeft,
                thumbnailFlipPolicy = source.thumbnailFlipPolicy,
                thumbnailSourceFacing = source.thumbnailSourceFacing,
                useSymmetricLayoutByTail = source.useSymmetricLayoutByTail,
                tailForwardOffsetPx = source.tailForwardOffsetPx,
                minHalfExtentByTailPx = source.minHalfExtentByTailPx,
                textPaddingOnNonThumbnailSidePx = source.textPaddingOnNonThumbnailSidePx,
                textPaddingOnThumbnailSidePx = source.textPaddingOnThumbnailSidePx,
                thumbnailGapPx = source.thumbnailGapPx,
                useProjectWorldOffset = source.useProjectWorldOffset,
                worldOffset = source.worldOffset,
                worldOffsetXPolicy = source.worldOffsetXPolicy,
            };
        }

        /// <summary>
        /// 화면 페이드 효과 데이터를 깊은 복사합니다.
        /// </summary>
        private static ScreenFadeData CloneScreenFadeData(ScreenFadeData source)
        {
            return source == null ? null : new ScreenFadeData
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

        /// <summary>
        /// 화면 오버레이 텍스트 데이터를 깊은 복사합니다.
        /// </summary>
        private static OverlayTextData CloneOverlayTextData(OverlayTextData source)
        {
            return source == null ? null : new OverlayTextData
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

        /// <summary>
        /// 캐릭터 화이트 오버레이 효과 데이터를 깊은 복사합니다.
        /// </summary>
        private static CharacterWhiteOverlayData CloneCharacterWhiteOverlayData(CharacterWhiteOverlayData source)
        {
            return source == null ? null : new CharacterWhiteOverlayData
            {
                target = CloneCharacterReference(source.target),
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

        /// <summary>
        /// 캐릭터 참조 데이터를 깊은 복사합니다.
        /// </summary>
        private static CutsceneCharacterReference CloneCharacterReference(CutsceneCharacterReference source)
        {
            return source == null ? null : new CutsceneCharacterReference
            {
                sourceMode = source.sourceMode,
                characterType = source.characterType,
                characterUid = source.characterUid,
                runtimeTargetKey = source.runtimeTargetKey,
            };
        }

        /// <summary>
        /// UI 패널 애니메이션 및 상태 데이터를 깊은 복사합니다.
        /// </summary>
        private static UiPanelData CloneUiPanelData(UiPanelData source)
        {
            return source == null ? null : new UiPanelData
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

        /// <summary>
        /// UI 윈도우 표시/숨김 상태 데이터를 깊은 복사합니다.
        /// </summary>
        private static UiWindowVisibilityData CloneUiWindowVisibilityData(UiWindowVisibilityData source)
        {
            return source == null ? null : new UiWindowVisibilityData
            {
                mode = source.mode,
                targetWindows = source.targetWindows != null
                    ? new List<UIWindowConstants.WindowUid>(source.targetWindows)
                    : new List<UIWindowConstants.WindowUid>(),
                exceptWindows = source.exceptWindows != null
                    ? new List<UIWindowConstants.WindowUid>(source.exceptWindows)
                    : new List<UIWindowConstants.WindowUid>(),
                show = source.show,
                restoreOnStop = source.restoreOnStop,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
            };
        }

        /// <summary>
        /// 타임 스케일(게임 속도) 제어 데이터를 깊은 복사합니다.
        /// </summary>
        private static TimeScaleData CloneTimeScaleData(TimeScaleData source)
        {
            return source == null ? null : new TimeScaleData
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

        /// <summary>
        /// 월드 오브젝트 표시 상태 제어 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 월드 오브젝트 표시 상태 제어 데이터입니다.</returns>
        private static WorldObjectVisibilityData CloneWorldObjectVisibilityData(WorldObjectVisibilityData source)
        {
            return source == null ? null : new WorldObjectVisibilityData
            {
                targetMode = source.targetMode,
                targetGroupKeys = source.targetGroupKeys != null
                    ? new List<string>(source.targetGroupKeys)
                    : new List<string>(),
                exceptGroupKeys = source.exceptGroupKeys != null
                    ? new List<string>(source.exceptGroupKeys)
                    : new List<string>(),
                searchEntireScene = source.searchEntireScene,
                includeInactiveTargets = source.includeInactiveTargets,
                show = source.show,
                applyMode = source.applyMode,
                restoreOnStop = source.restoreOnStop,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
            };
        }

        /// <summary>
        /// 캐릭터 조작 잠금 제어 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 캐릭터 조작 잠금 제어 데이터입니다.</returns>
        private static CharacterControlLockData CloneCharacterControlLockData(CharacterControlLockData source)
        {
            return source == null ? null : new CharacterControlLockData
            {
                targetScope = source.targetScope,
                target = CloneCharacterReference(source.target),
                lockMask = source.lockMask,
                stopImmediately = source.stopImmediately,
                releaseOnClipEnd = source.releaseOnClipEnd,
                releaseOnCutsceneEnd = source.releaseOnCutsceneEnd,
            };
        }

        /// <summary>
        /// 화면 글리치 효과 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 화면 글리치 효과 데이터입니다.</returns>
        private static ScreenGlitchData CloneScreenGlitchData(ScreenGlitchData source)
        {
            return source == null ? null : new ScreenGlitchData
            {
                fromIntensity = source.fromIntensity,
                toIntensity = source.toIntensity,
                holdFinalState = source.holdFinalState,
                restoreOnCutsceneEnd = source.restoreOnCutsceneEnd,
                rgbSplit = source.rgbSplit,
                horizontalJitter = source.horizontalJitter,
                verticalJump = source.verticalJump,
                blockNoise = source.blockNoise,
                scanlineStrength = source.scanlineStrength,
                colorDrift = source.colorDrift,
                useUnscaledTime = source.useUnscaledTime,
                noiseSpeed = source.noiseSpeed,
                seed = source.seed,
                easing = source.easing,
            };
        }

        /// <summary>
        /// 캐릭터 페이드 효과 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 캐릭터 페이드 효과 데이터입니다.</returns>
        private static CharacterFadeData CloneCharacterFadeData(CharacterFadeData source)
        {
            return source == null ? null : new CharacterFadeData
            {
                target = CloneCharacterReference(source.target),
                characterType = source.characterType,
                characterUid = source.characterUid,
                fadeMode = source.fadeMode,
                useCustomAlphaRange = source.useCustomAlphaRange,
                fromAlpha = source.fromAlpha,
                toAlpha = source.toAlpha,
                preserveCurrentRgb = source.preserveCurrentRgb,
                tintColor = source.tintColor,
                holdFinalState = source.holdFinalState,
                deactivateOnFadeOutComplete = source.deactivateOnFadeOutComplete,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
            };
        }

        /// <summary>
        /// 캐릭터 공중 상태 제어 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 캐릭터 공중 상태 제어 데이터입니다.</returns>
        private static CharacterAirborneData CloneCharacterAirborneData(CharacterAirborneData source)
        {
            return source == null ? null : new CharacterAirborneData
            {
                target = CloneCharacterReference(source.target),
                characterType = source.characterType,
                characterUid = source.characterUid,
                airborneEnabled = source.airborneEnabled,
                targetAirHeight = source.targetAirHeight,
                allowReplace = source.allowReplace,
                useUnscaledTime = source.useUnscaledTime,
                easing = source.easing,
                keepAirborneGravity = source.keepAirborneGravity,
                restoreHeightOnStop = source.restoreHeightOnStop,
                restoreHeightOnCutsceneEnd = source.restoreHeightOnCutsceneEnd,
            };
        }

        /// <summary>
        /// <see cref="DialogueWindowData"/>를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 대화창 컷신 데이터입니다.</returns>
        private static DialogueWindowData CloneDialogueWindowData(DialogueWindowData source)
        {
            return source == null ? null : new DialogueWindowData
            {
                dialogueUid = source.dialogueUid,
                npcUid = source.npcUid,
                waitUntilEnd = source.waitUntilEnd,
                releaseWaitOnLoadFailed = source.releaseWaitOnLoadFailed,
                closeOtherWindows = source.closeOtherWindows,
            };
        }

        /// <summary>
        /// 캐릭터 생성 제어 데이터를 깊은 복사합니다.
        /// </summary>
        /// <param name="source">복사할 원본 데이터입니다.</param>
        /// <returns>복제된 캐릭터 생성 제어 데이터입니다.</returns>
        private static CharacterSpawnData CloneCharacterSpawnData(CharacterSpawnData source)
        {
            return source == null ? null : new CharacterSpawnData
            {
                characterType = source.characterType,
                characterUid = source.characterUid,
                characterScale = source.characterScale,
                positionMode = source.positionMode,
                worldPosition = source.worldPosition,
                playerRelativeDirection = source.playerRelativeDirection,
                playerRelativeDistance = source.playerRelativeDistance,
                positionOffset = source.positionOffset,
                spawnVisible = source.spawnVisible,
                settleToMapOnCutsceneEnd = source.settleToMapOnCutsceneEnd,
                visibilityPolicyAfterCutscene = source.visibilityPolicyAfterCutscene,
            };
        }
    }
}
