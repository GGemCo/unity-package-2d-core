using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo.Editor
{
    /// <summary>
    /// 대사 노드 커스텀 inspector 
    /// </summary>
    [CustomEditor(typeof(DialogueNode))]
    public class DialogueNodeEditor : UnityEditor.Editor
    {
        private ReorderableList optionList;
        private int selectedQuestIndex;
        
        private TableLoaderManager tableLoaderManager;
        private TableNpc tableNpc;
        private TableMonster tableMonster;
        private TableQuest tableQuest;
        
        private List<string> nameNpc = new List<string>();
        private List<string> nameMonster = new List<string>();
        private List<string> nameQuest = new List<string>();
        
        private Dictionary<int, StruckTableNpc> struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private Dictionary<int, StruckTableMonster> struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        private Dictionary<int, StruckTableQuest> struckTableQuest = new Dictionary<int, StruckTableQuest>(); 
        
        private int selectedIndexNpc;
        private int selectedIndexMonster;
        private int selectedIndexQuest;
        
        private void OnEnable()
        {
            optionList = new ReorderableList(serializedObject,
                serializedObject.FindProperty("options"),
                true, true, true, true)
            {
                drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "선택지 목록");
                }
            };
            
            tableLoaderManager = new TableLoaderManager();
            tableNpc = tableLoaderManager.LoadNpcTable();
            tableMonster = tableLoaderManager.LoadMonsterTable();
            tableQuest = tableLoaderManager.LoadQuestTable();
            LoadNpcInfoData();
            LoadMonsterInfoData();
            LoadQuestInfoData();
            
            DialogueNode dialogueNode = serializedObject.targetObject as DialogueNode;
            if (dialogueNode != null)
            {
                selectedIndexNpc = dialogueNode.characterUid > 0 ? nameNpc.FindIndex(x => x.Contains(dialogueNode.characterUid.ToString())) : 0;
                selectedIndexMonster = dialogueNode.characterUid > 0 ? nameMonster.FindIndex(x => x.Contains(dialogueNode.characterUid.ToString())) : 0;
                selectedIndexQuest = dialogueNode.startQuestUid > 0 ? nameQuest.FindIndex(x => x.Contains(dialogueNode.startQuestUid.ToString())) : 0;
            }

            optionList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = optionList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width * 0.5f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("optionText"), GUIContent.none);
                
                // nextNodeGuid 읽기 전용 처리
                GUI.enabled = false;
                EditorGUI.PropertyField(
                    new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.45f, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("nextNodeGuid"), GUIContent.none);
                GUI.enabled = true;
            };
        }

        private void LoadQuestInfoData()
        {
            Dictionary<int, Dictionary<string, string>> dictionary = tableQuest.GetDatas();
             
            nameQuest = new List<string>();
            int index = 0;
            nameQuest.Add("0");
            struckTableQuest.TryAdd(index++, new StruckTableQuest());
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in dictionary)
            {
                var info = tableQuest.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                nameQuest.Add($"{info.Uid} - {info.Name}");
                struckTableQuest.TryAdd(index, info);
                index++;
            }
        }

        private void LoadMonsterInfoData()
        {
            Dictionary<int, Dictionary<string, string>> monsterDictionary = tableMonster.GetDatas();
             
            nameMonster = new List<string>();
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = tableMonster.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                nameMonster.Add($"{info.Uid} - {info.Name}");
                struckTableMonsters.TryAdd(index, info);
                index++;
            }
        }

        private void LoadNpcInfoData()
        {
            Dictionary<int, Dictionary<string, string>> npcDictionary = tableNpc.GetDatas();
             
            nameNpc = new List<string>();
            int index = 0;
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in npcDictionary)
            {
                var info = tableNpc.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                nameNpc.Add($"{info.Uid} - {info.Name}");
                struckTableNpcs.TryAdd(index, info);
                index++;
            }
        }

        public override void OnInspectorGUI()
        {
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
            if (dialogueNode != null)
            {
                if (dialogueNode.characterType == CharacterConstants.Type.Npc)
                {
                    selectedIndexNpc = EditorGUILayout.Popup("characterUid", selectedIndexNpc, nameNpc.ToArray());
                    dialogueNode.characterUid = struckTableNpcs.GetValueOrDefault(selectedIndexNpc)?.Uid ?? 0;
                }
                else if (dialogueNode.characterType == CharacterConstants.Type.Monster)
                {
                    selectedIndexMonster = EditorGUILayout.Popup("characterUid", selectedIndexMonster, nameMonster.ToArray());
                    dialogueNode.characterUid = struckTableMonsters.GetValueOrDefault(selectedIndexMonster)?.Uid ?? 0;
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
            if (dialogueNode != null)
            {
                selectedIndexQuest = EditorGUILayout.Popup("startQuestUid", selectedIndexQuest, nameQuest.ToArray());
                dialogueNode.startQuestUid = struckTableQuest.GetValueOrDefault(selectedIndexQuest)?.Uid ?? 0;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startQuestStep"));
            
            GUILayout.Space(20);
            optionList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}