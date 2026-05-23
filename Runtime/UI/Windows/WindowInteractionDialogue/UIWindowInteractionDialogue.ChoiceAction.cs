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
                    case ChoiceType.Quest:
                        await OnClickChoiceQuest(data.NpcQuestData);
                        break;
                    case ChoiceType.Interaction:
                        OnClickChoiceInteraction(data);
                        break;
                    case ChoiceType.Dialogue:
                        await OnClickChoiceDialogue(index);
                        break;
                }
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
                await ApplyCurrentDialogueNodeAsync(requestVersion);
                return;
            }

            if (_dialogueSession.IsCompleted)
            {
                HandleDialogueSequenceCompleted();
            }
        }

        /// <summary>
        /// 퀘스트 버튼 클릭 처리를 수행합니다.
        /// </summary>
        /// <param name="npcQuestData">선택한 퀘스트 데이터입니다.</param>
        private async Task OnClickChoiceQuest(NpcQuestData npcQuestData)
        {
            try
            {
                CloseDialogueByChoice();
                if (npcQuestData.Status == QuestConstants.Status.Ready)
                {
                    if (await _questManager.StartQuest(npcQuestData.QuestUid, _currentCharacterUid) == false)
                    {
                        return;
                    }
                }
                else if (npcQuestData.Status == QuestConstants.Status.InProgress)
                {
                    DialogEventData data = new DialogEventData(
                        npcUid: _currentCharacterUid);
                    GameEventManager.DialogStart(data);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError(e.Message);
            }
        }

        /// <summary>
        /// interaction 버튼 클릭 처리를 수행합니다.
        /// </summary>
        /// <param name="data">버튼에 연결된 interaction 데이터입니다.</param>
        private void OnClickChoiceInteraction(InteractionData data)
        {
            bool handled = false;

            if (data.HasBuiltInInteraction)
            {
                handled = ExecuteBuiltInInteraction(data.InteractionType, data.Value);
            }
            else if (data.HasCustomInteraction)
            {
                handled = InteractionCustomHandlerRegistry.TryExecute(data.CustomTypeKey, SceneGame, _currentNpc, data.Value);
                if (!handled)
                {
                    GcLogger.LogError($"커스텀 interaction 처리기가 등록되지 않았습니다. key: {data.CustomTypeKey}");
                }
            }

            if (handled)
            {
                CloseDialogueByChoice();
            }
        }

        /// <summary>
        /// 기본 제공 interaction 타입을 실행합니다.
        /// </summary>
        /// <param name="interactionType">실행할 interaction 타입입니다.</param>
        /// <param name="value">보조 값입니다.</param>
        /// <returns>실행 성공 시 true입니다.</returns>
        private bool ExecuteBuiltInInteraction(InteractionConstants.Type interactionType, int value)
        {
            if (interactionType == InteractionConstants.Type.None)
            {
                return false;
            }

            switch (interactionType)
            {
                case InteractionConstants.Type.Shop:
                    _uiWindowShop?.Show(true);
                    _uiWindowShop?.SetInfoByShopUid(value);
                    return true;
                case InteractionConstants.Type.Stash:
                    _uiWindowStash?.Show(true);
                    return true;
                case InteractionConstants.Type.ShopSale:
                    _uiWindowShopSale?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemUpgrade:
                    _uiWindowItemUpgrade?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemSalvage:
                    _uiWindowItemSalvage?.Show(true);
                    return true;
                case InteractionConstants.Type.ItemCraft:
                    _uiWindowItemCraft?.Show(true);
                    _uiWindowItemCraft?.SetInfoByItemCraftUid(value);
                    return true;
                case InteractionConstants.Type.SaveGame:
                    SaveGameBySleep();
                    return true;
                case InteractionConstants.Type.StatReset:
                    return OpenPlayerStatReset();
                case InteractionConstants.Type.WorldMap:
                    _uiWindowWorldMap?.Show(true);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 스탯 초기화 창을 열기 전에 비용 조건을 검사합니다.
        /// </summary>
        /// <returns>창을 열었으면 true입니다.</returns>
        private bool OpenPlayerStatReset()
        {
            if (_playerSettings.statPointResetCost > 0)
            {
                long playerGold = _playerData.CurrentGold;
                if (playerGold < _playerSettings.statPointResetCost)
                {
                    ShowLocalizedInteractionFeedbackMessage("Text_Not_Enough_Gold", _playerSettings.statPointResetCost);
                    return false;
                }
            }

            _uiWindowPlayerStatReset?.Show(true);
            return true;
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
