using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 선택지 실행과 인터랙션 액션 처리 책임을 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// 인터랙션 선택지 실행 후 대화 세션을 처리할 방식을 정의합니다.
        /// </summary>
        private enum InteractionExecutionResult
        {
            /// <summary>
            /// 선택지를 처리하지 못해 현재 대화를 유지합니다.
            /// </summary>
            NotHandled = 0,

            /// <summary>
            /// 선택지 실행을 완료하여 현재 인터랙션을 종료합니다.
            /// </summary>
            CompleteInteraction = 1,

            /// <summary>
            /// 자식 UIWindow 결과를 기다리기 위해 현재 인터랙션을 일시 중단합니다.
            /// </summary>
            SuspendForChildWindow = 2,
        }

        private InteractionManager.InteractionSuspensionToken
            _playerStatResetSuspensionToken;
        private InteractionManager.InteractionSuspensionToken
            _worldMapSuspensionToken;

        /// <summary>
        /// 선택지 버튼에 표시할 텍스트를 설정합니다.
        /// </summary>
        /// <param name="button">대상 버튼입니다.</param>
        /// <param name="label">표시할 문구입니다.</param>
        private void SetChoiceButtonLabel(Button button, string label)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = label;
            }
        }

        /// <summary>
        /// 커스텀 interaction 표시 이름을 해석합니다.
        /// </summary>
        /// <param name="customTypeKey">커스텀 interaction 키입니다.</param>
        /// <param name="value">interaction 값입니다.</param>
        /// <returns>표시 이름입니다.</returns>
        private string ResolveCustomInteractionDisplayName(string customTypeKey, int value)
        {
            if (InteractionCustomHandlerRegistry.TryGetDisplayName(customTypeKey, value, out string displayName))
            {
                return displayName;
            }

            return customTypeKey;
        }

        /// <summary>
        /// 선택지 버튼 클릭을 처리합니다.
        /// </summary>
        /// <param name="index">클릭한 버튼 인덱스입니다.</param>
        private async void OnClickChoice(int index)
        {
            if (_isExecutingChoice)
            {
                return;
            }

            if (!_interactionData.TryGetValue(index, out InteractionData data))
            {
                return;
            }

            _isExecutingChoice = true;
            try
            {
                _hasAutoStartedCurrentChoiceSet = true;

                switch (data.ChoiceType)
                {
                    case ChoiceType.External:
                        if (data.ExternalChoice?.ExecuteAsync != null)
                        {
                            await data.ExternalChoice.ExecuteAsync();
                        }
                        break;
                    case ChoiceType.Interaction:
                        OnClickChoiceInteraction(data);
                        break;
                    case ChoiceType.Dialogue:
                        await OnClickChoiceDialogue(index);
                        break;
                }
            }
            catch (Exception exception)
            {
                GcLogger.LogException(exception);
            }
            finally
            {
                _isExecutingChoice = false;
            }
        }

        /// <summary>
        /// dialogue 노드 선택지를 처리합니다.
        /// </summary>
        /// <param name="optionIndex">선택한 dialogue option 인덱스입니다.</param>
        /// <returns>처리 완료를 기다리는 Task입니다.</returns>
        private async Task OnClickChoiceDialogue(int optionIndex)
        {
            if (!_dialogueSession.IsActive)
            {
                return;
            }

            if (_dialogueSession.TrySelectOption(optionIndex))
            {
                int requestVersion = _dialogueLoadVersion;
                BeginDeferredInitialReveal(requestVersion);
                await ApplyCurrentDialogueNodeAsync(requestVersion);
                return;
            }

            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
            }
        }

        /// <summary>
        /// interaction 버튼 클릭 처리를 수행합니다.
        /// </summary>
        /// <param name="data">버튼에 연결된 interaction 데이터입니다.</param>
        private void OnClickChoiceInteraction(InteractionData data)
        {
            InteractionExecutionResult result =
                InteractionExecutionResult.NotHandled;

            if (data.HasBuiltInInteraction)
            {
                result = ExecuteBuiltInInteraction(
                    data.InteractionType,
                    data.Value);
            }
            else if (data.HasCustomInteraction)
            {
                bool handled = InteractionCustomHandlerRegistry.TryExecute(
                    data.CustomTypeKey,
                    SceneGame,
                    _currentNpc,
                    data.Value);
                result = handled
                    ? InteractionExecutionResult.CompleteInteraction
                    : InteractionExecutionResult.NotHandled;
                if (!handled)
                {
                    GcLogger.LogError($"커스텀 interaction 처리기가 등록되지 않았습니다. key: {data.CustomTypeKey}");
                }
            }

            switch (result)
            {
                case InteractionExecutionResult.CompleteInteraction:
                    CloseDialogueByChoice();
                    break;
                case InteractionExecutionResult.SuspendForChildWindow:
                    HideDialogueForChildWindow();
                    break;
            }
        }

        /// <summary>
        /// 기본 제공 interaction 타입을 실행합니다.
        /// </summary>
        /// <param name="interactionType">실행할 interaction 타입입니다.</param>
        /// <param name="value">보조 값입니다.</param>
        /// <returns>실행 후 현재 대화를 종료하거나 일시 중단할 방식을 반환합니다.</returns>
        private InteractionExecutionResult ExecuteBuiltInInteraction(
            InteractionConstants.Type interactionType,
            int value)
        {
            if (interactionType == InteractionConstants.Type.None)
            {
                return InteractionExecutionResult.NotHandled;
            }

            switch (interactionType)
            {
                case InteractionConstants.Type.Shop:
                    _uiWindowShop?.Show(true);
                    _uiWindowShop?.SetInfoByShopUid(value);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.Stash:
                    _uiWindowStash?.Show(true);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.ShopSale:
                    _uiWindowShopSale?.Show(true);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.ItemUpgrade:
                    _uiWindowItemUpgrade?.Show(true);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.ItemSalvage:
                    _uiWindowItemSalvage?.Show(true);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.ItemCraft:
                    _uiWindowItemCraft?.Show(true);
                    _uiWindowItemCraft?.SetInfoByItemCraftUid(value);
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.SaveGame:
                    SaveGameBySleep();
                    return InteractionExecutionResult.CompleteInteraction;
                case InteractionConstants.Type.StatReset:
                    return OpenPlayerStatReset()
                        ? InteractionExecutionResult.SuspendForChildWindow
                        : InteractionExecutionResult.NotHandled;
                case InteractionConstants.Type.WorldMap:
                    return OpenWorldMapForInteraction()
                        ? InteractionExecutionResult.SuspendForChildWindow
                        : InteractionExecutionResult.NotHandled;
                default:
                    return InteractionExecutionResult.NotHandled;
            }
        }

        /// <summary>
        /// 현재 NPC 인터랙션을 일시 중단한 뒤 월드맵을 자식 윈도우로 엽니다.
        /// 월드맵 표시가 실패하면 중단 토큰을 취소하여 현재 대화 선택지를 유지합니다.
        /// </summary>
        /// <returns>인터랙션을 중단하고 월드맵을 열었으면 <see langword="true"/>입니다.</returns>
        private bool OpenWorldMapForInteraction()
        {
            InteractionManager interactionManager =
                SceneGame?.InteractionManager;
            if (_uiWindowWorldMap == null || interactionManager == null)
            {
                return false;
            }

            if (!interactionManager.TrySuspendCurrentInteraction(
                    _currentNpc,
                    out InteractionManager.InteractionSuspensionToken token))
            {
                return false;
            }

            // NPC 인터랙션의 자식 창은 상단 탭 메뉴 같은 월드맵 연결 윈도우를 열지 않습니다.
            // 취소 후에는 월드맵 닫힘 콜백을 통해 현재 NPC 인터랙션만 복원합니다.
            if (_uiWindowWorldMap.ShowWithCloseCallback(
                    HandleInteractionWorldMapClosed,
                    followLinkedWindows: false))
            {
                _worldMapSuspensionToken = token;
                return true;
            }

            interactionManager.CancelCurrentInteractionSuspension(token);
            return false;
        }

        /// <summary>
        /// 인터랙션에서 연 월드맵의 종료 사유에 따라 중단한 NPC 인터랙션을 재개하거나 종료합니다.
        /// 취소하기 버튼으로 닫은 경우에만 NPC와 플레이어의 유효 범위를 다시 검사하고 선택지를 재구성합니다.
        /// </summary>
        /// <param name="closeReason">월드맵이 닫힌 최종 사유입니다.</param>
        private void HandleInteractionWorldMapClosed(
            WorldMapWindowCloseReason closeReason)
        {
            InteractionManager.InteractionSuspensionToken token =
                _worldMapSuspensionToken;
            _worldMapSuspensionToken = default;

            InteractionManager interactionManager =
                SceneGame?.InteractionManager;
            if (interactionManager == null)
            {
                return;
            }

            if (closeReason == WorldMapWindowCloseReason.Cancelled)
            {
                interactionManager.ResumeSuspendedInteraction(token);
                return;
            }

            interactionManager.CompleteSuspendedInteraction(token);
        }

        /// <summary>
        /// 스탯 초기화 창을 열기 전에 비용 조건을 검사합니다.
        /// </summary>
        /// <returns>창을 열었으면 true입니다.</returns>
        private bool OpenPlayerStatReset()
        {
            if (_playerStatSettings != null && _playerStatSettings.statPointResetCost > 0)
            {
                long playerGold = _playerData.CurrentGold;
                if (playerGold < _playerStatSettings.statPointResetCost)
                {
                    ShowLocalizedInteractionFeedbackMessage("Text_Not_Enough_Gold", _playerStatSettings.statPointResetCost);
                    return false;
                }
            }

            InteractionManager interactionManager =
                SceneGame?.InteractionManager;
            if (_uiWindowPlayerStatReset == null ||
                interactionManager == null)
            {
                return false;
            }

            if (!interactionManager.TrySuspendCurrentInteraction(
                    _currentNpc,
                    out InteractionManager.InteractionSuspensionToken token))
            {
                return false;
            }

            if (_uiWindowPlayerStatReset.ShowWithCloseCallback(
                    HandlePlayerStatResetClosed))
            {
                _playerStatResetSuspensionToken = token;
                return true;
            }

            interactionManager.CancelCurrentInteractionSuspension(token);
            return false;
        }

        /// <summary>
        /// 스탯 초기화 창 종료 결과에 따라 중단한 NPC 인터랙션을 재개하거나 완전히 종료합니다.
        /// 취소하기 버튼으로 닫힌 경우에만 선택지와 동적 대사 컨텍스트를 다시 구성합니다.
        /// </summary>
        /// <param name="closeReason">스탯 초기화 창이 닫힌 이유입니다.</param>
        private void HandlePlayerStatResetClosed(
            PlayerStatResetCloseReason closeReason)
        {
            InteractionManager.InteractionSuspensionToken token =
                _playerStatResetSuspensionToken;
            _playerStatResetSuspensionToken = default;

            InteractionManager interactionManager =
                SceneGame?.InteractionManager;
            if (interactionManager == null)
            {
                return;
            }

            if (closeReason == PlayerStatResetCloseReason.Cancelled)
            {
                interactionManager.ResumeSuspendedInteraction(token);
                return;
            }

            interactionManager.CompleteSuspendedInteraction(token);
        }

        /// <summary>
        /// 로컬라이즈 키를 사용해 인터랙션 피드백 메시지를 표시합니다.
        /// GGemCoNpcInteractionSettings 의 대사 연출 정책을 그대로 따르도록 즉시 노출은 사용하지 않습니다.
        /// </summary>
        /// <param name="localizationKey">출력할 로컬라이즈 키입니다.</param>
        /// <param name="arguments">Smart String 치환에 사용할 인자 목록입니다.</param>
        private void ShowLocalizedInteractionFeedbackMessage(string localizationKey, params object[] arguments)
        {
            if (_localizationManager == null || string.IsNullOrWhiteSpace(localizationKey))
            {
                return;
            }

            string message = _localizationManager.GetSmartInteractionByKey(localizationKey, arguments);
            ShowInteractionFeedbackMessage(message);
        }

        /// <summary>
        /// 인터랙션 실행 실패 또는 안내용 피드백 메시지를 표시합니다.
        /// 선택지는 메시지 출력이 끝난 뒤 다시 표시되도록 일반 대사와 동일한 타자 효과 파이프라인을 사용합니다.
        /// </summary>
        /// <param name="message">표시할 피드백 메시지입니다.</param>
        private void ShowInteractionFeedbackMessage(string message)
        {
            ApplyDialogueMessage(message, revealImmediately: false);
        }

        /// <summary>
        /// 선택지 실행으로 대화창이 닫힐 때 세션 상태를 정리합니다.
        /// </summary>
        private void CloseDialogueByChoice()
        {
            _currentNpc = null;
            SceneGame?.InteractionManager?.RemoveCurrentNpc();
            Show(false);
        }

        /// <summary>
        /// 자식 UIWindow가 열려 있는 동안 현재 대화 UI만 숨깁니다.
        /// NPC 참조와 플레이어 조작 잠금은 InteractionManager가 재개 또는 종료 결과를 받을 때까지 유지합니다.
        /// </summary>
        private void HideDialogueForChildWindow()
        {
            Show(false);
        }

        /// <summary>
        /// 대화 종료 정책 또는 닫기 버튼으로 창을 종료할 때 공통 정리를 수행합니다.
        /// </summary>
        private void CloseInteractionWindow()
        {
            _currentNpc = null;
            SceneGame?.InteractionManager?.RemoveCurrentNpc();
            Show(false);
        }
    }
}
