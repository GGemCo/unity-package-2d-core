using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 범용 플로팅 텍스트 매니저입니다.
    /// </summary>
    public class UIFloatingTextManager : MonoBehaviour
    {
        private const int PoolSize = 20;

        protected Transform canvasTransform;
        protected GGemCoSettings settings;

        private Easing.EaseType _defaultEaseType;
        private float _defaultMoveUpTime = 0.3f;
        private float _defaultMoveUpDistance = 50f;
        private float _defaultFadeOutTime = 0.1f;
        private float _defaultRandomXRange = 10f;

        private readonly Queue<TextMeshProUGUI> _textPool = new Queue<TextMeshProUGUI>();

        protected virtual void Awake()
        {
            InitializeManager();
        }

        protected void InitializeManager()
        {
            settings = AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.settings : null;
            InitializeDefaults();
            CreateFloatingTextCanvas();
            InitializePool();
        }

        private void InitializeDefaults()
        {
            if (settings == null)
            {
                _defaultEaseType = Easing.EaseType.EaseOutCubic;
                return;
            }

            _defaultEaseType = settings.damageTextEasingType;
            _defaultMoveUpTime = settings.damageTextMoveUpTime;
            _defaultMoveUpDistance = settings.damageTextMoveUpDistance;
            _defaultFadeOutTime = settings.damageTextFadeOutTime;
            _defaultRandomXRange = settings.damageTextRandomXRange;
        }

        protected virtual string GetCanvasName() => "CanvasFloatingText";

        protected virtual GameObject LoadTextPrefab()
        {
            return ConfigResources.TextDamage.Load();
        }

        private void CreateFloatingTextCanvas()
        {
            GameObject gameObjectCanvas = new GameObject(GetCanvasName());
            Canvas canvas = gameObjectCanvas.AddComponent<Canvas>();
            gameObjectCanvas.AddComponent<CanvasScaler>();
            gameObjectCanvas.AddComponent<GraphicRaycaster>();

            canvas.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.UI);
            canvas.sortingOrder = 999;
            canvas.renderMode = settings != null ? settings.damageTextCanvasRenderMode : RenderMode.ScreenSpaceOverlay;

            canvasTransform = gameObjectCanvas.transform;
        }

        private void InitializePool()
        {
            _textPool.Clear();
            GameObject prefab = LoadTextPrefab();
            if (prefab == null || canvasTransform == null)
            {
                return;
            }

            for (int i = 0; i < PoolSize; i++)
            {
                GameObject gameObjectText = Instantiate(prefab, canvasTransform);
                TextMeshProUGUI text = gameObjectText.GetComponent<TextMeshProUGUI>();
                if (text == null)
                {
                    Destroy(gameObjectText);
                    continue;
                }

                text.gameObject.SetActive(false);
                _textPool.Enqueue(text);
            }
        }

        public virtual void ShowFloatingText(UIFloatingTextRequest request)
        {
            if (request == null || _textPool.Count == 0)
            {
                return;
            }

            TextMeshProUGUI text = _textPool.Dequeue();
            string displayText = request.ResolveDisplayText();
            if (string.IsNullOrEmpty(displayText))
            {
                _textPool.Enqueue(text);
                return;
            }

            text.text = displayText;
            text.color = request.Color;
            text.fontSize = request.FontSize > 0
                ? request.FontSize
                : settings != null && settings.damageTextFontSize > 0
                    ? settings.damageTextFontSize
                    : 24f;

            Vector3 worldPosition = request.WorldPosition;
            float randomXRange = request.RandomXRange >= 0f ? request.RandomXRange : _defaultRandomXRange;
            worldPosition.x += Random.Range(-randomXRange, randomXRange);
            text.transform.position = worldPosition;
            text.gameObject.SetActive(true);

            StartCoroutine(AnimateFloatingText(text, request));
        }

        private IEnumerator AnimateFloatingText(TextMeshProUGUI text, UIFloatingTextRequest request)
        {
            if (text == null)
            {
                yield break;
            }

            Vector3 startPos = text.transform.position;
            float moveDistance = request.MoveUpDistance > 0f ? request.MoveUpDistance : _defaultMoveUpDistance;
            Vector3 endPos = startPos + new Vector3(0f, moveDistance, 0f);
            float moveUpTime = request.MoveUpTime > 0f ? request.MoveUpTime : _defaultMoveUpTime;
            float fadeOutTime = request.FadeOutTime > 0f ? request.FadeOutTime : _defaultFadeOutTime;
            Easing.EaseType easeType = request.EaseType ?? _defaultEaseType;
            Color originalColor = text.color;

            float elapsedTime = 0f;
            while (elapsedTime < moveUpTime)
            {
                float t = Mathf.Clamp01(elapsedTime / Mathf.Max(0.0001f, moveUpTime));
                float easedT = Easing.Apply(t, easeType);

                text.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b,
                    1f - (elapsedTime / Mathf.Max(0.0001f, fadeOutTime)));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            text.gameObject.SetActive(false);
            text.transform.position = startPos;
            text.color = originalColor;
            _textPool.Enqueue(text);
        }
    }
}
