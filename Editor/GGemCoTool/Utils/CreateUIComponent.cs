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
        public readonly string FiledName;
        public readonly string Text;
        public readonly string LocalizationTable;
        public readonly string LocalizationKey;

        public MetaDataButton(string filedName, string text = "", string localizationTable = "", string localizationKey = "")
        {
            FiledName = filedName;
            Text = text;
            LocalizationTable = localizationTable;
            LocalizationKey = localizationKey;
        }
    }
    public class MetaDataTextMeshProGUI
    {
        public readonly Vector2 Pivot;
        public readonly Vector2 Position;
        public readonly AnchorPresets AnchorPresets;
        public readonly float Width;
        public readonly float Height;
        public readonly float FontSize;
        public readonly TextMeshProHelper.HorizontalAlignment HorizontalAlignment;
        public readonly TextMeshProHelper.VerticalAlignment VerticalAlignment;

        public MetaDataTextMeshProGUI(Vector2 pivot, Vector2 position, AnchorPresets anchorPresets, float width = 0,
            float height = 0, float fontSize = 0,
            TextMeshProHelper.HorizontalAlignment horizontalAlignment = TextMeshProHelper.HorizontalAlignment.Center,
            TextMeshProHelper.VerticalAlignment verticalAlignment = TextMeshProHelper.VerticalAlignment.Middle)
        {
            Pivot = pivot;
            Position = position;
            AnchorPresets = anchorPresets;
            Width = width;
            Height = height;
            FontSize = fontSize;
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;
        }
    }
    public abstract class CreateUIComponent
    {
        private static string GenerateObjectName(string objectName)
        {
            return objectName.StartsWith($"{ConfigEditor.NamePrefixCore}_") ? objectName : $"{ConfigEditor.NamePrefixCore}_{objectName}";
        }
        public static Canvas CreateObjectCanvas()
        {
            string objectName = GenerateObjectName("Canvas");
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
        public static GameObject CreateGameObjectByPrefab(string objectName, Transform parent = null, string prefabPath = "")
        {
            objectName = GenerateObjectName(objectName);
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject)
            {
                if (parent != null)
                {
                    gameObject.transform.SetParent(parent);
                }
                return gameObject;
            }
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

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

            Undo.RegisterCreatedObjectUndo(gameObject, "Create Default Button");
            gameObject.name = objectName;
            if (parent != null)
            {
                gameObject.transform.SetParent(parent);
            }

            // 프리팹 해제
            PrefabUtility.UnpackPrefabInstance(
                gameObject,
                PrefabUnpackMode.Completely,
                InteractionMode.UserAction
            );
            return gameObject;
        }
        public static Button CreateObjectButton(MetaDataButton metaDataButton)
        {
            string objectName = GenerateObjectName(metaDataButton.FiledName);
            string text = metaDataButton.Text;
            string localizationTable = metaDataButton.LocalizationTable;
            string localizationKey = metaDataButton.LocalizationKey;
            
            objectName = GenerateObjectName(objectName);
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<Button>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas();

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PathPrefabDefaultUIButton;
            obj = CreateGameObjectByPrefab(objectName, canvas.transform, prefabPath);
            obj.transform.localPosition = Vector3.zero;
            Button button = obj.GetComponent<Button>();
            if (!button)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText)
            {
                if (!string.IsNullOrEmpty(localizationTable) && !string.IsNullOrEmpty(localizationKey))
                {
                    var localizeEvent = buttonText.gameObject.GetComponent<LocalizeStringEvent>();
                    if (localizeEvent == null)
                    {
                        localizeEvent = buttonText.gameObject.AddComponent<LocalizeStringEvent>();
                    }

                    // 테이블 및 키 설정
                    localizeEvent.SetTable(localizationTable);
                    localizeEvent.SetEntry(localizationKey);

#if UNITY_6000_0_OR_NEWER
                    // Update String 에 추가하기
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(localizeEvent.OnUpdateString, buttonText.SetText);
#else
                    var proxy = buttonText.GetComponent<LocalizationTextProxy>();
                    if (proxy == null)
                    {
                        proxy = buttonText.gameObject.AddComponent<LocalizationTextProxy>();
                        proxy.target = buttonText;
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

                buttonText.text = text;
            }

            return button;
        }
        public static TextMeshProUGUI CreateObjectText(string objectName, MetaDataTextMeshProGUI metaDataTextMeshProGUI = null)
        {
            objectName = GenerateObjectName(objectName);
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas();

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PathPrefabDefaultUITextMeshProGUI;
            obj = CreateGameObjectByPrefab(objectName, canvas.transform, prefabPath);
            
            TextMeshProUGUI textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            if (!textMeshProUGUI)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }

            if (metaDataTextMeshProGUI == null) return textMeshProUGUI;
            
            textMeshProUGUI.rectTransform.SetAnchor(metaDataTextMeshProGUI.AnchorPresets);
            textMeshProUGUI.rectTransform.anchoredPosition = metaDataTextMeshProGUI.Position;
            if (metaDataTextMeshProGUI.Width > 0 && metaDataTextMeshProGUI.Height > 0)
            {
                textMeshProUGUI.rectTransform.sizeDelta =
                    new Vector2(metaDataTextMeshProGUI.Width, metaDataTextMeshProGUI.Height);
            }

            if (metaDataTextMeshProGUI.FontSize > 0)
            {
                textMeshProUGUI.fontSize = metaDataTextMeshProGUI.FontSize;
            }

            TextMeshProHelper.SetAlignment(textMeshProUGUI, metaDataTextMeshProGUI.HorizontalAlignment, metaDataTextMeshProGUI.VerticalAlignment);

            return textMeshProUGUI;
        }

        public static GameObject Find(string objectName)
        {
            return GameObject.Find(GenerateObjectName(objectName));
        }
    }
}
