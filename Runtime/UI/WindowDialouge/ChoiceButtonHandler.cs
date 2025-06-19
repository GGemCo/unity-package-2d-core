using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 선택지 버튼 관리
    /// </summary>
    public class ChoiceButtonHandler
    {
        private const int ButtonCount = 3;
        private readonly Dictionary<int, Button> buttons;
        private readonly Transform container;
        private readonly int paddingWidth;
        private readonly GameObject prefabButtonAnswer;

        public Action<int> OnChoiceSelected;

        public ChoiceButtonHandler(Transform container, int padding, GameObject prefabButtonAnswer)
        {
            this.container = container;
            this.paddingWidth = padding;
            this.prefabButtonAnswer = prefabButtonAnswer;
            buttons = new Dictionary<int, Button>();
        }

        /// <summary>
        /// interaction 버튼 초기화
        /// </summary>
        public void InitializeButtonChoice()
        {
            if (prefabButtonAnswer == null)
            {
                GcLogger.LogError("선택 버튼 프리팹이 없습니다.");
                return;
            }
            if (container == null)
            {
                GcLogger.LogError("선택 버튼 container 가 없습니다.");
                return;
            }
            buttons.Clear();

            for (int i = 0; i < ButtonCount; i++)
            {
                GameObject buttonObj = Object.Instantiate(prefabButtonAnswer, container);
                Button button = buttonObj.GetComponent<Button>();
                if (button == null) continue;
                buttons.TryAdd(i, button);
                button.gameObject.SetActive(false); // 초기 상태 비활성화
            }
        }
        /// <summary>
        /// 선택지 버튼 정보 업데이트
        /// </summary>
        /// <param name="options"></param>
        public void SetupButtons(List<DialogueOption> options)
        {
            if (options == null || options.Count == 0)
            {
                container.gameObject.SetActive(false);
                return;
            }

            float maxWidth = 0;
            container.gameObject.SetActive(true);

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons.GetValueOrDefault(i);
                if (i < options.Count && options[i] != null)
                {
                    var answerComponent = button.GetComponent<UIButtonAnswer>();
                    float width = answerComponent.SetButtonTitle(options[i].optionText);
                    maxWidth = Mathf.Max(maxWidth, width);

                    int index = i;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnChoiceSelected?.Invoke(index));
                    button.gameObject.SetActive(true);
                }
                else
                {
                    button.gameObject.SetActive(false);
                }
            }

            float targetWidth = maxWidth + paddingWidth;
            foreach (var btn in buttons.Values)
            {
                if (btn.gameObject.activeSelf)
                {
                    btn.GetComponent<UIButtonAnswer>().ChangeWidth(targetWidth);
                }
            }
        }
        /// <summary>
        /// 선택지 버튼 안보이게 처리
        /// </summary>
        public void HideButtons()
        {
            container?.gameObject.SetActive(false);
            foreach (var btn in buttons.Values)
            {
                btn.gameObject.SetActive(false);
            }
        }
    }
}
