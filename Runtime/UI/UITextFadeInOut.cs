using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class UITextFadeInOut : MonoBehaviour
    {
        public float fadeDuration = 1.0f; // 페이드 인/아웃 지속 시간
        public bool repeat; // 반복 여부 설정
        public float startAlpha; // 시작 알파값
        public float endAlpha = 1f; // 종료 알파값

        private void Awake() {
        }
        private void Start()
        {
            StartCoroutine(FadeText());
        }

        private IEnumerator FadeText()
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
            float elapsedTime = 0;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}
