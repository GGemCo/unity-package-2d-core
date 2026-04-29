using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
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
        public float fadeDuration = 0.2f;
        [Header("UI Effect")]
        [SerializeField] private UIEffectTarget effectTarget;
        [SerializeField] private UIEffectPreset popupOpenPreset;
        [SerializeField] private UIEffectPreset popupClosePreset;

        private CanvasGroup _canvasGroup;
        private bool _isClosableByClick;
        private Coroutine _fallbackFadeCoroutine;
        private bool _isClosing;

        public event Action<DefaultPopup> Closed;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (effectTarget == null)
                effectTarget = UIEffectTarget.GetOrAdd(gameObject);
            else
                effectTarget.AutoBind();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize(PopupMetadata popupMetadata)
        {
            PopupType = popupMetadata.PopupType;
            _isClosableByClick = popupMetadata.IsClosableByClick;

            SetupTitle(popupMetadata.Title);
            SetupMessage(popupMetadata.Message, popupMetadata.MessageColor);
            SetupButtons(popupMetadata);

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelContent);
            OnInitialize(popupMetadata);
        }

        protected virtual void OnInitialize(PopupMetadata popupMetadata)
        {
            
        }
        
        private void SetupTitle(string title)
        {
            if (textTitle == null) return;
            if (string.IsNullOrEmpty(title)) return;
            
            string localeTitle = LocalizationManager.Instance.GetSystemByKey(title);
            if (!string.IsNullOrEmpty(localeTitle))
            {
                textTitle.text = localeTitle;
            }
            else if (!string.IsNullOrEmpty(title) && textTitle != null)
            {
                textTitle.text = title;
            }
        }

        private void SetupMessage(string message, Color color)
        {
            if (textMessage == null) return;
            if (string.IsNullOrEmpty(message)) return;

            string localeMessage = LocalizationManager.Instance.GetSystemByKey(message);
            if (!string.IsNullOrEmpty(localeMessage))
            {
                textMessage.gameObject.SetActive(true);
                textMessage.text = localeMessage;
                textMessage.color = color;
            }
            else if (!string.IsNullOrEmpty(message))
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

        private void SetupButtons(PopupMetadata popupMetadata)
        {
            popupMetadata.OnConfirm ??= delegate { };
            popupMetadata.OnCancel ??= delegate { };

            SetupButton(buttonConfirm, popupMetadata.ShowConfirmButton, popupMetadata.OnConfirm, "Confirm 버튼이 없습니다.");
            SetupButton(buttonCancel, popupMetadata.ShowCancelButton, popupMetadata.OnCancel, "Cancel 버튼이 없습니다.");
        }

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
            button.onClick.RemoveAllListeners();

            if (!isActive) return;

            button.onClick.AddListener(() => callback?.Invoke());
            button.onClick.AddListener(ClosePopup);
        }

        /// <summary>
        /// 팝업창 띄우기
        /// </summary>
        public void ShowPopup()
        {
            _isClosing = false;
            PlayPresetOrFallback(popupOpenPreset, 0f, 1f, OnFadeInEnd);
        }

        /// <summary>
        /// 팝업창 닫기
        /// </summary>
        public void ClosePopup()
        {
            if (_isClosing) return;
            _isClosing = true;
            PlayPresetOrFallback(popupClosePreset, 1f, 0f, CompleteClose);
        }

        private void PlayPresetOrFallback(UIEffectPreset preset, float fallbackStartAlpha, float fallbackEndAlpha, Action onComplete)
        {
            if (_fallbackFadeCoroutine != null)
            {
                StopCoroutine(_fallbackFadeCoroutine);
                _fallbackFadeCoroutine = null;
            }

            if (preset != null && effectTarget != null)
            {
                UIEffectService.Play(this, effectTarget, preset, onComplete: () => onComplete?.Invoke());
                return;
            }

            _fallbackFadeCoroutine = StartCoroutine(FadeCoroutine(fallbackStartAlpha, fallbackEndAlpha, onComplete));
        }

        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, Action onEnd)
        {
            float duration = Mathf.Max(0.0001f, fadeDuration);
            float elapsedTime = 0f;
            _canvasGroup.alpha = startAlpha;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, Easing.EaseOutQuintic(t));
                yield return null;
            }

            _canvasGroup.alpha = endAlpha;
            _fallbackFadeCoroutine = null;
            onEnd?.Invoke();
        }

        private void OnFadeInEnd()
        {
            _canvasGroup.alpha = 1f;
        }

        private void CompleteClose()
        {
            if (!this) return;
            Closed?.Invoke(this);
            Destroy(gameObject);
        }
        /// <summary>
        /// 마우스 클릭했을때 처리 
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isClosableByClick)
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
                button.onClick.RemoveAllListeners();
            }
        }
    }
}
