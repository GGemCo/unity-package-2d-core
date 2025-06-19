using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class CreateItemTool : EditorWindow
    {
        private const string Title = "아이템 생성툴";
        private TableItem tableItem;
        private int selectedItemIndex;
        private int makeItemCount;
        private int makeGoldCount;
        private int makeSilverCount;
        private List<string> itemNames;
        private List<int> itemUids;
        private Dictionary<int, Dictionary<string, string>> itemDictionary;
        
        [MenuItem(ConfigEditor.NameToolCreateItem, false, (int)ConfigEditor.ToolOrdering.CreateItem)]
        public static void ShowWindow()
        {
            GetWindow<CreateItemTool>(Title);
        }
        private void OnEnable()
        {
            selectedItemIndex = 0;
            TableLoaderManager tableLoaderManager = new TableLoaderManager();
            tableItem = tableLoaderManager.LoadItemTable();
            itemDictionary = tableItem.GetDatas();
            
            LoadItemInfoData();
        }

        private void OnGUI()
        {
            selectedItemIndex = EditorGUILayout.Popup("아이템 선택", selectedItemIndex, itemNames.ToArray());
            makeItemCount = EditorGUILayout.IntField("추가할 개수", makeItemCount);
            if (GUILayout.Button("인벤토리에 아이템 추가"))
            {
                AddItem();
            }
            GUILayout.Space(20);

            makeGoldCount = EditorGUILayout.IntField("추가할 골드", makeGoldCount);
            if (GUILayout.Button("인벤토리에 골드 추가"))
            {
                AddCurrency(CurrencyConstants.Type.Gold);
            }
            GUILayout.Space(20);

            makeSilverCount = EditorGUILayout.IntField("추가할 실버", makeSilverCount);
            if (GUILayout.Button("인벤토리에 실버 추가"))
            {
                AddCurrency(CurrencyConstants.Type.Silver);
            }
            
            GUILayout.Space(20);

            if (GUILayout.Button("인벤토리 모든 아이템 삭제"))
            {
                RemoveAllInventoryItem();
            }
            GUILayout.Space(20);

            if (GUILayout.Button("골드 삭제"))
            {
                RemoveCurrency(CurrencyConstants.Type.Gold);
            }
            GUILayout.Space(20);

            if (GUILayout.Button("실버 삭제"))
            {
                RemoveCurrency(CurrencyConstants.Type.Silver);
            }
        }

        private void RemoveCurrency(CurrencyConstants.Type type)
        {
            if (SceneGame.Instance == null)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Gold)
            {
                SceneGame.Instance.saveDataManager.Player.MinusCurrency(type, SceneGame.Instance.saveDataManager.Player.CurrentGold);
            }
            else if (type == CurrencyConstants.Type.Silver)
            {
                SceneGame.Instance.saveDataManager.Player.MinusCurrency(type, SceneGame.Instance.saveDataManager.Player.CurrentSilver);
            }
        }

        private void AddCurrency(CurrencyConstants.Type type)
        {
            if (SceneGame.Instance == null)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Gold)
            {
                if (makeGoldCount <= 0)
                {
                    EditorUtility.DisplayDialog(Title, "골드 수량을 입력해주세요.", "OK");
                    return;
                }
                SceneGame.Instance.saveDataManager.Inventory.AddItem(CurrencyConstants.ItemUidGold, makeGoldCount);
            }
            else if (type == CurrencyConstants.Type.Silver)
            {
                if (makeSilverCount <= 0)
                {
                    EditorUtility.DisplayDialog(Title, "실버 수량을 입력해주세요.", "OK");
                    return;
                }
                SceneGame.Instance.saveDataManager.Inventory.AddItem(CurrencyConstants.ItemUidSilver, makeSilverCount);
            }

        }

        private void RemoveAllInventoryItem()
        {
            SceneGame.Instance.saveDataManager.Inventory.RemoveAllItems();
            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                .Inventory);
            if (inventory != null)
            {
                inventory.LoadIcons();
            }
        }
        private void AddItem()
        {
            if (SceneGame.Instance == null)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (makeItemCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템 개수를 입력해주세요.", "OK");
                return;
            }

            int itemUid = itemUids[selectedItemIndex];
            if (itemUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템을 선택해주세요.", "OK");
                return;
            }
            var result = SceneGame.Instance.saveDataManager.Inventory.AddItem(itemUid, makeItemCount);
            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid
                .Inventory);
            if (inventory != null)
            {
                inventory.SetIcons(result);
            }
        }
        /// <summary>
        ///  몬스터 정보 불러오기
        /// </summary>
        private void LoadItemInfoData()
        {
            itemNames = new List<string>();
            itemUids = new List<int>();
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, Dictionary<string, string>> outerPair in itemDictionary)
            {
                var info = tableItem.GetDataByUid(outerPair.Key);
                if (info.Uid <= 0) continue;
                itemNames.Add($"{info.Uid} - {info.Name}");
                itemUids.Add(info.Uid);
            }
        }
    }
}