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
        /// 현재 대사의 화자 캐릭터 머리 위에 대화창을 배치합니다.
        /// Player 대사면 플레이어, NPC 대사면 NPC, Monster 대사면 몬스터를 기준으로 배치합니다.
        /// </summary>
        private void RefreshPositionCharacterTop()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            CharacterBase speaker = ResolveCurrentSpeakerCharacter();
            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            if (speaker == null || panelDialogue == null || sceneGame == null || sceneGame.containerDialogueBalloon == null)
            {
                return;
            }

            panelDialogue.transform.SetParent(sceneGame.containerDialogueBalloon.transform, false);
            Vector3 worldPosition = speaker.transform.position + new Vector3(0f, speaker.GetHeightByScale(), 0f) + offsetPanelDialogue;
            panelDialogue.transform.position = worldPosition;
        }

        /// <summary>
        /// 현재 대사 노드의 캐릭터 타입을 기준으로 말풍선이 따라갈 화자 캐릭터를 반환합니다.
        /// 대화 노드가 없거나 대상 캐릭터를 찾지 못하면 현재 인터랙션 NPC를 반환합니다.
        /// </summary>
        /// <returns>말풍선을 따라갈 화자 캐릭터입니다.</returns>
        private CharacterBase ResolveCurrentSpeakerCharacter()
        {
            DialogueNodeData node = _currentDialogueNode;
            if (node == null)
            {
                return _currentNpc;
            }

            switch (node.characterType)
            {
                case CharacterConstants.Type.Player:
                    return ResolvePlayerCharacter() ?? _currentNpc;

                case CharacterConstants.Type.Npc:
                    return ResolveNpcSpeaker(node.characterUid) ?? _currentNpc;

                case CharacterConstants.Type.Monster:
                    return ResolveMonsterSpeaker(node.characterUid) ?? _currentNpc;

                default:
                    return _currentNpc;
            }
        }

        /// <summary>
        /// 현재 윈도우에 주입된 SceneGame을 우선 사용하고, 없으면 SceneGame 싱글톤을 반환합니다.
        /// 윈도우 Start 이전에 위치 갱신이 호출되는 경우에도 캐릭터/컨테이너 조회를 안전하게 수행하기 위한 보조 함수입니다.
        /// </summary>
        /// <returns>현재 활성 게임 씬입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private global::GGemCo2DCore.SceneGame ResolveActiveSceneGame()
        {
            return SceneGame != null ? SceneGame : global::GGemCo2DCore.SceneGame.Instance;
        }

        /// <summary>
        /// 현재 게임 씬에서 플레이어 캐릭터를 찾아 반환합니다.
        /// SceneGame 참조가 아직 주입되지 않은 초기 프레임도 고려해 싱글톤을 함께 확인합니다.
        /// </summary>
        /// <returns>플레이어 캐릭터입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolvePlayerCharacter()
        {
            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            GameObject playerObject = sceneGame != null ? sceneGame.player : null;

            if (playerObject == null)
            {
                return null;
            }

            return playerObject.GetComponent<CharacterBase>();
        }

        /// <summary>
        /// 대사 노드에 지정된 NPC UID를 기준으로 현재 맵의 NPC 캐릭터를 반환합니다.
        /// UID가 비어 있거나 현재 인터랙션 NPC와 같으면 현재 NPC를 우선 사용합니다.
        /// </summary>
        /// <param name="npcUid">대사 노드에 지정된 NPC UID입니다.</param>
        /// <returns>NPC 캐릭터입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveNpcSpeaker(int npcUid)
        {
            if (npcUid <= 0 || (_currentNpc != null && _currentNpc.uid == npcUid))
            {
                return _currentNpc;
            }

            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            MapManager mapManager = sceneGame != null ? sceneGame.mapManager : null;
            return mapManager != null ? mapManager.GetNpcByUid(npcUid) : null;
        }

        /// <summary>
        /// 대사 노드에 지정된 Monster UID를 기준으로 현재 맵의 몬스터 캐릭터를 반환합니다.
        /// </summary>
        /// <param name="monsterUid">대사 노드에 지정된 Monster UID입니다.</param>
        /// <returns>몬스터 캐릭터입니다. 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveMonsterSpeaker(int monsterUid)
        {
            if (monsterUid <= 0)
            {
                return null;
            }

            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            MapManager mapManager = sceneGame != null ? sceneGame.mapManager : null;
            return mapManager != null ? mapManager.GetMonsterByUid(monsterUid) : null;
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
