using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 생성툴 - 메뉴
    /// </summary>
    public class ToolbarHandler
    {
        private readonly DialogueEditorWindow editorWindow;

        private readonly TableDialogue tableDialogue;
        private int selectedDialogueIndex;

        private readonly List<string> dialogueMemos = new List<string>();
        private readonly Dictionary<int, StruckTableDialogue> dialogueInfos = new Dictionary<int, StruckTableDialogue>(); 
        
        private int previousIndex;
        public ToolbarHandler(DialogueEditorWindow window)
        {
            editorWindow = window;
            
            var tableLoaderManager = new TableLoaderManager();
            tableDialogue = tableLoaderManager.LoadDialogueTable();
            LoadCutsceneInfoData();
        }

        private void LoadCutsceneInfoData()
        {
            if (tableDialogue == null) return;
            Dictionary<int, Dictionary<string, string>> npcDictionary = tableDialogue.GetDatas();
             
            dialogueMemos.Clear();
            dialogueInfos.Clear();
            int index = 0;
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in npcDictionary)
            {
                var info = tableDialogue.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                dialogueMemos.Add($"{info.Uid} - {info.Memo}");
                dialogueInfos.TryAdd(index, info);
                index++;
            }
        }

        public void DrawToolbar()
        {
            if (editorWindow == null) return;
            GUILayout.BeginVertical(EditorStyles.toolbar, GUILayout.Width(250));

            // if (GUILayout.Button("자동 배치"))
            // {
            //     AutoLayout();
            // }
            EditorGUILayout.Space(20);
            
            selectedDialogueIndex = EditorGUILayout.Popup("", selectedDialogueIndex, dialogueMemos.ToArray());
            if (previousIndex != selectedDialogueIndex)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadDialogue())
                {
                    previousIndex = selectedDialogueIndex;
                }
                else
                {
                    selectedDialogueIndex = previousIndex;
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("저장"))
            {
                editorWindow.FileHandler?.SaveToJson(selectedDialogueIndex, dialogueInfos);
            }
            if (GUILayout.Button("불러오기"))
            {
                LoadDialogue();
            }
            
            if (GUILayout.Button("미리보기"))
            {
                if (SceneGame.Instance == null)
                {
                    EditorUtility.DisplayDialog("대사 생성툴", "게임을 실행해주세요.", "OK");
                    return;
                }
                var info = dialogueInfos.GetValueOrDefault(selectedDialogueIndex);
                UIWindowDialogue uiWindowDialogue =
                    SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowDialogue>(UIWindowManager.WindowUid
                        .Dialogue);
                uiWindowDialogue?.LoadDialogue(info.Uid);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            Common.GUILine(2);
            EditorGUILayout.Space();
            if (GUILayout.Button("노드 추가"))
            {
                editorWindow.NodeHandler?.AddNode();
            }

            // 100% 보기 버튼
            if (GUILayout.Button("100% 보기"))
            {
                editorWindow.ZoomPanHandler?.SetZoom(1.0f);
                editorWindow.panOffset = Vector2.zero; // 위치도 초기화
            }
            EditorGUILayout.Space();
            Common.GUILine(2);
            EditorGUILayout.Space();
            if (GUILayout.Button("모두 지우기"))
            {
                bool result = EditorUtility.DisplayDialog("삭제", "정말로 삭제하시겠습니까?", "확인", "취소");
                if (result)
                {
                    editorWindow?.nodes?.Clear();
                }
            }

            // // 화면 꽉차게 보기 버튼
            // if (GUILayout.Button("화면 꽉차게 보기"))
            // {
            //     editorWindow.ZoomPanHandler?.FitViewToNodes();
            // }

            GUILayout.EndVertical();
        }

        private bool LoadDialogue()
        {
            if (editorWindow.nodes?.Count > 0)
            {
                bool result = EditorUtility.DisplayDialog("불러오기", "현재 대사 Node 가 만들어진 상태입니다.\n저장 하셨나요?", "네", "아니요");
                if (result)
                {
                    var info = dialogueInfos.GetValueOrDefault(selectedDialogueIndex);
                    editorWindow.FileHandler?.LoadFromJson(info.FileName);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var info = dialogueInfos.GetValueOrDefault(selectedDialogueIndex);
                editorWindow.FileHandler?.LoadFromJson(info.FileName);
            }
            return true;
        }
    }
}