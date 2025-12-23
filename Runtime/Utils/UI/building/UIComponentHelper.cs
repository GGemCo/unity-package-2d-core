using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// 프리팹 기반 uGUI 요소 생성 유틸.
    /// - "생성"만 담당하고, 레이아웃/스타일 정책은 프리팹에서 관리하는 것을 권장합니다.
    /// </summary>
    public static class UIComponentHelper
    {
        public sealed class MetaDataToggle
        {
            public GameObject Prefab { get; }
            public string Text { get; }
            public string LocalizationTable { get; }
            public string LocalizationKey { get; }

            public MetaDataToggle(GameObject prefab, string text = "", string localizationTable = "", string localizationKey = "")
            {
                Prefab = prefab;
                Text = text ?? string.Empty;
                LocalizationTable = localizationTable ?? string.Empty;
                LocalizationKey = localizationKey ?? string.Empty;
            }
        }

        public static GameObject CreateButton(GameObject prefab, string text, Transform parent = null, bool worldPositionStays = false)
        {
            if (!prefab) return null;

            var go = Object.Instantiate(prefab, parent, worldPositionStays);
            if (!go) return null;

            SetLabel(go, text);
            return go;
        }

        public static Toggle CreateToggle(MetaDataToggle meta, Transform parent = null, bool worldPositionStays = false)
        {
            if (meta == null || !meta.Prefab) return null;

            var go = Object.Instantiate(meta.Prefab, parent, worldPositionStays);
            if (!go) return null;

            SetLabel(go, meta.Text, meta.LocalizationTable, meta.LocalizationKey);
            return go.GetComponent<Toggle>();
        }

        public static Button BindButtonClick(GameObject go, UnityAction onClick, bool removeAllListeners = true)
        {
            if (!go) return null;

            var button = go.GetComponent<Button>();
            if (!button) return null;

            if (removeAllListeners) button.onClick.RemoveAllListeners();
            if (onClick != null) button.onClick.AddListener(onClick);

            return button;
        }

        private static void SetLabel(GameObject root, string text, string localizationTable = "", string localizationKey = "")
        {
            if (!root) return;

            // Legacy uGUI Text 우선
            var legacy = root.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = text ?? string.Empty;
                return;
            }

            // TMP
            var tmp = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null) return;

            // Localization 사용 시 LocalizeStringEvent 바인딩
            if (!string.IsNullOrEmpty(localizationTable) && !string.IsNullOrEmpty(localizationKey))
            {
                var localizeEvent = tmp.GetComponent<LocalizeStringEvent>();
                if (localizeEvent == null) localizeEvent = tmp.gameObject.AddComponent<LocalizeStringEvent>();

                localizeEvent.StringReference.TableReference = localizationTable;
                localizeEvent.StringReference.TableEntryReference = localizationKey;

                // 런타임 리스너로 바인딩
                localizeEvent.OnUpdateString.RemoveAllListeners();
#if UNITY_6000_0_OR_NEWER
                localizeEvent.OnUpdateString.AddListener(tmp.SetText);
#else
                var localizationTextProxy = tmp.GetComponent<LocalizationTextProxy>();
                if(localizationTextProxy == null) localizationTextProxy = tmp.gameObject.AddComponent<LocalizationTextProxy>();
                localizeEvent.OnUpdateString.AddListener(localizationTextProxy.SetText);
#endif

                localizeEvent.RefreshString();
                return;
            }

            tmp.SetText(text ?? string.Empty);
        }
    }
}
