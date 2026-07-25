using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/>의 말풍선 레이아웃과 화자 방향 시각 처리를 담당합니다.
    /// </summary>
    public partial class UIWindowDialogue
    {
        /// <summary>
        /// 말풍선 패널, 썸네일, 말꼬리 정렬을 현재 노드 기준으로 갱신합니다.
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
                    float thumbnailWidth =
                        _thumbnailRectTransform != null ? _thumbnailRectTransform.rect.width : 0f;
                    float thumbnailSpan = Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth;
                    panelCenterX =
                        tailX - (side * thumbnailSpan * 0.5f) - (thumbnailOffset.x * 0.5f);
                }
            }

            SetPanelMessageCenterX(panelCenterX);

            if (hasThumbnail && _thumbnailRectTransform != null)
            {
                float thumbnailHalfWidth = _thumbnailRectTransform.rect.width * 0.5f;
                float thumbnailCenterXInRootSpace =
                    panelCenterX +
                    side * (panelHalfWidth + Mathf.Max(0f, thumbnailGapPx) + thumbnailHalfWidth) +
                    thumbnailOffset.x;
                RectTransform thumbnailParentRectTransform =
                    _thumbnailRectTransform.parent as RectTransform;
                float thumbnailCenterX = IsThumbnailChildOfPanel()
                    ? side * (panelHalfWidth + Mathf.Max(0f, thumbnailGapPx) + thumbnailHalfWidth) +
                      thumbnailOffset.x
                    : ConvertRootSpaceXToParentLocalX(
                        thumbnailParentRectTransform,
                        thumbnailCenterXInRootSpace);

                Vector2 thumbnailAnchoredPosition = _thumbnailRectTransform.anchoredPosition;
                thumbnailAnchoredPosition.x = thumbnailCenterX;
                thumbnailAnchoredPosition.y = thumbnailOffset.y;
                _thumbnailRectTransform.anchoredPosition = thumbnailAnchoredPosition;
            }

            ApplyThumbnailFlip();
            RefreshSpeechBubbleEnterIndicatorPosition();
        }

        /// <summary>
        /// 말풍선 레이아웃 참조를 캐시하고, 비어 있는 참조는 이름 기반으로 보정합니다.
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

            _thumbnailRectTransform =
                imageThumbnail != null ? imageThumbnail.GetComponent<RectTransform>() : null;
            if (_thumbnailRectTransform != null && !_hasThumbnailBaseScale)
            {
                _thumbnailBaseScale = _thumbnailRectTransform.localScale;
                _hasThumbnailBaseScale = true;
            }

            _tailRectTransform =
                imageTail != null ? imageTail.GetComponent<RectTransform>() : _tailRectTransform;
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
        /// 말풍선 레이아웃 계산에 필요한 참조가 준비되었는지 확인합니다.
        /// </summary>
        /// <returns>필수 참조가 준비되었으면 <see langword="true"/>입니다.</returns>
        private bool TryEnsureSpeechBubbleLayoutReferences()
        {
            if (panelMessage == null ||
                _thumbnailRectTransform == null ||
                _panelDialogueRectTransform == null)
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
        /// <returns>말꼬리 참조가 준비되었으면 <see langword="true"/>입니다.</returns>
        private bool TryEnsureTailReferences()
        {
            if (imageTail == null || _tailRectTransform == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            return imageTail != null && _tailRectTransform != null;
        }

        /// <summary>
        /// 현재 썸네일 스프라이트와 노드 배치 설정에 맞춰 표시 상태를 갱신합니다.
        /// </summary>
        private void ApplyThumbnailVisibilityAfterBinding()
        {
            if (imageThumbnail == null)
            {
                return;
            }

            bool shouldShow =
                imageThumbnail.sprite != null &&
                ResolveEffectiveThumbnailPositionType() != ConfigCommon.ThumbnailPositionType.None;
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
        /// 썸네일이 현재 실제로 표시 가능한 상태인지 확인합니다.
        /// </summary>
        /// <returns>활성 썸네일이 있으면 <see langword="true"/>입니다.</returns>
        private bool HasVisibleThumbnail()
        {
            return imageThumbnail != null &&
                   imageThumbnail.gameObject.activeSelf &&
                   imageThumbnail.sprite != null &&
                   ResolveEffectiveThumbnailPositionType() != ConfigCommon.ThumbnailPositionType.None;
        }

        /// <summary>
        /// 썸네일 방향과 입력 안내 이미지 너비를 반영해 텍스트 패딩을 갱신합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="thumbnailPositionType">썸네일 배치 방향입니다.</param>
        private void ApplySpeechBubblePanelPaddingByThumbnailSide(
            bool hasThumbnail,
            ConfigCommon.ThumbnailPositionType thumbnailPositionType)
        {
            if (_panelMessageLayoutGroup == null)
            {
                return;
            }

            if (!hasThumbnail)
            {
                RestoreSpeechBubblePanelPadding();
            }
            else
            {
                bool isThumbnailLeft =
                    thumbnailPositionType == ConfigCommon.ThumbnailPositionType.Left;
                _panelMessageLayoutGroup.padding.left = isThumbnailLeft
                    ? Mathf.Max(0, textPaddingOnThumbnailSidePx)
                    : Mathf.Max(0, textPaddingOnNonThumbnailSidePx);
                _panelMessageLayoutGroup.padding.right = isThumbnailLeft
                    ? Mathf.Max(0, textPaddingOnNonThumbnailSidePx)
                    : Mathf.Max(0, textPaddingOnThumbnailSidePx);
            }

            float reservedWidth = GetEnterIndicatorReservedWidthPx();
            if (reservedWidth > 0f)
            {
                _panelMessageLayoutGroup.padding.right += Mathf.CeilToInt(reservedWidth);
            }
        }

        /// <summary>
        /// 텍스트 패널 패딩을 프리팹 기본값으로 복원합니다.
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
        /// 말꼬리 중심 대칭 규칙을 기준으로 패널 최소 너비를 갱신합니다.
        /// </summary>
        /// <param name="hasThumbnail">썸네일 표시 여부입니다.</param>
        /// <param name="side">썸네일 배치 방향 부호입니다.</param>
        /// <param name="thumbnailOffsetX">썸네일 X 오프셋입니다.</param>
        private void UpdatePanelMinimumWidthByTailSymmetry(
            bool hasThumbnail,
            float side,
            float thumbnailOffsetX)
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

            float thumbnailWidth =
                hasThumbnail && _thumbnailRectTransform != null
                    ? _thumbnailRectTransform.rect.width
                    : 0f;
            float thumbnailSpan =
                hasThumbnail ? Mathf.Max(0f, thumbnailGapPx) + thumbnailWidth : 0f;
            float requiredPanelWidth =
                2f * Mathf.Max(0f, minHalfExtentByTailPx) -
                thumbnailSpan -
                side * thumbnailOffsetX;
            _panelMessageLayoutElement.minWidth = Mathf.Max(0f, requiredPanelWidth);
        }

        /// <summary>
        /// 텍스트 패널의 중심 X 좌표를 설정합니다.
        /// </summary>
        /// <param name="panelCenterX">적용할 중심 X 좌표입니다.</param>
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
        /// 썸네일이 텍스트 패널의 직접 자식인지 확인합니다.
        /// </summary>
        /// <returns>직접 자식이면 <see langword="true"/>입니다.</returns>
        private bool IsThumbnailChildOfPanel()
        {
            return _thumbnailRectTransform != null &&
                   panelMessage != null &&
                   _thumbnailRectTransform.parent == panelMessage;
        }

        /// <summary>
        /// 말풍선 루트 좌표의 X값을 지정한 부모 RectTransform 로컬 좌표로 변환합니다.
        /// </summary>
        /// <param name="parentRectTransform">변환 기준 부모입니다.</param>
        /// <param name="rootSpaceX">말풍선 루트 기준 X값입니다.</param>
        /// <returns>부모 로컬 좌표계의 X값입니다.</returns>
        private float ConvertRootSpaceXToParentLocalX(
            RectTransform parentRectTransform,
            float rootSpaceX)
        {
            if (_panelDialogueRectTransform == null || parentRectTransform == null)
            {
                return rootSpaceX;
            }

            Vector3 worldPoint =
                _panelDialogueRectTransform.TransformPoint(new Vector3(rootSpaceX, 0f, 0f));
            return parentRectTransform.InverseTransformPoint(worldPoint).x;
        }

        /// <summary>
        /// 화자 방향을 반영한 말꼬리 X 좌표를 계산합니다.
        /// </summary>
        /// <returns>말꼬리의 목표 anchoredPosition.x입니다.</returns>
        private float ResolveTailAnchorX()
        {
            if (tailForwardOffsetPx <= 0f ||
                !TryResolveSpeakerFacingRight(out bool isFacingRight))
            {
                return 0f;
            }

            return isFacingRight ? tailForwardOffsetPx : -tailForwardOffsetPx;
        }

        /// <summary>
        /// 말꼬리 X 위치를 갱신합니다.
        /// </summary>
        /// <returns>위치가 변경되었으면 <see langword="true"/>입니다.</returns>
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
        /// 말꼬리 이미지를 화자 방향에 맞춰 좌우 반전합니다.
        /// </summary>
        private void ApplyTailFlip()
        {
            if (!TryEnsureTailReferences())
            {
                return;
            }

            bool isFacingRight =
                !TryResolveSpeakerFacingRight(out bool speakerFacingRight) || speakerFacingRight;
            float baseAbsX = Mathf.Max(Mathf.Abs(_tailBaseScale.x), Mathf.Epsilon);
            _tailRectTransform.localScale = new Vector3(
                isFacingRight ? baseAbsX : -baseAbsX,
                _tailBaseScale.y,
                _tailBaseScale.z);
        }

        /// <summary>
        /// 현재 노드의 썸네일 반전 정책을 적용합니다.
        /// </summary>
        private void ApplyThumbnailFlip()
        {
            if (_thumbnailRectTransform == null || !HasVisibleThumbnail())
            {
                return;
            }

            float baseAbsX = Mathf.Max(Mathf.Abs(_thumbnailBaseScale.x), Mathf.Epsilon);
            _thumbnailRectTransform.localScale = new Vector3(
                ResolveShouldFlipThumbnail() ? -baseAbsX : baseAbsX,
                _thumbnailBaseScale.y,
                _thumbnailBaseScale.z);
        }

        /// <summary>
        /// 현재 노드의 정책과 목표 방향을 조합해 썸네일 반전 필요 여부를 반환합니다.
        /// </summary>
        /// <returns>좌우 반전이 필요하면 <see langword="true"/>입니다.</returns>
        private bool ResolveShouldFlipThumbnail()
        {
            DialogueBalloonThumbnailFlipPolicy policy = _currentDialogue != null
                ? _currentDialogue.thumbnailFlipPolicy
                : DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;

            switch (policy)
            {
                case DialogueBalloonThumbnailFlipPolicy.KeepOriginal:
                    return false;
                case DialogueBalloonThumbnailFlipPolicy.ForceFlip:
                    return true;
                case DialogueBalloonThumbnailFlipPolicy.AutoBySpeakerFacing:
                    return ShouldFlipToDesiredFacing(
                        TryResolveSpeakerFacingRight(out bool speakerFacingRight)
                            ? speakerFacingRight
                            : ResolveDesiredFacingRightByThumbnailPosition());
                case DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition:
                default:
                    return ShouldFlipToDesiredFacing(
                        ResolveDesiredFacingRightByThumbnailPosition());
            }
        }

        /// <summary>
        /// 썸네일 배치 위치를 기준으로 목표 바라보기 방향을 계산합니다.
        /// </summary>
        /// <returns>오른쪽을 바라봐야 하면 <see langword="true"/>입니다.</returns>
        private bool ResolveDesiredFacingRightByThumbnailPosition()
        {
            return ResolveEffectiveThumbnailPositionType() ==
                   ConfigCommon.ThumbnailPositionType.Left;
        }

        /// <summary>
        /// 원본 썸네일 방향과 목표 방향을 비교합니다.
        /// </summary>
        /// <param name="desiredFacingRight">목표 방향이 오른쪽인지 여부입니다.</param>
        /// <returns>반전이 필요하면 <see langword="true"/>입니다.</returns>
        private bool ShouldFlipToDesiredFacing(bool desiredFacingRight)
        {
            DialogueBalloonThumbnailSourceFacing sourceFacing =
                _currentDialogue != null
                    ? _currentDialogue.thumbnailSourceFacing
                    : DialogueBalloonThumbnailSourceFacing.Right;
            return (sourceFacing == DialogueBalloonThumbnailSourceFacing.Right) != desiredFacingRight;
        }

        /// <summary>
        /// 현재 화자의 8방향 값에서 수평 방향을 추출합니다.
        /// </summary>
        /// <param name="isFacingRight">오른쪽을 바라보는지 여부입니다.</param>
        /// <returns>수평 방향을 판별할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool TryResolveSpeakerFacingRight(out bool isFacingRight)
        {
            isFacingRight = false;
            CharacterBase speaker = ResolveCurrentSpeakerCharacter();
            if (speaker == null)
            {
                return false;
            }

            switch (speaker.CurrentFacing)
            {
                case CharacterConstants.FacingDirection8.Right:
                case CharacterConstants.FacingDirection8.UpRight:
                case CharacterConstants.FacingDirection8.DownRight:
                    isFacingRight = true;
                    return true;
                case CharacterConstants.FacingDirection8.Left:
                case CharacterConstants.FacingDirection8.UpLeft:
                case CharacterConstants.FacingDirection8.DownLeft:
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
            if (_thumbnailRectTransform != null && _hasThumbnailBaseScale)
            {
                _thumbnailRectTransform.localScale = _thumbnailBaseScale;
            }
        }

        /// <summary>
        /// 말꼬리 스케일을 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreTailScaleToBase()
        {
            if (_tailRectTransform != null && _hasTailBaseScale)
            {
                _tailRectTransform.localScale = _tailBaseScale;
            }
        }

        /// <summary>
        /// 말풍선 레이아웃에서 변경한 런타임 값을 프리팹 기본 상태로 복원합니다.
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
        /// 말풍선 모드에서 화자 방향에 따라 달라지는 시각 상태를 프레임 단위로 갱신합니다.
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
