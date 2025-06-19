using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class ResultCommon
    {
        public enum Type
        {
            Fail = 0,
            Success = 1,
        }
        public Type Code;
        public string Message;
        public List<SaveDataIcon> ResultIcons;

        public ResultCommon(Type type, string message = "", List<SaveDataIcon> resultIcons = null)
        {
            Code = type;
            Message = message;
            ResultIcons = resultIcons;
        }
        public bool IsSuccess() => Code == Type.Success;
    }
    public class SystemMessageManager : MonoBehaviour
    {
        public enum MessageType
        {
            None,
            Normal,
            Warning,
            Error,
        }
        [Tooltip("메시지 텍스트 UI")]
        [SerializeField] private GameObject objectText;

        [Header("시스템 메시지 디폴트 설정 값")]
        [Tooltip("디폴트 타입")]
        public MessageType type = MessageType.Normal;
        [Tooltip("보여질 시간(초)")]
        public float duration = 2f;
        [Tooltip("fade in 시간(초)")]
        public float fadeInTime = 0.1f;
        [Tooltip("fade out 시간(초)")]
        public float fadeOutTime = 0.1f;
        [Tooltip("폰트 색상, 일반 타입 폰트 색상")]
        public Color textColor = Color.white;
        [Tooltip("폰트 크기")]
        public int fontSize = 36;
        
        [Header("타입별 폰트 색상")]
        [Tooltip("경고 메시지")]
        public Color warningColor = Color.yellow;
        [Tooltip("에러 메시지")]
        public Color errorColor = Color.red;
        private Dictionary<MessageType, Color> messageTypeColors = new Dictionary<MessageType, Color>();
        
        private TextMeshProUGUI textMessage;    // 메시지 텍스트 UI
        private CanvasGroup canvasGroup; // Fade In/Out을 위한 CanvasGroup
        private Coroutine messageCoroutine;

        private void Awake()
        {
            textMessage = objectText.GetComponent<TextMeshProUGUI>();
            // text interaction 안되도록 처리
            textMessage.raycastTarget = false;
            canvasGroup = objectText.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0; // 처음엔 안 보이게 설정
            messageTypeColors = new Dictionary<MessageType, Color>
            {
                { MessageType.Normal, textColor },
                { MessageType.Warning, warningColor},
                { MessageType.Error, errorColor },
            };
        }
        /// <summary>
        /// 디폴트 SystemMessage 만들기
        /// </summary>
        /// <returns></returns>
        private SystemMessage GetDefaultSystemMessage()
        {
            return new SystemMessage(type, duration, fadeInTime, fadeOutTime, textColor, fontSize);
        }
        /// <summary>
        /// warning 메시지 보여주기
        /// </summary>
        /// <param name="message"></param>
        public void ShowMessageWarning(string message)
        {
            SystemMessage systemMessage = GetDefaultSystemMessage();
            systemMessage.Type = MessageType.Warning;
            systemMessage.TextColor = messageTypeColors[systemMessage.Type];
            ShowMessage(message, systemMessage);
        }
        /// <summary>
        /// 시스템 메시지를 표시하는 함수
        /// </summary>
        private void ShowMessage(string message, SystemMessage systemMessage)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }
            messageCoroutine = StartCoroutine(DisplayMessage(message, systemMessage));
        }

        private IEnumerator DisplayMessage(string message, SystemMessage systemMessage)
        {
            // 메시지 설정
            textMessage.text = message;
            textMessage.color = systemMessage.TextColor;
            textMessage.fontSize = systemMessage.FontSize;

            // Fade In
            yield return StartCoroutine(FadeCanvasGroup(0, 1, systemMessage.FadeInTime));

            // 유지 시간
            yield return new WaitForSeconds(systemMessage.Duration);

            // Fade Out
            yield return StartCoroutine(FadeCanvasGroup(1, 0, systemMessage.FadeOutTime));
        }

        /// <summary>
        /// CanvasGroup을 이용한 Fade In/Out 효과
        /// </summary>
        private IEnumerator FadeCanvasGroup(float startAlpha, float endAlpha, float fadeDuration)
        {
            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = endAlpha;
        }
        /// <summary>
        /// 재화 부족할때 보여주는 메시지
        /// </summary>
        /// <param name="currencyType"></param>
        public void ShowWarningCurrency(CurrencyConstants.Type currencyType)
        {
            if (currencyType == CurrencyConstants.Type.Gold)
            {
                ShowMessageWarning("골드가 부족합니다.");
            }
            else if (currencyType == CurrencyConstants.Type.Silver)
            {
                ShowMessageWarning("실버가 부족합니다.");
            }
        }
        private void OnDestroy()
        {
            
        }
    }
}
