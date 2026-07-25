using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/> 말풍선의 입력 안내 이미지 표시를 담당합니다.
    /// </summary>
    public partial class UIWindowDialogue
    {
        /// <summary>
        /// 입력 안내 이미지와 RectTransform 참조 및 기본값을 캐시합니다.
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

            _enterRectTransform =
                imageEnter != null ? imageEnter.GetComponent<RectTransform>() : null;
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
        /// 프로젝트 공통 설정 또는 현재 프리팹 설정에서 입력 안내 이미지 기본값을 결정합니다.
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

                if (imageEnter != null && resolvedSprite != null)
                {
                    imageEnter.sprite = resolvedSprite;
                }

                return;
            }

            _resolvedEnterIndicatorGapPx = Mathf.Max(0f, enterIndicatorGapPx);
            _resolvedEnterIndicatorBlinkHz = Mathf.Max(0f, enterIndicatorBlinkHz);
            _resolvedEnterIndicatorMinAlpha = Mathf.Clamp01(enterIndicatorMinAlpha);
            if (imageEnter != null && enterIndicatorSpriteOverride != null)
            {
                imageEnter.sprite = enterIndicatorSpriteOverride;
            }
        }

        /// <summary>
        /// 입력 안내 이미지 기능을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>말풍선 모드이고 이미지 참조가 유효하면 <see langword="true"/>입니다.</returns>
        private bool HasConfiguredSpeechBubbleEnterIndicator()
        {
            return dialogueVisualMode == DialogueVisualMode.SpeechBubble &&
                   imageEnter != null &&
                   _enterRectTransform != null &&
                   imageEnter.sprite != null;
        }

        /// <summary>
        /// 텍스트 레이아웃이 입력 안내 이미지에 예약해야 할 너비를 반환합니다.
        /// </summary>
        /// <returns>예약할 너비(px)입니다.</returns>
        private float GetEnterIndicatorReservedWidthPx()
        {
            if (!HasConfiguredSpeechBubbleEnterIndicator())
            {
                return 0f;
            }

            float enterWidth = Mathf.Max(0f, _enterRectTransform.rect.width);
            return enterWidth > 0f ? enterWidth + _resolvedEnterIndicatorGapPx : 0f;
        }

        /// <summary>
        /// 새 대화 세션을 위해 입력 안내 이미지 설정과 표시 상태를 초기화합니다.
        /// </summary>
        private void PrepareSpeechBubbleEnterIndicatorForNewSession()
        {
            CacheSpeechBubbleEnterIndicatorReferences();
            ResolveSpeechBubbleEnterIndicatorDefaults();
            if (imageEnter != null && imageEnter.sprite != null)
            {
                imageEnter.SetNativeSize();
                _enterRectTransform = imageEnter.GetComponent<RectTransform>();
            }

            RestoreSpeechBubbleEnterIndicatorAnchoredPosition();
            SetSpeechBubbleEnterIndicatorVisible(false, 1f);
            _lastKnownEnterIndicatorVisibleCharacters = -1;
        }

        /// <summary>
        /// 입력 안내 이미지 런타임 상태를 기본값으로 복원합니다.
        /// </summary>
        private void ResetSpeechBubbleEnterIndicatorState()
        {
            _lastKnownEnterIndicatorVisibleCharacters = -1;
            RestoreSpeechBubbleEnterIndicatorAnchoredPosition();
            SetSpeechBubbleEnterIndicatorVisible(false, 1f);
        }

        /// <summary>
        /// 입력 안내 이미지 위치를 프리팹 기본값으로 복원합니다.
        /// </summary>
        private void RestoreSpeechBubbleEnterIndicatorAnchoredPosition()
        {
            if (_enterRectTransform != null && _hasEnterBaseAnchoredPosition)
            {
                _enterRectTransform.anchoredPosition = _enterBaseAnchoredPosition;
            }
        }

        /// <summary>
        /// 입력 안내 이미지의 활성 상태와 알파값을 설정합니다.
        /// </summary>
        /// <param name="isVisible">이미지 표시 여부입니다.</param>
        /// <param name="alphaMultiplier">기본 알파값에 적용할 배율입니다.</param>
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

            Color color = _enterBaseColor;
            color.a = _enterBaseColor.a * Mathf.Clamp01(alphaMultiplier);
            imageEnter.color = color;
            if (imageEnter.gameObject.activeSelf != isVisible)
            {
                imageEnter.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 현재 페이지 상태에 맞춰 입력 안내 이미지 위치와 깜빡임을 갱신합니다.
        /// 선택지가 표시되는 마지막 페이지에서는 잘못된 진행 안내를 방지하기 위해 숨깁니다.
        /// </summary>
        private void UpdateSpeechBubbleEnterIndicatorRuntime()
        {
            if (!HasConfiguredSpeechBubbleEnterIndicator())
            {
                SetSpeechBubbleEnterIndicatorVisible(false, 1f);
                return;
            }

            int visibleCharacters = textMessage != null ? textMessage.maxVisibleCharacters : -1;
            if (visibleCharacters != _lastKnownEnterIndicatorVisibleCharacters)
            {
                _lastKnownEnterIndicatorVisibleCharacters = visibleCharacters;
                RefreshSpeechBubbleEnterIndicatorPosition();
            }

            bool isWaitingForChoice =
                HasCurrentOptions() && _indexMessage >= _messages.Count;
            if (!_isCurrentPageVisible || isWaitingForChoice)
            {
                SetSpeechBubbleEnterIndicatorVisible(false, 1f);
                return;
            }

            float alphaMultiplier = _resolvedEnterIndicatorBlinkHz <= 0f
                ? 1f
                : Mathf.Lerp(
                    _resolvedEnterIndicatorMinAlpha,
                    1f,
                    Mathf.PingPong(
                        Time.unscaledTime * _resolvedEnterIndicatorBlinkHz,
                        1f));
            SetSpeechBubbleEnterIndicatorVisible(true, alphaMultiplier);
        }

        /// <summary>
        /// 입력 안내 이미지를 현재 페이지 마지막 글자의 오른쪽에 배치합니다.
        /// </summary>
        private void RefreshSpeechBubbleEnterIndicatorPosition()
        {
            if (!HasConfiguredSpeechBubbleEnterIndicator() ||
                panelMessage == null ||
                textMessage == null ||
                !TryResolveSpeechBubbleEnterCenterInPanelSpace(out Vector2 centerInPanelSpace))
            {
                return;
            }

            SetThumbnailLocalPositionRelativeToPanel(_enterRectTransform, centerInPanelSpace);
        }

        /// <summary>
        /// 현재 페이지 마지막 글자를 기준으로 입력 안내 이미지 중심 좌표를 계산합니다.
        /// </summary>
        /// <param name="centerInPanelSpace">텍스트 패널 로컬 좌표 기준 이미지 중심입니다.</param>
        /// <returns>좌표를 계산했으면 <see langword="true"/>입니다.</returns>
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

            TMP_CharacterInfo characterInfo = textInfo.characterInfo[visibleCharacterCount - 1];
            float enterHalfWidth = Mathf.Max(0f, _enterRectTransform.rect.width * 0.5f);
            float centerX = characterInfo.xAdvance + _resolvedEnterIndicatorGapPx + enterHalfWidth;
            float centerY = ResolveCharacterMidY(textInfo, characterInfo);
            Vector3 centerInWorld =
                textMessage.rectTransform.TransformPoint(new Vector3(centerX, centerY, 0f));
            Vector3 centerInPanelLocal = panelMessage.InverseTransformPoint(centerInWorld);
            centerInPanelSpace = new Vector2(centerInPanelLocal.x, centerInPanelLocal.y);
            return true;
        }

        /// <summary>
        /// TMP 문자 또는 문자 행 정보에서 입력 안내 이미지의 세로 중심을 계산합니다.
        /// </summary>
        /// <param name="textInfo">현재 텍스트 메시 정보입니다.</param>
        /// <param name="characterInfo">기준 문자 정보입니다.</param>
        /// <returns>문자 또는 문자 행의 세로 중심입니다.</returns>
        private static float ResolveCharacterMidY(
            TMP_TextInfo textInfo,
            TMP_CharacterInfo characterInfo)
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
