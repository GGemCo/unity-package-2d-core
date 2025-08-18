using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public static class UIComponentHelper
    {
        public static GameObject CreateButton(GameObject prefab, string text)
        {
            var go = Object.Instantiate(prefab);
            if (!go) return null;
            SetButtonLabel(go, text);
            return go;
        }
        public static Toggle CreateToggle(Toggle prefab, string text)
        {
            var go = Object.Instantiate(prefab);
            if (!go) return null;
            SetButtonLabel(go.gameObject, text);
            return go;
        }
        // 버튼 라벨 텍스트 설정 (uGUI Text 우선, 없으면 TMP가정 분기)
        private static void SetButtonLabel(GameObject go, string text)
        {
            // Legacy uGUI Text
            var legacy = go.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = text;
                return;
            }

            // TextMesh Pro (패키지 사용 시)
            // 패키지 의존성을 안전하게 처리하려면 try-catch + Reflection 혹은
            // Scripting Define Symbols(TMP_PRESENT 등)을 사용하는 방법을 추천합니다.
            var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType == null) return;
            var tmp = go.GetComponentInChildren(tmpType, true);
            if (tmp == null) return;
            var textProp = tmpType.GetProperty("text");
            textProp?.SetValue(tmp, text);
            var forceMethod = tmpType.GetMethod("ForceMeshUpdate", Type.EmptyTypes);
            forceMethod?.Invoke(tmp, null); // 즉시 반영
        }
    }
}