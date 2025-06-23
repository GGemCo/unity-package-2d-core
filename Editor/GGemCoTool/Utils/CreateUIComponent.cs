using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Editor
{
    public abstract class CreateUIComponent
    {
        public static Button CreateObjectButton(FieldInfo buttonField, string text)
        {
            // 버튼 찾기 
            GameObject objButton = GameObject.Find(buttonField.Name);
            if (objButton)
            {
                return objButton.GetComponentInChildren<Button>();
            }
            
            // 캔버스 찾기 또는 생성
#if UNITY_6000
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
#else
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
#endif
            
            if (!canvas)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // 이벤트 시스템 생성
#if UNITY_6000
                if (!GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>())
#else
                if (!GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>())
#endif
                {
                    GameObject eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                }
            }

            // 패키지 프리팹 로드
            string prefabPath = ConfigEditor.PrefabPathDefaultUIButton;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError($"프리팹을 찾을 수 없습니다: {prefabPath}");
                return null;
            }

            // 프리팹 인스턴스화
            GameObject buttonObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (!buttonObj)
            {
                Debug.LogError("프리팹 인스턴스 생성 실패");
                return null;
            }

            Undo.RegisterCreatedObjectUndo(buttonObj, "Create Default Button");
            buttonObj.name = buttonField.Name;
            buttonObj.transform.SetParent(canvas.transform, false);
            
            // 프리팹 해제
            PrefabUtility.UnpackPrefabInstance(
                buttonObj,
                PrefabUnpackMode.Completely,
                InteractionMode.UserAction
            );
            
            // 필드에 할당
            Button button = buttonObj.GetComponent<Button>();
            if (!button)
            {
                Debug.LogError("Button 컴포넌트를 찾을 수 없습니다.");
                return null;
            }

            button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            return button;
            
            // Undo.RecordObject(sceneIntro, "Assign Button");
            // buttonField.SetValue(sceneIntro, button);
            // EditorUtility.SetDirty(sceneIntro);
        }
    }
}