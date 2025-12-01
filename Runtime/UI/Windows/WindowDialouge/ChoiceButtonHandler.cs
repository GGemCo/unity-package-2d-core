using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
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

        private readonly Dictionary<int, DialogueOption> optionData = new();

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
            optionData.Clear();

            for (int i = 0; i < ButtonCount; i++)
            {
                GameObject buttonObj = Object.Instantiate(prefabButtonAnswer, container);
                Button button = buttonObj.GetComponent<Button>();
                if (button == null) continue;

                int capturedIndex = i; // 캡처된 인덱스를 고정된 리스너로 등록
                button.onClick.AddListener(() => OnButtonClicked(capturedIndex));

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
                HideButtons();
                return;
            }

            optionData.Clear();
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

                    // 인덱스에 대한 데이터를 Dictionary에 저장
                    optionData[i] = options[i];
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
        /// 버튼 클릭 시 처리 (인덱스 기반으로 호출)
        /// </summary>
        /// <param name="index"></param>
        private void OnButtonClicked(int index)
        {
            if (optionData.TryGetValue(index, out var option))
            {
                OnChoiceSelected?.Invoke(index);
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
