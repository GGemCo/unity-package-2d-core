#if UNITY_EDITOR
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class TestDropItemRate : DefaultEditorWindow
    {
        private const string Title = "아이템 드랍 확률";
        private ItemManager.DropTestResult _testResult;
        private int _selectedMonsterUid;
        private int _testCount;
        private ItemManager _itemManager;
        private TableMonster _tableMonster;
        private TableItem _tableItem;
        private readonly List<SearchableDropdownUtility.Option<int>> _monsterOptions = new List<SearchableDropdownUtility.Option<int>>();
        private Vector2 _scrollPos;
        
        private Dictionary<ItemConstants.Category, List<StruckTableItem>> _dictionaryByCategory;
        private Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> _dictionaryBySubCategory;
        private Dictionary<int, List<StruckTableItemDropGroup>> _dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        private Dictionary<int, List<StruckTableMonsterDropRate>> _monsterDropDictionary = new Dictionary<int, List<StruckTableMonsterDropRate>>();

        [MenuItem(ConfigEditor.NameToolDropItemRate, false, (int)ConfigEditor.ToolOrdering.DropItemRate)]
        public static void ShowWindow()
        {
            GetWindow<TestDropItemRate>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedMonsterUid = 0;
            _testCount = 10000;

            _itemManager = new ItemManager();
            _tableMonster = TableLoaderManager.LoadMonsterTable();
            
            _tableItem = TableLoaderManager.LoadItemTable();
            TableItemDropGroup tableItemDropGroup = TableLoaderManager.LoadItemDropGroupTable();
            TableMonsterDropRate tableMonsterDropRate = TableLoaderManager.LoadMonsterDropRateTable();
            
            _dictionaryByCategory = _tableItem.GetDictionaryByCategory();
            _dictionaryBySubCategory = _tableItem.GetDictionaryBySubCategory();
            _dropGroupDictionary = tableItemDropGroup.GetDropGroups();
            _monsterDropDictionary = tableMonsterDropRate.GetMonsterDropDictionary();

            LoadMonsterInfoData();
        }
        /// <summary>
        ///  몬스터 정보 불러오기
        /// </summary>
        private void LoadMonsterInfoData()
        {
            int previousUid = _selectedMonsterUid;

            _monsterOptions.Clear();

            if (_tableMonster == null)
            {
                _selectedMonsterUid = 0;
                return;
            }

            Dictionary<int, StruckTableMonster> monsterDictionary = _tableMonster.GetDatas();
            foreach (KeyValuePair<int, StruckTableMonster> outerPair in monsterDictionary)
            {
                StruckTableMonster info = outerPair.Value;
                if (info == null || info.Uid <= 0)
                {
                    continue;
                }

                _monsterOptions.Add(new SearchableDropdownUtility.Option<int>(
                    info.Uid.ToString(),
                    info.Name,
                    info.Uid));
            }

            _selectedMonsterUid = TryGetPreservedUid(_monsterOptions, previousUid);
        }


        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_monsterOptions.Count == 0)
            {
                EditorGUILayout.HelpBox("선택 가능한 몬스터 데이터가 없습니다. 테이블 로드 상태를 확인해주세요.", MessageType.Warning);
            }

            int selectedMonsterIndex = FindOptionIndexByUid(_monsterOptions, _selectedMonsterUid);
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                "몬스터 선택",
                _monsterOptions,
                selectedMonsterIndex,
                (_, option) =>
                {
                    _selectedMonsterUid = option.Data;
                    Repaint();
                },
                noneText: "(몬스터 선택)");

            _testCount = EditorGUILayout.IntField("테스트 횟수", _testCount);

            using (new EditorGUI.DisabledScope(_itemManager == null || _tableMonster == null || _selectedMonsterUid <= 0))
            {
                if (GUILayout.Button("테스트 실행", GUILayout.Height(30)))
                {
                    RunDropTest();
                }
            }

            EditorGUILayout.Space();
            HelperEditorUI.OnGUITitle("테스트 결과");

            if (_testResult != null)
            {
                EditorGUILayout.LabelField($"몬스터 UID: {_testResult.monsterUid}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"테스트 횟수: {_testResult.iterations}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                DrawTable("Monster Drop Rate", _testResult.dropRateCounts);
                DrawTable("Item Category", _testResult.categoryCounts);
                DrawTable("Item SubCategory", _testResult.subCategoryCounts);
            
                GUILayout.Space(20);
            }
            else
            {
                EditorGUILayout.HelpBox("테스트 실행 후 결과가 여기에 표시됩니다.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }


        private static int FindOptionIndexByUid(IReadOnlyList<SearchableDropdownUtility.Option<int>> options, int selectedUid)
        {
            if (options == null || options.Count == 0 || selectedUid <= 0)
            {
                return -1;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Data == selectedUid)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int TryGetPreservedUid(IReadOnlyList<SearchableDropdownUtility.Option<int>> options, int previousUid)
        {
            if (options == null || options.Count == 0)
            {
                return 0;
            }

            return FindOptionIndexByUid(options, previousUid) >= 0
                ? previousUid
                : options[0].Data;
        }

        private void RunDropTest()
        {
            StruckTableMonster monsterData = _tableMonster.GetDataByUid(_selectedMonsterUid);
            if (monsterData == null || monsterData.Uid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "몬스터 데이터가 없습니다.", "OK");
                return;
            }

            ItemManager.DropTestResult dropTestResult = _itemManager.TestDropRates(
                monsterData.Uid,
                _testCount,
                _dictionaryByCategory,
                _dictionaryBySubCategory,
                _dropGroupDictionary,
                _monsterDropDictionary,
                _tableItem);

            if (dropTestResult == null)
            {
                return;
            }

            EditorUtility.DisplayDialog(
                Title,
                $"테스트 완료: 몬스터 UID {monsterData.Uid}, {_testCount}회 실행됨.",
                "OK");

            _testResult = dropTestResult;
            Repaint();
        }

        private void DrawTable<T>(string subTitle, Dictionary<T, int> data)
        {
            if (data.Count == 0) return;

            EditorGUILayout.LabelField(subTitle, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(200));
            EditorGUILayout.LabelField("Count", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Percentage", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            foreach (var entry in data)
            {
                float percentage = (entry.Value / (float)_testResult.iterations) * 100;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.Key.ToString(), GUILayout.Width(200));
                EditorGUILayout.LabelField(entry.Value.ToString(), GUILayout.Width(80));
                EditorGUILayout.LabelField($"{percentage:F2}%", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
#endif
