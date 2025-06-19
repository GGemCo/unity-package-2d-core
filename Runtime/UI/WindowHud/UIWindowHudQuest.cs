using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class UIWindowHudQuest : UIWindow
    {
        [Header("윈도우 필수속성")]
        [Tooltip("퀘스트 UI Element 프리팹")]
        public GameObject prefabElement;
        [Tooltip("퀘스트 UI Element가 들어갈 오브젝트")]
        public GameObject containerElement;
        
        private QuestData questData;
        private Dictionary<int, UIElementHudQuest> elements = new Dictionary<int, UIElementHudQuest>();
        
        protected override void Awake()
        {
            // uid 를 먼저 지정해야 한다.
            uid = UIWindowManager.WindowUid.HudQuest;
            base.Awake();
        }
        /// <summary>
        /// 퀘스트 Element 추가하기
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="questStepIndex"></param>
        public void AddQuestElement(int questUid, int questStepIndex)
        {
            UIElementHudQuest elementHudQuest = elements.GetValueOrDefault(questUid);
            if (elementHudQuest == null)
            {
                GameObject element = Instantiate(prefabElement, containerElement.transform);
                if (element == null) return;
                elementHudQuest = element.GetComponent<UIElementHudQuest>();
                if (elementHudQuest == null) return;
                elements.TryAdd(questUid, elementHudQuest);
                elementHudQuest.InitializeInfo(questUid, questStepIndex);
            }
            else
            {
                elementHudQuest.InitializeInfo(questUid, questStepIndex);
                elementHudQuest.UpdateInfo();
            }
        }
        public UIElementHudQuest GetQuestElement(int questUid)
        {
            return elements.GetValueOrDefault(questUid);
        }
        /// <summary>
        /// 퀘스트 Element 지우기
        /// </summary>
        /// <param name="questUid"></param>
        public void RemoveQuestElement(int questUid)
        {
            UIElementHudQuest element = elements.GetValueOrDefault(questUid);
            if (element == null) return;
            elements.Remove(questUid);
            Destroy(element.gameObject);
        }
        public void SetCount(int questUid, int count)
        {
            UIElementHudQuest element = elements.GetValueOrDefault(questUid);
            if (element == null) return;
            element.SetCount(count);
        }
    }
}