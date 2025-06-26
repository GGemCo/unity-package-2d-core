using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    public class MetaDataTextMeshProGUI
    {
        public Vector2 Pivot;
        public Vector2 Position;
        public AnchorPresets AnchorPresets;
        public float Width;
        public float Height;
        public float FontSize;
        public TextMeshProHelper.HorizontalAlignment HorizontalAlignment;
        public TextMeshProHelper.VerticalAlignment VerticalAlignment;

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
        public static Button CreateObjectButton(string objectName, string text)
        {
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

            button.GetComponentInChildren<TextMeshProUGUI>().text = text;
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
