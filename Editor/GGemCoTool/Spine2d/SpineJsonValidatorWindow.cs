using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Spine2D 이벤트 Json 문법 검사기
    /// </summary>
    public class SpineJsonValidatorWindow : EditorWindow
    {
        private Vector2 leftScroll;
        private Vector2 rightScroll;
        private List<SpineJsonValidationResult> validationResults = new();
        private SpineJsonValidationResult selectedResult;

        [MenuItem("GGemCo/Tools/Spine2D JSON 검사기", true)]
        private static void ShowWindow() => GetWindow<SpineJsonValidatorWindow>("Spine JSON 검사기");

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 50,
                fontSize = 20
            };
            if (GUILayout.Button("GGemCo 폴더 검사", style)) RunValidation();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            Common.OnGUITitle("수정해야 될 목록");
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            foreach (var result in validationResults)
            {
                if (GUILayout.Button($"{Path.GetFileName(result.FilePath)}", result == selectedResult ? EditorStyles.boldLabel : EditorStyles.label))
                    selectedResult = result;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 30,
                fontSize = 15
            };
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

            if (selectedResult != null)
            {
                EditorGUILayout.LabelField("파일 경로:", selectedResult.FilePath, EditorStyles.wordWrappedLabel);
                foreach (var error in selectedResult.Errors)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"이벤트 이름: {error.EventName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("문자열:", error.OriginalValue);
                    EditorGUILayout.LabelField("에러:", error.ErrorMessage, EditorStyles.wordWrappedLabel);

                    string newValue = EditorGUILayout.TextArea(error.OriginalValue, GUILayout.ExpandHeight(true));

                    error.OriginalValue = newValue; // <-- 상태 반영
                    if (GUILayout.Button("문자열 수정 및 저장", style))
                    {
                        // 1. 파일 수정
                        SpineJsonFileScanner.UpdateJsonValue(selectedResult.FilePath, error.JsonPath, newValue);

                        // 2. 재검사 수행
                        RunValidation();

                        // 3. 수정한 파일을 다시 선택
                        selectedResult = validationResults.Find(r => r.FilePath == selectedResult.FilePath);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void RunValidation()
        {
            validationResults = SpineJsonFileScanner.ValidateAllSkeletonJsons("Assets/GGemCo");
            selectedResult = validationResults.Count > 0 ? validationResults[0] : null;
        }
    }
}
