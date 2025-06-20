using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo.Editor
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
        private TableQuest _tableQuest;
        
        private readonly List<string> _nameNpc = new List<string>();
        private readonly List<string> _nameMonster = new List<string>();
        private readonly List<string> _nameQuest = new List<string>();
        
        private readonly Dictionary<int, StruckTableNpc> _struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private readonly Dictionary<int, StruckTableMonster> _struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        private readonly Dictionary<int, StruckTableQuest> _struckTableQuest = new Dictionary<int, StruckTableQuest>(); 
        
        private int _selectedIndexNpc;
        private int _selectedIndexMonster;
        private int _selectedIndexQuest;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedIndexNpc = 0;
            _selectedIndexMonster = 0;
            _selectedIndexQuest = 0;
            
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                // 순차 로드
                // _tableNpc = await TableLoaderManager.LoadNpcTableAsync();
                
                // 병렬 로드
                var loadNpcTask = TableLoaderManager.LoadNpcTableAsync();
                var loadMonsterTask = TableLoaderManager.LoadMonsterTableAsync();
                var loadQuestTask = TableLoaderManager.LoadQuestTableAsync();
                await Task.WhenAll(loadNpcTask, loadMonsterTask, loadQuestTask);

                _tableNpc = loadNpcTask.Result;
                _tableMonster = loadMonsterTask.Result;
                _tableQuest = loadQuestTask.Result;

                LoadNpcInfoData();
                LoadMonsterInfoData();
                LoadQuestInfoData();

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
                    _selectedIndexQuest = dialogueNode.startQuestUid > 0
                        ? _nameQuest.FindIndex(x => x.Contains(dialogueNode.startQuestUid.ToString()))
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

        private void LoadQuestInfoData()
        {
            Dictionary<int, Dictionary<string, string>> dictionary = _tableQuest.GetDatas();
             
            int index = 0;
            _nameQuest.Add("0");
            _struckTableQuest.TryAdd(index++, new StruckTableQuest());
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
            {
                var info = _tableQuest.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _nameQuest.Add($"{info.Uid} - {info.Name}");
                _struckTableQuest.TryAdd(index, info);
                index++;
            }
        }

        private void LoadMonsterInfoData()
        {
            Dictionary<int, Dictionary<string, string>> monsterDictionary = _tableMonster.GetDatas();
             
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = _tableMonster.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _nameMonster.Add($"{info.Uid} - {info.Name}");
                _struckTableMonsters.TryAdd(index, info);
                index++;
            }
        }
        
        private void LoadNpcInfoData()
        {
            Dictionary<int, Dictionary<string, string>> npcDictionary = _tableNpc.GetDatas();
             
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in npcDictionary)
            {
                var info = _tableNpc.GetDataByUid(outerPair.Key);
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
                GcLogger.LogError("퀘스트 node 가 없습니다.");
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thumbnailImage"));

            GUILayout.Space(20);
            GUILayout.Label("퀘스트", EditorStyles.boldLabel);
            if (dialogueNode)
            {
                _selectedIndexQuest = EditorGUILayout.Popup("startQuestUid", _selectedIndexQuest, _nameQuest.ToArray());
                dialogueNode.startQuestUid = _struckTableQuest.GetValueOrDefault(_selectedIndexQuest)?.Uid ?? 0;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startQuestStep"));
            
            GUILayout.Space(20);
            _optionList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}