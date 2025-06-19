using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor.CustomInspector
{
    [CustomEditor(typeof(SceneIntro))]
    public class SceneIntro_Inspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            SceneIntro sceneIntro = (SceneIntro)target;

            // popupManager 필드 가져오기 (private 이므로 리플렉션 필요)
            var popupManagerField = typeof(SceneIntro)
                .GetField("popupManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            PopupManager currentValuePopupManager = popupManagerField?.GetValue(sceneIntro) as PopupManager;
            
            if (!currentValuePopupManager)
            {
                if (GUILayout.Button("PopupManager 만들기"))
                {
                    CreateObjectPopupManager(sceneIntro, popupManagerField);
                }
            }
            // else
            // {
            //     EditorGUILayout.HelpBox("PopupManager가 이미 연결되어 있습니다.", MessageType.Info);
            // }
            
            var uIWindowLoadSaveData = typeof(SceneIntro)
                .GetField("uIWindowLoadSaveData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            UIWindowLoadSaveData currentValueUIWindowLoadSaveData = uIWindowLoadSaveData?.GetValue(sceneIntro) as UIWindowLoadSaveData;
            if (!currentValueUIWindowLoadSaveData)
            {
                if (GUILayout.Button("불러오기 윈도우 만들기"))
                {
                }
            }
        }

        private void CreateObjectPopupManager(SceneIntro sceneIntro, System.Reflection.FieldInfo popupManagerField)
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

            // 현재 선택된 SceneIntro 컴포넌트를 가져옴

            // Undo 등록 (에디터 작업 되돌리기 지원)
            Undo.RecordObject(sceneIntro, "Assign PopupManager");

            // popupManager 필드에 할당
            popupManagerField.SetValue(sceneIntro, popupManager);

            // 변경 사항 저장
            EditorUtility.SetDirty(sceneIntro);
        }
    }
}