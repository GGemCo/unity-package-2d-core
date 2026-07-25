using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/>의 화자 추적 위치와 부모 복원을 담당합니다.
    /// </summary>
    public partial class UIWindowDialogue
    {
        /// <summary>
        /// 설정된 배치 방식에 따라 대화창 위치를 갱신합니다.
        /// </summary>
        private void RefreshPosition()
        {
            if (positionType == PositionType.CharacterTop)
            {
                RefreshPositionCharacterTop();
            }
        }

        /// <summary>
        /// 현재 대사 노드의 화자 머리 위에 대화 패널을 배치합니다.
        /// </summary>
        private void RefreshPositionCharacterTop()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            CharacterBase speaker = ResolveCurrentSpeakerCharacter();
            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            if (speaker == null ||
                panelDialogue == null ||
                sceneGame == null ||
                sceneGame.containerDialogueBalloon == null)
            {
                return;
            }

            panelDialogue.transform.SetParent(sceneGame.containerDialogueBalloon.transform, false);
            Vector3 baseWorldPosition =
                speaker.transform.position + new Vector3(0f, speaker.GetHeightByScale(), 0f);
            panelDialogue.transform.position = baseWorldPosition + ResolveDialogueWorldOffset();
        }

        /// <summary>
        /// 말풍선 모드의 월드 오프셋과 화자 방향 보정 정책을 적용합니다.
        /// </summary>
        /// <returns>대화 패널에 적용할 최종 월드 오프셋입니다.</returns>
        private Vector3 ResolveDialogueWorldOffset()
        {
            if (dialogueVisualMode != DialogueVisualMode.SpeechBubble)
            {
                return Vector3.zero;
            }

            bool hasSpeakerFacing = TryResolveSpeakerFacingRight(out bool isFacingRight);
            return DialogueBalloonWorldOffsetUtility.ResolveOffsetByPolicy(
                speechBubbleWorldOffset,
                speechBubbleWorldOffsetXPolicy,
                hasSpeakerFacing,
                isFacingRight);
        }

        /// <summary>
        /// 현재 대사 노드에 지정된 실제 씬 화자를 반환합니다.
        /// 노드 화자를 찾지 못하면 <see cref="LoadDialogue(int, int)"/>에 전달된 NPC를 사용합니다.
        /// </summary>
        /// <returns>현재 화자 캐릭터이며 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveCurrentSpeakerCharacter()
        {
            if (_currentDialogue != null)
            {
                switch (_currentDialogue.characterType)
                {
                    case CharacterConstants.Type.Player:
                        return ResolvePlayerCharacter() ?? ResolveFallbackNpc();

                    case CharacterConstants.Type.Npc:
                        return ResolveNpcSpeaker(_currentDialogue.characterUid) ?? ResolveFallbackNpc();

                    case CharacterConstants.Type.Monster:
                        return ResolveMonsterSpeaker(_currentDialogue.characterUid) ?? ResolveFallbackNpc();
                }
            }

            return ResolveFallbackNpc();
        }

        /// <summary>
        /// 현재 활성 게임 씬 참조를 안전하게 반환합니다.
        /// </summary>
        /// <returns>활성 게임 씬이며 준비되지 않았으면 <see langword="null"/>입니다.</returns>
        private global::GGemCo2DCore.SceneGame ResolveActiveSceneGame()
        {
            return SceneGame != null ? SceneGame : global::GGemCo2DCore.SceneGame.Instance;
        }

        /// <summary>
        /// 현재 플레이어 캐릭터를 반환합니다.
        /// </summary>
        /// <returns>플레이어 캐릭터이며 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolvePlayerCharacter()
        {
            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            return sceneGame != null && sceneGame.player != null
                ? sceneGame.player.GetComponent<CharacterBase>()
                : null;
        }

        /// <summary>
        /// NPC UID에 대응하는 현재 맵의 캐릭터를 반환합니다.
        /// </summary>
        /// <param name="npcUid">조회할 NPC UID입니다.</param>
        /// <returns>NPC 캐릭터이며 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveNpcSpeaker(int npcUid)
        {
            if (npcUid <= 0)
            {
                return null;
            }

            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            return sceneGame != null && sceneGame.mapManager != null
                ? sceneGame.mapManager.GetNpcByUid(npcUid)
                : null;
        }

        /// <summary>
        /// Monster UID에 대응하는 현재 맵의 캐릭터를 반환합니다.
        /// </summary>
        /// <param name="monsterUid">조회할 Monster UID입니다.</param>
        /// <returns>Monster 캐릭터이며 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveMonsterSpeaker(int monsterUid)
        {
            if (monsterUid <= 0)
            {
                return null;
            }

            global::GGemCo2DCore.SceneGame sceneGame = ResolveActiveSceneGame();
            return sceneGame != null && sceneGame.mapManager != null
                ? sceneGame.mapManager.GetMonsterByUid(monsterUid)
                : null;
        }

        /// <summary>
        /// 대화 시작 시 전달받은 NPC UID를 기준으로 대체 화자를 반환합니다.
        /// </summary>
        /// <returns>대체 NPC 캐릭터이며 찾지 못하면 <see langword="null"/>입니다.</returns>
        private CharacterBase ResolveFallbackNpc()
        {
            return ResolveNpcSpeaker(_currentNpcUid);
        }

        /// <summary>
        /// CharacterTop 모드에서 외부 컨테이너로 이동한 패널을 원래 윈도우 아래로 복원합니다.
        /// </summary>
        private void ResetPanelDialogueParent()
        {
            if (panelDialogue == null)
            {
                CacheSpeechBubbleLayoutReferences();
            }

            if (positionType == PositionType.CharacterTop &&
                panelDialogue != null &&
                panelDialogue.transform.parent != transform)
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
    }
}
