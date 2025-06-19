using System;
using GGemCo.Scripts;
using TMPro;
using UnityEngine;

namespace GGemCo.Editor
{
    public class DefaultExporter
    {
        public TextMeshProUGUI CreateInfoCanvas(CharacterBase character) 
        {
            GameObject canvasObject = new GameObject("canvas");
            canvasObject.transform.SetParent(character.transform);
            canvasObject.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            canvasObject.transform.localPosition = Vector3.zero;
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            canvas.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.UI);

            GameObject infoObject = new GameObject("info");
            infoObject.transform.SetParent(canvas.transform);
            infoObject.transform.localPosition = Vector3.zero;
            RectTransform rectTransform = infoObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1f, 1f);
            rectTransform.anchorMin = new Vector2(0.5f,1f);
            rectTransform.anchorMax = new Vector2(0.5f,1f);
            rectTransform.pivot = new Vector2(0.5f,1f);
            TextMeshProUGUI text = infoObject.AddComponent<TextMeshProUGUI>();
            
#if UNITY_6000_0_OR_NEWER
            text.textWrappingMode = TextWrappingModes.NoWrap;
#else
            text.enableWordWrapping = false;
#endif
            text.alignment = TextAlignmentOptions.Top;
            text.fontSize = 20;
            return text;
        }
    }
}