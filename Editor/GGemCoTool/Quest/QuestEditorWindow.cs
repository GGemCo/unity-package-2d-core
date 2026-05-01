using System;
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
        public List<string> NameLicense;
        public Dictionary<int, StruckTableQuest> StruckTableQuests;
        public Dictionary<int, StruckTableNpc> StruckTableNpcs;
        public Dictionary<int, StruckTableMonster> StruckTableMonsters;
        public Dictionary<int, StruckTableMap> StruckTableMaps;
        public Dictionary<int, StruckTableDialogue> StruckTableDialogues;
        public Dictionary<int, StruckTableItem> StruckTableItems;
        public Dictionary<int, StruckTableLicense> StruckTableLicenses;

        public MetadataQuestStepListDrawer(List<string> nameQuest, List<string> nameNpc, List<string> nameMonster,
            List<string> nameMap, List<string> nameDialogue, List<string> nameItem, List<string> nameLicense,
            Dictionary<int, StruckTableQuest> struckTableQuests, Dictionary<int, StruckTableNpc> struckTableNpcs,
            Dictionary<int, StruckTableMonster> struckTableMonsters, Dictionary<int, StruckTableMap> struckTableMaps,
            Dictionary<int, StruckTableDialogue> struckTableDialogues, Dictionary<int, StruckTableItem> struckTableItems,
            Dictionary<int, StruckTableLicense> struckTableLicenses)
        {
            NameQuest = nameQuest;
            NameNpc = nameNpc;
            NameMonster = nameMonster;
            NameMap = nameMap;
            NameDialogue = nameDialogue;
            NameItem = nameItem;
            NameLicense = nameLicense;
            StruckTableQuests = struckTableQuests;
            StruckTableNpcs = struckTableNpcs;
            StruckTableMonsters = struckTableMonsters;
            StruckTableMaps = struckTableMaps;
            StruckTableDialogues = struckTableDialogues;
            StruckTableItems = struckTableItems;
            StruckTableLicenses = struckTableLicenses;
        }
    }
    public class QuestEditorWindow : EditorWindow
    {
        private Quest _quest = new Quest();
        private Vector2 _scrollPos;
        private ReorderableList _stepList;
        private ReorderableList _rewardItemList;
        private const float LabelWidth = 70f;
        
        private TableQuest _tableQuest;
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableMap _tableMap;
        private TableDialogue _tableDialogue;
        private TableItem _tableItem;
        private TableLicense _tableLicense;
        
        private int _selectedQuestIndex;
        private readonly List<SearchableDropdownUtility.Option<int>> _questOptions =
            new List<SearchableDropdownUtility.Option<int>>();
        private List<string> _nameQuest = new List<string>();
        private List<string> _nameNpc = new List<string>();
        private List<string> _nameMonster = new List<string>();
        private List<string> _nameMap = new List<string>();
        private List<string> _nameDialogue = new List<string>();
        private List<string> _nameItem = new List<string>();
        private List<string> _nameLicense = new List<string>();
        private Dictionary<int, StruckTableQuest> _struckTableQuests = new Dictionary<int, StruckTableQuest>(); 
        private Dictionary<int, StruckTableNpc> _struckTableNpcs = new Dictionary<int, StruckTableNpc>(); 
        private Dictionary<int, StruckTableMonster> _struckTableMonsters = new Dictionary<int, StruckTableMonster>(); 
        private Dictionary<int, StruckTableMap> _struckTableMaps = new Dictionary<int, StruckTableMap>(); 
        private Dictionary<int, StruckTableDialogue> _struckTableDialogues = new Dictionary<int, StruckTableDialogue>(); 
        private Dictionary<int, StruckTableItem> _struckTableItems = new Dictionary<int, StruckTableItem>(); 
        private Dictionary<int, StruckTableLicense> _struckTableLicenses = new Dictionary<int, StruckTableLicense>();

        private QuestStepListDrawer _questStepListDrawer;
        private RewardItemListDrawer _rewardItemListDrawer;
        
        private AddressableSettingsLoader _addressableSettingsLoader;
        private int _maxSlotCount;
        private string _saveDirectory;
        private SaveDataContainer _saveDataContainer;
        private int _previousIndex;
        
        [MenuItem(ConfigEditor.NameToolQuest, false, (int)ConfigEditor.ToolOrdering.Quest)]
        public static void ShowWindow()
        {
            GetWindow<QuestEditorWindow>(ConfigEditor.NameToolQuest);
        }
        private void OnEnable()
        {
            TableLoaderManager.LoadTableData(
                ConfigAddressableTable.Quest,
                out _tableQuest,
                out _nameQuest,
                out _struckTableQuests,
                info => $"{info.Uid} - {info.Name}"
            );
            BuildQuestDropdownOptions();
            
            TableLoaderManager.LoadTableData<TableNpc, StruckTableNpc>(
                ConfigAddressableTable.Npc,
                out _tableNpc,
                out _nameNpc,
                out _struckTableNpcs,
                info => $"{info.Uid} - {info.Name}"
            );
            TableLoaderManager.LoadTableData<TableMonster, StruckTableMonster>(
                ConfigAddressableTable.Monster,
                out _tableMonster,
                out _nameMonster,
                out _struckTableMonsters,
                info => $"{info.Uid} - {info.Name}"
            );
            TableLoaderManager.LoadTableData<TableMap, StruckTableMap>(
                ConfigAddressableTable.Map,
                out _tableMap,
                out _nameMap,
                out _struckTableMaps,
                info => $"{info.Uid} - {info.Name}"
            );
            TableLoaderManager.LoadTableData<TableDialogue, StruckTableDialogue>(
                ConfigAddressableTable.Dialogue,
                out _tableDialogue,
                out _nameDialogue,
                out _struckTableDialogues,
                info => $"{info.Uid} - {info.Memo}"
            );
            TableLoaderManager.LoadTableData<TableItem, StruckTableItem>(
                ConfigAddressableTable.Item,
                out _tableItem,
                out _nameItem,
                out _struckTableItems,
                info => $"{info.Uid} - {info.Name}"
            );
            TableLoaderManager.LoadTableData<TableLicense, StruckTableLicense>(
                ConfigAddressableTable.License,
                out _tableLicense,
                out _nameLicense,
                out _struckTableLicenses,
                info => $"{info.Uid} - {info.Key}"
            );
            
            _quest.steps ??= new List<QuestStep>();
            _quest.reward ??= new QuestReward();
            _quest.reward.items ??= new List<RewardItem>();
            _quest.reward.mapProgress ??= new QuestRewardMapProgress();
            _quest.reward.mapProgress.activateWorldMapNodeIds ??= new List<string>();
            _quest.reward.licenses ??= new List<QuestRewardLicense>();

            MetadataQuestStepListDrawer metadataQuestStepListDrawer = new MetadataQuestStepListDrawer(
                _nameQuest, _nameNpc, _nameMonster, _nameMap, _nameDialogue, _nameItem, _nameLicense,
                _struckTableQuests, 
                _struckTableNpcs,
                _struckTableMonsters,
                _struckTableMaps, 
                _struckTableDialogues, 
                _struckTableItems,
                _struckTableLicenses
                );
            _questStepListDrawer = new QuestStepListDrawer(_quest.steps, metadataQuestStepListDrawer);
            _rewardItemListDrawer = new RewardItemListDrawer(_quest.reward, metadataQuestStepListDrawer);
            
            _addressableSettingsLoader = new AddressableSettingsLoader();
            _ = _addressableSettingsLoader.InitializeAsync();
            _addressableSettingsLoader.OnLoadSettings += Initialize;
        }
        private void OnDisable()
        {
            if (_addressableSettingsLoader != null)
            {
                _addressableSettingsLoader.OnLoadSettings -= Initialize;
            }
        }
        private void Initialize(GGemCoSettings settings, GGemCoPlayerSettings playerSettings,
            GGemCoMapSettings mapSettings, GGemCoSaveSettings saveSettings)
        {
            _maxSlotCount = saveSettings.saveDataMaxSlotCount;
            _saveDirectory = saveSettings.SaveDataFolderName;
        }

        /// <summary>
        /// 퀘스트 테이블 데이터를 검색 가능한 드롭다운 항목으로 변환합니다.
        /// </summary>
        private void BuildQuestDropdownOptions()
        {
            _questOptions.Clear();

            foreach (var pair in _struckTableQuests)
            {
                StruckTableQuest questInfo = pair.Value;
                if (questInfo == null) continue;

                _questOptions.Add(new SearchableDropdownUtility.Option<int>(
                    questInfo.Uid.ToString(),
                    questInfo.Name,
                    pair.Key));
            }
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = LabelWidth; // 라벨 너비 축소
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            HelperEditorUI.OnGUITitle("저장/불러오기");
            DrawQuestSearchableDropdown();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("JSON 저장")) SaveQuestToJson();
            if (GUILayout.Button("JSON 불러오기")) LoadQuestFromJson();
            GUILayout.EndHorizontal();
            
            HelperEditorUI.GUILineBlue(2);
            // 퀘스트 정보 초기화
            HelperEditorUI.OnGUITitle("퀘스트 진행 상황 초기화");
            if (GUILayout.Button("초기화 하기"))
            {
                RemoveQuestSaveData();
            }

            HelperEditorUI.GUILineBlue(2);
            // 기본정보
            HelperEditorUI.OnGUITitle("퀘스트 기본 정보");
            DrawQuestBaseInfo();
           
            // 단계별 정보
            HelperEditorUI.GUILineBlue(2);
            _questStepListDrawer.List.DoLayoutList();

            // 보상
            HelperEditorUI.GUILineBlue(2);
            _rewardItemListDrawer.DoLayout();

            GUILayout.Space(30);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 검색 가능한 퀘스트 선택 드롭다운을 그리고 선택 변경을 처리합니다.
        /// </summary>
        private void DrawQuestSearchableDropdown()
        {
            if (_questOptions.Count <= 0)
            {
                EditorGUILayout.HelpBox("퀘스트 테이블 데이터가 없습니다.", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("퀘스트 선택");

                SearchableDropdownUtility.DrawButtonAndShow(
                    buttonText: GetSelectedQuestDropdownText(),
                    options: _questOptions,
                    selectedIndex: GetQuestOptionIndex(_selectedQuestIndex),
                    onSelected: OnQuestDropdownSelected,
                    defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
            }
        }

        /// <summary>
        /// 현재 선택된 퀘스트를 드롭다운 버튼에 표시할 문자열로 반환합니다.
        /// </summary>
        /// <returns>선택된 퀘스트 표시 문자열입니다.</returns>
        private string GetSelectedQuestDropdownText()
        {
            return _struckTableQuests.TryGetValue(_selectedQuestIndex, out StruckTableQuest questInfo) &&
                   questInfo != null
                ? $"{questInfo.Uid} - {questInfo.Name}"
                : "퀘스트 선택...";
        }

        /// <summary>
        /// 퀘스트 테이블 인덱스에 해당하는 드롭다운 옵션 인덱스를 찾습니다.
        /// </summary>
        /// <param name="questTableIndex">퀘스트 테이블 로더가 부여한 인덱스입니다.</param>
        /// <returns>드롭다운 옵션 인덱스입니다. 없으면 -1입니다.</returns>
        private int GetQuestOptionIndex(int questTableIndex)
        {
            return _questOptions.FindIndex(option => option.Data == questTableIndex);
        }

        /// <summary>
        /// 검색 드롭다운에서 선택한 퀘스트를 현재 선택 상태에 반영하고 JSON을 불러옵니다.
        /// 불러오기에 실패하면 이전 선택으로 되돌립니다.
        /// </summary>
        /// <param name="index">선택된 드롭다운 옵션 인덱스입니다.</param>
        /// <param name="option">선택된 퀘스트 옵션입니다.</param>
        private void OnQuestDropdownSelected(int index, SearchableDropdownUtility.Option<int> option)
        {
            int nextQuestIndex = option.Data;
            if (_selectedQuestIndex == nextQuestIndex) return;

            int previousQuestIndex = _selectedQuestIndex;
            _selectedQuestIndex = nextQuestIndex;
            if (LoadQuestFromJson())
            {
                _previousIndex = _selectedQuestIndex;
            }
            else
            {
                _selectedQuestIndex = previousQuestIndex;
                _previousIndex = previousQuestIndex;
            }

            Repaint();
        }

        /// <summary>
        /// 선택된 퀘스트의 기본 정보를 읽기 전용 비활성화 상태로 표시합니다.
        /// </summary>
        private void DrawQuestBaseInfo()
        {
            if (!_struckTableQuests.TryGetValue(_selectedQuestIndex, out StruckTableQuest info) || info == null)
            {
                EditorGUILayout.HelpBox("선택된 퀘스트 정보가 없습니다.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                _quest.uid = EditorGUILayout.IntField("Uid", info.Uid);
                _quest.title = EditorGUILayout.TextField("제목", info.Name);
            }
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
            string path = $"{ConfigAddressablePath.Narrative.Quests}/{info.FileName}.json";
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
            string path = $"{ConfigAddressablePath.Narrative.Quests}/{info.FileName}.json";
            
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
