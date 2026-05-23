using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/>의 위치 배치, 종료 처리, 세션 정리 책임을 담당하는 partial 스크립트입니다.
    /// </summary>
    public partial class UIWindowInteractionDialogue
    {
        /// <summary>
        /// PositionType 별 위치 조정을 수행합니다.
        /// </summary>
        private void RefreshPosition()
        {
            switch (positionType)
            {
                case PositionType.CharacterTop:
                    RefreshPositionCharacterTop();
                    break;
            }
        }

        /// <summary>
        /// NPC 머리 위에 대화창을 배치합니다.
        /// </summary>
        private void RefreshPositionCharacterTop()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            if (!_currentNpc || panelDialogue == null || SceneGame.containerDialogueBalloon == null)
            {
                return;
            }

            panelDialogue.transform.SetParent(SceneGame.containerDialogueBalloon.transform, false);
            Vector3 worldPosition = _currentNpc.transform.position + new Vector3(0, _currentNpc.GetHeightByScale(), 0) + offsetPanelDialogue;
            panelDialogue.transform.position = worldPosition;
        }

        /// <summary>
        /// NPC 이름 텍스트를 설정합니다.
        /// </summary>
        /// <param name="npcName">표시할 NPC 이름입니다.</param>
        private void SetNpcName(string npcName)
        {
            if (textName == null)
            {
                return;
            }

            textName.text = npcName;
        }

        /// <summary>
        /// 플레이어가 NPC 범위를 벗어나 인터랙션이 종료될 때 처리합니다.
        /// </summary>
        public void OnEndInteraction()
        {
            _currentNpc = null;
            Show(false);
        }

        /// <summary>
        /// 윈도우 표시 상태가 바뀔 때 대화 세션 관련 UI 상태를 정리합니다.
        /// </summary>
        /// <param name="show">표시 여부입니다.</param>
        public override void OnShow(bool show)
        {
            base.OnShow(show);

            if (!show)
            {
                HandleDialogueHidden();
            }
        }

        /// <summary>
        /// 대화창이 숨겨질 때 페이지 상태와 선택지 표시를 정리합니다.
        /// </summary>
        private void HandleDialogueHidden()
        {
            _dialogueLoadVersion++;
            _isLoadingDialogue = false;
            ResetPanelDialogue();
            ResetChoiceButtons();
            _defaultChoices.Clear();
            _currentNpcData = null;
            _currentInteractionData = null;
            _currentDialogueSelection = InteractionDialogueSelectionResult.None;
            ClearCurrentDialogueNode();
            _currentTextContext = InteractionDialogueTextContext.Empty;
            _currentQuestDatas.Clear();
            if (imageThumbnail != null)
            {
                imageThumbnail.sprite = null;
                ApplyThumbnailVisibilityAfterBinding();
            }
            _messagePlayer.Clear(textMessage);
            _dialogueSession.Clear();
            _isExecutingChoice = false;
            ClearPendingAutoStartChoice();
            ApplyMessageFontSize(0f);
            _lastKnownVisibleCharacters = -1;
            RestoreSpeechBubbleLayoutDefaults();
        }

        /// <summary>
        /// CharacterTop 모드에서 변경했던 부모를 원래 윈도우로 되돌립니다.
        /// </summary>
        private void ResetPanelDialogue()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            if (positionType == PositionType.CharacterTop && panelDialogue != null)
            {
                panelDialogue.transform.SetParent(transform, false);
            }

            if (panelMessage != null)
            {
                Vector2 anchoredPosition = panelMessage.anchoredPosition;
                anchoredPosition.x = 0f;
                panelMessage.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// 잠자기 상호작용을 통해 저장 후 다음 날로 넘깁니다.
        /// </summary>
        private void SaveGameBySleep()
        {
            SceneGame.saveDataManager.SaveData();
            SceneGame.systemMessageManager.ShowMessageInfo("System_Save_Game_By_Sleep");

            int startMapUid = SceneGame.saveDataManager.Player.CurrentMapUid;
            SceneGame.mapManager.LoadMap(startMapUid);
            SceneGame.gameTimeManager.SetNextDay();
        }

        /// <summary>
        /// Addressables 설정에서 NPC 인터랙션 설정을 가져오고,
        /// 없으면 런타임 기본값을 사용합니다.
        /// </summary>
        /// <returns>사용 가능한 NPC 인터랙션 설정입니다.</returns>
        private GGemCoNpcInteractionSettings ResolveNpcInteractionSettings()
        {
            if (AddressableLoaderSettings.Instance != null && AddressableLoaderSettings.Instance.npcInteractionSettings != null)
            {
                _npcInteractionSettings = AddressableLoaderSettings.Instance.npcInteractionSettings;
            }

            if (_npcInteractionSettings == null)
            {
                _npcInteractionSettings = GGemCoNpcInteractionSettings.CreateRuntimeDefault();
            }

            return _npcInteractionSettings;
        }
    }
}
