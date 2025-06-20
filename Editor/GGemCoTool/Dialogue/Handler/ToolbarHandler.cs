using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly DialogueEditorWindowWindow _editorWindowWindow;

        private readonly TableDialogue _tableDialogue;
        private int _selectedDialogueIndex;

        private readonly List<string> _dialogueMemos = new List<string>();
        private readonly Dictionary<int, StruckTableDialogue> _dialogueInfos = new Dictionary<int, StruckTableDialogue>(); 
        
        private int _previousIndex;
        public ToolbarHandler(DialogueEditorWindowWindow windowWindow, List<string> dialogueMemos, Dictionary<int, StruckTableDialogue> dialogueInfos)
        {
            _editorWindowWindow = windowWindow;
            _previousIndex = 0;
            _selectedDialogueIndex = 0;
            _dialogueMemos = dialogueMemos;
            _dialogueInfos = dialogueInfos;
        }
        public void DrawToolbar()
        {
            if (_editorWindowWindow == null) return;
            
            // 방어 코드 추가
            if (_dialogueMemos.Count == 0)
            {
                EditorGUILayout.LabelField("등록된 아이템이 없습니다.");
                return;
            }
            GUILayout.BeginVertical(EditorStyles.toolbar, GUILayout.Width(250));

            // if (GUILayout.Button("자동 배치"))
            // {
            //     AutoLayout();
            // }
            EditorGUILayout.Space(20);

            if (_selectedDialogueIndex >= _dialogueMemos.Count)
            {
                _selectedDialogueIndex = 0;
            }
            _selectedDialogueIndex = EditorGUILayout.Popup("", _selectedDialogueIndex, _dialogueMemos.ToArray());
            if (_previousIndex != _selectedDialogueIndex)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadDialogue())
                {
                    _previousIndex = _selectedDialogueIndex;
                }
                else
                {
                    _selectedDialogueIndex = _previousIndex;
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("저장"))
            {
                _editorWindowWindow.FileHandler?.SaveToJson(_selectedDialogueIndex, _dialogueInfos);
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
                var info = _dialogueInfos.GetValueOrDefault(_selectedDialogueIndex);
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
                _editorWindowWindow.NodeHandler?.AddNode();
            }

            // 100% 보기 버튼
            if (GUILayout.Button("100% 보기"))
            {
                _editorWindowWindow.ZoomPanHandler?.SetZoom(1.0f);
                _editorWindowWindow.panOffset = Vector2.zero; // 위치도 초기화
            }
            EditorGUILayout.Space();
            Common.GUILine(2);
            EditorGUILayout.Space();
            if (GUILayout.Button("모두 지우기"))
            {
                bool result = EditorUtility.DisplayDialog("삭제", "정말로 삭제하시겠습니까?", "확인", "취소");
                if (result)
                {
                    _editorWindowWindow?.nodes?.Clear();
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
            if (_editorWindowWindow.nodes?.Count > 0)
            {
                bool result = EditorUtility.DisplayDialog("불러오기", "현재 대사 Node 가 만들어진 상태입니다.\n저장 하셨나요?", "네", "아니요");
                if (result)
                {
                    var info = _dialogueInfos.GetValueOrDefault(_selectedDialogueIndex);
                    _editorWindowWindow.FileHandler?.LoadFromJson(info.FileName);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var info = _dialogueInfos.GetValueOrDefault(_selectedDialogueIndex);
                _editorWindowWindow.FileHandler?.LoadFromJson(info.FileName);
            }
            return true;
        }
    }
}