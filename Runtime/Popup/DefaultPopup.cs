using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 팝업 메타 데이터
    /// </summary>
    public class PopupMetadata
    {
        // 타입
        public PopupManager.Type PopupType = PopupManager.Type.Default;
        // 타이틀
        public string Title;
        // 메시지
        public string Message;
        // 메시지 색상
        public Color MessageColor = Color.gray;
        // 확인 버튼 보임/안보임
        public bool ShowConfirmButton = true;
        // 취소 버튼 보임/안보임
        public bool ShowCancelButton = false;
        // 확인 버튼 콜백 함수
        public System.Action OnConfirm;
        // 취소 버튼 콜백 함수
        public System.Action OnCancel;
        // 강제로 팝업창을 띄울 것인지
        public bool ForceShow = false;
        // 마우스 클릭했을때도 닫히게 할 것인지 
        public bool IsClosableByClick = true;
    }
    
    /// <summary>
    /// 디폴트 팝업창
    /// </summary>
    public class DefaultPopup : MonoBehaviour, IPointerClickHandler
    {
        protected PopupManager.Type PopupType;
        [Header("기본오브젝트")]
        [Tooltip("타이틀")]
        public TextMeshProUGUI textTitle;
        [Tooltip("메시지")]
        public TextMeshProUGUI textMessage;
        [Tooltip("확인 버튼")]
        public Button buttonConfirm;
        [Tooltip("취소 버튼")]
        public Button buttonCancel;
        [Tooltip("내용이 들어가는 Panel")]
        public RectTransform panelContent;
        [Tooltip("팝업창이 보여질때 Fade in/out 시간(초)")]
        public float fadeDuration;
        // 페이드 인/아웃을 위한 CanvasGroup
        private CanvasGroup canvasGroup;
        // 마우스 클릭했을때도 닫히게 할 것인지 
        private bool isClosableByClick;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="popupMetadata"></param>
        public virtual void Initialize(PopupMetadata popupMetadata)
        {
            PopupType = popupMetadata.PopupType;
            isClosableByClick = popupMetadata.IsClosableByClick;
            
            SetupTitle(popupMetadata.Title);
            SetupMessage(popupMetadata.Message, popupMetadata.MessageColor);
            SetupButtons(popupMetadata);

            // 레이아웃 업데이트
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelContent);
        }
        /// <summary>
        /// 타이틀 셋팅하기
        /// </summary>
        /// <param name="title"></param>
        private void SetupTitle(string title)
        {
            if (!string.IsNullOrEmpty(title) && textTitle != null)
            {
                textTitle.text = title;
            }
        }
        /// <summary>
        /// 메시지 셋팅하기
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        private void SetupMessage(string message, Color color)
        {
            if (textMessage == null) return;

            if (!string.IsNullOrEmpty(message))
            {
                textMessage.gameObject.SetActive(true);
                textMessage.text = message;
                textMessage.color = color;
            }
            else
            {
                textMessage.gameObject.SetActive(false);
            }
        }
        /// <summary>
        /// 확인, 취소 버튼 셋팅하기
        /// </summary>
        /// <param name="popupMetadata"></param>
        private void SetupButtons(PopupMetadata popupMetadata)
        {
            popupMetadata.OnConfirm ??= ClosePopup;
            popupMetadata.OnCancel ??= ClosePopup;

            SetupButton(buttonConfirm, popupMetadata.ShowConfirmButton, popupMetadata.OnConfirm, "Confirm 버튼이 없습니다.");
            SetupButton(buttonCancel, popupMetadata.ShowCancelButton, popupMetadata.OnCancel, "Cancel 버튼이 없습니다.");
        }
        /// <summary>
        /// 버튼 셋팅하기
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isActive"></param>
        /// <param name="callback"></param>
        /// <param name="errorMessage"></param>
        private void SetupButton(Button button, bool isActive, System.Action callback, string errorMessage)
        {
            if (button == null)
            {
                if (isActive)
                {
                    GcLogger.LogError(errorMessage);
                }
                return;
            }

            button.gameObject.SetActive(isActive);

            if (isActive)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(callback.Invoke);
                button.onClick.AddListener(ClosePopup);
            }
        }
        /// <summary>
        /// 팝업창 띄우기
        /// </summary>
        public void ShowPopup()
        {
            StartCoroutine(FadeCoroutine(0f, 1f, OnFadeInStart, OnFadeInEnd));
        }
        /// <summary>
        /// 팝업창 닫기
        /// </summary>
        public virtual void ClosePopup()
        {
            StartCoroutine(FadeCoroutine(1f, 0f, OnFadeOutDestroyStart, OnFadeOutDestroyEnd));
        }
        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, System.Action onStart, System.Action onEnd)
        {
            float elapsedTime = 0f;
            canvasGroup.alpha = startAlpha;

            onStart?.Invoke();

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, Easing.EaseOutQuintic(t));
                yield return null;
            }

            onEnd?.Invoke();
        }
        protected virtual void OnFadeInStart() { }
        protected virtual void OnFadeInEnd() => canvasGroup.alpha = 1.0f;

        protected virtual void OnFadeOutDestroyStart() { }
        protected virtual void OnFadeOutDestroyEnd()
        {
            Destroy(gameObject);
        }
        /// <summary>
        /// 마우스 클릭했을때 처리 
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (isClosableByClick)
            {
                ClosePopup();
            }
        }
        /// <summary>
        /// 비활성화 되면 버튼 리스너 삭제하기
        /// </summary>
        private void OnDisable()
        {
            RemoveButtonListeners(buttonConfirm);
            RemoveButtonListeners(buttonCancel);
        }

        private void RemoveButtonListeners(Button button)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(ClosePopup);
            }
        }
    }
}