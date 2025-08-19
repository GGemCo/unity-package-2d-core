#if UNITY_EDITOR
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.UI; // ★ ToggleEditor 사용
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(UIToggleConfirmable)), CanEditMultipleObjects]
    public class ConfirmableToggleEditor : ToggleEditor
    {
        private SerializedProperty _requireConfirmProp;

        protected override void OnEnable()
        {
            base.OnEnable(); // 기본 Toggle 에디터 초기화 유지
            _requireConfirmProp = serializedObject.FindProperty("requireConfirm");
        }

        public override void OnInspectorGUI()
        {
            // 1) 기본 Toggle 인스펙터(모든 내장 필드/이벤트) 그대로 그림
            base.OnInspectorGUI();

            // 2) 커스텀 필드 추가
            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_requireConfirmProp, new GUIContent("Require Confirm"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif