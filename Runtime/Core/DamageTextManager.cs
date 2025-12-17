using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    public class MetadataDamageText
    {
        public Vector3 WorldPosition;
        public float Damage;
        public Color Color;
        // damage 숫자 대신 텍스트를 사용해야 할때
        public string SpecialDamageText = "";
        public int FontSize = 0;
    }
    /// <summary>
    /// 데미지 텍스트 매니저
    /// </summary>
    public class DamageTextManager : MonoBehaviour
    {
        private Transform canvasTransform;
        private const int PoolSize = 20;
        private Easing.EaseType easeType;
        private float moveUpTime = 0.3f;
        private float moveUpDistance = 50.0f; // 추가된 이동 거리 설정
        private float fadeOutTime = 0.1f;
        private float randomXRange = 10.0f; // X 좌표 랜덤 범위 추가
        private GGemCoSettings _settings;
        
        private readonly Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private void Awake()
        {
            _settings = AddressableLoaderSettings.Instance.settings;
            CreateTextDamageCanvas();
            InitializePool();
            InitializeInfos();
        }

        private void InitializeInfos()
        {
            if (!AddressableLoaderSettings.Instance) return;
            var settings = AddressableLoaderSettings.Instance.settings;
            easeType = settings.damageTextEasingType;
            moveUpTime = settings.damageTextMoveUpTime;
            moveUpDistance = settings.damageTextMoveUpDistance;
            fadeOutTime = settings.damageTextFadeOutTime;
            randomXRange = settings.damageTextRandomXRange;
        }
        /// <summary>
        /// 데미지 텍스트가 들어갈 canvas 만들기
        /// </summary>
        private void CreateTextDamageCanvas()
        {
            GameObject gameObjectCanvas = new GameObject("CanvasTextDamage");
            Canvas canvas = gameObjectCanvas.gameObject.AddComponent<Canvas>();
            gameObjectCanvas.gameObject.AddComponent<CanvasScaler>();
            gameObjectCanvas.gameObject.AddComponent<GraphicRaycaster>();
            
            canvas.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.UI);
            canvas.sortingOrder = 999;
            canvas.renderMode = _settings.damageTextCanvasRenderMode;

            canvasTransform = gameObjectCanvas.transform;
        }
        /// <summary>
        /// Addressable 에 등록된 damageText 를 불러와서 pool 을 만든다 
        /// </summary>
        private void InitializePool()
        {
            if (AddressableLoaderSettings.Instance == null) return;
            textPool.Clear();
            GameObject textFloatingDamage = ConfigResources.TextDamage.Load();
            if (textFloatingDamage == null) return;
            for (int i = 0; i < PoolSize; i++)
            {
                GameObject gameObjectText = Instantiate(textFloatingDamage, canvasTransform);
                TextMeshProUGUI text = gameObjectText.GetComponent<TextMeshProUGUI>();
                text.gameObject.SetActive(false);
                textPool.Enqueue(text);
            }
        }
        /// <summary>
        /// 데미지 텍스트 보여주기
        /// </summary>
        /// <param name="metadataDamageText"></param>
        public void ShowDamageText(MetadataDamageText metadataDamageText)
        {
            if (textPool.Count == 0)
                return;

            TextMeshProUGUI text = textPool.Dequeue();
            text.text = $"{metadataDamageText.Damage}";
            if (_settings.useDamageTextMinus)
            {
                text.text = $"-{metadataDamageText.Damage}";
            }
            if (!string.IsNullOrEmpty(metadataDamageText.SpecialDamageText))
            {
                text.text = metadataDamageText.SpecialDamageText;
            }
            text.color = metadataDamageText.Color;
            text.fontSize = _settings.damageTextFontSize > 0 ? _settings.damageTextFontSize : 24f;
            if (metadataDamageText.FontSize > 0)
            {
                text.fontSize = metadataDamageText.FontSize;
            }

            // X 좌표를 -10 ~ +10 범위에서 랜덤 설정
            metadataDamageText.WorldPosition.x += Random.Range(-randomXRange, randomXRange);
        
            text.transform.position = metadataDamageText.WorldPosition;
            
            text.gameObject.SetActive(true);

            StartCoroutine(AnimateDamageText(text));
        }
        /// <summary>
        /// 데미지 floating 애니메이션
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private IEnumerator AnimateDamageText(TextMeshProUGUI text)
        {
            if (text == null) yield break;
            
            Vector3 startPos = text.transform.position;
            Vector3 endPos = startPos + new Vector3(0, moveUpDistance, 0); // 이동 거리 적
            float elapsedTime = 0f;
            Color originalColor = text.color;

            // Move Up
            while (elapsedTime < moveUpTime)
            {
                float t = Mathf.Clamp01(elapsedTime / moveUpTime);
                float easedT = Easing.Apply(t, easeType);
                
                text.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Fade Out
            elapsedTime = 0f;
            while (elapsedTime < fadeOutTime)
            {
                text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1 - (elapsedTime / fadeOutTime));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            text.gameObject.SetActive(false);
            text.color = originalColor;
            textPool.Enqueue(text);
        }
        private void OnDestroy()
        {
            
        }
    }
}
