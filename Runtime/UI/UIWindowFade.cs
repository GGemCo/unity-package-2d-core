using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class UIWindowFade : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private const float FadeDuration = 0.3f;
        private UIWindow uiWindow;
        private Coroutine coroutineFadeIn;
        private Coroutine coroutineFadeOut;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            uiWindow = GetComponent<UIWindow>();
        }

        private void StartFadeOut()
        {
            // 패널 비활성화 시 페이드 아웃
            coroutineFadeOut = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeIn()
        {
            float elapsedTime = 0.0f;
            canvasGroup.alpha = 0.0f;
            
            // 미리 처리해야 깔끔하게 보인다.
            uiWindow?.OnShow(true);
            
            while (elapsedTime < FadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FadeDuration);
                canvasGroup.alpha = Easing.EaseOutQuintic(t);
                yield return null;
            }
            canvasGroup.alpha = 1.0f;
        }

        private IEnumerator FadeOut()
        {
            float elapsedTime = 0.0f;
            canvasGroup.alpha = 1.0f;

            while (elapsedTime < FadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FadeDuration);
                canvasGroup.alpha = Easing.EaseInQuintic(1.0f - t);
                yield return null;
            }
            canvasGroup.alpha = 0.0f;
            uiWindow?.OnShow(false);
            // 페이드 아웃 완료 후 비활성화
            gameObject.SetActive(false);
        }
        /// <summary>
        /// window 열기
        /// </summary>
        public void ShowPanel()
        {
            if (uiWindow.gameObject.activeSelf) return;
            // 먼저 활성화 해야 fade in 이 작동함
            gameObject.SetActive(true);
            // 패널 활성화 시 페이드 인
            StartCoroutine(FadeIn());
        }
        /// <summary>
        /// window 닫기
        /// </summary>
        public void HidePanel()
        {
            if (!uiWindow.gameObject.activeSelf) return;
            // 페이드 아웃을 시작하도록 OnDisable을 호출
            StartFadeOut();
        }
    }
}