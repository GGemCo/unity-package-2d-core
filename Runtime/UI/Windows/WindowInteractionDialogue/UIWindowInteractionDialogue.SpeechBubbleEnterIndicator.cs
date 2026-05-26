using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 말풍선 입력 안내 이미지 처리 책임을 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 말풍선 입력 안내 이미지 참조를 캐시합니다.
        /// </summary>
        private void CacheSpeechBubbleEnterIndicatorReferences()
        {
            if (imageEnter == null)
            {
                Transform enterTransform = panelMessage?.Find("ImageEnter");
                enterTransform ??= _panelDialogueRectTransform?.Find("Panel/ImageEnter");
                enterTransform ??= _panelDialogueRectTransform?.Find("ImageEnter");
                enterTransform ??= transform.Find("PanelDialogue/Panel/ImageEnter");
                enterTransform ??= transform.Find("ImageEnter");
                if (enterTransform != null)
                {
                    imageEnter = enterTransform.GetComponent<Image>();
                }
            }

            _enterRectTransform = imageEnter != null
                ? imageEnter.GetComponent<RectTransform>()
                : null;

            if (imageEnter != null && !_hasEnterBaseColor)
            {
                _enterBaseColor = imageEnter.color;
                _hasEnterBaseColor = true;
            }

            if (_enterRectTransform != null && !_hasEnterBaseAnchoredPosition)
            {
                _enterBaseAnchoredPosition = _enterRectTransform.anchoredPosition;
                _hasEnterBaseAnchoredPosition = true;
            }
        }

        /// <summary>
        /// 말풍선 입력 안내 이미지 기본값을 설정 정책에 맞게 해석합니다.
        /// </summary>
        private void ResolveSpeechBubbleEnterIndicatorDefaults()
        {
            if (useProjectEnterIndicatorDefaultsInSpeechBubble)
            {
                DialogueBalloonSettingsRuntimeResolver.ResolveEnterIndicatorDefaults(
                    enterIndicatorGapPx,
                    enterIndicatorBlinkHz,
                    enterIndicatorMinAlpha,
                    out _resolvedEnterIndicatorGapPx,
                    out _resolvedEnterIndicatorBlinkHz,
                    out _resolvedEnterIndicatorMinAlpha,
                    out Sprite resolvedSprite);

                if (imageEnter == null)
                {
                    CacheSpeechBubbleEnterIndicatorReferences();
                }

                if (imageEnter != null && resolvedSprite != null)
                {
                    imageEnter.sprite = resolvedSprite;
                }

                return;
            }

            _resolvedEnterIndicatorGapPx = Mathf.Max(0f, enterIndicatorGapPx);
            _resolvedEnterIndicatorBlinkHz = Mathf.Max(0f, enterIndicatorBlinkHz);
            _resolvedEnterIndicatorMinAlpha = Mathf.Clamp01(enterIndicatorMinAlpha);

            if (imageEnter == null)
            {
                CacheSpeechBubbleEnterIndicatorReferences();
            }

            if (imageEnter != null && enterIndicatorSpriteOverride != null)
            {
                imageEnter.sprite = enterIndicatorSpriteOverride;
            }
        }

        /// <summary>
        /// 현재 설정에서 말풍선 입력 안내 이미지 기능을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>말풍선 모드이고 입력 안내 이미지 참조가 준비되었으면 <see langword="true"/>를 반환합니다.</returns>
        private bool HasConfiguredSpeechBubbleEnterIndicator()
        {
            return dialogueVisualMode == DialogueVisualMode.SpeechBubble &&
                   imageEnter != null &&
                   _enterRectTransform != null &&
                   imageEnter.sprite != null;
        }

        /// <summary>
        /// 말풍선 레이아웃 우측에 입력 안내 이미지 공간을 예약하기 위한 너비(px)를 계산합니다.
        /// </summary>
        /// <returns>예약할 우측 공간(px)입니다.</returns>
        private float GetEnterIndicatorReservedWidthPx()
        {
            if (!HasConfiguredSpeechBubbleEnterIndicator())
            {
                return 0f;
            }

            float enterWidth = Mathf.Max(0f, _enterRectTransform.rect.width);
            if (enterWidth <= 0f)
            {
                return 0f;
            }

            return enterWidth + _resolvedEnterIndicatorGapPx;
        }

        /// <summary>
        /// 입력 안내 이미지 표시 사이클 시작 시 상태를 초기화합니다.
        /// </summary>
        private void PrepareSpeechBubbleEnterIndicatorForNewSession()
        {
            CacheSpeechBubbleEnterIndicatorReferences();
            ResolveSpeechBubbleEnterIndicatorDefaults();
            CacheSpeechBubbleEnterIndicatorReferences();
            ApplySpeechBubbleEnterIndicatorNativeSize();
            RestoreSpeechBubbleEnterIndicatorAnchoredPosition();
            SetSpeechBubbleEnterIndicatorVisible(false, 1f);
            _lastKnownEnterIndicatorVisibleCharacters = -1;
        }

        /// <summary>
        /// 입력 안내 이미지의 런타임 상태를 기본값으로 되돌립니다.
        /// </summary>
        private void ResetSpeechBubbleEnterIndicatorState()
        {
            _lastKnownEnterIndicatorVisibleCharacters = -1;
            RestoreSpeechBubbleEnterIndicatorAnchoredPosition();
            SetSpeechBubbleEnterIndicatorVisible(false, 1f);
        }

        /// <summary>
        /// 입력 안내 이미지 RectTransform 크기를 스프라이트 원본 크기로 갱신합니다.
        /// </summary>
        private void ApplySpeechBubbleEnterIndicatorNativeSize()
        {
            if (imageEnter == null || imageEnter.sprite == null)
            {
                return;
            }

            imageEnter.SetNativeSize();
            if (_enterRectTransform == null)
            {
                _enterRectTransform = imageEnter.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// 입력 안내 이미지를 프리팹 기본 anchoredPosition으로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubbleEnterIndicatorAnchoredPosition()
        {
            if (_enterRectTransform == null || !_hasEnterBaseAnchoredPosition)
            {
                return;
            }

            _enterRectTransform.anchoredPosition = _enterBaseAnchoredPosition;
        }

        /// <summary>
        /// 입력 안내 이미지 알파/활성 상태를 적용합니다.
        /// </summary>
        /// <param name="isVisible">표시 여부입니다.</param>
        /// <param name="alphaMultiplier">원본 알파 대비 배율(0~1)입니다.</param>
        private void SetSpeechBubbleEnterIndicatorVisible(bool isVisible, float alphaMultiplier)
        {
            if (imageEnter == null)
            {
                return;
            }

            if (!_hasEnterBaseColor)
            {
                _enterBaseColor = imageEnter.color;
                _hasEnterBaseColor = true;
            }

            float normalizedMultiplier = Mathf.Clamp01(alphaMultiplier);
            Color color = _enterBaseColor;
            color.a = _enterBaseColor.a * normalizedMultiplier;
            imageEnter.color = color;

            if (imageEnter.gameObject.activeSelf != isVisible)
            {
                imageEnter.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 말풍선 모드에서 입력 안내 이미지의 위치와 깜빡임 상태를 프레임 단위로 갱신합니다.
        /// </summary>
        private void UpdateSpeechBubbleEnterIndicatorRuntime()
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble)
            {
                SetSpeechBubbleEnterIndicatorVisible(false, 1f);
                return;
            }

            if (!HasConfiguredSpeechBubbleEnterIndicator())
            {
                return;
            }

            int currentVisibleCharacters = textMessage != null ? textMessage.maxVisibleCharacters : -1;
            if (currentVisibleCharacters != _lastKnownEnterIndicatorVisibleCharacters)
            {
                _lastKnownEnterIndicatorVisibleCharacters = currentVisibleCharacters;
                RefreshSpeechBubbleEnterIndicatorPosition();
            }

            if (!_messagePlayer.IsCurrentPageFullyRevealed)
            {
                SetSpeechBubbleEnterIndicatorVisible(false, 1f);
                return;
            }

            float alphaMultiplier = _resolvedEnterIndicatorBlinkHz <= 0f
                ? 1f
                : Mathf.Lerp(
                    _resolvedEnterIndicatorMinAlpha,
                    1f,
                    Mathf.PingPong(Time.unscaledTime * _resolvedEnterIndicatorBlinkHz, 1f));

            SetSpeechBubbleEnterIndicatorVisible(true, alphaMultiplier);
        }

        /// <summary>
        /// 입력 안내 이미지를 현재 대사 마지막 글자 우측에 배치합니다.
        /// 계산 실패 시에는 기존 anchoredPosition을 유지합니다.
        /// </summary>
        private void RefreshSpeechBubbleEnterIndicatorPosition()
        {
            if (!HasConfiguredSpeechBubbleEnterIndicator() || panelMessage == null || textMessage == null)
            {
                return;
            }

            if (!TryResolveSpeechBubbleEnterCenterInPanelSpace(out Vector2 centerInPanelSpace))
            {
                return;
            }

            SetThumbnailLocalPositionRelativeToPanel(_enterRectTransform, centerInPanelSpace);
        }

        /// <summary>
        /// 현재 표시 중인 대사 마지막 글자 위치를 기준으로 입력 안내 이미지 중심 좌표를 계산합니다.
        /// </summary>
        /// <param name="centerInPanelSpace">계산된 panelMessage 로컬 좌표 기준 중심점입니다.</param>
        /// <returns>계산에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryResolveSpeechBubbleEnterCenterInPanelSpace(out Vector2 centerInPanelSpace)
        {
            centerInPanelSpace = default;
            if (_enterRectTransform == null || panelMessage == null || textMessage == null)
            {
                return false;
            }

            textMessage.ForceMeshUpdate();
            TMP_TextInfo textInfo = textMessage.textInfo;
            if (textInfo == null || textInfo.characterCount <= 0)
            {
                return false;
            }

            int visibleCharacterCount = textMessage.maxVisibleCharacters == int.MaxValue
                ? textInfo.characterCount
                : Mathf.Min(textMessage.maxVisibleCharacters, textInfo.characterCount);
            if (visibleCharacterCount <= 0)
            {
                return false;
            }

            int lastVisibleIndex = visibleCharacterCount - 1;
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[lastVisibleIndex];
            float enterHalfWidth = Mathf.Max(0f, _enterRectTransform.rect.width * 0.5f);
            float tailX = characterInfo.xAdvance + _resolvedEnterIndicatorGapPx + enterHalfWidth;
            float tailY = ResolveCharacterMidY(textInfo, characterInfo);

            Vector3 centerInWorld = textMessage.rectTransform.TransformPoint(new Vector3(tailX, tailY, 0f));
            Vector3 centerInPanelLocal = panelMessage.InverseTransformPoint(centerInWorld);
            centerInPanelSpace = new Vector2(centerInPanelLocal.x, centerInPanelLocal.y);
            return true;
        }

        /// <summary>
        /// TMP 문자 정보에서 입력 안내 이미지가 따라갈 Y 중심 좌표를 계산합니다.
        /// </summary>
        /// <param name="textInfo">현재 텍스트 메쉬 정보입니다.</param>
        /// <param name="characterInfo">기준 문자 정보입니다.</param>
        /// <returns>기준 문자 또는 라인의 중심 Y 좌표입니다.</returns>
        private static float ResolveCharacterMidY(TMP_TextInfo textInfo, TMP_CharacterInfo characterInfo)
        {
            if (characterInfo.isVisible)
            {
                return (characterInfo.ascender + characterInfo.descender) * 0.5f;
            }

            if (textInfo != null && textInfo.lineCount > 0)
            {
                int lineIndex = Mathf.Clamp(characterInfo.lineNumber, 0, textInfo.lineCount - 1);
                TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];
                return (lineInfo.ascender + lineInfo.descender) * 0.5f;
            }

            return (characterInfo.ascender + characterInfo.descender) * 0.5f;
        }
    }
}
