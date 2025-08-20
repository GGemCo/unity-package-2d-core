using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public class MetaDataToggle
    {
        public readonly GameObject prefab;
        public readonly string text;
        public readonly string localizationTable;
        public readonly string localizationKey;

        public MetaDataToggle(GameObject prefab, string text = "", string localizationTable = "", string localizationKey = "")
        {
            this.prefab = prefab;
            this.text = text;
            this.localizationTable = localizationTable;
            this.localizationKey = localizationKey;
        }
    }
    public static class UIComponentHelper
    {
        public static GameObject CreateButton(GameObject prefab, string text)
        {
            var go = Object.Instantiate(prefab);
            if (!go) return null;
            SetButtonLabel(go, text);
            return go;
        }
        public static Toggle CreateToggle(MetaDataToggle metaDataToggle)
        {
            GameObject prefab = metaDataToggle.prefab;
            string text = metaDataToggle.text;
            string localizationTable = metaDataToggle.localizationTable;
            string localizationKey = metaDataToggle.localizationKey;
            
            var go = Object.Instantiate(prefab);
            if (!go) return null;
            SetButtonLabel(go.gameObject, text, localizationTable, localizationKey);
            return go.GetComponent<Toggle>();
        }
        // 버튼 라벨 텍스트 설정 (uGUI Text 우선, 없으면 TMP가정 분기)
        private static void SetButtonLabel(GameObject go, string text, string localizationTable = "", string localizationKey = "")
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
            TextMeshProUGUI objectText = go.GetComponentInChildren<TextMeshProUGUI>();
            if (objectText == null) return;
            if (!string.IsNullOrEmpty(localizationTable) && !string.IsNullOrEmpty(localizationKey))
            {
                LocalizeStringEvent localizeEvent = objectText.gameObject.GetComponent<LocalizeStringEvent>();
                if (localizeEvent == null)
                {
                    localizeEvent = objectText.gameObject.AddComponent<LocalizeStringEvent>();
                }

                if (localizeEvent != null)
                {
                    // 테이블 및 키 설정
                    localizeEvent.SetTable(localizationTable);
                    localizeEvent.SetEntry(localizationKey);
                }
                
#if UNITY_6000_0_OR_NEWER
                // Update String 에 추가하기
                UnityEditor.Events.UnityEventTools.AddPersistentListener(localizeEvent.OnUpdateString,
                    objectText.SetText);
#else
            var proxy = objectText.gameObject.GetComponent<LocalizationTextProxy>();
            if (proxy == null)
            {
                proxy = objectText.gameObject.AddComponent<LocalizationTextProxy>();
                proxy.target = objectText;
            }
            // Update String 에 추가하기
            UnityEditor.Events.UnityEventTools.AddPersistentListener(localizeEvent.OnUpdateString, proxy.SetText);
#endif
                // EditorAndRuntime 모드로 작동되도록 설정
                for (var i = 0; i < localizeEvent.OnUpdateString.GetPersistentEventCount(); i++)
                {
                    localizeEvent.OnUpdateString.SetPersistentListenerState(i, UnityEventCallState.EditorAndRuntime);
                }
                    
                localizeEvent.RefreshString();
            }

            objectText.SetText(text);
            // var textProp = tmpType.GetProperty("text");
            // textProp?.SetValue(tmp, text);
            //
            // var forceMethod = tmpType.GetMethod("ForceMeshUpdate", Type.EmptyTypes);
            // forceMethod?.Invoke(tmp, null); // 즉시 반영
        }
    }
}