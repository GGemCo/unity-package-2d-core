using System.Reflection;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo.Editor
{
    [CustomEditor(typeof(GGemCo.Scripts.SceneIntro))]
    public class SceneIntroInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Common.GUILineBlue(1);

            Common.OnGUITitleBold("생성하기");
            // GGemCo.Scripts.SceneIntro sceneIntro = (GGemCo.Scripts.SceneIntro)target;

            var buttonNewGameField = typeof(GGemCo.Scripts.SceneIntro)
                .GetField("buttonNewGame", BindingFlags.NonPublic | BindingFlags.Instance);
            Button currentValueButtonNewGame = buttonNewGameField?.GetValue(target) as Button;
            
            if (buttonNewGameField != null && !currentValueButtonNewGame)
            {
                if (GUILayout.Button("Button New Game 만들기"))
                {
                    CreateButton(buttonNewGameField, "New Game");
                }
            }
            
            var buttonGameContinueField = typeof(GGemCo.Scripts.SceneIntro)
                .GetField("buttonGameContinue", BindingFlags.NonPublic | BindingFlags.Instance);
            Button currentValueButtonGameContinue = buttonGameContinueField?.GetValue(target) as Button;
            
            if (!currentValueButtonGameContinue)
            {
                if (GUILayout.Button("Button Game Continue 만들기"))
                {
                    CreateButton(buttonGameContinueField, "Game Continue");
                }
            }
            
            var popupManagerField = typeof(GGemCo.Scripts.SceneIntro)
                .GetField("popupManager", BindingFlags.NonPublic | BindingFlags.Instance);
            PopupManager currentValuePopupManager = popupManagerField?.GetValue(target) as PopupManager;
            
            if (!currentValuePopupManager)
            {
                if (GUILayout.Button("PopupManager 만들기"))
                {
                    CreateObjectPopupManager(popupManagerField);
                }
            }
            
            var uIWindowLoadSaveData = typeof(GGemCo.Scripts.SceneIntro)
                .GetField("uIWindowLoadSaveData", BindingFlags.NonPublic | BindingFlags.Instance);
            UIWindowLoadSaveData currentValueUIWindowLoadSaveData = uIWindowLoadSaveData?.GetValue(target) as UIWindowLoadSaveData;
            if (!currentValueUIWindowLoadSaveData)
            {
                if (GUILayout.Button("불러오기 윈도우 만들기"))
                {
                }
            }
        }

        private void CreateButton(FieldInfo buttonField, string text)
        {
            Button button = CreateUIComponent.CreateObjectButton(buttonField, text);
            Undo.RecordObject(target, "Assign Button");
            buttonField.SetValue(target, button);
            EditorUtility.SetDirty(target);
        }

        private void CreateObjectPopupManager(FieldInfo popupManagerField)
        {
            GameObject popupManagerObj = GameObject.Find("PopupManager");
            PopupManager popupManager;
            if (popupManagerObj)
            {
                popupManager = popupManagerObj.GetComponent<PopupManager>();
                Debug.LogWarning("PopupManager 가 이미 만들어져 있습니다.");
            }
            else
            {
                popupManagerObj = new GameObject("PopupManager");
                popupManager = popupManagerObj.AddComponent<PopupManager>();
            }
            // Undo 등록 (에디터 작업 되돌리기 지원)
            Undo.RecordObject(target, "Assign PopupManager");

            // popupManager 필드에 할당
            popupManagerField.SetValue(target, popupManager);

            // 변경 사항 저장
            EditorUtility.SetDirty(target);
        }
    }
}