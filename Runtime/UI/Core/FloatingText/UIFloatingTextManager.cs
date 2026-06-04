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
        private readonly Queue<Image> _imagePool = new Queue<Image>();

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
            _imagePool.Clear();
            if (canvasTransform == null)
            {
                return;
            }

            GameObject prefab = LoadTextPrefab();
            if (prefab != null)
            {
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

            for (int i = 0; i < PoolSize; i++)
            {
                Image image = CreateFloatingImage();
                if (image == null)
                    continue;

                image.gameObject.SetActive(false);
                _imagePool.Enqueue(image);
            }
        }

        /// <summary>
        /// 플로팅 피드백에 사용할 UI Image 오브젝트를 생성합니다.
        /// </summary>
        /// <returns>생성된 Image 컴포넌트입니다.</returns>
        private Image CreateFloatingImage()
        {
            if (canvasTransform == null)
                return null;

            var imageObject = new GameObject("FloatingImage");
            imageObject.transform.SetParent(canvasTransform, false);
            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 플로팅 표시 요청을 텍스트 또는 이미지로 출력합니다.
        /// </summary>
        /// <param name="request">월드 위치, 색상, 표시 리소스를 포함한 플로팅 표시 요청입니다.</param>
        public virtual void ShowFloatingText(UIFloatingTextRequest request)
        {
            if (request == null)
            {
                return;
            }

            if (request.HasImageSprite())
            {
                ShowFloatingImage(request);
                return;
            }

            if (_textPool.Count == 0)
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

        /// <summary>
        /// 플로팅 이미지 요청을 이미지 풀에서 꺼내 화면에 표시합니다.
        /// </summary>
        /// <param name="request">표시할 스프라이트와 위치 정보를 포함한 요청입니다.</param>
        private void ShowFloatingImage(UIFloatingTextRequest request)
        {
            if (_imagePool.Count == 0 || request.ImageSprite == null)
                return;

            Image image = _imagePool.Dequeue();
            image.sprite = request.ImageSprite;
            image.color = request.Color;

            RectTransform rectTransform = image.rectTransform;
            if (request.ImageSize.x > 0f && request.ImageSize.y > 0f)
                rectTransform.sizeDelta = request.ImageSize;
            else
                image.SetNativeSize();

            Vector3 worldPosition = request.WorldPosition;
            float randomXRange = request.RandomXRange >= 0f ? request.RandomXRange : _defaultRandomXRange;
            worldPosition.x += Random.Range(-randomXRange, randomXRange);
            image.transform.position = worldPosition;
            image.gameObject.SetActive(true);

            StartCoroutine(AnimateFloatingImage(image, request));
        }

        private IEnumerator AnimateFloatingText(TextMeshProUGUI text, UIFloatingTextRequest request)
        {
            if (text == null)
            {
                yield break;
            }

            yield return AnimateFloatingGraphic(text, request);

            text.gameObject.SetActive(false);
            text.text = string.Empty;
            _textPool.Enqueue(text);
        }

        /// <summary>
        /// 플로팅 이미지를 이동/페이드 처리한 뒤 이미지 풀에 반환합니다.
        /// </summary>
        /// <param name="image">표시 중인 플로팅 이미지입니다.</param>
        /// <param name="request">애니메이션 옵션을 포함한 표시 요청입니다.</param>
        private IEnumerator AnimateFloatingImage(Image image, UIFloatingTextRequest request)
        {
            if (image == null)
            {
                yield break;
            }

            yield return AnimateFloatingGraphic(image, request);

            image.sprite = null;
            image.gameObject.SetActive(false);
            _imagePool.Enqueue(image);
        }

        /// <summary>
        /// 텍스트와 이미지가 공유하는 플로팅 이동/페이드 애니메이션을 실행합니다.
        /// </summary>
        /// <param name="graphic">애니메이션을 적용할 UI Graphic입니다.</param>
        /// <param name="request">이동 거리, 시간, 이징 옵션을 포함한 표시 요청입니다.</param>
        private IEnumerator AnimateFloatingGraphic(Graphic graphic, UIFloatingTextRequest request)
        {
            if (graphic == null)
            {
                yield break;
            }

            Vector3 startPos = graphic.transform.position;
            float moveDistance = request.MoveUpDistance > 0f ? request.MoveUpDistance : _defaultMoveUpDistance;
            Vector3 endPos = startPos + new Vector3(0f, moveDistance, 0f);
            float moveUpTime = request.MoveUpTime > 0f ? request.MoveUpTime : _defaultMoveUpTime;
            float fadeOutTime = request.FadeOutTime > 0f ? request.FadeOutTime : _defaultFadeOutTime;
            Easing.EaseType easeType = request.EaseType ?? _defaultEaseType;
            Color originalColor = graphic.color;

            float elapsedTime = 0f;
            while (elapsedTime < moveUpTime)
            {
                float t = Mathf.Clamp01(elapsedTime / Mathf.Max(0.0001f, moveUpTime));
                float easedT = Easing.Apply(t, easeType);

                graphic.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                graphic.color = new Color(originalColor.r, originalColor.g, originalColor.b,
                    1f - (elapsedTime / Mathf.Max(0.0001f, fadeOutTime)));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            graphic.transform.position = startPos;
            graphic.color = originalColor;
        }
    }
}
