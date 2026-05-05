using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Npc와의 상호작용 처리 매니저입니다.
    /// </summary>
    public class InteractionManager
    {
        private SceneGame _sceneGame;
        private TableNpc _tableNpc;
        private TableInteraction _tableInteraction;
        private UIWindowInteractionDialogue _uiWindowInteractionDialogue;
        private CharacterBase _currentNpc;
        private GGemCoNpcInteractionSettings _npcInteractionSettings;

        public CharacterBase CurrentNpc => _currentNpc;

        /// <summary>
        /// 상호작용 매니저에 씬 의존성을 연결합니다.
        /// </summary>
        /// <param name="scene">현재 게임 씬입니다.</param>
        public void Initialize(SceneGame scene)
        {
            _sceneGame = scene;
            _tableNpc = TableLoaderManager.Instance.TableNpc;
            _tableInteraction = TableLoaderManager.Instance.TableInteraction;
            _npcInteractionSettings = ResolveNpcInteractionSettings();
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        public void SetInfo(CharacterBase characterBase)
        {
            SetInfo(characterBase, InteractionDialogueTextContext.Empty);
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// 대사 본문과 선택지에서 사용할 위치 기반 파라미터를 함께 전달합니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        /// <param name="dialogueParameters">대사 포맷에 사용할 위치 기반 파라미터입니다.</param>
        public void SetInfo(CharacterBase characterBase, params object[] dialogueParameters)
        {
            SetInfo(characterBase, InteractionDialogueTextContext.FromArgs(dialogueParameters));
        }

        /// <summary>
        /// NPC의 interaction 정보를 읽어 인터랙션 대화창을 엽니다.
        /// </summary>
        /// <param name="characterBase">대화 대상 NPC입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        public void SetInfo(CharacterBase characterBase, InteractionDialogueTextContext textContext)
        {
            // 연출 중이면 실행하지 않는다.
            if (_sceneGame.CutsceneManager.IsPlaying())
            {
                return;
            }

            if (characterBase == null)
            {
                GcLogger.LogError("Npc 스크립트가 없습니다.");
                return;
            }

            StruckTableNpc infoNpc = _tableNpc.GetDataByUid(characterBase.uid);
            if (infoNpc == null)
            {
                GcLogger.LogError("npc 테이블에 정보가 없습니다. npc uid: " + characterBase.uid);
                return;
            }

            _currentNpc = characterBase;
            _npcInteractionSettings = ResolveNpcInteractionSettings();

            // 퀘스트 정보
            Npc npc = _currentNpc as Npc;
            List<NpcQuestData> npcQuestDatas = npc?.GetQuestInfos();

            // 인터렉션 정보
            StruckTableInteraction infoInteraction = null;
            if (infoNpc.InteractionUid > 0)
            {
                infoInteraction = _tableInteraction.GetDataByUid(infoNpc.InteractionUid);
            }

            // 다른 윈도우가 열려있으면 닫아주기
            if (_npcInteractionSettings != null && _npcInteractionSettings.ui.hideOtherUiOnStart)
            {
                _sceneGame.uIWindowManager?.CloseAll(new List<UIWindowConstants.WindowUid>
                {
                    UIWindowConstants.WindowUid.InteractionDialogue,
                });
            }

            // dialogue 랜덤 선택 결과를 먼저 확정합니다.
            InteractionDialogueSelectionResult dialogueSelection =
                InteractionDialogueSelector.Select(infoInteraction);

            // 인터렉션 대화창 보여주기
            ShowDialogue(
                _currentNpc,
                infoNpc,
                infoInteraction,
                npcQuestDatas,
                _npcInteractionSettings,
                dialogueSelection,
                textContext);
        }

        /// <summary>
        /// 인터랙션 대화창에 현재 NPC 상호작용 정보를 전달합니다.
        /// </summary>
        /// <param name="npc">현재 NPC입니다.</param>
        /// <param name="struckTableNpc">NPC 테이블 데이터입니다.</param>
        /// <param name="struckTableInteraction">인터랙션 테이블 데이터입니다.</param>
        /// <param name="questInfos">NPC 퀘스트 목록입니다.</param>
        /// <param name="npcInteractionSettings">NPC 인터랙션 설정입니다.</param>
        /// <param name="dialogueSelection">이번 인터랙션에서 선택된 dialogue 정보입니다.</param>
        /// <param name="textContext">대사 포맷에 사용할 텍스트 컨텍스트입니다.</param>
        private void ShowDialogue(
            CharacterBase npc,
            StruckTableNpc struckTableNpc,
            StruckTableInteraction struckTableInteraction,
            List<NpcQuestData> questInfos,
            GGemCoNpcInteractionSettings npcInteractionSettings,
            InteractionDialogueSelectionResult dialogueSelection,
            InteractionDialogueTextContext textContext)
        {
            if (_uiWindowInteractionDialogue == null)
            {
                _uiWindowInteractionDialogue =
                    _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInteractionDialogue>(
                        UIWindowConstants.WindowUid.InteractionDialogue);
            }

            _uiWindowInteractionDialogue?.SetInfos(
                npc,
                struckTableNpc,
                struckTableInteraction,
                questInfos,
                npcInteractionSettings,
                dialogueSelection,
                textContext);
        }

        /// <summary>
        /// 현재 상호작용 중인 NPC 참조를 제거합니다.
        /// </summary>
        public void RemoveCurrentNpc()
        {
            _currentNpc = null;
        }

        /// <summary>
        /// 현재 인터랙션 대화창을 종료합니다.
        /// </summary>
        public void EndInteraction()
        {
            _uiWindowInteractionDialogue?.OnEndInteraction();
            _currentNpc = null;
        }

        /// <summary>
        /// 현재 인터랙션이 활성 상태인지 여부를 반환합니다.
        /// </summary>
        /// <returns>현재 NPC 참조가 있으면 true입니다.</returns>
        public bool IsInteractioning()
        {
            return _currentNpc != null;
        }

        /// <summary>
        /// Addressables 로더에서 NPC 인터랙션 설정을 가져오고,
        /// 없으면 런타임 기본값을 사용합니다.
        /// </summary>
        /// <returns>사용 가능한 NPC 인터랙션 설정입니다.</returns>
        private GGemCoNpcInteractionSettings ResolveNpcInteractionSettings()
        {
            if (AddressableLoaderSettings.Instance != null &&
                AddressableLoaderSettings.Instance.npcInteractionSettings != null)
            {
                _npcInteractionSettings = AddressableLoaderSettings.Instance.npcInteractionSettings;
            }

            if (_npcInteractionSettings == null)
            {
                _npcInteractionSettings = GGemCoNpcInteractionSettings.CreateRuntimeDefault();
            }

            return _npcInteractionSettings;
        }

        /// <summary>
        /// 매니저 종료 시 정리할 작업을 처리합니다.
        /// </summary>
        public void OnDestroy()
        {
        }
    }
}
