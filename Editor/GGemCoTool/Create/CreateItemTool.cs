using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class CreateItemTool : DefaultEditorWindow
    {
        private const string Title = "아이템 생성툴";
        private TableItem _tableItem;
        private int _selectedItemIndex;
        private int _makeItemCount;
        private int _makeGoldCount;
        private int _makeSilverCount;
        private readonly List<string> _itemNames = new List<string>();
        private readonly List<int> _itemUids = new List<int>();
        private Dictionary<int, Dictionary<string, string>> _itemDictionary;

        [MenuItem(ConfigEditor.NameToolCreateItem, false, (int)ConfigEditor.ToolOrdering.CreateItem)]
        public static void ShowWindow()
        {
            GetWindow<CreateItemTool>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedItemIndex = 0;
            _ = LoadAsync();
        }
        private async Task LoadAsync()
        {
            try
            {
                _tableItem = await TableLoaderManager.LoadItemTableAsync();

                if (_tableItem == null)
                {
                    EditorUtility.DisplayDialog(Title, "Item 테이블을 불러오지 못했습니다.", "OK");
                    return;
                }

                _itemDictionary = _tableItem.GetDatas();
                LoadItemInfoData();
                IsLoading = false;
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CreateItemTool] LoadAsync 예외 발생: {ex.Message}");
                EditorUtility.DisplayDialog(Title, "아이템 테이블 로딩 중 오류가 발생했습니다.", "OK");
                IsLoading = false;
            }
        }

        private void OnGUI()
        {
            if (IsLoading)
            {
                EditorGUILayout.LabelField("아이템 테이블 로딩 중...");
                return;
            }
            // 방어 코드 추가
            if (_itemNames.Count == 0)
            {
                EditorGUILayout.LabelField("등록된 아이템이 없습니다.");
                return;
            }

            if (_selectedItemIndex >= _itemNames.Count)
            {
                _selectedItemIndex = 0;
            }
            _selectedItemIndex = EditorGUILayout.Popup("아이템 선택", _selectedItemIndex, _itemNames.ToArray());
            _makeItemCount = EditorGUILayout.IntField("추가할 개수", _makeItemCount);
            if (GUILayout.Button("인벤토리에 아이템 추가")) AddItem();

            GUILayout.Space(20);
            _makeGoldCount = EditorGUILayout.IntField("추가할 골드", _makeGoldCount);
            if (GUILayout.Button("인벤토리에 골드 추가")) AddCurrency(CurrencyConstants.Type.Gold);

            GUILayout.Space(20);
            _makeSilverCount = EditorGUILayout.IntField("추가할 실버", _makeSilverCount);
            if (GUILayout.Button("인벤토리에 실버 추가")) AddCurrency(CurrencyConstants.Type.Silver);

            GUILayout.Space(20);
            if (GUILayout.Button("인벤토리 모든 아이템 삭제")) RemoveAllInventoryItem();

            GUILayout.Space(20);
            if (GUILayout.Button("골드 삭제")) RemoveCurrency(CurrencyConstants.Type.Gold);

            GUILayout.Space(20);
            if (GUILayout.Button("실버 삭제")) RemoveCurrency(CurrencyConstants.Type.Silver);
        }

        private void RemoveCurrency(CurrencyConstants.Type type)
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            var player = SceneGame.Instance.saveDataManager.Player;

            if (type == CurrencyConstants.Type.Gold)
            {
                player.MinusCurrency(type, player.CurrentGold);
            }
            else if (type == CurrencyConstants.Type.Silver)
            {
                player.MinusCurrency(type, player.CurrentSilver);
            }
        }

        private void AddCurrency(CurrencyConstants.Type type)
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Gold && _makeGoldCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "골드 수량을 입력해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Silver && _makeSilverCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "실버 수량을 입력해주세요.", "OK");
                return;
            }

            int uid = (type == CurrencyConstants.Type.Gold)
                ? CurrencyConstants.ItemUidGold
                : CurrencyConstants.ItemUidSilver;

            int count = (type == CurrencyConstants.Type.Gold) ? _makeGoldCount : _makeSilverCount;

            SceneGame.Instance.saveDataManager.Inventory.AddItem(uid, count);
        }

        private void RemoveAllInventoryItem()
        {
            SceneGame.Instance.saveDataManager.Inventory.RemoveAllItems();

            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid.Inventory);
            if (!inventory) return;
            inventory.LoadIcons();
        }

        private void AddItem()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (_makeItemCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템 개수를 입력해주세요.", "OK");
                return;
            }

            int itemUid = _itemUids[_selectedItemIndex];
            if (itemUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템을 선택해주세요.", "OK");
                return;
            }

            var result = SceneGame.Instance.saveDataManager.Inventory.AddItem(itemUid, _makeItemCount);

            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid.Inventory);
            if (!inventory) return;
            inventory.SetIcons(result);
        }

        private void LoadItemInfoData()
        {
            _itemNames.Clear();
            _itemUids.Clear();

            foreach (var kvp in _itemDictionary)
            {
                var info = _tableItem.GetDataByUid(kvp.Key);
                if (info.Uid <= 0) continue;

                _itemNames.Add($"{info.Uid} - {info.Name}");
                _itemUids.Add(info.Uid);
            }
            _selectedItemIndex = 0; // 추가
        }
    }
}
