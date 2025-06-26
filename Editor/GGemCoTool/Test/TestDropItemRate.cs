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
        private int _selectedMonsterIndex;
        private int _testCount;
        private ItemManager _itemManager;
        private TableMonster _tableMonster;
        private TableItem _tableItem;
        private List<string> _monsterNames; // 몬스터 이름 목록
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
            _selectedMonsterIndex = 0;
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
            Dictionary<int, Dictionary<string, string>> monsterDictionary = _tableMonster.GetDatas();

            _monsterNames = new List<string>();
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = _tableMonster.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                _monsterNames.Add($"{info.Uid} - {info.Name}");
            }
        }


        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            // 몬스터 드롭다운
            _selectedMonsterIndex = EditorGUILayout.Popup("몬스터 선택", _selectedMonsterIndex, _monsterNames.ToArray());
            // 테스트 횟수
            _testCount = EditorGUILayout.IntField("테스트 횟수", _testCount);

            // 테스트 실행 버튼
            if (GUILayout.Button("테스트 실행", GUILayout.Height(30)))
            {
                if (_itemManager != null)
                {
                    var monsterDictionary = _tableMonster.GetDatas();
                    int index = 0;
                    StruckTableMonster monsterData = new StruckTableMonster();

                    foreach (var outerPair in monsterDictionary)
                    {
                        if (index == _selectedMonsterIndex)
                        {
                            monsterData = _tableMonster.GetDataByUid(outerPair.Key);
                            break;
                        }
                        index++;
                    }

                    if (monsterData.Uid > 0)
                    {
                        ItemManager.DropTestResult dropTestResult = _itemManager.TestDropRates(monsterData.Uid,
                            _testCount, _dictionaryByCategory, _dictionaryBySubCategory, _dropGroupDictionary,
                            _monsterDropDictionary, _tableItem);

                        if (dropTestResult != null)
                        {
                            EditorUtility.DisplayDialog(Title, $"테스트 완료: 몬스터 UID {monsterData.Uid}, {_testCount}회 실행됨.",
                                "OK");
                            _testResult = dropTestResult;
                            Repaint(); // UI 갱신
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(Title, "몬스터 데이터가 없습니다.", "OK");
                    }
                }
            }

            EditorGUILayout.Space();
            Common.OnGUITitle("테스트 결과");

            if (_testResult != null)
            {
                EditorGUILayout.LabelField($"몬스터 UID: {_testResult.MonsterUid}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"테스트 횟수: {_testResult.Iterations}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                DrawTable("Monster Drop Rate", _testResult.DropRateCounts);
                DrawTable("Item Category", _testResult.CategoryCounts);
                DrawTable("Item SubCategory", _testResult.SubCategoryCounts);
            
                GUILayout.Space(20);
            }
            else
            {
                EditorGUILayout.HelpBox("테스트 실행 후 결과가 여기에 표시됩니다.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
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
                float percentage = (entry.Value / (float)_testResult.Iterations) * 100;
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
