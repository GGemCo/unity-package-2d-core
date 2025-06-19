
using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public class InteractionManager
    {
        private SceneGame sceneGame;
        private TableNpc tableNpc;
        private TableInteraction tableInteraction;
        private UIWindowInteractionDialogue uiWindowInteractionDialogue;
        private CharacterBase currentNpc;
        
        public void Initialize(SceneGame scene)
        {
            sceneGame = scene;
            tableNpc = TableLoaderManager.Instance.TableNpc;
            tableInteraction = TableLoaderManager.Instance.TableInteraction;
            uiWindowInteractionDialogue =
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInteractionDialogue>(UIWindowManager.WindowUid.InteractionDialogue);
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowShopSale>(UIWindowManager.WindowUid.ShopSale);
        }
        /// <summary>
        /// Npc 의 interaction 정보 가져오기
        /// </summary>
        /// <param name="characterBase"></param>
        public void SetInfo(CharacterBase characterBase)
        {
            // 연출 중이면 실행하지 않는다.
            if (sceneGame.CutsceneManager.IsPlaying()) return;
            
            if (characterBase == null)
            {
                GcLogger.LogError("Npc 스크립트가 없습니다.");
                return;
            }

            var infoNpc = tableNpc.GetDataByUid(characterBase.uid);
            if (infoNpc == null)
            {
                GcLogger.LogError("npc 테이블에 정보가 없습니다. npc uid: "+characterBase.uid);
                return;
            }

            currentNpc = characterBase;
            
            // 퀘스트 정보
            Npc npc = currentNpc as Npc;
            List<NpcQuestData> npcQuestDatas = npc?.GetQuestInfos();
            
            // 인터렉션 정보
            StruckTableInteraction infoInteraction = null;
            if (infoNpc.InteractionUid > 0)
            {
                infoInteraction = tableInteraction.GetDataByUid(infoNpc.InteractionUid);
            }
            // 다른 윈도우가 열려있으면 닫아주기
            sceneGame.uIWindowManager.CloseAll(new List<UIWindowManager.WindowUid>
                { UIWindowManager.WindowUid.InteractionDialogue });
            // 인터렉션 대화창 보여주기
            ShowDialogue(infoNpc, infoInteraction, npcQuestDatas);
        }

        private void ShowDialogue(StruckTableNpc struckTableNpc, StruckTableInteraction struckTableInteraction, List<NpcQuestData> questInfos)
        {
            uiWindowInteractionDialogue?.SetInfos(struckTableNpc, struckTableInteraction, questInfos);
            uiWindowInteractionDialogue?.Show(true);
        }

        public void RemoveCurrentNpc()
        {
            currentNpc = null;
        }
        /// <summary>
        /// interaction 종료하기
        /// </summary>
        public void EndInteraction()
        {
            // npc 가 interaction 범위면 다시 열기
            if (currentNpc != null)
            {
                sceneGame?.uIWindowManager?.CloseAll(new List<UIWindowManager.WindowUid>
                    { UIWindowManager.WindowUid.InteractionDialogue });
                SetInfo(currentNpc);
                return;
            }
            sceneGame?.uIWindowManager?.CloseAll();
            uiWindowInteractionDialogue?.OnEndInteraction();
        }
        public bool IsInteractioning()
        {
            return currentNpc != null;
        }

        public void OnDestroy()
        {
        }
    }
}