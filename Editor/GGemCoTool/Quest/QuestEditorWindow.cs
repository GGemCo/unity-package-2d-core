﻿using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class MetadataQuestStepListDrawer
    {
        public List<string> NameQuest;
        public List<string> NameNpc;
        public List<string> NameMonster;
        public List<string> NameMap;
        public List<string> NameDialogue;
        public List<string> NameItem;
        public Dictionary<int, StruckTableQuest> StruckTableQuests;
        public Dictionary<int, StruckTableNpc> StruckTableNpcs;
        public Dictionary<int, StruckTableMonster> StruckTableMonsters;
        public Dictionary<int, StruckTableMap> StruckTableMaps;
        public Dictionary<int, StruckTableDialogue> StruckTableDialogues;
        public Dictionary<int, StruckTableItem> StruckTableItems;

        public MetadataQuestStepListDrawer(List<string> nameQuest, List<string> nameNpc, List<string> nameMonster,
            List<string> nameMap, List<string> nameDialogue, List<string> nameItem,
            Dictionary<int, StruckTableQuest> struckTableQuests, Dictionary<int, StruckTableNpc> struckTableNpcs,
            Dictionary<int, StruckTableMonster> struckTableMonsters, Dictionary<int, StruckTableMap> struckTableMaps,
            Dictionary<int, StruckTableDialogue> struckTableDialogues, Dictionary<int, StruckTableItem> struckTableItems)
        {
            NameQuest = nameQuest;
            NameNpc = nameNpc;
            NameMonster = nameMonster;
            NameMap = nameMap;
            NameDialogue = nameDialogue;
            NameItem = nameItem;
            StruckTableQuests = struckTableQuests;
            StruckTableNpcs = struckTableNpcs;
            StruckTableMonsters = struckTableMonsters;
            StruckTableMaps = struckTableMaps;
            StruckTableDialogues = struckTableDialogues;
            StruckTableItems = struckTableItems;
        }
    }
    public class QuestEditorWindow : EditorWindow
    {
        private Quest _quest = new Quest();
        private Vector2 _scrollPos;
        private ReorderableList _stepList;
        private ReorderableList _rewardItemList;
        private const float LabelWidth = 70f;
        
        private TableLoaderManager _tableLoaderManager;
        private TableQuest _tableQuest;
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableMap _tableMap;
        private TableDialogue _tableDialogue;
        private TableItem _tableItem;
        
        private int _selectedQuestIndex;
        private List<string> _nameQuest = new List<string>();
        private List<string> _nameNpc = new List<string>();
        private List<string> _nameMonster = new List<string>();
        private List<string> _nameMap = new List<string>();
        private List<string> _nameDialogue = new List<string>();
        private List<string> _nameItem = new List<string>();
        private Dictionary<int, StruckTableQuest> _struckTableQuests = new Dictionary<int, StruckTableQuest>(); 
        private Dictionary<int, StruckTableNpc> _struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private Dictionary<int, StruckTableMonster> _struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        private Dictionary<int, StruckTableMap> _struckTableMaps = new Dictionary<int, StruckTableMap>(); 
        private Dictionary<int, StruckTableDialogue> _struckTableDialogues = new Dictionary<int, StruckTableDialogue>(); 
        private Dictionary<int, StruckTableItem> _struckTableItems = new Dictionary<int, StruckTableItem>(); 

        private QuestStepListDrawer _questStepListDrawer;
        private RewardItemListDrawer _rewardItemListDrawer;
        
        private AddressableSettingsLoader _addressableSettingsLoader;
        private int _maxSlotCount;
        private string _saveDirectory;
        private SaveDataContainer _saveDataContainer;
        
        [MenuItem(ConfigEditor.NameToolQuest, false, (int)ConfigEditor.ToolOrdering.Quest)]
        public static void ShowWindow()
        {
            GetWindow<QuestEditorWindow>(ConfigEditor.NameToolQuest);
        }
        private void OnEnable()
        {
            _tableLoaderManager = new TableLoaderManager();
            _tableLoaderManager.LoadTableData<TableQuest, StruckTableQuest>(
                ConfigAddressableTable.Quest,
                out _tableQuest,
                out _nameQuest,
                out _struckTableQuests,
                info => $"{info.Uid} - {info.Name}"
            );
            
            _tableLoaderManager.LoadTableData<TableNpc, StruckTableNpc>(
                ConfigAddressableTable.Npc,
                out _tableNpc,
                out _nameNpc,
                out _struckTableNpcs,
                info => $"{info.Uid} - {info.Name}"
            );
            _tableLoaderManager.LoadTableData<TableMonster, StruckTableMonster>(
                ConfigAddressableTable.Monster,
                out _tableMonster,
                out _nameMonster,
                out _struckTableMonsters,
                info => $"{info.Uid} - {info.Name}"
            );
            _tableLoaderManager.LoadTableData<TableMap, StruckTableMap>(
                ConfigAddressableTable.Map,
                out _tableMap,
                out _nameMap,
                out _struckTableMaps,
                info => $"{info.Uid} - {info.Name}"
            );
            _tableLoaderManager.LoadTableData<TableDialogue, StruckTableDialogue>(
                ConfigAddressableTable.Dialogue,
                out _tableDialogue,
                out _nameDialogue,
                out _struckTableDialogues,
                info => $"{info.Uid} - {info.Memo}"
            );
            _tableLoaderManager.LoadTableData<TableItem, StruckTableItem>(
                ConfigAddressableTable.Item,
                out _tableItem,
                out _nameItem,
                out _struckTableItems,
                info => $"{info.Uid} - {info.Name}"
            );
            
            _quest.steps ??= new List<QuestStep>();
            _quest.reward ??= new QuestReward();
            _quest.reward.items ??= new List<RewardItem>();

            MetadataQuestStepListDrawer metadataQuestStepListDrawer = new MetadataQuestStepListDrawer(
                _nameQuest, _nameNpc, _nameMonster, _nameMap, _nameDialogue, _nameItem, 
                _struckTableQuests, 
                _struckTableNpcs,
                _struckTableMonsters,
                _struckTableMaps, 
                _struckTableDialogues, 
                _struckTableItems
                );
            _questStepListDrawer = new QuestStepListDrawer(_quest.steps, metadataQuestStepListDrawer);
            _rewardItemListDrawer = new RewardItemListDrawer(_quest.reward, metadataQuestStepListDrawer);
            
            _addressableSettingsLoader = new AddressableSettingsLoader();
            _ = _addressableSettingsLoader.InitializeAsync();
            _addressableSettingsLoader.OnLoadSettings += Initialize;
        }
        private void Initialize(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings)
        {
            _maxSlotCount = saveSettings.saveDataMaxSlotCount;
            _saveDirectory = saveSettings.SaveDataFolderName;
        }
        private int previousIndex;
        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = LabelWidth; // 라벨 너비 축소
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            Common.OnGUITitle("저장/불러오기");
            _selectedQuestIndex = EditorGUILayout.Popup("연출 선택", _selectedQuestIndex, _nameQuest.ToArray());
            if (previousIndex != _selectedQuestIndex)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadQuestFromJson())
                {
                    previousIndex = _selectedQuestIndex;
                }
                else
                {
                    _selectedQuestIndex = previousIndex;
                }
            }
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("JSON 저장")) SaveQuestToJson();
            if (GUILayout.Button("JSON 불러오기")) LoadQuestFromJson();
            GUILayout.EndHorizontal();
            
            Common.GUILineBlue(2);
            // 퀘스트 정보 초기화
            Common.OnGUITitle("퀘스트 진행 상황 초기화");
            if (GUILayout.Button("초기화 하기"))
            {
                RemoveQuestSaveData();
            }

            Common.GUILineBlue(2);
            // 기본정보
            Common.OnGUITitle("퀘스트 기본 정보");
            // nextNodeGuid 읽기 전용 처리
            GUI.enabled = false;
            var info = _struckTableQuests.GetValueOrDefault(_selectedQuestIndex);
            _quest.uid = EditorGUILayout.IntField("Uid", info.Uid);
            _quest.title = EditorGUILayout.TextField("제목", info.Name);
            GUI.enabled = true;
           
            // 단계별 정보
            Common.GUILineBlue(2);
            _questStepListDrawer.List.DoLayoutList();

            // 보상
            Common.GUILineBlue(2);
            _rewardItemListDrawer.DoLayout();

            GUILayout.Space(30);

            EditorGUILayout.EndScrollView();
        }

        private void StartQuest()
        {
        }

        private void RemoveQuestSaveData()
        {
            bool result = EditorUtility.DisplayDialog("초기화", "현재 플레이한 퀘스트 정보가 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            if (!result) return;
            
            int slotIndex = PlayerPrefsManager.LoadSaveDataSlotIndex();
            SaveFileController saveFileController = new SaveFileController(_saveDirectory, _maxSlotCount);
            string filePath = saveFileController.GetSaveFilePath(slotIndex);
            string json = File.ReadAllText(filePath);
            if (json != "")
            {
                _saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainer>(json);
            }

            _saveDataContainer.QuestData = new QuestData();
            json = JsonConvert.SerializeObject(_saveDataContainer);
            File.WriteAllText(filePath, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(ConfigEditor.NameToolQuest, "퀘스트 플레이 정보 초기화 완료", "OK");
        }
        /// <summary>
        /// json 으로 저장하기
        /// </summary>
        private void SaveQuestToJson()
        {
            bool result = EditorUtility.DisplayDialog("저장하기", "현재 선택된 퀘스트에 저장하시겠습니까?", "네", "아니요");
            if (!result) return;
            var info = _struckTableQuests.GetValueOrDefault(_selectedQuestIndex);
            if (info == null) return;
            string path = $"{ConfigAddressables.PathJsonQuest}/{info.FileName}.json";
            // 저장 전에 Unity가 리스트를 최신 상태로 반영하게 강제한다.
            EditorUtility.SetDirty(this);
            string json = JsonConvert.SerializeObject(_quest, Formatting.Indented);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(ConfigEditor.NameToolQuest, "Json 저장하기 완료", "OK");
        }
        /// <summary>
        /// json 불러오기
        /// </summary>
        private bool LoadQuestFromJson()
        {
            bool result = EditorUtility.DisplayDialog("불러오기", "현재 불러온 내용이 초기화 됩니다.\n계속 진행할가요?", "네", "아니요");
            if (!result) return false;
            
            var info = _struckTableQuests.GetValueOrDefault(_selectedQuestIndex);
            if (info == null) return false;
            string path = $"{ConfigAddressables.PathJsonQuest}/{info.FileName}.json";
            
            try
            {
                TextAsset textFile = AssetDatabaseLoaderManager.LoadAsset<TextAsset>(path);
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        _quest = JsonConvert.DeserializeObject<Quest>(content);
                        if (_quest != null)
                        {
                            OnEnable(); // 리스트 다시 초기화
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"퀘스트 json 파일을 불러오는중 오류가 발생했습니다. {path}: {ex.Message}");
            }

            return false;
        }
    }
}
