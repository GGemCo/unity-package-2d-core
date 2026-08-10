using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
#if GGEMCO_USE_NEW_INPUT
using UnityEngine.InputSystem;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 대화 흐름과 선택지 구성 책임을 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 선택지 버튼 풀을 초기화합니다.
        /// </summary>
        private void InitializeButtonChoice()
        {
            if (GcLogger.IsNull(prefabButtonChoice, "선택 버튼 프리팹이 없습니다."))
            {
                return;
            }

            if (GcLogger.IsNull(containerButton, "선택 버튼 container 가 없습니다."))
            {
                return;
            }

            _buttonChoices.Clear();
            _interactionData.Clear();

            for (int i = 0; i < ButtonCount; i++)
            {
                GameObject buttonObj = Instantiate(prefabButtonChoice, containerButton);
                Button button = buttonObj.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                int capturedIndex = i;
                button.onClick.AddListener(() => OnClickChoice(capturedIndex));
                _buttonChoices[i] = button;
                button.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// NPC 인터랙션 정보와 외부 선택지를 UI에 바인딩하고 대화 세션을 시작합니다.
        /// </summary>
        /// <param name="npc">대상 NPC입니다.</param>
        /// <param name="npcData">NPC 테이블 데이터입니다.</param>
        /// <param name="interactionData">인터랙션 테이블 데이터입니다.</param>
        /// <param name="externalChoices">외부 패키지가 제공한 선택지 목록입니다.</param>
        /// <param name="npcInteractionSettings">NPC 인터랙션 설정입니다.</param>
        /// <param name="dialogueSelection">이번 인터랙션에서 선택된 dialogue 정보입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        public void SetInfos(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> externalChoices,
            GGemCoNpcInteractionSettings npcInteractionSettings = null,
            InteractionDialogueSelectionResult dialogueSelection = default(InteractionDialogueSelectionResult),
            InteractionDialogueTextContext textContext = null)
        {
            SetInfos(
                npc,
                npcData,
                interactionData,
                externalChoices,
                npcInteractionSettings,
                dialogueSelection,
                textContext,
                firstDialogueCompleted: null);
        }

        /// <summary>
        /// NPC 인터랙션 정보와 첫 대화 완료 콜백을 UI에 바인딩하고 대화 세션을 시작합니다.
        /// </summary>
        /// <param name="npc">현재 NPC입니다.</param>
        /// <param name="npcData">NPC 테이블 데이터입니다.</param>
        /// <param name="interactionData">인터랙션 테이블 데이터입니다.</param>
        /// <param name="externalChoices">외부 패키지가 제공한 선택지 목록입니다.</param>
        /// <param name="npcInteractionSettings">NPC 인터랙션 설정입니다.</param>
        /// <param name="dialogueSelection">이번 인터랙션에서 선택된 dialogue 정보입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        /// <param name="firstDialogueCompleted">첫 대화 정상 완료 시 호출할 콜백입니다.</param>
        public void SetInfos(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> externalChoices,
            GGemCoNpcInteractionSettings npcInteractionSettings,
            InteractionDialogueSelectionResult dialogueSelection,
            InteractionDialogueTextContext textContext,
            Action firstDialogueCompleted)
        {
            _dialogueLoadVersion++;
            _currentNpc = npc;
            _currentNpcData = npcData;
            _currentInteractionData = interactionData;
            _currentDialogueSelection = dialogueSelection.HasDialogue
                ? dialogueSelection
                : InteractionDialogueSelector.Select(interactionData);
            _currentTextContext = textContext ?? InteractionDialogueTextContext.Empty;
            _firstDialogueCompleted = firstDialogueCompleted;
            _currentExternalChoices.Clear();
            if (externalChoices != null)
            {
                _currentExternalChoices.AddRange(externalChoices);
            }
            _npcInteractionSettings = npcInteractionSettings != null ? npcInteractionSettings : ResolveNpcInteractionSettings();
            _currentCharacterUid = npcData != null ? npcData.Uid : 0;
            _isLoadingDialogue = false;

            ResetRuntimeStateForNewInteraction();
            CacheDefaultChoices(_currentExternalChoices, interactionData);
            RestoreNpcPresentation();

            BeginDeferredInitialReveal(_dialogueLoadVersion);
            Show(true);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
            RefreshPosition();

            if (_currentDialogueSelection.HasDialogue)
            {
                StartInteractionDialogueAsync(_dialogueLoadVersion, _currentDialogueSelection);
                return;
            }

            BeginDefaultChoiceFlow();
        }

        /// <summary>
        /// 새 인터랙션 세션 시작 전에 이전 런타임 상태를 초기화합니다.
        /// </summary>
        private void ResetRuntimeStateForNewInteraction()
        {
            ResetChoiceButtons();
            _messagePlayer.Clear(textMessage);
            _dialogueSession.Clear();
            ClearCurrentDialogueNode();
            _isExecutingChoice = false;
            ClearPendingAutoStartChoice();
            ApplyMessageFontSize(0f);
            if (imageThumbnail != null)
            {
                imageThumbnail.sprite = null;
                ApplyThumbnailVisibilityAfterBinding();
            }
            _lastKnownVisibleCharacters = -1;
            RestoreSpeechBubbleLayoutDefaults();
            PrepareSpeechBubbleEnterIndicatorForNewSession();
        }

        /// <summary>
        /// 현재 UI에 출력할 런타임 대화 노드를 지정합니다.
        /// </summary>
        /// <param name="node">UI 표시와 썸네일 위치 계산 기준이 되는 대화 노드입니다.</param>
        private void SetCurrentDialogueNode(DialogueNodeData node)
        {
            _currentDialogueNode = node;
        }

        /// <summary>
        /// 런타임 대화 노드 표시 상태를 해제해 기본 NPC 표시 규칙을 사용하도록 되돌립니다.
        /// </summary>
        private void ClearCurrentDialogueNode()
        {
            _currentDialogueNode = null;
        }

        /// <summary>
        /// DialogueData를 비동기로 로드하고 interaction 전용 대화 세션을 시작합니다.
        /// </summary>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <param name="dialogueSelection">이번 세션에서 선택된 dialogue 정보입니다.</param>
        private async void StartInteractionDialogueAsync(int requestVersion, InteractionDialogueSelectionResult dialogueSelection)
        {
            _isLoadingDialogue = true;
            DialogueData data = await DialogueLoader.LoadDialogueData(dialogueSelection.DialogueUid);

            if (requestVersion != _dialogueLoadVersion)
            {
                return;
            }

            _isLoadingDialogue = false;
            if (data == null)
            {
                _firstDialogueCompleted = null;
                BeginDefaultChoiceFlow();
                return;
            }

            _dialogueSession.Start(data, dialogueSelection.StartNodeGuid);
            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
                return;
            }

            await ApplyCurrentDialogueNodeAsync(requestVersion);
        }

        /// <summary>
        /// 기본 interaction/quest 선택지 흐름으로 진입합니다.
        /// 대화 그래프가 없거나 로드 실패했을 때의 fallback 진입점입니다.
        /// </summary>
        private void BeginDefaultChoiceFlow()
        {
            RestoreNpcPresentation();
            BindVisibleChoices(_defaultChoices);
            ApplyDialogueMessage(
                ResolveInitialMessage(_currentInteractionData, _currentExternalChoices),
                revealImmediately: false);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
            TryCompleteDeferredInitialReveal(_dialogueLoadVersion);
        }

        /// <summary>
        /// 현재 대사 노드를 UI에 반영합니다.
        /// </summary>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <returns>표시 적용 작업 완료를 기다리는 Task입니다.</returns>
        private async Task ApplyCurrentDialogueNodeAsync(int requestVersion)
        {
            DialogueNodeData node = _dialogueSession.CurrentNode;
            if (node == null)
            {
                HandleDialogueSequenceCompleted();
                return;
            }

            SetCurrentDialogueNode(node);
            ApplyMessageFontSize(node.fontSize);
            SetNpcName(ResolveDialogueSpeakerName(node));
            await BindDialogueThumbnailAsync(node, requestVersion);
            if (requestVersion != _dialogueLoadVersion || _currentDialogueNode != node)
            {
                return;
            }

            BindVisibleChoices(BuildDialogueChoiceEntries(node));
            ApplyDialogueMessage(ResolveDialogueNodeText(node), revealImmediately: false);
            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
            TryCompleteDeferredInitialReveal(requestVersion);
        }

        /// <summary>
        /// 대화 노드 전용 선택지 목록을 생성합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>노드 선택지 목록입니다.</returns>
        private List<InteractionData> BuildDialogueChoiceEntries(DialogueNodeData node)
        {
            List<InteractionData> result = new List<InteractionData>();
            if (node?.options == null)
            {
                return result;
            }

            foreach (DialogueOption option in node.options)
            {
                if (option == null)
                {
                    continue;
                }

                result.Add(new InteractionData
                {
                    ChoiceType = ChoiceType.Dialogue,
                    DialogueOption = option,
                    Label = ResolveDialogueOptionText(option),
                });
            }

            return result;
        }

        /// <summary>
        /// 대화 그래프가 종료되었을 때 정책에 맞춰 후속 UI를 구성합니다.
        /// 기본 선택지가 남아 있으면 마지막 대사/말풍선 상태를 유지한 채 선택지만 노출합니다.
        /// </summary>
        private void HandleDialogueSequenceCompleted()
        {
            _dialogueSession.Clear();
            InvokeFirstDialogueCompletedIfEligible();
            BindVisibleChoices(_defaultChoices);

            if (_currentInteractionData != null && _currentInteractionData.DialogueEndPolicy == InteractionDialogueEndPolicy.Close)
            {
                ClearCurrentDialogueNode();
                CloseInteractionWindow();
                return;
            }

            if (_defaultChoices.Count > 0)
            {
                // 선택지가 남아 있는 종료 분기에서는 현재 노드 표시 상태를 유지한다.
                // (_currentDialogueNode를 유지해야 말풍선 썸네일 좌/우 및 Flip 계산이 초기화되지 않는다.)
                RefreshChoiceButtonsVisibility();
                RefreshThumbnailPosition();
                TryCompleteDeferredInitialReveal(_dialogueLoadVersion);
                return;
            }

            ClearCurrentDialogueNode();
            RefreshChoiceButtonsVisibility();
            TryCompleteDeferredInitialReveal(_dialogueLoadVersion);
        }

        /// <summary>
        /// 유효한 대사 노드를 한 번 이상 표시한 첫 대화가 정상 종료된 경우 완료 콜백을 한 번만 호출합니다.
        /// 로드 실패나 잘못된 시작 노드로 대사가 시작되지 않은 경우에는 완료 처리하지 않습니다.
        /// </summary>
        private void InvokeFirstDialogueCompletedIfEligible()
        {
            if (_firstDialogueCompleted == null)
            {
                return;
            }

            Action callback = _firstDialogueCompleted;
            _firstDialogueCompleted = null;
            if (_currentDialogueNode == null)
            {
                return;
            }

            callback.Invoke();
        }

        /// <summary>
        /// 기본 interaction 및 외부 패키지 선택지 목록을 캐시합니다.
        /// dialogue 종료 후 같은 데이터를 다시 바인딩할 수 있도록 UI 상태와 분리해 저장합니다.
        /// </summary>
        /// <param name="externalChoices">외부 패키지가 제공한 선택지 목록입니다.</param>
        /// <param name="interactionData">interaction 테이블 데이터입니다.</param>
        private void CacheDefaultChoices(
            List<InteractionChoiceContribution> externalChoices,
            StruckTableInteraction interactionData)
        {
            _defaultChoices.Clear();

            if (externalChoices != null)
            {
                for (int i = 0; i < externalChoices.Count; i++)
                {
                    InteractionChoiceContribution externalChoice = externalChoices[i];
                    if (externalChoice == null)
                    {
                        continue;
                    }

                    _defaultChoices.Add(new InteractionData
                    {
                        ChoiceType = ChoiceType.External,
                        ExternalChoice = externalChoice,
                        Label = externalChoice.Label,
                    });
                }
            }

            if (interactionData == null)
            {
                return;
            }

            TryAddDefaultInteractionChoice(interactionData.Type1, interactionData.Value1, interactionData.CustomTypeKey1);
            TryAddDefaultInteractionChoice(interactionData.Type2, interactionData.Value2, interactionData.CustomTypeKey2);
            TryAddDefaultInteractionChoice(interactionData.Type3, interactionData.Value3, interactionData.CustomTypeKey3);
        }

        /// <summary>
        /// 기본 interaction 선택지를 캐시 목록에 추가합니다.
        /// </summary>
        /// <param name="interactionType">기본 interaction 타입입니다.</param>
        /// <param name="value">보조 값입니다.</param>
        /// <param name="customTypeKey">커스텀 interaction 키입니다.</param>
        private void TryAddDefaultInteractionChoice(
            InteractionConstants.Type interactionType,
            int value,
            string customTypeKey)
        {
            bool hasBuiltIn = interactionType != InteractionConstants.Type.None;
            bool hasCustom = string.IsNullOrWhiteSpace(customTypeKey) == false;
            if (!hasBuiltIn && !hasCustom)
            {
                return;
            }

            _defaultChoices.Add(new InteractionData
            {
                ChoiceType = ChoiceType.Interaction,
                InteractionType = interactionType,
                CustomTypeKey = hasBuiltIn ? string.Empty : customTypeKey,
                Value = value,
                Label = hasBuiltIn
                    ? InteractionConstants.GetTypeName(interactionType)
                    : ResolveCustomInteractionDisplayName(customTypeKey, value),
            });
        }

        /// <summary>
        /// 현재 표시할 선택지 목록을 버튼 UI에 다시 바인딩합니다.
        /// </summary>
        /// <param name="choices">현재 단계에서 표시할 선택지 목록입니다.</param>
        private void BindVisibleChoices(IReadOnlyList<InteractionData> choices)
        {
            _interactionData.Clear();
            _isExecutingChoice = false;

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                pair.Value.gameObject.SetActive(false);
                SetChoiceButtonLabel(pair.Value, string.Empty);
            }

            if (choices == null)
            {
                ClearPendingAutoStartChoice();
                return;
            }

            int count = Mathf.Min(ButtonCount, choices.Count);
            for (int i = 0; i < count; i++)
            {
                Button button = _buttonChoices.GetValueOrDefault(i);
                if (button == null)
                {
                    continue;
                }

                _interactionData[i] = choices[i];
                SetChoiceButtonLabel(button, choices[i].Label);
            }

            ConfigureAutoStartChoice(choices, count);
        }

        /// <summary>
        /// 매 프레임 타자 효과를 진행하고 마지막 페이지 도달 시 선택지 표시 상태를 갱신합니다.
        /// </summary>
        private void UpdateDialogueMessageReveal()
        {
            if (textMessage == null)
            {
                return;
            }

            bool wasCurrentPageFullyRevealed = _messagePlayer.IsCurrentPageFullyRevealed;
            _messagePlayer.Tick(textMessage, GetRevealDeltaTime());
            StopTypewriterSoundWhenRevealCompleted(wasCurrentPageFullyRevealed);
            RefreshChoiceButtonsVisibility();

            if (dialogueVisualMode == DialogueVisualMode.SpeechBubble)
            {
                int currentVisibleCharacters = textMessage.maxVisibleCharacters;
                if (_lastKnownVisibleCharacters != currentVisibleCharacters)
                {
                    _lastKnownVisibleCharacters = currentVisibleCharacters;
                    RefreshThumbnailPosition();
                }

                UpdateSpeechBubbleEnterIndicatorRuntime();
            }
        }

        /// <summary>
        /// 클릭 또는 터치 입력으로 대화 페이지 또는 다음 대사 노드를 진행합니다.
        /// </summary>
        private void TryHandleAdvancePointerInput()
        {
            if (!CanAdvanceDialogue())
            {
                return;
            }

            if (!TryGetAdvancePointerPosition(out Vector2 screenPoint))
            {
                return;
            }

            TryAdvanceDialogueByPointer(screenPoint);
        }

        /// <summary>
        /// 지정한 화면 좌표의 포인터 입력으로 현재 대화를 한 단계 진행합니다.
        /// 실제 사용자 입력과 외부 호출이 동일한 입력 정책 및 버튼 영역 차단 규칙을 사용합니다.
        /// </summary>
        /// <param name="screenPoint">대화 진행 입력이 발생한 화면 좌표입니다.</param>
        /// <returns>메시지 표시 또는 다음 대사 노드 진행을 처리했으면 true입니다.</returns>
        public bool TryAdvanceDialogueByPointer(Vector2 screenPoint)
        {
            if (!gameObject.activeInHierarchy ||
                !CanAdvanceDialogue() ||
                IsAdvancePointerBlocked(screenPoint))
            {
                return false;
            }

            InteractionDialogueAdvanceResult result = _messagePlayer.Advance(textMessage);
            SynchronizeTypewriterSoundAfterAdvance(result);
            if (result != InteractionDialogueAdvanceResult.None)
            {
                RefreshChoiceButtonsVisibility();
                RefreshThumbnailPosition();
                return true;
            }

            if (_messagePlayer.IsSequenceCompleted)
            {
                TryAdvanceDialogueNodeAfterMessageEnd();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 메시지 시퀀스가 끝난 뒤, 다음 대사 노드로 자동 진행할 수 있으면 진행합니다.
        /// </summary>
        private void TryAdvanceDialogueNodeAfterMessageEnd()
        {
            if (_isLoadingDialogue || !_dialogueSession.IsActive)
            {
                return;
            }

            if (_dialogueSession.HasCurrentOptions)
            {
                RefreshChoiceButtonsVisibility();
                return;
            }

            if (_dialogueSession.TryMoveNext())
            {
                int requestVersion = _dialogueLoadVersion;
                BeginDeferredInitialReveal(requestVersion);
                _ = ApplyCurrentDialogueNodeAsync(requestVersion);
                return;
            }

            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
            }
        }

        /// <summary>
        /// 현재 상태에서 대화 진행 입력을 받을 수 있는지 확인합니다.
        /// </summary>
        /// <returns>대화 진행 입력을 받을 수 있으면 true입니다.</returns>
        private bool CanAdvanceDialogue()
        {
            if (_isInitialRevealPending)
            {
                return false;
            }

            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            if (settings.page.advanceInputPolicy != InteractionDialogueAdvanceInputPolicy.PointerClickOrTap)
            {
                return false;
            }

            if (!_messagePlayer.HasMessage)
            {
                return false;
            }

            return !_messagePlayer.IsSequenceCompleted || CanAdvanceAfterSequenceCompleted();
        }

        /// <summary>
        /// 현재 페이지 시퀀스가 끝난 뒤에도 다음 노드로 넘어갈 수 있는지 확인합니다.
        /// dialogue graph가 활성 상태이고 현재 노드에 선택지가 없을 때만 true를 반환합니다.
        /// </summary>
        /// <returns>다음 노드 자동 진행이 가능하면 true입니다.</returns>
        private bool CanAdvanceAfterSequenceCompleted()
        {
            if (_isLoadingDialogue || !_dialogueSession.IsActive)
            {
                return false;
            }

            return !_dialogueSession.HasCurrentOptions;
        }

        /// <summary>
        /// 현재 프로젝트의 입력 시스템 정의 심볼에 맞춰 대화 진행 입력 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 대화 진행 입력이 발생했으면 true입니다.</returns>
        private bool TryGetAdvancePointerPosition(out Vector2 screenPoint)
        {
#if GGEMCO_USE_OLD_INPUT
            return TryGetAdvancePointerPositionOldInput(out screenPoint);
#elif GGEMCO_USE_NEW_INPUT
            return TryGetAdvancePointerPositionNewInput(out screenPoint);
#else
            screenPoint = default;
            return false;
#endif
        }

        /// <summary>
        /// Legacy Input Manager 기준으로 클릭 또는 터치 시작 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 입력이 감지되었으면 true입니다.</returns>
        private bool TryGetAdvancePointerPositionOldInput(out Vector2 screenPoint)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == UnityEngine.TouchPhase.Began)
                {
                    screenPoint = touch.position;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePosition = Input.mousePosition;
                screenPoint = new Vector2(mousePosition.x, mousePosition.y);
                return true;
            }

            screenPoint = default;
            return false;
        }

        /// <summary>
        /// New Input System 기준으로 클릭 또는 터치 시작 좌표를 가져옵니다.
        /// </summary>
        /// <param name="screenPoint">입력이 발생한 화면 좌표입니다.</param>
        /// <returns>현재 프레임에 입력이 감지되었으면 true입니다.</returns>
        private bool TryGetAdvancePointerPositionNewInput(out Vector2 screenPoint)
        {
#if GGEMCO_USE_NEW_INPUT
            if (Touchscreen.current != null)
            {
                var primaryTouch = Touchscreen.current.primaryTouch;
                if (primaryTouch.press.wasPressedThisFrame)
                {
                    screenPoint = primaryTouch.position.ReadValue();
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPoint = Mouse.current.position.ReadValue();
                return true;
            }
#endif

            screenPoint = default;
            return false;
        }

        /// <summary>
        /// 클릭 또는 터치가 선택지/닫기 버튼 위에서 발생했는지 확인합니다.
        /// </summary>
        /// <param name="screenPoint">화면 좌표입니다.</param>
        /// <returns>기존 버튼 입력과 충돌하면 true입니다.</returns>
        private bool IsAdvancePointerBlocked(Vector2 screenPoint)
        {
            if (IsPointerInsideButton(buttonClose, screenPoint))
            {
                return true;
            }

            foreach (Button button in _buttonChoices.Values)
            {
                if (IsPointerInsideButton(button, screenPoint))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 화면 좌표가 버튼 Rect 내부인지 확인합니다.
        /// </summary>
        /// <param name="button">확인할 버튼입니다.</param>
        /// <param name="screenPoint">화면 좌표입니다.</param>
        /// <returns>버튼 내부 좌표이면 true입니다.</returns>
        private bool IsPointerInsideButton(Button button, Vector2 screenPoint)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!button.TryGetComponent(out RectTransform rectTransform))
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, GetUiEventCamera());
        }

        /// <summary>
        /// 현재 UI가 속한 Canvas의 이벤트 카메라를 반환합니다.
        /// </summary>
        /// <returns>Screen Space Overlay가 아니면 Canvas 카메라를 반환합니다.</returns>
        private Camera GetUiEventCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        /// <summary>
        /// 현재 타자 효과 갱신에 사용할 deltaTime을 반환합니다.
        /// </summary>
        /// <returns>설정에 따라 보정된 deltaTime입니다.</returns>
        private float GetRevealDeltaTime()
        {
            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            return settings.reveal.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        /// <summary>
        /// 현재 NPC 기본 정보를 기준으로 이름과 썸네일을 복원합니다.
        /// </summary>
        private void RestoreNpcPresentation()
        {
            ClearCurrentDialogueNode();
            if (_currentNpcData == null)
            {
                return;
            }

            SetNpcName(_currentNpcData.Name);
            ApplyMessageFontSize(0f);
            BindNpcThumbnail(_currentNpcData);
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// NPC 썸네일을 로드해 바인딩합니다.
        /// </summary>
        /// <param name="npcData">NPC 테이블 데이터입니다.</param>
        private void BindNpcThumbnail(StruckTableNpc npcData)
        {
            if (imageThumbnail == null)
            {
                return;
            }

            imageThumbnail.sprite = null;
            if (npcData == null || string.IsNullOrEmpty(npcData.ImageThumbnailFileName))
            {
                ApplyThumbnailVisibilityAfterBinding();
                return;
            }

            string key = ConfigAddressableKey.GetKeyThumbnailNpc(npcData.ImageThumbnailFileName);
            Sprite sprite = _addressableLoaderCharacterThumbnail.GetCharacterThumbnailByName(key);
            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }

            ApplyThumbnailVisibilityAfterBinding();
        }

        /// <summary>
        /// 대사 노드 기준 썸네일을 비동기로 바인딩합니다.
        /// 노드에 전용 썸네일이 없으면 현재 NPC 기본 썸네일을 유지합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <param name="requestVersion">요청 시점 버전입니다.</param>
        /// <returns>썸네일 로드 완료를 기다리는 Task입니다.</returns>
        private async Task BindDialogueThumbnailAsync(DialogueNodeData node, int requestVersion)
        {
            if (imageThumbnail == null)
            {
                return;
            }

            BindNpcThumbnail(_currentNpcData);
            Sprite sprite = await DialogueCharacterHelper.GetThumbnail(node);
            if (requestVersion != _dialogueLoadVersion || _currentDialogueNode != node)
            {
                return;
            }

            if (sprite != null)
            {
                imageThumbnail.sprite = sprite;
            }

            ApplyThumbnailVisibilityAfterBinding();
        }

        /// <summary>
        /// 인터랙션 시작 시 표시할 첫 메시지를 계산합니다.
        /// </summary>
        /// <param name="interactionData">인터랙션 데이터입니다.</param>
        /// <param name="externalChoices">외부 패키지가 제공한 선택지 목록입니다.</param>
        /// <returns>초기 표시 메시지입니다.</returns>
        private string ResolveInitialMessage(
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> externalChoices)
        {
            if (interactionData != null && !string.IsNullOrEmpty(interactionData.Message))
            {
                return ResolveInteractionLocalizedMessage(interactionData.Message);
            }

            if (externalChoices != null && externalChoices.Count > 0)
            {
                return FormatInteractionText(messageQuestSelect);
            }

            return string.Empty;
        }

        /// <summary>
        /// 현재 대화 노드의 발화자 이름을 해석합니다.
        /// 이름을 찾지 못하면 현재 NPC 이름을 유지합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>표시할 발화자 이름입니다.</returns>
        private string ResolveDialogueSpeakerName(DialogueNodeData node)
        {
            string speakerName = DialogueCharacterHelper.GetName(node);
            if (!string.IsNullOrEmpty(speakerName))
            {
                return speakerName;
            }

            return _currentNpcData != null ? _currentNpcData.Name : string.Empty;
        }


        /// <summary>
        /// 현재 대화 노드 본문을 localization table/key 기준으로 해석합니다.
        /// localization 정보가 없거나 실패하면 기존 raw 문자열 포맷 결과를 fallback 으로 사용합니다.
        /// </summary>
        /// <param name="node">현재 대화 노드입니다.</param>
        /// <returns>표시할 본문 문자열입니다.</returns>
        private string ResolveDialogueNodeText(DialogueNodeData node)
        {
            string fallback = node != null ? FormatInteractionText(node.dialogueText) : string.Empty;
            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return DialogueLocalizationRuntimeResolver.Resolve(node?.dialogueTable, node?.dialogueKey, fallback, arguments);
        }

        /// <summary>
        /// 현재 대화 선택지 문자열을 localization table/key 기준으로 해석합니다.
        /// localization 정보가 없거나 실패하면 기존 raw 문자열 포맷 결과를 fallback 으로 사용합니다.
        /// </summary>
        /// <param name="option">현재 선택지입니다.</param>
        /// <returns>표시할 선택지 문자열입니다.</returns>
        private string ResolveDialogueOptionText(DialogueOption option)
        {
            string fallback = option != null ? FormatInteractionText(option.optionText) : string.Empty;
            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return DialogueLocalizationRuntimeResolver.Resolve(option?.optionTable, option?.optionKey, fallback, arguments);
        }

        /// <summary>
        /// 현재 인터랙션 텍스트 컨텍스트를 사용해 원본 문자열을 포맷합니다.
        /// </summary>
        /// <param name="template">원본 문자열입니다.</param>
        /// <returns>포맷이 적용된 문자열입니다.</returns>
        private string FormatInteractionText(string template)
        {
            return InteractionDialogueFormatter.FormatRaw(template, _currentTextContext);
        }

        /// <summary>
        /// 인터랙션 로컬라이즈 키를 현재 텍스트 컨텍스트와 함께 평가합니다.
        /// </summary>
        /// <param name="localizationKey">평가할 로컬라이즈 키입니다.</param>
        /// <returns>치환이 적용된 로컬라이즈 문자열입니다.</returns>
        private string ResolveInteractionLocalizedMessage(string localizationKey)
        {
            if (_localizationManager == null || string.IsNullOrWhiteSpace(localizationKey))
            {
                return string.Empty;
            }

            object[] arguments = _currentTextContext?.PositionalArgs ?? Array.Empty<object>();
            return _localizationManager.GetSmartInteractionByKey(localizationKey, arguments);
        }

        /// <summary>
        /// 설정된 정책에 따라 메시지를 새로 바인딩합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        /// <param name="revealImmediately">true이면 현재 페이지를 즉시 모두 노출합니다.</param>
        private void ApplyDialogueMessage(string message, bool revealImmediately)
        {
            ApplyProjectTypewriterSoundDefaults();
            if (textMessage == null)
            {
                return;
            }

            _messagePlayer.Configure(textMessage, message, ResolveNpcInteractionSettings());
            if (revealImmediately)
            {
                _messagePlayer.RevealCurrentPage(textMessage);
            }

            TryStartTypewriterSound();
            _lastKnownVisibleCharacters = textMessage.maxVisibleCharacters;
            _lastKnownEnterIndicatorVisibleCharacters = -1;

            RefreshChoiceButtonsVisibility();
            RefreshThumbnailPosition();
        }

        /// <summary>
        /// 메시지 텍스트 폰트 크기를 기본값 또는 지정값으로 적용합니다.
        /// </summary>
        /// <param name="fontSize">적용할 폰트 크기입니다. 0 이하이면 기본 크기를 사용합니다.</param>
        private void ApplyMessageFontSize(float fontSize)
        {
            if (textMessage == null)
            {
                return;
            }

            textMessage.fontSize = fontSize > 0f ? fontSize : _defaultMessageFontSize;
        }

        /// <summary>
        /// 선택지 버튼 상태를 초기화합니다.
        /// </summary>
        private void ResetChoiceButtons()
        {
            _interactionData.Clear();

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                Button button = pair.Value;
                if (button == null)
                {
                    continue;
                }

                button.gameObject.SetActive(false);
                SetChoiceButtonLabel(button, string.Empty);
            }

            if (containerButton != null)
            {
                containerButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 현재 메시지 완료 여부에 따라 선택지 버튼 표시 상태를 갱신합니다.
        /// </summary>
        private void RefreshChoiceButtonsVisibility()
        {
            bool shouldShowChoices = _interactionData.Count > 0 && _messagePlayer.IsSequenceCompleted;
            if (containerButton != null)
            {
                containerButton.gameObject.SetActive(shouldShowChoices);
            }

            foreach (KeyValuePair<int, Button> pair in _buttonChoices)
            {
                Button button = pair.Value;
                bool show = shouldShowChoices && _interactionData.ContainsKey(pair.Key);
                if (button != null)
                {
                    button.gameObject.SetActive(show);
                }
            }

            TryAutoStartSingleChoice();
        }

        /// <summary>
        /// 현재 선택지 상태를 기준으로 단일 선택 자동 시작 예약을 갱신합니다.
        /// </summary>
        /// <param name="choices">현재 바인딩한 선택지 목록입니다.</param>
        /// <param name="count">실제로 바인딩된 선택지 수입니다.</param>
        private void ConfigureAutoStartChoice(IReadOnlyList<InteractionData> choices, int count)
        {
            ClearPendingAutoStartChoice();

            if (!CanAutoStartWhenOneChoice())
            {
                return;
            }

            if (choices == null || count != 1)
            {
                return;
            }

            _pendingAutoStartChoiceIndex = 0;
            _hasAutoStartedCurrentChoiceSet = false;
        }

        /// <summary>
        /// 현재 선택지 목록이 단일 선택 자동 시작 정책을 만족하면 한 번만 자동 실행합니다.
        /// </summary>
        private void TryAutoStartSingleChoice()
        {
            if (_pendingAutoStartChoiceIndex < 0)
            {
                return;
            }

            if (_hasAutoStartedCurrentChoiceSet || _isExecutingChoice)
            {
                return;
            }

            if (!_messagePlayer.IsSequenceCompleted)
            {
                return;
            }

            if (!_interactionData.ContainsKey(_pendingAutoStartChoiceIndex))
            {
                ClearPendingAutoStartChoice();
                return;
            }

            _hasAutoStartedCurrentChoiceSet = true;
            OnClickChoice(_pendingAutoStartChoiceIndex);
        }

        /// <summary>
        /// 현재 인터랙션 상태에서 단일 선택 자동 시작 정책을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>정책 사용 가능 시 true입니다.</returns>
        private bool CanAutoStartWhenOneChoice()
        {
            GGemCoNpcInteractionSettings settings = ResolveNpcInteractionSettings();
            return settings != null && settings.autoStartWhenOneChoice;
        }

        /// <summary>
        /// 현재 선택지 집합에 대한 자동 시작 예약 상태를 초기화합니다.
        /// </summary>
        private void ClearPendingAutoStartChoice()
        {
            _pendingAutoStartChoiceIndex = -1;
            _hasAutoStartedCurrentChoiceSet = false;
        }
    }
}
