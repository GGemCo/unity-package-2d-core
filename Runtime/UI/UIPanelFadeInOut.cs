using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GGemCo.Scripts
{
    public class UIPanelFadeInOut : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        public float fadeDuration = 1.0f; // 페이드 인/아웃 지속 시간
        public bool repeat; // 반복 여부 설정
        public float startAlpha; // 시작 알파값
        public float endAlpha = 1f; // 종료 알파값
        public UnityEvent onFadeInStart; // 페이드 인 시작할때
        public UnityEvent onFadeOutEnd; // 페이드 아웃 끝났을때
        private bool isFading;

        private void Awake() {
            canvasGroup = GetComponent<CanvasGroup>();
            isFading = false;
        }
        private void Start()
        {
            if (isFading) return;
            StartCoroutine(FadeCanvasGroup());
        }

        private void OnEnable()
        {
            if (isFading) return;
            StartCoroutine(FadeCanvasGroup());
        }
        private void OnDisable()
        {
            if (!isFading) return;
            isFading = false;
            StopCoroutine(FadeCanvasGroup());
        }

        private IEnumerator FadeCanvasGroup()
        {
            while (true)
            {
                // 페이드 인
                yield return StartCoroutine(Fade(startAlpha, endAlpha));

                if (!repeat)
                    yield break;

                // 페이드 아웃
                yield return StartCoroutine(Fade(endAlpha, startAlpha));
            }
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha)
        {
            isFading = true;
            float elapsedTime = 0;
            canvasGroup.alpha = fromAlpha;
            if (fromAlpha == 0)
            {
                onFadeInStart?.Invoke();
            }

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
            if (toAlpha == 0)
            {
                onFadeOutEnd?.Invoke();
            }
        }
    }
}