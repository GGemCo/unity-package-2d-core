using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 말풍선 모드에서 패널/썸네일/말꼬리 정렬을 함께 갱신합니다.
        /// </summary>
        private void RefreshSpeechBubbleLayout()
        {
            if (!TryEnsureSpeechBubbleLayoutReferences())
            {
                return;
            }

            ApplyThumbnailVisibilityAfterBinding();
            ConfigCommon.ThumbnailPositionType thumbnailPositionType = ResolveEffectiveThumbnailPositionType();
            bool hasThumbnail = HasVisibleThumbnail();
            float side = ResolveThumbnailSideSign(thumbnailPositionType);
            Vector3 thumbnailOffset = ResolveThumbnailOffset(thumbnailPositionType);

            ApplySpeechBubblePanelPaddingByThumbnailSide(hasThumbnail, thumbnailPositionType);
            UpdatePanelMinimumWidthByTailSymmetry(hasThumbnail, side, thumbnailOffset.x);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelMessage);

            float panelHalfWidth = panelMessage.rect.width * 0.5f;
            float panelCenterX = 0f;
            if (TryEnsureTailReferences())
            {
                float tailX = _tailRectTransform.anchoredPosition.x;
                panelCenterX = tailX;
                if (useSymmetricLayoutByTail && hasThumbnail)
                {
                    float thumbnailWidth = _thumbnailRectTransform != null ? _thumbnailRectTransform.rect.width : 0f;
                    float thumbnailSpan = Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth;
                    panelCenterX = tailX - (side * (thumbnailSpan * 0.5f)) - (thumbnailOffset.x * 0.5f);
                }
            }

            SetPanelMessageCenterX(panelCenterX);

            if (hasThumbnail && _thumbnailRectTransform != null)
            {
                float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
                float thumbnailCenterXInRootSpace =
                    panelCenterX +
                    (side * (panelHalfWidth + Mathf.Max(0f, thumbnailGapPx) + thumbnailHalfWidth)) +
                    thumbnailOffset.x;
                RectTransform thumbnailParentRectTransform = _thumbnailRectTransform.parent as RectTransform;
                float thumbnailCenterX = IsThumbnailChildOfPanel()
                    ? (side * (panelHalfWidth + Mathf.Max(0f, thumbnailGapPx) + thumbnailHalfWidth)) + thumbnailOffset.x
                    : ConvertRootSpaceXToParentLocalX(thumbnailParentRectTransform, thumbnailCenterXInRootSpace);

                Vector2 thumbnailAnchoredPosition = _thumbnailRectTransform.anchoredPosition;
                thumbnailAnchoredPosition.x = thumbnailCenterX;
                thumbnailAnchoredPosition.y = thumbnailOffset.y;
                _thumbnailRectTransform.anchoredPosition = thumbnailAnchoredPosition;
            }

            ApplyThumbnailFlip();
            RefreshSpeechBubbleEnterIndicatorPosition();
        }

        /// <summary>
        /// 말풍선 레이아웃 참조를 캐시합니다.
        /// 프리팹 참조가 비어 있어도 이름 기반으로 자동 탐색해 보정합니다.
        /// </summary>
        private void CacheSpeechBubbleLayoutReferences()
        {
            if (panelDialogue == null)
            {
                panelDialogue = transform.Find("Panel")?.gameObject;
            }

            if (_panelDialogueRectTransform == null)
            {
                _panelDialogueRectTransform = panelDialogue != null
                    ? panelDialogue.transform as RectTransform
                    : transform.Find("Panel") as RectTransform;
            }

            if (panelMessage == null)
            {
                panelMessage = _panelDialogueRectTransform?.Find("Panel") as RectTransform;
                panelMessage ??= transform.Find("Panel/Panel") as RectTransform;
                panelMessage ??= textMessage != null ? textMessage.transform.parent as RectTransform : null;
            }

            if (imageThumbnail == null)
            {
                Transform thumbnailTransform = panelMessage?.Find("ImageThumbnail");
                thumbnailTransform ??= _panelDialogueRectTransform?.Find("Panel/ImageThumbnail");
                thumbnailTransform ??= transform.Find("Panel/Panel/ImageThumbnail");
                thumbnailTransform ??= transform.Find("ImageThumbnail");
                if (thumbnailTransform != null)
                {
                    imageThumbnail = thumbnailTransform.GetComponent<Image>();
                }
            }

            if (imageTail == null)
            {
                Transform tailTransform = _panelDialogueRectTransform?.Find("IconTail");
                tailTransform ??= panelMessage?.Find("IconTail");
                tailTransform ??= transform.Find("Panel/IconTail");
                tailTransform ??= transform.Find("IconTail");
                if (tailTransform != null)
                {
                    imageTail = tailTransform.GetComponent<Image>();
                }
            }

            _thumbnailRectTransform = imageThumbnail != null
                ? imageThumbnail.GetComponent<RectTransform>()
                : null;

            if (_thumbnailRectTransform != null && !_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            _tailRectTransform = imageTail != null
                ? imageTail.GetComponent<RectTransform>()
                : _tailRectTransform;

            if (_tailRectTransform != null && !_hasTailBaseScale)
            {
                _tailBaseScale = _tailRectTransform.localScale;
                _hasTailBaseScale = true;
            }

            if (panelMessage != null)
            {
                _panelMessageLayoutGroup = panelMessage.GetComponent<VerticalLayoutGroup>();
                _panelMessageLayoutElement = panelMessage.GetComponent<LayoutElement>();
                if (_panelMessageLayoutElement == null)
                {
                    _panelMessageLayoutElement = panelMessage.gameObject.AddComponent<LayoutElement>();
                }

                if (_panelMessageLayoutGroup != null && !_hasDefaultPanelMessagePadding)
                {
                    _defaultPanelMessagePaddingLeft = _panelMessageLayoutGroup.padding.left;
                    _defaultPanelMessagePaddingRight = _panelMessageLayoutGroup.padding.right;
                    _hasDefaultPanelMessagePadding = true;
                }

                if (_panelMessageLayoutElement != null && !_hasDefaultPanelMessageMinWidth)
                {
                    _defaultPanelMessageMinWidth = _panelMessageLayoutElement.minWidth;
                    _hasDefaultPanelMessageMinWidth = true;
                }
            }

            CacheSpeechBubbleEnterIndicatorReferences();
        }

        /// <summary>
        /// 말풍선 레이아웃 계산에 필요한 참조가 준비되어 있는지 확인합니다.
        /// </summary>
        /// <returns>참조가 준비되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureSpeechBubbleLayoutReferences()
        {
            if (panelMessage == null || _thumbnailRectTransform == null || _panelDialogueRectTransform == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            return panelMessage != null &&
                   _panelDialogueRectTransform != null &&
                   _thumbnailRectTransform != null;
        }

        /// <summary>
        /// 말꼬리 처리에 필요한 참조가 준비되었는지 확인합니다.
        /// </summary>
        /// <returns>말꼬리 참조가 준비되어 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryEnsureTailReferences()
        {
            if (imageTail == null || _tailRectTransform == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            return imageTail != null && _tailRectTransform != null;
        }

        /// <summary>
        /// 썸네일 스프라이트 바인딩 결과를 바탕으로 표시 상태를 갱신합니다.
        /// </summary>
        private void ApplyThumbnailVisibilityAfterBinding()
        {
            if (imageThumbnail == null)
            {
                return;
            }

            bool shouldShow = imageThumbnail.sprite != null;
            if (ResolveEffectiveThumbnailPositionType() == ConfigCommon.ThumbnailPositionType.None)
            {
                shouldShow = false;
            }

            if (shouldShow && dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                imageThumbnail.SetNativeSize();
            }

            imageThumbnail.gameObject.SetActive(shouldShow);
            if (!shouldShow)
            {
                RestoreThumbnailScaleToBase();
            }
        }

        /// <summary>
        /// 현재 프레임에서 썸네일이 실제로 보이는 상태인지 반환합니다.
        /// </summary>
        /// <returns>썸네일이 활성화되고 스프라이트가 유효하면 <see langword="true"/>를 반환합니다.</returns>
        private bool HasVisibleThumbnail()
        {
            return imageThumbnail != null &&
                   imageThumbnail.gameObject.activeSelf &&
                   imageThumbnail.sprite != null &&
                   ResolveEffectiveThumbnailPositionType() != ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 말풍선 모드에서 텍스트 패널 좌우 패딩을 썸네일 배치 방향에 맞게 조정합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        private void ApplySpeechBubblePanelPaddingByThumbnailSide(bool hasThumbnail, ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            if (_panelMessageLayoutGroup == null)
            {
                return;
            }

            if (!hasThumbnail)
            {
                RestoreSpeechBubblePanelPadding();
                float enterReservedWidthWhenNoThumbnail = GetEnterIndicatorReservedWidthPx();
                if (enterReservedWidthWhenNoThumbnail > 0f)
                {
                    _panelMessageLayoutGroup.padding.right += Mathf.CeilToInt(enterReservedWidthWhenNoThumbnail);
                }

                return;
            }

            bool isThumbnailLeft = thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
            _panelMessageLayoutGroup.padding.left = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnNonThumbnailSidePx);
            _panelMessageLayoutGroup.padding.right = isThumbnailLeft
                ? Mathf.Max(0, textPaddingOnNonThumbnailSidePx)
                : Mathf.Max(0, textPaddingOnThumbnailSidePx);

            float enterReservedWidth = GetEnterIndicatorReservedWidthPx();
            if (enterReservedWidth > 0f)
            {
                _panelMessageLayoutGroup.padding.right += Mathf.CeilToInt(enterReservedWidth);
            }
        }

        /// <summary>
        /// 말풍선 텍스트 패널 패딩을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubblePanelPadding()
        {
            if (_panelMessageLayoutGroup == null || !_hasDefaultPanelMessagePadding)
            {
                return;
            }

            _panelMessageLayoutGroup.padding.left = _defaultPanelMessagePaddingLeft;
            _panelMessageLayoutGroup.padding.right = _defaultPanelMessagePaddingRight;
        }

        /// <summary>
        /// 말꼬리 중심 대칭 규칙을 기반으로 패널 최소 너비를 갱신합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="side">썸네일 배치 방향 부호입니다.</param>
        /// <param name="thumbnailOffsetX">썸네일 X 오프셋입니다.</param>
        private void UpdatePanelMinimumWidthByTailSymmetry(bool hasThumbnail, float side, float thumbnailOffsetX)
        {
            if (_panelMessageLayoutElement == null)
            {
                return;
            }

            if (!useSymmetricLayoutByTail || minHalfExtentByTailPx <= 0f)
            {
                if (_hasDefaultPanelMessageMinWidth)
                {
                    _panelMessageLayoutElement.minWidth = _defaultPanelMessageMinWidth;
                }

                return;
            }

            float thumbnailWidth = hasThumbnail && _thumbnailRectTransform != null ? _thumbnailRectTransform.rect.width : 0f;
            float thumbnailSpan = hasThumbnail ? Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth : 0f;
            float requiredPanelWidth = (2f * Mathf.Max(0f, minHalfExtentByTailPx)) - thumbnailSpan - (side * thumbnailOffsetX);
            _panelMessageLayoutElement.minWidth = Mathf.Max(0f, requiredPanelWidth);
        }

        /// <summary>
        /// 말풍선 텍스트 패널의 중심 X를 설정합니다.
        /// </summary>
        /// <param name="panelCenterX">설정할 중심 X 값입니다.</param>
        private void SetPanelMessageCenterX(float panelCenterX)
        {
            if (panelMessage == null)
            {
                return;
            }

            Vector2 anchoredPosition = panelMessage.anchoredPosition;
            anchoredPosition.x = panelCenterX;
            panelMessage.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// 썸네일이 텍스트 패널 하위에 배치되어 있는지 확인합니다.
        /// 텍스트 패널 자식이면 패널 기준 상대 좌표를 사용하고, 다른 부모이면 루트 좌표 변환을 사용합니다.
        /// </summary>
        /// <returns>썸네일 부모가 텍스트 패널이면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsThumbnailChildOfPanel()
        {
            return _thumbnailRectTransform != null &&
                   panelMessage != null &&
                   _thumbnailRectTransform.parent == panelMessage;
        }

        /// <summary>
        /// 말풍선 루트 좌표계의 X 값을 지정한 부모 RectTransform의 로컬 X 값으로 변환합니다.
        /// 썸네일과 텍스트 패널의 부모가 다를 때 Left/Right 오프셋이 다른 좌표계에 적용되는 문제를 방지합니다.
        /// </summary>
        /// <param name="parentRectTransform">변환 기준 부모 RectTransform입니다.</param>
        /// <param name="rootSpaceX">말풍선 루트 좌표계 기준 X 값입니다.</param>
        /// <returns>부모 로컬 좌표계의 X 값입니다.</returns>
        private float ConvertRootSpaceXToParentLocalX(RectTransform parentRectTransform, float rootSpaceX)
        {
            if (_panelDialogueRectTransform == null || parentRectTransform == null)
            {
                return rootSpaceX;
            }

            Vector3 worldPoint = _panelDialogueRectTransform.TransformPoint(new Vector3(rootSpaceX, 0f, 0f));
            Vector3 parentLocalPoint = parentRectTransform.InverseTransformPoint(worldPoint);
            return parentLocalPoint.x;
        }

        /// <summary>
        /// 화자 방향을 반영한 말꼬리 X 오프셋을 계산합니다.
        /// </summary>
        /// <returns>말꼬리의 목표 anchoredPosition.x 입니다.</returns>
        private float ResolveTailAnchorX()
        {
            if (tailForwardOffsetPx <= 0f)
            {
                return 0f;
            }

            if (TryResolveSpeakerFacingRight(out bool isFacingRight))
            {
                return isFacingRight ? tailForwardOffsetPx : -tailForwardOffsetPx;
            }

            return 0f;
        }

        /// <summary>
        /// 말꼬리 X 위치를 갱신하고 변경 여부를 반환합니다.
        /// </summary>
        /// <returns>말꼬리 X 값이 변경되었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool RefreshTailAnchorPosition()
        {
            if (!TryEnsureTailReferences())
            {
                return false;
            }

            float targetTailX = ResolveTailAnchorX();
            Vector2 anchoredPosition = _tailRectTransform.anchoredPosition;
            if (Mathf.Abs(anchoredPosition.x - targetTailX) <= 0.01f)
            {
                return false;
            }

            anchoredPosition.x = targetTailX;
            _tailRectTransform.anchoredPosition = anchoredPosition;
            return true;
        }

        /// <summary>
        /// 말꼬리 이미지를 화자 방향에 맞게 좌우 반전합니다.
        /// </summary>
        private void ApplyTailFlip()
        {
            if (!TryEnsureTailReferences())
            {
                return;
            }

            if (!_hasTailBaseScale)
            {
                _tailBaseScale = _tailRectTransform.localScale;
                _hasTailBaseScale = true;
            }

            bool isFacingRight = true;
            if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
            {
                isFacingRight = speakerFacingRight;
            }

            float baseAbsX = Mathf.Abs(_tailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = isFacingRight ? baseAbsX : -baseAbsX;
            _tailRectTransform.localScale = new Vector3(
                x,
                _tailBaseScale.y,
                _tailBaseScale.z);
        }

        /// <summary>
        /// 썸네일 이미지에 Flip 정책을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (_thumbnailRectTransform == null || !HasVisibleThumbnail())
            {
                return;
            }

            if (!_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            bool shouldFlip = ResolveShouldFlipThumbnail();
            float baseAbsX = Mathf.Abs(_thumbnailBaseScale.x);
            if (baseAbsX <= Mathf.Epsilon)
            {
                baseAbsX = 1f;
            }

            float x = shouldFlip ? -baseAbsX : baseAbsX;
            _thumbnailRectTransform.localScale = new Vector3(
                x,
                _thumbnailBaseScale.y,
                _thumbnailBaseScale.z);
        }

        /// <summary>
        /// 썸네일 Flip 정책과 배치 방향을 조합해 좌우 반전 필요 여부를 계산합니다.
        /// </summary>
        /// <returns>좌우 반전이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveShouldFlipThumbnail()
        {
            switch (ResolveCurrentThumbnailFlipPolicy())
            {
                case DialogueBalloonThumbnailFlipPolicy.KeepOriginal:
                    return false;

                case DialogueBalloonThumbnailFlipPolicy.ForceFlip:
                    return true;

                case DialogueBalloonThumbnailFlipPolicy.AutoBySpeakerFacing:
                    if (TryResolveSpeakerFacingRight(out bool speakerFacingRight))
                    {
                        return ShouldFlipToDesiredFacing(speakerFacingRight);
                    }

                    return ShouldFlipToDesiredFacing(ResolveDesiredFacingRightByThumbnailPosition());

                case DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition:
                default:
                    return ShouldFlipToDesiredFacing(ResolveDesiredFacingRightByThumbnailPosition());
            }
        }

        /// <summary>
        /// 현재 출력 중인 대사 노드의 썸네일 Flip 정책을 반환합니다.
        /// 노드 데이터가 아직 바인딩되지 않은 경우 기존 말풍선 기본 동작을 유지합니다.
        /// </summary>
        /// <returns>현재 노드에 적용할 썸네일 Flip 정책입니다.</returns>
        private DialogueBalloonThumbnailFlipPolicy ResolveCurrentThumbnailFlipPolicy()
        {
            return _currentDialogueNode != null
                ? _currentDialogueNode.thumbnailFlipPolicy
                : DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;
        }

        /// <summary>
        /// 현재 출력 중인 대사 노드의 원본 썸네일 바라보기 방향을 반환합니다.
        /// 노드 데이터가 아직 바인딩되지 않은 경우 오른쪽 방향을 기본값으로 사용합니다.
        /// </summary>
        /// <returns>현재 노드에 적용할 원본 썸네일 방향입니다.</returns>
        private DialogueBalloonThumbnailSourceFacing ResolveCurrentThumbnailSourceFacing()
        {
            return _currentDialogueNode != null
                ? _currentDialogueNode.thumbnailSourceFacing
                : DialogueBalloonThumbnailSourceFacing.Right;
        }

        /// <summary>
        /// 썸네일 배치 기준으로 목표 바라보기 방향을 계산합니다.
        /// </summary>
        /// <returns>오른쪽을 바라봐야 하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ResolveDesiredFacingRightByThumbnailPosition()
        {
            return ResolveEffectiveThumbnailPositionType() == ConfigCommon.ThumbnailPositionType.Left;
        }

        /// <summary>
        /// 원본 썸네일 방향과 목표 방향을 비교해 Flip 필요 여부를 반환합니다.
        /// </summary>
        /// <param name="desiredFacingRight">목표 방향이 오른쪽이면 <see langword="true"/>입니다.</param>
        /// <returns>Flip 이 필요하면 <see langword="true"/>를 반환합니다.</returns>
        private bool ShouldFlipToDesiredFacing(bool desiredFacingRight)
        {
            bool sourceFacingRight = ResolveCurrentThumbnailSourceFacing() == DialogueBalloonThumbnailSourceFacing.Right;
            return sourceFacingRight != desiredFacingRight;
        }

        /// <summary>
        /// 현재 화자 캐릭터의 방향에서 수평(좌/우) 방향을 추출합니다.
        /// Player 대사에서는 플레이어 방향을, NPC/Monster 대사에서는 해당 캐릭터 방향을 기준으로 사용합니다.
        /// </summary>
        /// <param name="isFacingRight">오른쪽을 바라보면 <see langword="true"/>를 반환합니다.</param>
        /// <returns>좌우 방향을 판별할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryResolveSpeakerFacingRight(out bool isFacingRight)
        {
            isFacingRight = false;
            CharacterBase speaker = ResolveCurrentSpeakerCharacter();
            if (speaker == null)
            {
                return false;
            }

            CharacterConstants.FacingDirection8 facing = speaker.CurrentFacing;
            switch (facing)
            {
                case CharacterConstants.FacingDirection8.Right:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.DownRight:
                    isFacingRight = true;
                    return true;

                case CharacterConstants.FacingDirection8.Left:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
                    isFacingRight = false;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 썸네일 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreThumbnailScaleToBase()
        {
            if (_thumbnailRectTransform == null || !_hasThumbnailBaseScale)
            {
                return;
            }

            _thumbnailRectTransform.localScale = _thumbnailBaseScale;
        }

        /// <summary>
        /// 말꼬리 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreTailScaleToBase()
        {
            if (_tailRectTransform == null || !_hasTailBaseScale)
            {
                return;
            }

            _tailRectTransform.localScale = _tailBaseScale;
        }

        /// <summary>
        /// 말풍선 레이아웃 커스텀 값을 기본 상태로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubbleLayoutDefaults()
        {
            RestoreSpeechBubblePanelPadding();

            if (_panelMessageLayoutElement != null && _hasDefaultPanelMessageMinWidth)
            {
                _panelMessageLayoutElement.minWidth = _defaultPanelMessageMinWidth;
            }

            RestoreThumbnailScaleToBase();
            RestoreTailScaleToBase();

            if (_tailRectTransform != null)
            {
                Vector2 anchoredPosition = _tailRectTransform.anchoredPosition;
                anchoredPosition.x = 0f;
                _tailRectTransform.anchoredPosition = anchoredPosition;
            }

            ResetSpeechBubbleEnterIndicatorState();
        }

        /// <summary>
        /// 말풍선 모드에서 프레임 단위로 필요한 시각 상태를 갱신합니다.
        /// </summary>
        private void RefreshSpeechBubbleRuntimeVisuals()
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble)
            {
                SetSpeechBubbleEnterIndicatorVisible(false, 1f);
                return;
            }

            bool didTailAnchorChange = false;
            if (!(imageTail == null && _tailRectTransform == null))
            {
                didTailAnchorChange = RefreshTailAnchorPosition();
                ApplyTailFlip();
            }

            ApplyThumbnailFlip();

            if (didTailAnchorChange)
            {
                RefreshThumbnailPosition();
            }
        }
    }
}
