using System.Collections;
using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class UIFloatingText : MonoBehaviour
    {
        private TextMeshProUGUI floatingText; // UI Text element
        public float moveDuration = 1.0f; // Time to move up
        public float fadeDuration = 0.5f; // Time to fade out
        public int moveY = 20;

        private Vector3 initialPosition;
        private Color initialColor;

        private void Awake()
        {
            floatingText = GetComponent<TextMeshProUGUI>();
        }
        void Start()
        {
            if (floatingText.text == "0")
            {
                Destroy(gameObject);
                return;
            }
            initialPosition = floatingText.transform.position;
            initialColor = floatingText.color;
            StartCoroutine(AnimateText());
        }

        public void SetFontSize(float size)
        {
            floatingText.fontSize = size;
        }
        public void SetColor(Color color)
        {
            floatingText.color = color;
        }
        public void SetText(string text)
        {
            floatingText.text = text;
        }

        private IEnumerator AnimateText()
        {
            float elapsedTime = 0f;

            // Move text upwards
            while (elapsedTime < moveDuration)
            {
                floatingText.transform.position = Vector3.Lerp(initialPosition, initialPosition + Vector3.up * moveY, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            floatingText.transform.position = initialPosition + Vector3.up * moveY;

            elapsedTime = 0f;
            Color startColor = floatingText.color;

            // Fade out text
            while (elapsedTime < fadeDuration)
            {
                floatingText.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), elapsedTime / fadeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            floatingText.color = new Color(startColor.r, startColor.g, startColor.b, 0);

            // Optionally, destroy the GameObject after animation
            Destroy(gameObject);
        }
    }
}