using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 대사 노드 커스텀 Inspector 
    /// </summary>
    [CustomEditor(typeof(DialogueNode))]
    public class DialogueNodeEditor : DefaultEditor
    {
        private const string Title = "대사 노드 커스텀 Inspector";
        private ReorderableList _optionList;
        
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        
        private readonly List<string> _nameNpc = new List<string>();
        private readonly List<string> _nameMonster = new List<string>();
        
        private readonly Dictionary<int, StruckTableNpc> _struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private readonly Dictionary<int, StruckTableMonster> _struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        
        private int _selectedIndexNpc;
        private int _selectedIndexMonster;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndexNpc = 0;
            _selectedIndexMonster = 0;
            
            LoadTable();
        }

        private void LoadTable()
        {
            try
            {
                // 순차 로드
                // _tableNpc = await TableLoaderManager.LoadNpcTableAsync();
                
                _tableNpc = TableLoaderManager.LoadNpcTable();
                _tableMonster = TableLoaderManager.LoadMonsterTable();
            
                LoadNpcInfoData();
                LoadMonsterInfoData();
            
                _optionList = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("options"),
                    true, true, true, true)
                {
                    drawHeaderCallback = (rect) => { EditorGUI.LabelField(rect, "선택지 목록"); }
                };
            
                DialogueNode dialogueNode = serializedObject.targetObject as DialogueNode;
                if (dialogueNode)
                {
                    _selectedIndexNpc = dialogueNode.characterUid > 0
                        ? _nameNpc.FindIndex(x => x.Contains(dialogueNode.characterUid.ToString()))
                        : 0;
                    _selectedIndexMonster = dialogueNode.characterUid > 0
                        ? _nameMonster.FindIndex(x => x.Contains(dialogueNode.characterUid.ToString()))
                        : 0;
                }
            
                _optionList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    SerializedProperty element = _optionList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width * 0.5f, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("optionText"), GUIContent.none);
            
                    // nextNodeGuid 읽기 전용 처리
                    GUI.enabled = false;
                    EditorGUI.PropertyField(
                        new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.45f,
                            EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("nextNodeGuid"), GUIContent.none);
                    GUI.enabled = true;
                };
            
            
                IsLoading = false;
                Repaint();
            }
            catch (System.Exception ex)
            {
                ShowLoadTableException(Title, ex);
            }
        }

        private void LoadMonsterInfoData()
        {
            Dictionary<int, StruckTableMonster> monsterDictionary = _tableMonster.GetDatas();
             
            int index = 0;
            foreach (KeyValuePair<int, StruckTableMonster> outerPair in monsterDictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;
                _nameMonster.Add($"{info.Uid} - {info.Name}");
                _struckTableMonsters.TryAdd(index, info);
                index++;
            }
        }
        
        private void LoadNpcInfoData()
        {
            Dictionary<int, StruckTableNpc> npcDictionary = _tableNpc.GetDatas();
             
            int index = 0;
            foreach (KeyValuePair<int, StruckTableNpc> outerPair in npcDictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;
                _nameNpc.Add($"{info.Uid} - {info.Name}");
                _struckTableNpcs.TryAdd(index, info);
                index++;
            }
        }

        public override void OnInspectorGUI()
        {
            if (IsLoading)
            {
                EditorGUILayout.LabelField("테이블 로딩 중...");
                return;
            }
            
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueText"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"));
            // EditorGUILayout.PropertyField(serializedObject.FindProperty("position"));
            // nextNodeGuid 읽기 전용 처리
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("nextNodeGuid"));
            GUI.enabled = true;
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterType"));
            DialogueNode dialogueNode = serializedObject.targetObject as DialogueNode;
            if (dialogueNode)
            {
                if (dialogueNode.characterType == CharacterConstants.Type.Npc)
                {
                    _selectedIndexNpc = EditorGUILayout.Popup("characterUid", _selectedIndexNpc, _nameNpc.ToArray());
                    dialogueNode.characterUid = _struckTableNpcs.GetValueOrDefault(_selectedIndexNpc)?.Uid ?? 0;
                }
                else if (dialogueNode.characterType == CharacterConstants.Type.Monster)
                {
                    _selectedIndexMonster = EditorGUILayout.Popup("characterUid", _selectedIndexMonster, _nameMonster.ToArray());
                    dialogueNode.characterUid = _struckTableMonsters.GetValueOrDefault(_selectedIndexMonster)?.Uid ?? 0;
                }
                else
                {
                    dialogueNode.characterUid = 0;
                }
            }
            else
            {
                Debug.LogError("퀘스트 node 가 없습니다.");
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailImage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailPositionType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailFlipPolicy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailSourceFacing"));

            GUILayout.Space(20);
            GUILayout.Label("현재 대화가 끝났을 때 시작되는 외부 콘텐츠", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startQuestUid"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startQuestStep"));
            
            GUILayout.Space(20);
            _optionList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
