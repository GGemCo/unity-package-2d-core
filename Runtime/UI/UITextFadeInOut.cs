using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    public class UITextFadeInOut : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("페이드 인/아웃 지속 시간 (초)")]
        public float fadeDuration = 1.0f;

        [Tooltip("페이드 인/아웃 반복 여부")]
        public bool repeat;

        [Tooltip("시작 알파값 (0 ~ 1)")]
        [Range(0f, 1f)] public float startAlpha = 0f;

        [Tooltip("종료 알파값 (0 ~ 1)")]
        [Range(0f, 1f)] public float endAlpha = 1f;

        [Tooltip("알파 변화 이징 방식")]
        public Easing.EaseType easeType = Easing.EaseType.Linear;

        private TextMeshProUGUI _textMesh;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
            if (!_textMesh)
            {
                GcLogger.LogError($"UITextFadeInOut: TextMeshProUGUI 컴포넌트가 없습니다.");
                enabled = false;
            }
        }

        private void Start()
        {
            if (_textMesh != null)
                _textMesh.alpha = startAlpha;

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
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / fadeDuration);

                if (_textMesh != null)
                {
                    float easedT = Easing.Apply(t, easeType);
                    _textMesh.alpha = Mathf.Lerp(fromAlpha, toAlpha, easedT);
                }

                yield return null;
            }

            if (_textMesh != null)
                _textMesh.alpha = toAlpha;
        }
    }
}
