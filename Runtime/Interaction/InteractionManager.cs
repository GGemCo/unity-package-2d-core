using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class InteractionManager
    {
        private SceneGame _sceneGame;
        private TableNpc _tableNpc;
        private TableInteraction _tableInteraction;
        private UIWindowInteractionDialogue _uiWindowInteractionDialogue;
        private CharacterBase _currentNpc;
        
        public void Initialize(SceneGame scene)
        {
            _sceneGame = scene;
            _tableNpc = TableLoaderManager.Instance.TableNpc;
            _tableInteraction = TableLoaderManager.Instance.TableInteraction;
            _uiWindowInteractionDialogue =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInteractionDialogue>(UIWindowConstants.WindowUid.InteractionDialogue);
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShopSale>(UIWindowConstants.WindowUid.ShopSale);
        }
        /// <summary>
        /// Npc 의 interaction 정보 가져오기
        /// </summary>
        /// <param name="characterBase"></param>
        public void SetInfo(CharacterBase characterBase)
        {
            // 연출 중이면 실행하지 않는다.
            if (_sceneGame.CutsceneManager.IsPlaying()) return;
            
            if (characterBase == null)
            {
                GcLogger.LogError("Npc 스크립트가 없습니다.");
                return;
            }

            var infoNpc = _tableNpc.GetDataByUid(characterBase.uid);
            if (infoNpc == null)
            {
                GcLogger.LogError("npc 테이블에 정보가 없습니다. npc uid: "+characterBase.uid);
                return;
            }

            _currentNpc = characterBase;
            
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
            _sceneGame.uIWindowManager?.CloseAll(new List<UIWindowConstants.WindowUid>
                { UIWindowConstants.WindowUid.InteractionDialogue });
            // 인터렉션 대화창 보여주기
            ShowDialogue(infoNpc, infoInteraction, npcQuestDatas);
        }

        private void ShowDialogue(StruckTableNpc struckTableNpc, StruckTableInteraction struckTableInteraction, List<NpcQuestData> questInfos)
        {
            _uiWindowInteractionDialogue?.SetInfos(struckTableNpc, struckTableInteraction, questInfos);
            _uiWindowInteractionDialogue?.Show(true);
        }

        public void RemoveCurrentNpc()
        {
            _currentNpc = null;
        }
        /// <summary>
        /// interaction 종료하기
        /// </summary>
        public void EndInteraction()
        {
            // npc 가 interaction 범위면 다시 열기
            if (_currentNpc != null)
            {
                _sceneGame?.uIWindowManager?.CloseAll(new List<UIWindowConstants.WindowUid>
                    { UIWindowConstants.WindowUid.InteractionDialogue });
                SetInfo(_currentNpc);
                return;
            }
            _sceneGame?.uIWindowManager?.CloseAll();
            _uiWindowInteractionDialogue?.OnEndInteraction();
        }
        public bool IsInteractioning()
        {
            return _currentNpc != null;
        }

        public void OnDestroy()
        {
        }
    }
}