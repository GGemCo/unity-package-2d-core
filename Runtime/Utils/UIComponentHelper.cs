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

            // TMP
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null) return;

            // Localization 세팅이 있으면 LocalizeStringEvent로 바인딩
            if (!string.IsNullOrEmpty(localizationTable) && !string.IsNullOrEmpty(localizationKey))
            {
                var localizeEvent = tmp.gameObject.GetComponent<LocalizeStringEvent>();
                if (localizeEvent == null)
                    localizeEvent = tmp.gameObject.AddComponent<LocalizeStringEvent>();

                // 테이블/키 지정
                localizeEvent.StringReference.TableReference = localizationTable;
                localizeEvent.StringReference.TableEntryReference = localizationKey;

                // 중복 리스너 방지 후 런타임 리스너로 바인딩 (Editor API 불필요)
                // 필요 시 RemoveAllListeners 대신 특정 메서드만 제거할 수도 있음.
                localizeEvent.OnUpdateString.RemoveAllListeners();
                localizeEvent.OnUpdateString.AddListener(tmp.SetText);

                // 즉시 갱신
                localizeEvent.RefreshString();
                return;
            }

            // 로컬라이즈 미사용 시 기본 텍스트 설정
            tmp.SetText(text);
        }
    }
}