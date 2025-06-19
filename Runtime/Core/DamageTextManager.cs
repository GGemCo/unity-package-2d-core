using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace GGemCo.Scripts
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
        [SerializeField] private int poolSize = 20;
        [SerializeField] private float moveUpTime = 0.3f;
        [SerializeField] private float fadeOutTime = 0.1f;
        [SerializeField] private float moveUpDistance = 50.0f; // 추가된 이동 거리 설정
        [SerializeField] private float randomXRange = 10.0f; // X 좌표 랜덤 범위 추가

        private readonly Queue<TextMeshProUGUI> textPool = new Queue<TextMeshProUGUI>();
        private void Awake()
        {
            CreateTextDamageCanvas();
            InitializePool();
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
            canvas.renderMode = RenderMode.WorldSpace;
            
            canvasTransform = gameObjectCanvas.transform;
        }
        /// <summary>
        /// Addressable 에 등록된 damageText 를 불러와서 pool 을 만든다 
        /// </summary>
        private void InitializePool()
        {
            if (AddressableSettingsLoader.Instance == null) return;
            textPool.Clear();
            if (poolSize <= 0) return;
            GameObject textFloatingDamage = AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyTextFloatingDamage);
            if (textFloatingDamage == null) return;
            for (int i = 0; i < poolSize; i++)
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
            if (!string.IsNullOrEmpty(metadataDamageText.SpecialDamageText))
            {
                text.text = metadataDamageText.SpecialDamageText;
            }
            text.color = metadataDamageText.Color;
            text.fontSize = 24;
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
                text.transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / moveUpTime);
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
