using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    public abstract class CreateUIComponent
    {
        public static Canvas CreateObjectCanvas()
        {
            Canvas canvas = GameObject.Find("Canvas")?.GetComponent<Canvas>();
            
            if (!canvas)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            GameObject gameObject = GameObject.Find(objectName);
            if (gameObject)
            {
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
                gameObject.transform.SetParent(parent, false);
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
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<Button>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas();

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PrefabPathDefaultUIButton;
            obj = CreateGameObjectByPrefab(objectName, canvas.transform, prefabPath);
            
            Button button = obj.GetComponent<Button>();
            if (!button)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }

            button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            return button;
        }
        public static TextMeshProUGUI CreateObjectText(string objectName)
        {
            // 버튼 찾기 
            GameObject obj = GameObject.Find(objectName);
            if (obj)
            {
                return obj.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // 캔버스 찾기 또는 생성
            Canvas canvas = CreateObjectCanvas();

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PrefabPathDefaultUITextMeshProGUI;
            obj = CreateGameObjectByPrefab(objectName, canvas.transform, prefabPath);
            
            TextMeshProUGUI textMeshProUGUI = obj.GetComponent<TextMeshProUGUI>();
            if (!textMeshProUGUI)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }
            return textMeshProUGUI;
        }
    }
}