using System;
using System.Collections.Generic;
using System.IO;
using GGemCo.Scripts;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo.Editor
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
        private Quest quest = new Quest();
        private Vector2 scrollPos;
        private ReorderableList stepList;
        private ReorderableList rewardItemList;
        private const float LabelWidth = 70f;
        
        private TableLoaderManager tableLoaderManager;
        private TableQuest tableQuest;
        private TableNpc tableNpc;
        private TableMonster tableMonster;
        private TableMap tableMap;
        private TableDialogue tableDialogue;
        private TableItem tableItem;
        
        private int selectedQuestIndex;
        private List<string> nameQuest = new List<string>();
        private List<string> nameNpc = new List<string>();
        private List<string> nameMonster = new List<string>();
        private List<string> nameMap = new List<string>();
        private List<string> nameDialogue = new List<string>();
        private List<string> nameItem = new List<string>();
        private Dictionary<int, StruckTableQuest> struckTableQuests = new Dictionary<int, StruckTableQuest>(); 
        private Dictionary<int, StruckTableNpc> struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private Dictionary<int, StruckTableMonster> struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        private Dictionary<int, StruckTableMap> struckTableMaps = new Dictionary<int, StruckTableMap>(); 
        private Dictionary<int, StruckTableDialogue> struckTableDialogues = new Dictionary<int, StruckTableDialogue>(); 
        private Dictionary<int, StruckTableItem> struckTableItems = new Dictionary<int, StruckTableItem>(); 

        private QuestStepListDrawer questStepListDrawer;
        private RewardItemListDrawer rewardItemListDrawer;
        
        private AddressableSettingsLoader addressableSettingsLoader;
        private int maxSlotCount;
        private string saveDirectory;
        private SaveDataContainer saveDataContainer;
        
        [MenuItem(ConfigEditor.NameToolQuest, false, (int)ConfigEditor.ToolOrdering.Quest)]
        public static void ShowWindow()
        {
            GetWindow<QuestEditorWindow>(ConfigEditor.NameToolQuest);
        }
        private void OnEnable()
        {
            tableLoaderManager = new TableLoaderManager();
            tableLoaderManager.LoadTableData<TableQuest, StruckTableQuest>(
                ConfigTableFileName.Quest,
                out tableQuest,
                out nameQuest,
                out struckTableQuests,
                info => $"{info.Uid} - {info.Name}"
            );
            
            tableLoaderManager.LoadTableData<TableNpc, StruckTableNpc>(
                ConfigTableFileName.Npc,
                out tableNpc,
                out nameNpc,
                out struckTableNpcs,
                info => $"{info.Uid} - {info.Name}"
            );
            tableLoaderManager.LoadTableData<TableMonster, StruckTableMonster>(
                ConfigTableFileName.Monster,
                out tableMonster,
                out nameMonster,
                out struckTableMonsters,
                info => $"{info.Uid} - {info.Name}"
            );
            tableLoaderManager.LoadTableData<TableMap, StruckTableMap>(
                ConfigTableFileName.Map,
                out tableMap,
                out nameMap,
                out struckTableMaps,
                info => $"{info.Uid} - {info.Name}"
            );
            tableLoaderManager.LoadTableData<TableDialogue, StruckTableDialogue>(
                ConfigTableFileName.Dialogue,
                out tableDialogue,
                out nameDialogue,
                out struckTableDialogues,
                info => $"{info.Uid} - {info.Memo}"
            );
            tableLoaderManager.LoadTableData<TableItem, StruckTableItem>(
                ConfigTableFileName.Item,
                out tableItem,
                out nameItem,
                out struckTableItems,
                info => $"{info.Uid} - {info.Name}"
            );
            
            quest.steps ??= new List<QuestStep>();
            quest.reward ??= new QuestReward();
            quest.reward.items ??= new List<RewardItem>();

            MetadataQuestStepListDrawer metadataQuestStepListDrawer = new MetadataQuestStepListDrawer(
                nameQuest, nameNpc, nameMonster, nameMap, nameDialogue, nameItem, 
                struckTableQuests, 
                struckTableNpcs,
                struckTableMonsters,
                struckTableMaps, 
                struckTableDialogues, 
                struckTableItems
                );
            questStepListDrawer = new QuestStepListDrawer(quest.steps, metadataQuestStepListDrawer);
            rewardItemListDrawer = new RewardItemListDrawer(quest.reward, metadataQuestStepListDrawer);
            
            addressableSettingsLoader = new AddressableSettingsLoader();
            _ = addressableSettingsLoader.InitializeAsync();
            addressableSettingsLoader.OnLoadSettings += Initialize;
        }
        private void Initialize(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings)
        {
            maxSlotCount = saveSettings.saveDataMaxSlotCount;
            saveDirectory = saveSettings.SaveDataFolderName;
        }
        private int previousIndex;
        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = LabelWidth; // 라벨 너비 축소
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            Common.OnGUITitle("저장/불러오기");
            selectedQuestIndex = EditorGUILayout.Popup("연출 선택", selectedQuestIndex, nameQuest.ToArray());
            if (previousIndex != selectedQuestIndex)
            {
                // 선택이 바뀌었을 때 실행할 코드
                // Debug.Log($"선택이 변경되었습니다: {questTitle[selectedQuestIndex]}");
                if (LoadQuestFromJson())
                {
                    previousIndex = selectedQuestIndex;
                }
                else
                {
                    selectedQuestIndex = previousIndex;
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
            var info = struckTableQuests.GetValueOrDefault(selectedQuestIndex);
            quest.uid = EditorGUILayout.IntField("Uid", info.Uid);
            quest.title = EditorGUILayout.TextField("제목", info.Name);
            GUI.enabled = true;
           
            // 단계별 정보
            Common.GUILineBlue(2);
            questStepListDrawer.List.DoLayoutList();

            // 보상
            Common.GUILineBlue(2);
            rewardItemListDrawer.DoLayout();

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
            SaveFileController saveFileController = new SaveFileController(saveDirectory, maxSlotCount);
            string filePath = saveFileController.GetSaveFilePath(slotIndex);
            string json = File.ReadAllText(filePath);
            if (json != "")
            {
                saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainer>(json);
            }

            saveDataContainer.QuestData = new QuestData();
            json = JsonConvert.SerializeObject(saveDataContainer);
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
            var info = struckTableQuests.GetValueOrDefault(selectedQuestIndex);
            if (info == null) return;
            string fileName = info.FileName;
            string path = Path.Combine(QuestConstants.GetJsonFolderPath(), fileName+".json");
            // 저장 전에 Unity가 리스트를 최신 상태로 반영하게 강제한다.
            EditorUtility.SetDirty(this);
            string json = JsonConvert.SerializeObject(quest, Formatting.Indented);
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
            
            var info = struckTableQuests.GetValueOrDefault(selectedQuestIndex);
            if (info == null) return false;
            string path = Path.Combine(QuestConstants.JsonFolderName, info.FileName);
            
            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{path}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        quest = JsonConvert.DeserializeObject<Quest>(content);
                        if (quest != null)
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
