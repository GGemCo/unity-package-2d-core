using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    public class MetaDataButton
    {
        public readonly string filedName;
        public readonly string text;
        public readonly string localizationTable;
        public readonly string localizationKey;

        public MetaDataButton(string filedName, string text = "", string localizationTable = "", string localizationKey = "")
        {
            this.filedName = filedName;
            this.text = text;
            this.localizationTable = localizationTable;
            this.localizationKey = localizationKey;
        }
    }
    public class MetaDataTextMeshProGUI
    {
        public readonly Vector2 pivot;
        public readonly Vector2 position;
        public readonly AnchorPresets anchorPresets;
        public readonly float width;
        public readonly float height;
        public readonly float fontSize;
        public readonly TextMeshProHelper.HorizontalAlignment horizontalAlignment;
        public readonly TextMeshProHelper.VerticalAlignment verticalAlignment;
        public readonly string localizationTable;
        public readonly string localizationKey;

        public MetaDataTextMeshProGUI(Vector2 pivot, Vector2 position, AnchorPresets anchorPresets, float width = 0,
            float height = 0, float fontSize = 0,
            TextMeshProHelper.HorizontalAlignment horizontalAlignment = TextMeshProHelper.HorizontalAlignment.Center,
            TextMeshProHelper.VerticalAlignment verticalAlignment = TextMeshProHelper.VerticalAlignment.Middle,
            string localizationTable = "", string localizationKey = "")
        {
            this.pivot = pivot;
            this.position = position;
            this.anchorPresets = anchorPresets;
            this.width = width;
            this.height = height;
            this.fontSize = fontSize;
            this.horizontalAlignment = horizontalAlignment;
            this.verticalAlignment = verticalAlignment;
            this.localizationTable = localizationTable;
            this.localizationKey = localizationKey;
        }
    }
    public abstract class CreateUIComponent
    {
        public static string GenerateObjectName(string objectName, ConfigPackageInfo.PackageType packageType)
        {
            string prefix = ConfigPackageInfo.GetPackagePrefix(packageType);
            return objectName.StartsWith($"{prefix}_") ? objectName : $"{prefix}_{objectName}";
        }
        public static Canvas CreateObjectCanvas(ConfigPackageInfo.PackageType packageType)
        {
            string objectName = GenerateObjectName("Canvas", packageType);
            Canvas canvas = GameObject.Find(objectName)?.GetComponent<Canvas>();
            
            if (!canvas)
            {
                GameObject canvasObj = new GameObject(objectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // 이벤트 시스템 생성
                CreateEventSystemIfNotExists();
            }

            return canvas;
        }
        
        private static void CreateEventSystemIfNotExists()
        {
#if UNITY_6000_0_OR_NEWER
            // Unity 6 이상
            if (Object.FindFirstObjectByType<EventSystem>() == null)
#else
            if (Object.FindObjectOfType<EventSystem>() == null)
#endif
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
                // 새로운 Input System을 사용하는 경우
                eventSystem.AddComponent<InputSystemUIInputModule>();
#else
                // 기존 Input Manager를 사용하는 경우
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
            }
        }
        public static GameObject CreateGameObjectByPrefab(string objectName, ConfigPackageInfo.PackageType packageType, Transform parent = null, string prefabPath = "")
        {
            objectName = GenerateObjectName(objectName, packageType);
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject)
            {
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent);
                }
                return gameObject;
            }

            bool isUsePrefab = false;
            if (string.IsNullOrEmpty(prefabPath))
            {
                gameObject = new GameObject(objectName);
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab == null)
                {
                    Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
                    return null;
                }
                // 프리팹 인스턴스화
                gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (!gameObject)
                {
                    Debug.LogError("프리팹 인스턴스 생성 실패");
                    return null;
                }

                isUsePrefab = true;
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Default Button");
            gameObject.name = objectName;
            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            // 프리팹 해제
            if (isUsePrefab)
            {
                PrefabUtility.UnpackPrefabInstance(
                    gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.UserAction
                );
            }

            return gameObject;
        }
        public static Button CreateObjectButton(MetaDataButton metaDataButton, ConfigPackageInfo.PackageType packageType)
        {
            string objectName = GenerateObjectName(metaDataButton.filedName, packageType);
            string text = metaDataButton.text;
            string localizationTable = metaDataButton.localizationTable;
            string localizationKey = metaDataButton.localizationKey;
            
            objectName = GenerateObjectName(objectName, packageType);
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<Button>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas(packageType);

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PathPrefabDefaultUIButton;
            obj = CreateGameObjectByPrefab(objectName, packageType, canvas.transform, prefabPath);
            obj.transform.localPosition = Vector3.zero;
            Button button = obj.GetComponent<Button>();
            if (!button)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            AddLocalizeStringEvent(buttonText, localizationTable, localizationKey);
            if (buttonText)
            {
                buttonText.text = text;
            }

            return button;
        }

        private static void AddLocalizeStringEvent(TextMeshProUGUI objectText, string localizationTable, string localizationKey)
        {
            if (objectText == null) return;
            if (string.IsNullOrEmpty(localizationTable) || string.IsNullOrEmpty(localizationKey)) return;
            
            var localizeEvent = objectText.gameObject.GetComponent<LocalizeStringEvent>();
            if (localizeEvent == null)
            {
                localizeEvent = objectText.gameObject.AddComponent<LocalizeStringEvent>();
            }

            // 테이블 및 키 설정
            localizeEvent.SetTable(localizationTable);
            localizeEvent.SetEntry(localizationKey);

#if UNITY_6000_0_OR_NEWER
            // Update String 에 추가하기
            UnityEditor.Events.UnityEventTools.AddPersistentListener(localizeEvent.OnUpdateString, objectText.SetText);
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
        public static TextMeshProUGUI CreateObjectText(string objectName, ConfigPackageInfo.PackageType packageType, MetaDataTextMeshProGUI metaDataTextMeshProGUI = null)
        {
            objectName = GenerateObjectName(objectName, packageType);
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas(packageType);

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PathPrefabDefaultUITextMeshProGUI;
            obj = CreateGameObjectByPrefab(objectName, packageType, canvas.transform, prefabPath);
            
            TextMeshProUGUI textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            if (!textMeshProUGUI)
            {
                Debug.LogError("TextMeshProUGUI 컴포넌트를 찾을 수 없습니다.");
                return null;
            }
                
            if (metaDataTextMeshProGUI == null) return textMeshProUGUI;
            
            string localizationTable = metaDataTextMeshProGUI.localizationTable;
            string localizationKey = metaDataTextMeshProGUI.localizationKey;
            AddLocalizeStringEvent(textMeshProUGUI, localizationTable, localizationKey);
            
            textMeshProUGUI.rectTransform.SetAnchor(metaDataTextMeshProGUI.anchorPresets);
            textMeshProUGUI.rectTransform.anchoredPosition = metaDataTextMeshProGUI.position;
            if (metaDataTextMeshProGUI.width > 0 && metaDataTextMeshProGUI.height > 0)
            {
                textMeshProUGUI.rectTransform.sizeDelta =
                    new Vector2(metaDataTextMeshProGUI.width, metaDataTextMeshProGUI.height);
            }

            if (metaDataTextMeshProGUI.fontSize > 0)
            {
                textMeshProUGUI.fontSize = metaDataTextMeshProGUI.fontSize;
            }

            TextMeshProHelper.SetAlignment(textMeshProUGUI, metaDataTextMeshProGUI.horizontalAlignment, metaDataTextMeshProGUI.verticalAlignment);

            return textMeshProUGUI;
        }

        public static GameObject Find(string objectName, ConfigPackageInfo.PackageType packageType)
        {
            return GameObject.Find(GenerateObjectName(objectName, packageType));
        }
    }
}
