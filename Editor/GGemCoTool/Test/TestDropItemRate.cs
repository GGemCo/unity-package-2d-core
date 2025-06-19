#if UNITY_EDITOR
using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class TestDropItemRate : EditorWindow
    {
        private const string Title = "아이템 드랍 확률";
        private ItemManager.DropTestResult testResult;
        private int selectedMonsterIndex;
        private int testCount;
        private ItemManager itemManager;
        private TableMonster tableMonster;
        private TableItem tableItem;
        private List<string> monsterNames; // 몬스터 이름 목록
        
        private Dictionary<ItemConstants.Category, List<StruckTableItem>> dictionaryByCategory;
        private Dictionary<ItemConstants.SubCategory, List<StruckTableItem>> dictionaryBySubCategory;
        private Dictionary<int, List<StruckTableItemDropGroup>> dropGroupDictionary = new Dictionary<int, List<StruckTableItemDropGroup>>();
        private Dictionary<int, List<StruckTableMonsterDropRate>> monsterDropDictionary = new Dictionary<int, List<StruckTableMonsterDropRate>>();

        [MenuItem(ConfigEditor.NameToolDropItemRate, false, (int)ConfigEditor.ToolOrdering.DropItemRate)]
        public static void ShowWindow()
        {
            GetWindow<TestDropItemRate>(Title);
        }

        private void OnEnable()
        {
            selectedMonsterIndex = 0;
            testCount = 10000;

            itemManager = new ItemManager();
            TableLoaderManager tableLoaderManager = new TableLoaderManager();
            tableMonster = tableLoaderManager.LoadMonsterTable();
            
            tableItem = tableLoaderManager.LoadItemTable();
            TableItemDropGroup tableItemDropGroup = tableLoaderManager.LoadItemDropGroupTable();
            TableMonsterDropRate tableMonsterDropRate = tableLoaderManager.LoadMonsterDropRateTable();
            
            dictionaryByCategory = tableItem.GetDictionaryByCategory();
            dictionaryBySubCategory = tableItem.GetDictionaryBySubCategory();
            dropGroupDictionary = tableItemDropGroup.GetDropGroups();
            monsterDropDictionary = tableMonsterDropRate.GetMonsterDropDictionary();

            LoadMonsterInfoData();
        }
        /// <summary>
        ///  몬스터 정보 불러오기
        /// </summary>
        private void LoadMonsterInfoData()
        {
            Dictionary<int, Dictionary<string, string>> monsterDictionary = tableMonster.GetDatas();

            monsterNames = new List<string>();
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in monsterDictionary)
            {
                var info = tableMonster.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                monsterNames.Add($"{info.Uid} - {info.Name}");
            }
        }


        private void OnGUI()
        {
            // 몬스터 드롭다운
            selectedMonsterIndex = EditorGUILayout.Popup("몬스터 선택", selectedMonsterIndex, monsterNames.ToArray());
            // 테스트 횟수
            testCount = EditorGUILayout.IntField("테스트 횟수", testCount);

            // 테스트 실행 버튼
            if (GUILayout.Button("테스트 실행", GUILayout.Height(30)))
            {
                if (itemManager != null)
                {
                    var monsterDictionary = tableMonster.GetDatas();
                    int index = 0;
                    StruckTableMonster monsterData = new StruckTableMonster();

                    foreach (var outerPair in monsterDictionary)
                    {
                        if (index == selectedMonsterIndex)
                        {
                            monsterData = tableMonster.GetDataByUid(outerPair.Key);
                            break;
                        }
                        index++;
                    }

                    if (monsterData.Uid <= 0)
                    {
                        EditorUtility.DisplayDialog(Title, "몬스터 데이터가 없습니다.", "OK");
                        return;
                    }
                    ItemManager.DropTestResult dropTestResult = itemManager.TestDropRates(monsterData.Uid, testCount, dictionaryByCategory, dictionaryBySubCategory, dropGroupDictionary, monsterDropDictionary, tableItem);

                    if (dropTestResult == null) return;
                    EditorUtility.DisplayDialog(Title, $"테스트 완료: 몬스터 UID {monsterData.Uid}, {testCount}회 실행됨.", "OK");
                    testResult = dropTestResult;
                    Repaint(); // UI 갱신
                }
            }

            EditorGUILayout.Space();
            Common.OnGUITitle("테스트 결과");

            if (testResult == null)
            {
                EditorGUILayout.HelpBox("테스트 실행 후 결과가 여기에 표시됩니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"몬스터 UID: {testResult.MonsterUid}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"테스트 횟수: {testResult.Iterations}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawTable("Monster Drop Rate", testResult.DropRateCounts);
            DrawTable("Item Category", testResult.CategoryCounts);
            DrawTable("Item SubCategory", testResult.SubCategoryCounts);
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
                float percentage = (entry.Value / (float)testResult.Iterations) * 100;
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
